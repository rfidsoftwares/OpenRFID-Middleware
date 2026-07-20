using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using OpenRFID.Core.Abstractions;

namespace OpenRFID.Plugins.Sockets;

public sealed class TcpSocketReaderConnection : IReaderConnection
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

    public TcpSocketReaderConnection(ReaderConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        if (State == ConnectionState.Connected) return;

        UpdateState(ConnectionState.Connecting, "Connecting to TCP endpoint...");
        try
        {
            _tcpClient = new TcpClient();
            string host = _config.IpAddress ?? "127.0.0.1";
            int port = _config.Port ?? 5000;

            await _tcpClient.ConnectAsync(host, port, ct);
            _cts = new CancellationTokenSource();
            _readTask = Task.Run(() => ReadLoopAsync(_cts.Token));

            UpdateState(ConnectionState.Connected, $"Successfully connected to {host}:{port}");
        }
        catch (Exception ex)
        {
            UpdateState(ConnectionState.Faulted, $"TCP Connection failed: {ex.Message}", ex);
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

        StatusChanged?.Invoke(this, new ReaderStatusEventArgs(ReaderId, oldState, ConnectionState.Disconnected, "TCP Disconnected"));
    }

    public Task ApplyConfigAsync(ReaderConfig config, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        if (_tcpClient == null) return;

        using var stream = _tcpClient.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);

        while (!ct.IsCancellationRequested && _tcpClient.Connected)
        {
            try
            {
                string? line = await reader.ReadLineAsync(ct);
                if (line == null) break;

                ProcessRawLine(line);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                UpdateState(ConnectionState.Faulted, $"TCP Stream read error: {ex.Message}", ex);
                break;
            }
        }

        if (State == ConnectionState.Connected)
        {
            UpdateState(ConnectionState.Disconnected, "TCP Connection lost or closed by remote host");
        }
    }

    public void ProcessRawLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return;

        try
        {
            // Support JSON format or plain string EPC format
            if (line.TrimStart().StartsWith('{'))
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                string epc = root.TryGetProperty("epc", out var epcProp) ? epcProp.GetString() ?? "" : "";
                string? tid = root.TryGetProperty("tid", out var tidProp) ? tidProp.GetString() : null;
                float rssi = root.TryGetProperty("rssi", out var rssiProp) && rssiProp.TryGetSingle(out var r) ? r : -50f;
                int antenna = root.TryGetProperty("antenna", out var antProp) && antProp.TryGetInt32(out var a) ? a : 1;

                if (!string.IsNullOrEmpty(epc))
                {
                    EmitTag(epc, tid, rssi, antenna);
                }
            }
            else
            {
                EmitTag(line.Trim(), null, -50f, 1);
            }
        }
        catch
        {
            // Plain string fallback
            EmitTag(line.Trim(), null, -50f, 1);
        }
    }

    private void EmitTag(string epc, string? tid, float rssi, int antenna)
    {
        var tag = new TagReadEvent
        {
            EPC = epc,
            TID = tid,
            RSSI = rssi,
            AntennaPort = antenna,
            ReaderId = ReaderId,
            FirstSeenTime = DateTimeOffset.UtcNow,
            LastSeenTime = DateTimeOffset.UtcNow
        };

        TagRead?.Invoke(this, new TagReadEventArgs(tag));
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
