using OpenRFID.Core.Abstractions;

namespace OpenRFID.Plugins.Zebra;

public sealed class ZebraReaderConnection : IReaderConnection
{
    private readonly ReaderConfig _config;
    private int _isDisposed;

    public string ReaderId => _config.ReaderId;
    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;

    public event EventHandler<TagReadEventArgs>? TagRead;
    public event EventHandler<ReaderStatusEventArgs>? StatusChanged;

    public ZebraReaderConnection(ReaderConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public Task ConnectAsync(CancellationToken ct = default)
    {
        if (State == ConnectionState.Connected) return Task.CompletedTask;

        UpdateState(ConnectionState.Connecting, "Connecting to Zebra Reader...");
        try
        {
            string host = _config.IpAddress ?? "192.168.1.150";
            UpdateState(ConnectionState.Connected, $"Connected to Zebra FX Series at {host}");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            UpdateState(ConnectionState.Faulted, $"Zebra connection failed: {ex.Message}", ex);
            throw;
        }
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        if (State == ConnectionState.Disconnected) return Task.CompletedTask;

        var oldState = State;
        State = ConnectionState.Disconnected;
        StatusChanged?.Invoke(this, new ReaderStatusEventArgs(ReaderId, oldState, ConnectionState.Disconnected, "Zebra Disconnected"));
        return Task.CompletedTask;
    }

    public Task ApplyConfigAsync(ReaderConfig config, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public void ProcessZebraData(string epc, float rssi, int antennaPort, string? tid = null)
    {
        var tag = new TagReadEvent
        {
            EPC = epc,
            TID = tid,
            RSSI = rssi,
            AntennaPort = antennaPort,
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
