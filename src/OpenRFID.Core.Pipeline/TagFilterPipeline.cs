using OpenRFID.Core.Abstractions;

namespace OpenRFID.Core.Pipeline;

/// <summary>
/// Composite pipeline that executes an ordered sequence of ITagFilter stages, short-circuiting on the first drop.
/// </summary>
public sealed class TagFilterPipeline : ITagFilterPipeline
{
    private readonly List<ITagFilter> _filters = new();
    private long _totalEvaluated;
    private long _totalPassed;
    private long _totalDropped;

    public IReadOnlyList<ITagFilter> Filters
    {
        get
        {
            lock (_filters)
            {
                return _filters.ToList().AsReadOnly();
            }
        }
    }

    public long TotalEvaluated => Interlocked.Read(ref _totalEvaluated);
    public long TotalPassed => Interlocked.Read(ref _totalPassed);
    public long TotalDropped => Interlocked.Read(ref _totalDropped);

    public TagFilterPipeline(IEnumerable<ITagFilter>? filters = null)
    {
        if (filters != null)
        {
            _filters.AddRange(filters);
        }
    }

    public void AddFilter(ITagFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        lock (_filters)
        {
            _filters.Add(filter);
        }
    }

    public bool RemoveFilter(ITagFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        lock (_filters)
        {
            return _filters.Remove(filter);
        }
    }

    public FilterResult Process(TagReadEvent tag, DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(tag);
        Interlocked.Increment(ref _totalEvaluated);

        List<ITagFilter> currentFilters;
        lock (_filters)
        {
            currentFilters = _filters.ToList();
        }

        foreach (var filter in currentFilters)
        {
            FilterResult result = filter.Evaluate(tag, now);
            if (!result.IsPassed)
            {
                Interlocked.Increment(ref _totalDropped);
                return result;
            }
        }

        Interlocked.Increment(ref _totalPassed);
        return FilterResult.Pass();
    }
}
