using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using OpenRFID.Core.Abstractions;

namespace OpenRFID.Plugins.Sockets;

public sealed class SocketsReaderProvider : IReaderProvider
{
    public string ProviderId => "tcp-socket";
    public string BrandName => "Generic TCP/UDP Socket Reader";
    public IReadOnlyList<string> SupportedProtocols => new[] { "TCP-Raw", "UDP-Raw" };

    public Task<IReaderConnection> CreateConnectionAsync(ReaderConfig config, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (string.Equals(config.ProviderId, "udp-socket", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<IReaderConnection>(new UdpSocketReaderConnection(config));
        }

        return Task.FromResult<IReaderConnection>(new TcpSocketReaderConnection(config));
    }
}
