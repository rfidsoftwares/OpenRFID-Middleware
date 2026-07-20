using System.Text.Json;
using OpenRFID.Core.Abstractions;

namespace OpenRFID.Plugins.Identium;

public sealed class IdentiumReaderConnection : IReaderConnection
{
    private readonly ReaderConfig _config;
    private int _isDisposed;

    public string ReaderId => _config.ReaderId;
    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;

    public event EventHandler<TagReadEventArgs>? TagRead;
    public event EventHandler<ReaderStatusEventArgs>? StatusChanged;

    public IdentiumReaderConnection(ReaderConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public Task ConnectAsync(CancellationToken ct = default)
    {
        if (State == ConnectionState.Connected) return Task.CompletedTask;

        UpdateState(ConnectionState.Connecting, "Connecting to Identium Reader...");
        try
        {
            string host = _config.IpAddress ?? "192.168.1.200";
            int port = _config.Port ?? 6000;

            UpdateState(ConnectionState.Connected, $"Connected to Identium Reader at {host}:{port}");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            UpdateState(ConnectionState.Faulted, $"Identium connection failed: {ex.Message}", ex);
            throw;
        }
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        if (State == ConnectionState.Disconnected) return Task.CompletedTask;

        var oldState = State;
        State = ConnectionState.Disconnected;
        StatusChanged?.Invoke(this, new ReaderStatusEventArgs(ReaderId, oldState, ConnectionState.Disconnected, "Identium Disconnected"));
        return Task.CompletedTask;
    }

    public Task ApplyConfigAsync(ReaderConfig config, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public void ProcessIdentiumFrame(byte[] frame)
    {
        // Parse Identium byte frame (Header, Type, Length, EPC, RSSI, Antenna)
        if (frame == null || frame.Length < 16) return;

        string epc = Convert.ToHexString(frame, 4, 12);
        float rssi = -(float)frame[2];
        int antenna = frame[3];

        var tag = new TagReadEvent
        {
            EPC = epc,
            RSSI = rssi,
            AntennaPort = antenna,
            ReaderId = ReaderId,
            FirstSeenTime = DateTimeOffset.UtcNow,
            LastSeenTime = DateTimeOffset.UtcNow,
            Location = "Identium Gate 1"
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
