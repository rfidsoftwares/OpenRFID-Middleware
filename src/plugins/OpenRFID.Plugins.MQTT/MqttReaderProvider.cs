using OpenRFID.Core.Abstractions;

namespace OpenRFID.Plugins.MQTT;

public sealed class MqttReaderProvider : IReaderProvider
{
    public string ProviderId => "mqtt-broker";
    public string BrandName => "MQTT Reader Gateway";
    public IReadOnlyList<string> SupportedProtocols => new[] { "MQTT-v3.1.1", "MQTT-v5.0" };

    public Task<IReaderConnection> CreateConnectionAsync(ReaderConfig config, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        return Task.FromResult<IReaderConnection>(new MqttReaderConnection(config));
    }
}
