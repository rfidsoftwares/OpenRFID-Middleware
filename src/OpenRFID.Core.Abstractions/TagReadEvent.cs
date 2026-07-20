namespace OpenRFID.Core.Abstractions;

/// <summary>
/// Normalized event representation of a single RFID tag read across any reader vendor or protocol.
/// </summary>
public sealed record TagReadEvent
{
    public required string EPC { get; init; }
    public string? TID { get; init; }
    public string? UserMemory { get; init; }
    public int AntennaPort { get; init; } = 1;
    public float RSSI { get; init; }
    public int ReadCount { get; init; } = 1;
    public DateTimeOffset FirstSeenTime { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSeenTime { get; init; } = DateTimeOffset.UtcNow;
    public required string ReaderId { get; init; }
    public string? Location { get; init; }
    public Dictionary<string, string> ExtraMetadata { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
