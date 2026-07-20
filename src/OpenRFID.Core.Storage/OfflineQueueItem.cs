namespace OpenRFID.Core.Storage;

public sealed record OfflineQueueItem
{
    public required long Id { get; init; }
    public required string TransactionId { get; init; }
    public required string TargetUrl { get; init; }
    public required string HttpMethod { get; init; }
    public required string Payload { get; init; }
    public Dictionary<string, string> Headers { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public required int TagCount { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public int RetryCount { get; init; }
}
