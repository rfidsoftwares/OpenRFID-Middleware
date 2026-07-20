using OpenRFID.Core.Abstractions;
using OpenRFID.Core.Pipeline.Filters;
using Xunit;

namespace OpenRFID.Pipeline.Tests;

public class SlidingWindowFilterTests
{
    [Fact]
    public void SlidingWindow_DeduplicatesRepeatReadsWithinWindow()
    {
        var filter = new SlidingWindowFilter(windowSeconds: 5.0, scope: DeduplicationScope.Global);
        var tag = new TagReadEvent { EPC = "E2801111", ReaderId = "r1" };

        var t0 = DateTimeOffset.UtcNow;
        var t2 = t0.AddSeconds(2);
        var t8 = t0.AddSeconds(8);

        // First read at t=0s passes
        Assert.True(filter.Evaluate(tag, t0).IsPassed);

        // Second read 2s later is dropped (within 5s window) and updates last seen to t=2s
        Assert.False(filter.Evaluate(tag, t2).IsPassed);

        // Third read at t=8s passes (6s elapsed since last seen at t=2s)
        Assert.True(filter.Evaluate(tag, t8).IsPassed);
    }

    [Fact]
    public void SlidingWindow_PerAntennaScope_DeduplicatesIndividuallyPerPort()
    {
        var filter = new SlidingWindowFilter(windowSeconds: 10.0, scope: DeduplicationScope.PerAntenna);
        var tagPort1 = new TagReadEvent { EPC = "E2801111", ReaderId = "r1", AntennaPort = 1 };
        var tagPort2 = new TagReadEvent { EPC = "E2801111", ReaderId = "r1", AntennaPort = 2 };

        var now = DateTimeOffset.UtcNow;

        Assert.True(filter.Evaluate(tagPort1, now).IsPassed);
        Assert.False(filter.Evaluate(tagPort1, now.AddSeconds(1)).IsPassed);

        // Port 2 read passes despite same EPC and reader
        Assert.True(filter.Evaluate(tagPort2, now.AddSeconds(1)).IsPassed);
    }
}
