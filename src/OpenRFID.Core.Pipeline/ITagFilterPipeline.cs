using OpenRFID.Core.Abstractions;

namespace OpenRFID.Core.Pipeline;

/// <summary>
/// Pipeline interface for chaining multiple tag filters.
/// </summary>
public interface ITagFilterPipeline
{
    /// <summary>
    /// Ordered list of filters registered in the pipeline.
    /// </summary>
    IReadOnlyList<ITagFilter> Filters { get; }

    /// <summary>
    /// Processes a tag through the chain of filters, short-circuiting on the first drop result.
    /// </summary>
    FilterResult Process(TagReadEvent tag, DateTimeOffset? now = null);
}
