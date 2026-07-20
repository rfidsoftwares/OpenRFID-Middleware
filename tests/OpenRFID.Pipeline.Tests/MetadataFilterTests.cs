using OpenRFID.Core.Abstractions;
using OpenRFID.Core.Pipeline.Filters;
using Xunit;

namespace OpenRFID.Pipeline.Tests;

public class MetadataFilterTests
{
    [Fact]
    public void MetadataFilter_RssiThreshold_DropsWeakSignals()
    {
        var filter = new MetadataFilter(minRssiDbm: -65.0f);

        var strongTag = new TagReadEvent { EPC = "E2801111", ReaderId = "r1", RSSI = -55.0f };
        var weakTag = new TagReadEvent { EPC = "E2802222", ReaderId = "r1", RSSI = -75.0f };

        Assert.True(filter.Evaluate(strongTag).IsPassed);
        Assert.False(filter.Evaluate(weakTag).IsPassed);
    }

    [Fact]
    public void MetadataFilter_AntennaMask_FiltersByPortBitmask()
    {
        // Mask 5 = binary 0101 (Port 1 & Port 3 enabled, Port 2 & Port 4 disabled)
        var filter = new MetadataFilter(antennaMask: 5);

        var tagPort1 = new TagReadEvent { EPC = "E1", ReaderId = "r1", AntennaPort = 1 };
        var tagPort2 = new TagReadEvent { EPC = "E2", ReaderId = "r1", AntennaPort = 2 };
        var tagPort3 = new TagReadEvent { EPC = "E3", ReaderId = "r1", AntennaPort = 3 };
        var tagPort4 = new TagReadEvent { EPC = "E4", ReaderId = "r1", AntennaPort = 4 };

        Assert.True(filter.Evaluate(tagPort1).IsPassed);
        Assert.False(filter.Evaluate(tagPort2).IsPassed);
        Assert.True(filter.Evaluate(tagPort3).IsPassed);
        Assert.False(filter.Evaluate(tagPort4).IsPassed);
    }

    [Fact]
    public void MetadataFilter_EpcPatternFilters_PrefixSuffixRegexLength()
    {
        var filter = new MetadataFilter(
            epcPrefix: "E280",
            epcSuffix: "99",
            epcMinLength: 8,
            epcMaxLength: 16,
            regexPattern: "^E280[0-9A-F]+99$"
        );

        var validTag = new TagReadEvent { EPC = "E280ABCDEF99", ReaderId = "r1" };
        var invalidPrefix = new TagReadEvent { EPC = "3000ABCDEF99", ReaderId = "r1" };
        var invalidSuffix = new TagReadEvent { EPC = "E280ABCDEF00", ReaderId = "r1" };
        var invalidRegex = new TagReadEvent { EPC = "E280XYZ12399", ReaderId = "r1" };

        Assert.True(filter.Evaluate(validTag).IsPassed);
        Assert.False(filter.Evaluate(invalidPrefix).IsPassed);
        Assert.False(filter.Evaluate(invalidSuffix).IsPassed);
        Assert.False(filter.Evaluate(invalidRegex).IsPassed);
    }
}
