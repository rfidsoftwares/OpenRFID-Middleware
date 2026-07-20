using OpenRFID.Core.Abstractions;
using OpenRFID.Core.Pipeline.Filters;
using Xunit;

namespace OpenRFID.Pipeline.Tests;

public class DailyUniqueFilterTests
{
    [Fact]
    public void DailyUnique_AllowsTagOncePerShift_AndResetsOnShiftBoundary()
    {
        // Shift starts at 06:00:00
        var filter = new DailyUniqueFilter(shiftStartLocalTime: new TimeOnly(6, 0, 0), scope: DeduplicationScope.Global);
        var tag = new TagReadEvent { EPC = "TAG-EMPLOYEE-001", ReaderId = "gate-1" };

        var shift1Time1 = new DateTimeOffset(2026, 7, 20, 8, 30, 0, TimeSpan.FromHours(5.5)); // July 20 08:30
        var shift1Time2 = new DateTimeOffset(2026, 7, 20, 14, 0, 0, TimeSpan.FromHours(5.5)); // July 20 14:00
        var shift2Time1 = new DateTimeOffset(2026, 7, 21, 6, 15, 0, TimeSpan.FromHours(5.5)); // July 21 06:15 (New shift!)

        // July 20 Shift: First scan passes
        Assert.True(filter.Evaluate(tag, shift1Time1).IsPassed);

        // July 20 Shift: Second scan dropped
        Assert.False(filter.Evaluate(tag, shift1Time2).IsPassed);

        // July 21 Shift: New shift scan passes!
        Assert.True(filter.Evaluate(tag, shift2Time1).IsPassed);
    }
}
