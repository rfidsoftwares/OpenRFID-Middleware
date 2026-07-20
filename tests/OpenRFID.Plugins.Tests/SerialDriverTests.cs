using OpenRFID.Core.Abstractions;
using OpenRFID.Plugins.Serial;
using Xunit;

namespace OpenRFID.Plugins.Tests;

public class SerialDriverTests
{
    [Fact]
    public void SerialReader_ProcessLine_FiresTagReadEvent()
    {
        var config = new ReaderConfig { ReaderId = "serial-reader-1", ProviderId = "serial-com", ComPort = "COM3" };
        var connection = new SerialReaderConnection(config);
        TagReadEvent? emittedTag = null;

        connection.TagRead += (s, e) => emittedTag = e.Tag;

        connection.ProcessLine("E20000000000000000012345");

        Assert.NotNull(emittedTag);
        Assert.Equal("E20000000000000000012345", emittedTag.EPC);
        Assert.Equal("serial-reader-1", emittedTag.ReaderId);
    }
}
