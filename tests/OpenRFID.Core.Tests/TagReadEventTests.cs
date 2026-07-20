using OpenRFID.Core.Abstractions;
using Xunit;

namespace OpenRFID.Core.Tests;

public class TagReadEventTests
{
    [Fact]
    public void TagReadEvent_Initialization_DefaultValuesSetCorrectly()
    {
        var tag = new TagReadEvent
        {
            EPC = "E28011700000020102030405",
            ReaderId = "reader-01"
        };

        Assert.Equal("E28011700000020102030405", tag.EPC);
        Assert.Equal("reader-01", tag.ReaderId);
        Assert.Equal(1, tag.AntennaPort);
        Assert.Equal(1, tag.ReadCount);
        Assert.True((DateTimeOffset.UtcNow - tag.FirstSeenTime).TotalSeconds < 5);
        Assert.True((DateTimeOffset.UtcNow - tag.LastSeenTime).TotalSeconds < 5);
    }
}
