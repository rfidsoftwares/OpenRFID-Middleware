using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using OpenRFID.Core.Abstractions;

namespace OpenRFID.Plugins.Sockets;

public sealed class UdpSocketReaderConnection : IReaderConnection
{
    private readonly ReaderConfig _config;
    private UdpClient? _udpClient;
    private CancellationTokenSource? _cts;
    private Task? _readTask;
    private int _isDisposed;

    public string ReaderId => _config.ReaderId;
    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;

    public event EventHandler<TagReadEventArgs>? TagRead;
    public event EventHandler<ReaderStatusEventArgs>? StatusChanged;

    public UdpSocketReaderConnection(ReaderConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public Task ConnectAsync(CancellationToken ct = default)
    {
        if (State == ConnectionState.Connected) return Task.CompletedTask;

        UpdateState(ConnectionState.Connecting, "Initializing UDP Listener...");
        try
        {
            int port = _config.Port ?? 5000;
            _udpClient = new UdpClient(port);

            _cts = new CancellationTokenSource();
            _readTask = Task.Run(() => ListenLoopAsync(_cts.Token));

            UpdateState(ConnectionState.Connected, $"UDP Listener active on port {port}");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            UpdateState(ConnectionState.Faulted, $"UDP Initialization failed: {ex.Message}", ex);
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

        _udpClient?.Close();
        _udpClient?.Dispose();
        _udpClient = null;

        StatusChanged?.Invoke(this, new ReaderStatusEventArgs(ReaderId, oldState, ConnectionState.Disconnected, "UDP Listener stopped"));
    }

    public Task ApplyConfigAsync(ReaderConfig config, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    private async Task ListenLoopAsync(CancellationToken ct)
    {
        if (_udpClient == null) return;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                UdpReceiveResult result = await _udpClient.ReceiveAsync(ct);
                string payload = Encoding.UTF8.GetString(result.Buffer);
                ProcessPayload(payload);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                UpdateState(ConnectionState.Faulted, $"UDP Receive error: {ex.Message}", ex);
                break;
            }
        }
    }

    public void ProcessPayload(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload)) return;

        try
        {
            if (payload.TrimStart().StartsWith('{'))
            {
                using var doc = JsonDocument.Parse(payload);
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
                EmitTag(payload.Trim(), null, -50f, 1);
            }
        }
        catch
        {
            EmitTag(payload.Trim(), null, -50f, 1);
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
