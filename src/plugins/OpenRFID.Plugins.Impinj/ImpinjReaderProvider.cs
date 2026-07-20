using OpenRFID.Core.Abstractions;

namespace OpenRFID.Plugins.Impinj;

public sealed class ImpinjReaderProvider : IReaderProvider
{
    public string ProviderId => "impinj";
    public string BrandName => "Impinj SpeedWay / R700 Reader";
    public IReadOnlyList<string> SupportedProtocols => new[] { "Octane-SDK", "LLRP", "MQTT-IoT" };

    public Task<IReaderConnection> CreateConnectionAsync(ReaderConfig config, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        return Task.FromResult<IReaderConnection>(new ImpinjReaderConnection(config));
    }
}
