using OpenRFID.Core.Abstractions;

namespace OpenRFID.Core.Pipeline.Filters;

/// <summary>
/// Filter that enforces active operating schedule by day of week and daily time bounds.
/// </summary>
public sealed class ScheduleFilter : ITagFilter
{
    public string Name => "ScheduleFilter";

    public HashSet<DayOfWeek>? OperatingDays { get; }
    public TimeOnly? StartTime { get; }
    public TimeOnly? EndTime { get; }

    public ScheduleFilter(
        IEnumerable<DayOfWeek>? operatingDays = null,
        TimeOnly? startTime = null,
        TimeOnly? endTime = null)
    {
        OperatingDays = operatingDays != null ? new HashSet<DayOfWeek>(operatingDays) : null;
        StartTime = startTime;
        EndTime = endTime;
    }

    public FilterResult Evaluate(TagReadEvent tag, DateTimeOffset? now = null)
    {
        DateTimeOffset currentTime = now ?? DateTimeOffset.Now;

        if (OperatingDays != null && OperatingDays.Count > 0 && !OperatingDays.Contains(currentTime.DayOfWeek))
        {
            return FilterResult.Drop($"Tag read on {currentTime.DayOfWeek} outside configured operating days.");
        }

        if (StartTime.HasValue && EndTime.HasValue)
        {
            TimeOnly timeOfDay = TimeOnly.FromDateTime(currentTime.LocalDateTime);
            bool isWithinTime;

            if (StartTime.Value <= EndTime.Value)
            {
                isWithinTime = timeOfDay >= StartTime.Value && timeOfDay <= EndTime.Value;
            }
            else
            {
                // Overnight schedule (e.g. 22:00 to 06:00)
                isWithinTime = timeOfDay >= StartTime.Value || timeOfDay <= EndTime.Value;
            }

            if (!isWithinTime)
            {
                return FilterResult.Drop($"Time {timeOfDay:HH:mm:ss} is outside operating window ({StartTime.Value:HH:mm:ss} - {EndTime.Value:HH:mm:ss}).");
            }
        }

        return FilterResult.Pass();
    }
}
