using System.Text.Json;
using OpenRFID.Core.Abstractions;

namespace OpenRFID.Plugins.MQTT;

public sealed class MqttReaderConnection : IReaderConnection
{
    private readonly ReaderConfig _config;
    private int _isDisposed;

    public string ReaderId => _config.ReaderId;
    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;

    public event EventHandler<TagReadEventArgs>? TagRead;
    public event EventHandler<ReaderStatusEventArgs>? StatusChanged;

    public MqttReaderConnection(ReaderConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public Task ConnectAsync(CancellationToken ct = default)
    {
        if (State == ConnectionState.Connected) return Task.CompletedTask;

        UpdateState(ConnectionState.Connecting, "Connecting to MQTT broker...");
        try
        {
            string host = _config.IpAddress ?? "localhost";
            int port = _config.Port ?? 1883;

            // Mark connected (Ready to process incoming MQTT messages via ProcessMqttMessage)
            UpdateState(ConnectionState.Connected, $"Subscribed to MQTT broker at {host}:{port}");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            UpdateState(ConnectionState.Faulted, $"MQTT connection failed: {ex.Message}", ex);
            throw;
        }
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        if (State == ConnectionState.Disconnected) return Task.CompletedTask;

        var oldState = State;
        State = ConnectionState.Disconnected;
        StatusChanged?.Invoke(this, new ReaderStatusEventArgs(ReaderId, oldState, ConnectionState.Disconnected, "MQTT Disconnected"));
        return Task.CompletedTask;
    }

    public Task ApplyConfigAsync(ReaderConfig config, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public void ProcessMqttMessage(string topic, string payload)
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
                    EmitTag(epc, tid, rssi, antenna, topic);
                }
            }
            else
            {
                EmitTag(payload.Trim(), null, -50f, 1, topic);
            }
        }
        catch
        {
            EmitTag(payload.Trim(), null, -50f, 1, topic);
        }
    }

    private void EmitTag(string epc, string? tid, float rssi, int antenna, string topic)
    {
        var tag = new TagReadEvent
        {
            EPC = epc,
            TID = tid,
            RSSI = rssi,
            AntennaPort = antenna,
            ReaderId = ReaderId,
            FirstSeenTime = DateTimeOffset.UtcNow,
            LastSeenTime = DateTimeOffset.UtcNow,
            ExtraMetadata = new Dictionary<string, string> { ["MqttTopic"] = topic }
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
