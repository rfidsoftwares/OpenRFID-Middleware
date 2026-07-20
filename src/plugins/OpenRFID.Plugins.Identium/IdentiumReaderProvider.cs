using OpenRFID.Core.Abstractions;

namespace OpenRFID.Plugins.Identium;

public sealed class IdentiumReaderProvider : IReaderProvider
{
    public string ProviderId => "identium";
    public string BrandName => "Identium RFID Reader (4-Port Native/API)";
    public IReadOnlyList<string> SupportedProtocols => new[] { "Identium-API", "TCP-Raw" };

    public Task<IReaderConnection> CreateConnectionAsync(ReaderConfig config, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        return Task.FromResult<IReaderConnection>(new IdentiumReaderConnection(config));
    }
}
