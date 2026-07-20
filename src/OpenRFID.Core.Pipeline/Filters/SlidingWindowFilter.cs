using System.Collections.Concurrent;
using OpenRFID.Core.Abstractions;

namespace OpenRFID.Core.Pipeline.Filters;

public enum DeduplicationScope
{
    PerAntenna,
    PerReader,
    Global
}

/// <summary>
/// Thread-safe sliding time window deduplication filter that suppresses repetitive tag reads within a configurable duration.
/// </summary>
public sealed class SlidingWindowFilter : ITagFilter
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _seenCache = new(StringComparer.OrdinalIgnoreCase);
    private int _cleanupCounter;

    public string Name => "SlidingWindowFilter";
    public double WindowSeconds { get; }
    public DeduplicationScope Scope { get; }

    public int CacheCount => _seenCache.Count;

    public SlidingWindowFilter(double windowSeconds = 15.0, DeduplicationScope scope = DeduplicationScope.PerAntenna)
    {
        if (windowSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(windowSeconds), "Window duration must be greater than zero.");
        }
        WindowSeconds = windowSeconds;
        Scope = scope;
    }

    public FilterResult Evaluate(TagReadEvent tag, DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(tag);
        DateTimeOffset currentTime = now ?? DateTimeOffset.UtcNow;

        string key = GetCacheKey(tag);

        if (_seenCache.TryGetValue(key, out DateTimeOffset lastSeen))
        {
            double elapsedSeconds = (currentTime - lastSeen).TotalSeconds;
            if (elapsedSeconds >= 0 && elapsedSeconds < WindowSeconds)
            {
                // Refresh timestamp to extend window while tag remains continuously in RF field
                _seenCache[key] = currentTime;
                return FilterResult.Drop($"Tag '{tag.EPC}' deduplicated (last seen {elapsedSeconds:F1}s ago, window: {WindowSeconds}s).");
            }
        }

        _seenCache[key] = currentTime;

        // Periodic maintenance to prevent memory growth under continuous tag streams
        if (Interlocked.Increment(ref _cleanupCounter) % 1000 == 0)
        {
            PruneExpired(currentTime);
        }

        return FilterResult.Pass();
    }

    /// <summary>
    /// Evicts expired cache entries older than the window duration.
    /// </summary>
    public int PruneExpired(DateTimeOffset? now = null)
    {
        DateTimeOffset currentTime = now ?? DateTimeOffset.UtcNow;
        int evicted = 0;

        foreach (var pair in _seenCache)
        {
            if ((currentTime - pair.Value).TotalSeconds >= WindowSeconds)
            {
                if (_seenCache.TryRemove(pair.Key, out _))
                {
                    evicted++;
                }
            }
        }

        return evicted;
    }

    /// <summary>
    /// Clears all stored tag deduplication state.
    /// </summary>
    public void Clear() => _seenCache.Clear();

    private string GetCacheKey(TagReadEvent tag) => Scope switch
    {
        DeduplicationScope.PerAntenna => $"{tag.ReaderId}:{tag.AntennaPort}:{tag.EPC}",
        DeduplicationScope.PerReader => $"{tag.ReaderId}:{tag.EPC}",
        DeduplicationScope.Global => tag.EPC,
        _ => tag.EPC
    };
}
