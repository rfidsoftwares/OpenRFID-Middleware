using OpenRFID.Core.Abstractions;

namespace OpenRFID.Core.Pipeline;

/// <summary>
/// Interface for a single tag filtering or deduplication component.
/// </summary>
public interface ITagFilter
{
    /// <summary>
    /// Descriptive name of the filter.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Evaluates whether an incoming tag passes or should be dropped by this filter.
    /// </summary>
    FilterResult Evaluate(TagReadEvent tag, DateTimeOffset? now = null);
}
