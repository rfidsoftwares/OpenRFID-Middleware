using OpenRFID.Core.Abstractions;

namespace OpenRFID.Plugins.LLRP;

public sealed class LlrpReaderProvider : IReaderProvider
{
    public string ProviderId => "llrp";
    public string BrandName => "LLRP Reader (Low Level Reader Protocol)";
    public IReadOnlyList<string> SupportedProtocols => new[] { "LLRP-v1.0.1", "LLRP-v1.1" };

    public Task<IReaderConnection> CreateConnectionAsync(ReaderConfig config, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        return Task.FromResult<IReaderConnection>(new LlrpReaderConnection(config));
    }
}
