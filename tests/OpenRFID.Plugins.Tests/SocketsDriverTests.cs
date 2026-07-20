using OpenRFID.Core.Abstractions;
using OpenRFID.Plugins.Sockets;
using Xunit;

namespace OpenRFID.Plugins.Tests;

public class SocketsDriverTests
{
    [Fact]
    public void TcpSocket_ProcessRawLine_PlainText_FiresTagReadEvent()
    {
        var config = new ReaderConfig { ReaderId = "tcp-reader-1", ProviderId = "tcp-socket" };
        var connection = new TcpSocketReaderConnection(config);
        TagReadEvent? emittedTag = null;

        connection.TagRead += (s, e) => emittedTag = e.Tag;

        connection.ProcessRawLine("E28011700000020102030405");

        Assert.NotNull(emittedTag);
        Assert.Equal("E28011700000020102030405", emittedTag.EPC);
        Assert.Equal("tcp-reader-1", emittedTag.ReaderId);
    }

    [Fact]
    public void TcpSocket_ProcessRawLine_JsonPayload_ParsesMetadata()
    {
        var config = new ReaderConfig { ReaderId = "tcp-reader-2", ProviderId = "tcp-socket" };
        var connection = new TcpSocketReaderConnection(config);
        TagReadEvent? emittedTag = null;

        connection.TagRead += (s, e) => emittedTag = e.Tag;

        string json = "{\"epc\":\"1234567890ABCDEF\",\"tid\":\"9999\",\"rssi\":-62.5,\"antenna\":2}";
        connection.ProcessRawLine(json);

        Assert.NotNull(emittedTag);
        Assert.Equal("1234567890ABCDEF", emittedTag.EPC);
        Assert.Equal("9999", emittedTag.TID);
        Assert.Equal(-62.5f, emittedTag.RSSI);
        Assert.Equal(2, emittedTag.AntennaPort);
    }

    [Fact]
    public void UdpSocket_ProcessPayload_JsonPayload_ParsesMetadata()
    {
        var config = new ReaderConfig { ReaderId = "udp-reader-1", ProviderId = "udp-socket" };
        var connection = new UdpSocketReaderConnection(config);
        TagReadEvent? emittedTag = null;

        connection.TagRead += (s, e) => emittedTag = e.Tag;

        string json = "{\"epc\":\"FEDCBA0987654321\",\"rssi\":-45.0,\"antenna\":1}";
        connection.ProcessPayload(json);

        Assert.NotNull(emittedTag);
        Assert.Equal("FEDCBA0987654321", emittedTag.EPC);
        Assert.Equal(-45.0f, emittedTag.RSSI);
        Assert.Equal(1, emittedTag.AntennaPort);
    }
}
