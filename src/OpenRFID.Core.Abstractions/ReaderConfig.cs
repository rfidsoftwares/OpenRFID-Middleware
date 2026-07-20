namespace OpenRFID.Core.Abstractions;

/// <summary>
/// Configuration object for an RFID reader connection.
/// </summary>
public sealed record ReaderConfig
{
    public required string ReaderId { get; init; }
    public required string ProviderId { get; init; }
    public string? BrandName { get; init; }
    public string? IpAddress { get; init; }
    public int? Port { get; init; }
    public string? ComPort { get; init; }
    public int? BaudRate { get; init; }
    public ushort? AntennaMask { get; init; }
    public float? PowerDbm { get; init; }
    public int HealthCheckIntervalMs { get; init; } = 5000;
    public Dictionary<string, string> ExtraOptions { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
