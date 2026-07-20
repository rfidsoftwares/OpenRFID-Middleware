using OpenRFID.Core.Storage;
using Xunit;

namespace OpenRFID.Dispatch.Tests;

public class SqliteOfflineQueueTests
{
    [Fact]
    public async Task SqliteOfflineQueue_EnqueueAndPeek_StoresPayloadTransactionally()
    {
        string tempDb = Path.Combine(Path.GetTempPath(), $"test_queue_{Guid.NewGuid():N}.db");
        try
        {
            using var queue = new SqliteOfflineQueue(tempDb);

            await queue.EnqueueAsync("tx-1", "http://api.server.com/ingest", "POST", "{\"tags\":[\"E1\"]}", tagCount: 1);
            await queue.EnqueueAsync("tx-2", "http://api.server.com/ingest", "POST", "{\"tags\":[\"E2\"]}", tagCount: 1);

            long count = await queue.GetQueueCountAsync();
            Assert.Equal(2, count);

            var items = await queue.PeekBatchAsync(10);
            Assert.Equal(2, items.Count);
            Assert.Equal("tx-1", items[0].TransactionId);
            Assert.Equal("tx-2", items[1].TransactionId);

            await queue.AcknowledgeBatchAsync(new[] { items[0].Id });
            Assert.Equal(1, await queue.GetQueueCountAsync());
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(tempDb)) File.Delete(tempDb);
        }
    }
}
