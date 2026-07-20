namespace OpenRFID.Core.Dispatch.Http;

/// <summary>
/// Execution result of an HTTP dispatch attempt.
/// </summary>
public sealed record DispatchResult
{
    public required bool IsSuccess { get; init; }
    public required int HttpStatusCode { get; init; }
    public required string TargetUrl { get; init; }
    public string? ResponseContent { get; init; }
    public string? ErrorMessage { get; init; }
    public TimeSpan Elapsed { get; init; }
    public required string TransactionId { get; init; }
}
