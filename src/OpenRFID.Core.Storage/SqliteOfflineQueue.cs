using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace OpenRFID.Core.Storage;

/// <summary>
/// SQLite WAL mode persistent offline queue ensuring zero tag loss during network outages and server failures.
/// </summary>
public sealed class SqliteOfflineQueue : IDisposable
{
    private readonly string _connectionString;
    private readonly SemaphoreSlim _dbLock = new(1, 1);
    private int _isDisposed;

    public SqliteOfflineQueue(string? dbPath = null)
    {
        string path = dbPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "openrfid_offline.db");
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ConnectionString;

        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            PRAGMA journal_mode = WAL;
            PRAGMA synchronous = NORMAL;

            CREATE TABLE IF NOT EXISTS OfflineQueue (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TransactionId TEXT NOT NULL,
                TargetUrl TEXT NOT NULL,
                HttpMethod TEXT NOT NULL,
                Payload TEXT NOT NULL,
                HeadersJson TEXT,
                TagCount INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                RetryCount INTEGER NOT NULL DEFAULT 0
            );
        ";
        cmd.ExecuteNonQuery();
    }

    public async Task EnqueueAsync(
        string transactionId,
        string targetUrl,
        string httpMethod,
        string payload,
        IDictionary<string, string>? headers = null,
        int tagCount = 1,
        CancellationToken ct = default)
    {
        await _dbLock.WaitAsync(ct);
        try
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync(ct);

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO OfflineQueue (TransactionId, TargetUrl, HttpMethod, Payload, HeadersJson, TagCount, CreatedAt, RetryCount)
                VALUES ($txId, $url, $method, $payload, $headers, $tagCount, $createdAt, 0);
            ";

            cmd.Parameters.AddWithValue("$txId", transactionId);
            cmd.Parameters.AddWithValue("$url", targetUrl);
            cmd.Parameters.AddWithValue("$method", httpMethod);
            cmd.Parameters.AddWithValue("$payload", payload ?? "");
            cmd.Parameters.AddWithValue("$headers", headers != null ? JsonSerializer.Serialize(headers) : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$tagCount", tagCount);
            cmd.Parameters.AddWithValue("$createdAt", DateTimeOffset.UtcNow.ToString("o"));

            await cmd.ExecuteNonQueryAsync(ct);
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task<IReadOnlyList<OfflineQueueItem>> PeekBatchAsync(int count = 50, CancellationToken ct = default)
    {
        await _dbLock.WaitAsync(ct);
        try
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync(ct);

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, TransactionId, TargetUrl, HttpMethod, Payload, HeadersJson, TagCount, CreatedAt, RetryCount
                FROM OfflineQueue
                ORDER BY Id ASC
                LIMIT $count;
            ";
            cmd.Parameters.AddWithValue("$count", count);

            var items = new List<OfflineQueueItem>();
            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                string? headersJson = reader.IsDBNull(5) ? null : reader.GetString(5);
                var headers = !string.IsNullOrEmpty(headersJson)
                    ? JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson) ?? new()
                    : new Dictionary<string, string>();

                items.Add(new OfflineQueueItem
                {
                    Id = reader.GetInt64(0),
                    TransactionId = reader.GetString(1),
                    TargetUrl = reader.GetString(2),
                    HttpMethod = reader.GetString(3),
                    Payload = reader.GetString(4),
                    Headers = headers,
                    TagCount = reader.GetInt32(6),
                    CreatedAt = DateTimeOffset.Parse(reader.GetString(7)),
                    RetryCount = reader.GetInt32(8)
                });
            }

            return items.AsReadOnly();
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task AcknowledgeBatchAsync(IEnumerable<long> ids, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        if (idList.Count == 0) return;

        await _dbLock.WaitAsync(ct);
        try
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync(ct);

            using var cmd = conn.CreateCommand();
            var paramNames = new List<string>(idList.Count);
            for (int i = 0; i < idList.Count; i++)
            {
                string paramName = $"$p{i}";
                paramNames.Add(paramName);
                cmd.Parameters.AddWithValue(paramName, idList[i]);
            }

            cmd.CommandText = $"DELETE FROM OfflineQueue WHERE Id IN ({string.Join(',', paramNames)});";
            await cmd.ExecuteNonQueryAsync(ct);
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task<long> GetQueueCountAsync(CancellationToken ct = default)
    {
        await _dbLock.WaitAsync(ct);
        try
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync(ct);

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM OfflineQueue;";
            object? result = await cmd.ExecuteScalarAsync(ct);
            return Convert.ToInt64(result);
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) != 0) return;
        _dbLock.Dispose();
        SqliteConnection.ClearAllPools();
    }
}
