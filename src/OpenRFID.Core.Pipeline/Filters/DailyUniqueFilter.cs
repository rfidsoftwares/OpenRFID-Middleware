using System.Collections.Concurrent;
using OpenRFID.Core.Abstractions;

namespace OpenRFID.Core.Pipeline.Filters;

/// <summary>
/// Deduplication filter that permits each unique tag to pass only ONCE per calendar day or shift boundary.
/// Automatically resets state when crossing midnight or configured shift start time.
/// </summary>
public sealed class DailyUniqueFilter : ITagFilter
{
    private readonly ConcurrentDictionary<string, byte> _seenTags = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _rolloverLock = new();
    private string _activeShiftId;

    public string Name => "DailyUniqueFilter";
    public TimeOnly ShiftStartLocalTime { get; }
    public DeduplicationScope Scope { get; }
    public string ActiveShiftId => _activeShiftId;
    public int UniqueCount => _seenTags.Count;

    public event EventHandler<string>? ShiftRollover;

    public DailyUniqueFilter(TimeOnly? shiftStartLocalTime = null, DeduplicationScope scope = DeduplicationScope.Global)
    {
        ShiftStartLocalTime = shiftStartLocalTime ?? new TimeOnly(0, 0, 0);
        Scope = scope;
        _activeShiftId = CalculateShiftId(DateTimeOffset.Now);
    }

    public FilterResult Evaluate(TagReadEvent tag, DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(tag);
        DateTimeOffset evaluationTime = now ?? DateTimeOffset.Now;

        CheckAndExecuteShiftRollover(evaluationTime);

        string key = GetCacheKey(tag);

        if (!_seenTags.TryAdd(key, 0))
        {
            return FilterResult.Drop($"Tag '{tag.EPC}' already processed in shift '{_activeShiftId}'.");
        }

        return FilterResult.Pass();
    }

    /// <summary>
    /// Checks if the current evaluation time has crossed into a new shift boundary and resets unique registry.
    /// </summary>
    public bool CheckAndExecuteShiftRollover(DateTimeOffset localTime)
    {
        string currentShiftId = CalculateShiftId(localTime);

        if (_activeShiftId != currentShiftId)
        {
            lock (_rolloverLock)
            {
                if (_activeShiftId != currentShiftId)
                {
                    string oldShift = _activeShiftId;
                    _seenTags.Clear();
                    _activeShiftId = currentShiftId;
                    ShiftRollover?.Invoke(this, currentShiftId);
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Calculates the unique shift ID string for a given timestamp based on ShiftStartLocalTime.
    /// </summary>
    public string CalculateShiftId(DateTimeOffset timestamp)
    {
        DateTime local = timestamp.LocalDateTime;
        DateOnly date = DateOnly.FromDateTime(local);
        TimeOnly time = TimeOnly.FromDateTime(local);

        // If current time is before today's shift start, shift belongs to yesterday
        if (time < ShiftStartLocalTime)
        {
            date = date.AddDays(-1);
        }

        return $"{date:yyyy-MM-dd}_{ShiftStartLocalTime:HH:mm}";
    }

    /// <summary>
    /// Manually resets the unique tag store.
    /// </summary>
    public void Reset() => _seenTags.Clear();

    private string GetCacheKey(TagReadEvent tag) => Scope switch
    {
        DeduplicationScope.PerAntenna => $"{tag.ReaderId}:{tag.AntennaPort}:{tag.EPC}",
        DeduplicationScope.PerReader => $"{tag.ReaderId}:{tag.EPC}",
        DeduplicationScope.Global => tag.EPC,
        _ => tag.EPC
    };
}
