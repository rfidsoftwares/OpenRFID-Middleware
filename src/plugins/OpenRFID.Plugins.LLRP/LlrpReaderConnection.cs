using System.Buffers.Binary;
using System.Net.Sockets;
using OpenRFID.Core.Abstractions;

namespace OpenRFID.Plugins.LLRP;

public sealed class LlrpReaderConnection : IReaderConnection
{
    private readonly ReaderConfig _config;
    private TcpClient? _tcpClient;
    private CancellationTokenSource? _cts;
    private Task? _readTask;
    private int _isDisposed;

    public string ReaderId => _config.ReaderId;
    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;

    public event EventHandler<TagReadEventArgs>? TagRead;
    public event EventHandler<ReaderStatusEventArgs>? StatusChanged;

    public LlrpReaderConnection(ReaderConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        if (State == ConnectionState.Connected) return;

        UpdateState(ConnectionState.Connecting, "Connecting to LLRP Reader...");
        try
        {
            _tcpClient = new TcpClient();
            string host = _config.IpAddress ?? "127.0.0.1";
            int port = _config.Port ?? 5084; // LLRP default port

            await _tcpClient.ConnectAsync(host, port, ct);
            _cts = new CancellationTokenSource();
            _readTask = Task.Run(() => ReadLoopAsync(_cts.Token));

            UpdateState(ConnectionState.Connected, $"Connected to LLRP Reader at {host}:{port}");
        }
        catch (Exception ex)
        {
            UpdateState(ConnectionState.Faulted, $"LLRP Connection failed: {ex.Message}", ex);
            throw;
        }
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        if (State == ConnectionState.Disconnected) return;

        var oldState = State;
        State = ConnectionState.Disconnected;

        if (_cts != null)
        {
            _cts.Cancel();
            if (_readTask != null)
            {
                try { await _readTask; } catch (OperationCanceledException) { }
            }
            _cts.Dispose();
            _cts = null;
        }

        _tcpClient?.Close();
        _tcpClient?.Dispose();
        _tcpClient = null;

        StatusChanged?.Invoke(this, new ReaderStatusEventArgs(ReaderId, oldState, ConnectionState.Disconnected, "LLRP Disconnected"));
    }

    public Task ApplyConfigAsync(ReaderConfig config, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        if (_tcpClient == null) return;

        using var stream = _tcpClient.GetStream();
        byte[] headerBuffer = new byte[10];

        while (!ct.IsCancellationRequested && _tcpClient.Connected)
        {
            try
            {
                int read = await ReadExactAsync(stream, headerBuffer, 0, 10, ct);
                if (read < 10) break;

                uint messageLength = BinaryPrimitives.ReadUInt32BigEndian(headerBuffer.AsSpan(2, 4));
                if (messageLength < 10 || messageLength > 1024 * 1024) continue; // Sanity check

                byte[] messageBuffer = new byte[messageLength];
                Array.Copy(headerBuffer, 0, messageBuffer, 0, 10);

                int remaining = (int)messageLength - 10;
                if (remaining > 0)
                {
                    await ReadExactAsync(stream, messageBuffer, 10, remaining, ct);
                }

                ProcessLlrpMessage(messageBuffer);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                UpdateState(ConnectionState.Faulted, $"LLRP stream read error: {ex.Message}", ex);
                break;
            }
        }

        if (State == ConnectionState.Connected)
        {
            UpdateState(ConnectionState.Disconnected, "LLRP connection closed by reader");
        }
    }

    public void ProcessLlrpMessage(byte[] buffer)
    {
        var tags = LlrpMessageDecoder.DecodeRoAccessReport(buffer);
        foreach (var (epc, rssi, antenna) in tags)
        {
            var tag = new TagReadEvent
            {
                EPC = epc,
                RSSI = rssi,
                AntennaPort = antenna,
                ReaderId = ReaderId,
                FirstSeenTime = DateTimeOffset.UtcNow,
                LastSeenTime = DateTimeOffset.UtcNow
            };

            TagRead?.Invoke(this, new TagReadEventArgs(tag));
        }
    }

    private static async Task<int> ReadExactAsync(NetworkStream stream, byte[] buffer, int offset, int count, CancellationToken ct)
    {
        int totalRead = 0;
        while (totalRead < count)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(offset + totalRead, count - totalRead), ct);
            if (read == 0) break;
            totalRead += read;
        }
        return totalRead;
    }

    private void UpdateState(ConnectionState newState, string message, Exception? ex = null)
    {
        var oldState = State;
        State = newState;
        StatusChanged?.Invoke(this, new ReaderStatusEventArgs(ReaderId, oldState, newState, message, ex));
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) != 0) return;
        await DisconnectAsync();
    }
}
