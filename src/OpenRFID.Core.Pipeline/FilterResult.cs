namespace OpenRFID.Core.Pipeline;

/// <summary>
/// Result of evaluating a tag against a filter or pipeline chain.
/// </summary>
public readonly struct FilterResult
{
    public bool IsPassed { get; }
    public string? DroppedReason { get; }

    private FilterResult(bool isPassed, string? droppedReason)
    {
        IsPassed = isPassed;
        DroppedReason = droppedReason;
    }

    public static FilterResult Pass() => new(true, null);
    public static FilterResult Drop(string reason) => new(false, reason ?? "Dropped by filter");
}
