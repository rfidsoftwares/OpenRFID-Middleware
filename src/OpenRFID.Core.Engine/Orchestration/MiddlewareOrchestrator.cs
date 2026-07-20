using System.Collections.Concurrent;
using OpenRFID.Core.Abstractions;
using OpenRFID.Core.Dispatch.Http;
using OpenRFID.Core.Dispatch.Templating;
using OpenRFID.Core.Dispatch.Triggers;
using OpenRFID.Core.Engine.Configuration;
using OpenRFID.Core.Engine.Watchdog;
using OpenRFID.Core.Pipeline;
using OpenRFID.Core.Pipeline.Filters;
using OpenRFID.Core.Storage;

namespace OpenRFID.Core.Engine.Orchestration;

public sealed record SystemHealthStatus
{
    public required bool IsRunning { get; init; }
    public required TimeSpan Uptime { get; init; }
    public required int ConnectedReadersCount { get; init; }
    public required int TotalReadersCount { get; init; }
    public required long TotalRawTagsCount { get; init; }
    public required long TotalFilteredTagsCount { get; init; }
    public required long OfflineQueueCount { get; init; }
    public required List<ReaderStatusInfo> ReaderStatuses { get; init; }
}

public sealed record ReaderStatusInfo
{
    public required string ReaderId { get; init; }
    public required string ProviderId { get; init; }
    public required ConnectionState State { get; init; }
    public required DateTimeOffset LastSeenUtc { get; init; }
}

/// <summary>
/// Core orchestrator managing readers, filter pipeline, dispatchers, and offline queue.
/// </summary>
public sealed class MiddlewareOrchestrator : IAsyncDisposable
{
    private readonly ConfigurationService _configService;
    private readonly DateTimeOffset _startTime = DateTimeOffset.UtcNow;
    private readonly ConcurrentDictionary<string, IReaderConnection> _activeConnections = new();
    private readonly ConcurrentDictionary<string, ReaderHealthWatchdog> _watchdogs = new();
    private readonly ConcurrentDictionary<string, ReaderStatusInfo> _readerStatuses = new();

    private TagFilterPipeline _pipeline = new();
    private HttpClient? _httpClient;
    private HttpDispatcher? _httpDispatcher;
    private SqliteOfflineQueue? _offlineQueue;
    private ReplayController? _replayController;
    private BatchTriggerManager? _batchTriggerManager;
    private readonly PayloadTemplateEngine _templateEngine = new();

    private long _totalRawTags = 0;
    private long _totalFilteredTags = 0;

    public event EventHandler<TagReadEvent>? RawTagReceived;
    public event EventHandler<TagReadEvent>? FilteredTagDispatched;
    public event EventHandler<string>? LogOccurred;

    public MiddlewareOrchestrator(ConfigurationService configService)
    {
        _configService = configService;
        _configService.ConfigChanged += OnConfigChanged;
    }

    public bool IsRunning { get; private set; }

    /// <summary>
    /// Gets overall system health status asynchronously, avoiding sync-over-async deadlocks.
    /// </summary>
    public async Task<SystemHealthStatus> GetHealthStatusAsync(CancellationToken ct = default)
    {
        var statuses = _readerStatuses.Values.ToList();
        var connectedCount = statuses.Count(s => s.State == ConnectionState.Connected);
        var queueCount = _offlineQueue != null ? await _offlineQueue.GetQueueCountAsync(ct) : 0;

        return new SystemHealthStatus
        {
            IsRunning = IsRunning,
            Uptime = DateTimeOffset.UtcNow - _startTime,
            ConnectedReadersCount = connectedCount,
            TotalReadersCount = statuses.Count,
            TotalRawTagsCount = Interlocked.Read(ref _totalRawTags),
            TotalFilteredTagsCount = Interlocked.Read(ref _totalFilteredTags),
            OfflineQueueCount = queueCount,
            ReaderStatuses = statuses
        };
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning) return Task.CompletedTask;

        Log("[Orchestrator] Starting OpenRFID Middleware Orchestrator...");
        var config = _configService.Current;

        // 1. Initialize Pipeline & Filters
        RebuildPipeline(config.Filter);

        // 2. Initialize Dispatcher & Storage with bounded 2s HTTP timeout
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        _httpDispatcher = new HttpDispatcher(_httpClient);
        _offlineQueue = new SqliteOfflineQueue(config.Storage.DbPath);

        // 3. Initialize Replay Controller and start background playback task
        _replayController = new ReplayController(_offlineQueue, _httpDispatcher);
        _replayController.Start();

        // 4. Initialize Batch Trigger Manager according to configured TriggerMode
        RebuildBatchTriggerManager(config.Dispatch);

        // 5. Initialize Registered Reader Statuses
        foreach (var rConfig in config.Readers)
        {
            _readerStatuses[rConfig.ReaderId] = new ReaderStatusInfo
            {
                ReaderId = rConfig.ReaderId,
                ProviderId = rConfig.ProviderId,
                State = ConnectionState.Disconnected,
                LastSeenUtc = DateTimeOffset.UtcNow
            };
        }

        IsRunning = true;
        Log("[Orchestrator] Middleware Orchestrator started successfully.");
        return Task.CompletedTask;
    }

    public void RegisterReaderConnection(IReaderConnection connection, ReaderConfig config)
    {
        ArgumentNullException.ThrowIfNull(connection);

        if (_activeConnections.TryAdd(connection.ReaderId, connection))
        {
            connection.TagRead += OnRawTagFromReader;
            connection.StatusChanged += OnConnectionStatusChanged;

            var watchdog = new ReaderHealthWatchdog(connection, config);
            watchdog.StatusChanged += OnConnectionStatusChanged;
            watchdog.Start();
            _watchdogs[connection.ReaderId] = watchdog;

            _readerStatuses[connection.ReaderId] = new ReaderStatusInfo
            {
                ReaderId = connection.ReaderId,
                ProviderId = config.ProviderId,
                State = connection.State,
                LastSeenUtc = DateTimeOffset.UtcNow
            };
            Log($"[Orchestrator] Registered reader '{connection.ReaderId}'.");
        }
    }

    public void InjectRawTag(TagReadEvent tag)
    {
        OnRawTagEvent(tag);
    }

    private void OnRawTagFromReader(object? sender, TagReadEventArgs e)
    {
        OnRawTagEvent(e.Tag);
    }

    private void OnRawTagEvent(TagReadEvent tag)
    {
        Interlocked.Increment(ref _totalRawTags);
        RawTagReceived?.Invoke(this, tag);

        // Run through filter pipeline
        var result = _pipeline.Process(tag);
        if (result.IsPassed)
        {
            Interlocked.Increment(ref _totalFilteredTags);
            FilteredTagDispatched?.Invoke(this, tag);

            // Enqueue tag to BatchTriggerManager for dispatch
            _batchTriggerManager?.Enqueue(tag);
        }
    }

    private async Task OnBatchReadyAsync(IReadOnlyList<TagReadEvent> tags)
    {
        if (tags.Count == 0 || _httpDispatcher == null) return;

        var config = _configService.Current;
        var sampleTag = tags[0];
        var dummyReaderConfig = config.Readers.FirstOrDefault(r => r.ReaderId == sampleTag.ReaderId) ?? new ReaderConfig
        {
            ReaderId = sampleTag.ReaderId ?? "Middleware",
            ProviderId = "System",
            BrandName = "OpenRFID Reader"
        };

        var payload = _templateEngine.Render(config.Dispatch.CustomTemplate ?? PayloadTemplateEngine.DefaultJsonArrayTemplate, tags, dummyReaderConfig);

        try
        {
            var result = await _httpDispatcher.DispatchAsync(
                config.Dispatch.TargetUrl,
                config.Dispatch.HttpMethod,
                payload,
                headers: config.Dispatch.CustomHeaders);

            if (!result.IsSuccess && _offlineQueue != null)
            {
                await _offlineQueue.EnqueueAsync(
                    Guid.NewGuid().ToString("D"),
                    config.Dispatch.TargetUrl,
                    config.Dispatch.HttpMethod,
                    payload,
                    config.Dispatch.CustomHeaders,
                    tagCount: tags.Count);
            }
        }
        catch (Exception ex)
        {
            Log($"[Orchestrator] Batch dispatch error: {ex.Message}. Buffering batch ({tags.Count} tags) to offline queue.");
            if (_offlineQueue != null)
            {
                await _offlineQueue.EnqueueAsync(
                    Guid.NewGuid().ToString("D"),
                    config.Dispatch.TargetUrl,
                    config.Dispatch.HttpMethod,
                    payload,
                    config.Dispatch.CustomHeaders,
                    tagCount: tags.Count);
            }
        }
    }

    private void OnConnectionStatusChanged(object? sender, ReaderStatusEventArgs e)
    {
        var providerId = ConfigProviderLookup(e.ReaderId);

        _readerStatuses[e.ReaderId] = new ReaderStatusInfo
        {
            ReaderId = e.ReaderId,
            ProviderId = providerId,
            State = e.NewState,
            LastSeenUtc = DateTimeOffset.UtcNow
        };
        Log($"[Orchestrator] Reader '{e.ReaderId}' state changed from {e.PreviousState} to {e.NewState}.");
    }

    private string ConfigProviderLookup(string readerId)
    {
        var config = _configService.Current;
        var matched = config.Readers.FirstOrDefault(r => r.ReaderId == readerId);
        return matched?.ProviderId ?? "Unknown";
    }

    private void OnConfigChanged(object? sender, OpenRFIDConfig newConfig)
    {
        Log("[Orchestrator] Hot-reloading active configuration...");
        RebuildPipeline(newConfig.Filter);
        RebuildBatchTriggerManager(newConfig.Dispatch);
        Log("[Orchestrator] Hot-reload complete.");
    }

    private void RebuildBatchTriggerManager(DispatchConfig dispatchConfig)
    {
        if (!Enum.TryParse<DispatchTriggerMode>(dispatchConfig.TriggerMode, true, out var mode))
        {
            mode = DispatchTriggerMode.Instant;
        }

        _batchTriggerManager = new BatchTriggerManager(
            mode: mode,
            batchIntervalSeconds: dispatchConfig.PeriodicIntervalMs / 1000.0,
            maxBatchSize: dispatchConfig.BatchCountThreshold);

        _batchTriggerManager.BatchReady += OnBatchReadyAsync;
    }

    private void RebuildPipeline(FilterConfig filterConfig)
    {
        var filters = new List<ITagFilter>();

        if (filterConfig.SlidingWindowSeconds > 0)
        {
            filters.Add(new SlidingWindowFilter(filterConfig.SlidingWindowSeconds));
        }

        if (filterConfig.DailyUniqueEnabled)
        {
            filters.Add(new DailyUniqueFilter());
        }

        if (filterConfig.MinRssiDbm.HasValue || !string.IsNullOrWhiteSpace(filterConfig.EpcRegexPattern) || filterConfig.AntennaMask.HasValue)
        {
            filters.Add(new MetadataFilter(
                minRssiDbm: filterConfig.MinRssiDbm,
                antennaMask: filterConfig.AntennaMask,
                regexPattern: filterConfig.EpcRegexPattern));
        }

        if (filterConfig.AllowedScheduleStart.HasValue && filterConfig.AllowedScheduleEnd.HasValue)
        {
            filters.Add(new ScheduleFilter(
                startTime: TimeOnly.FromTimeSpan(filterConfig.AllowedScheduleStart.Value),
                endTime: TimeOnly.FromTimeSpan(filterConfig.AllowedScheduleEnd.Value)));
        }

        _pipeline = new TagFilterPipeline(filters);
    }

    private void Log(string message)
    {
        LogOccurred?.Invoke(this, $"[{DateTimeOffset.UtcNow:HH:mm:ss}] {message}");
    }

    public async Task StopAsync()
    {
        if (!IsRunning) return;
        Log("[Orchestrator] Stopping Orchestrator...");

        foreach (var watchdog in _watchdogs.Values)
        {
            await watchdog.DisposeAsync();
        }
        _watchdogs.Clear();

        foreach (var conn in _activeConnections.Values)
        {
            await conn.DisposeAsync();
        }
        _activeConnections.Clear();

        if (_batchTriggerManager != null)
        {
            await _batchTriggerManager.DisposeAsync();
            _batchTriggerManager = null;
        }

        if (_replayController != null)
        {
            await _replayController.DisposeAsync();
            _replayController = null;
        }

        IsRunning = false;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _offlineQueue?.Dispose();
        _httpClient?.Dispose();
    }
}
