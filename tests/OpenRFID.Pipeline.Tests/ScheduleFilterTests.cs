using OpenRFID.Core.Abstractions;
using OpenRFID.Core.Pipeline.Filters;
using Xunit;

namespace OpenRFID.Pipeline.Tests;

public class ScheduleFilterTests
{
    [Fact]
    public void ScheduleFilter_OperatingDays_PassesOnlyOnAllowedDays()
    {
        var filter = new ScheduleFilter(operatingDays: new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday });

        var tag = new TagReadEvent { EPC = "E2801111", ReaderId = "r1" };

        var mondayTime = new DateTimeOffset(2026, 7, 20, 10, 0, 0, TimeSpan.FromHours(5.5)); // Monday
        var tuesdayTime = new DateTimeOffset(2026, 7, 21, 10, 0, 0, TimeSpan.FromHours(5.5)); // Tuesday

        Assert.True(filter.Evaluate(tag, mondayTime).IsPassed);
        Assert.False(filter.Evaluate(tag, tuesdayTime).IsPassed);
    }

    [Fact]
    public void ScheduleFilter_TimeWindow_PassesOnlyWithinWorkingHours()
    {
        var filter = new ScheduleFilter(
            startTime: new TimeOnly(8, 0, 0),
            endTime: new TimeOnly(18, 0, 0)
        );

        var tag = new TagReadEvent { EPC = "E2801111", ReaderId = "r1" };

        var workingHour = new DateTimeOffset(2026, 7, 20, 14, 30, 0, TimeSpan.FromHours(5.5)); // 14:30
        var afterHour = new DateTimeOffset(2026, 7, 20, 19, 0, 0, TimeSpan.FromHours(5.5)); // 19:00

        Assert.True(filter.Evaluate(tag, workingHour).IsPassed);
        Assert.False(filter.Evaluate(tag, afterHour).IsPassed);
    }

    [Fact]
    public void ScheduleFilter_OvernightSchedule_EvaluatesCorrectly()
    {
        var filter = new ScheduleFilter(
            startTime: new TimeOnly(22, 0, 0),
            endTime: new TimeOnly(6, 0, 0)
        );

        var tag = new TagReadEvent { EPC = "E2801111", ReaderId = "r1" };

        var nightTime = new DateTimeOffset(2026, 7, 20, 23, 15, 0, TimeSpan.FromHours(5.5)); // 23:15
        var earlyMorning = new DateTimeOffset(2026, 7, 21, 4, 30, 0, TimeSpan.FromHours(5.5)); // 04:30
        var noonTime = new DateTimeOffset(2026, 7, 21, 12, 0, 0, TimeSpan.FromHours(5.5)); // 12:00

        Assert.True(filter.Evaluate(tag, nightTime).IsPassed);
        Assert.True(filter.Evaluate(tag, earlyMorning).IsPassed);
        Assert.False(filter.Evaluate(tag, noonTime).IsPassed);
    }
}
