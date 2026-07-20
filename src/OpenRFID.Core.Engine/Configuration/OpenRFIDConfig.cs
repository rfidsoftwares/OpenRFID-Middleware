using System.Text.Json.Serialization;
using OpenRFID.Core.Abstractions;

namespace OpenRFID.Core.Engine.Configuration;

/// <summary>
/// Root configuration schema for OpenRFID Middleware.
/// </summary>
public sealed record OpenRFIDConfig
{
    public List<ReaderConfig> Readers { get; init; } = [];
    public FilterConfig Filter { get; init; } = new();
    public DispatchConfig Dispatch { get; init; } = new();
    public StorageConfig Storage { get; init; } = new();
    public SecurityConfig Security { get; init; } = new();
}

public sealed record SecurityConfig
{
    public bool Enabled { get; init; } = false;
    public string ApiKey { get; init; } = "openrfid-secret-key-12345";
    public bool EnableCors { get; init; } = true;
    public List<string> AllowedOrigins { get; init; } = ["*"];
}

public sealed record FilterConfig
{
    public double SlidingWindowSeconds { get; init; } = 10.0;
    public bool DailyUniqueEnabled { get; init; } = false;
    public float? MinRssiDbm { get; init; } = null;
    public string? EpcRegexPattern { get; init; } = null;
    public ushort? AntennaMask { get; init; } = null;
    public TimeSpan? AllowedScheduleStart { get; init; } = null;
    public TimeSpan? AllowedScheduleEnd { get; init; } = null;
}

public sealed record DispatchConfig
{
    public string TargetUrl { get; init; } = "http://localhost:8080/api/v1/tags";
    public string HttpMethod { get; init; } = "POST"; // POST, PUT, GET, PATCH
    public Dictionary<string, string> CustomHeaders { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public string TemplateFormat { get; init; } = "json"; // json, form, xml, csv
    public string? CustomTemplate { get; init; } = null;
    public string TriggerMode { get; init; } = "Instant"; // Instant, Periodic, BatchCount, Hybrid
    public int PeriodicIntervalMs { get; init; } = 5000;
    public int BatchCountThreshold { get; init; } = 100;
}

public sealed record StorageConfig
{
    public string DbPath { get; init; } = "openrfid_offline_queue.db";
    public int MaxQueueSize { get; init; } = 100000;
    public int ReplayRatePerSecond { get; init; } = 50;
}
