using OpenRFID.Core.Abstractions;

namespace OpenRFID.Plugins.Zebra;

public sealed class ZebraReaderProvider : IReaderProvider
{
    public string ProviderId => "zebra";
    public string BrandName => "Zebra FX Series RFID Reader";
    public IReadOnlyList<string> SupportedProtocols => new[] { "Zebra-IoT", "LLRP" };

    public Task<IReaderConnection> CreateConnectionAsync(ReaderConfig config, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        return Task.FromResult<IReaderConnection>(new ZebraReaderConnection(config));
    }
}
