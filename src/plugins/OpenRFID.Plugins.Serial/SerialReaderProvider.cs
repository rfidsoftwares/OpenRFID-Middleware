using System.IO.Ports;
using OpenRFID.Core.Abstractions;

namespace OpenRFID.Plugins.Serial;

public sealed class SerialReaderProvider : IReaderProvider
{
    public string ProviderId => "serial-com";
    public string BrandName => "Generic Serial RS-232/RS-485 Reader";
    public IReadOnlyList<string> SupportedProtocols => new[] { "Serial-RS232", "Serial-RS485" };

    public Task<IReaderConnection> CreateConnectionAsync(ReaderConfig config, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        return Task.FromResult<IReaderConnection>(new SerialReaderConnection(config));
    }
}
