using OpenRFID.Core.Dispatch.Http;

namespace OpenRFID.Core.Storage;

/// <summary>
/// Background controller that monitors network availability and replays offline buffered payloads from SqliteOfflineQueue.
/// </summary>
public sealed class ReplayController : IAsyncDisposable
{
    private readonly SqliteOfflineQueue _queue;
    private readonly HttpDispatcher _dispatcher;
    private readonly TimeSpan _pollInterval;
    private CancellationTokenSource? _cts;
    private Task? _replayTask;
    private int _isDisposed;

    public bool IsReplaying { get; private set; }

    public event EventHandler<long>? ReplayProgress;

    public ReplayController(
        SqliteOfflineQueue queue,
        HttpDispatcher dispatcher,
        TimeSpan? pollInterval = null)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _pollInterval = pollInterval ?? TimeSpan.FromSeconds(5);
    }

    public void Start()
    {
        if (_cts != null) return;
        _cts = new CancellationTokenSource();
        _replayTask = Task.Run(() => ReplayLoopAsync(_cts.Token));
    }

    public async Task StopAsync()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            if (_replayTask != null)
            {
                try { await _replayTask; } catch (OperationCanceledException) { }
            }
            _cts.Dispose();
            _cts = null;
        }
    }

    public async Task<long> DrainQueueOnceAsync(CancellationToken ct = default)
    {
        long initialCount = await _queue.GetQueueCountAsync(ct);
        if (initialCount == 0) return 0;

        IsReplaying = true;
        long processedCount = 0;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var batch = await _queue.PeekBatchAsync(20, ct);
                if (batch.Count == 0) break;

                bool batchHadFailure = false;
                var acknowledgedIds = new List<long>();

                foreach (var item in batch)
                {
                    var result = await _dispatcher.DispatchAsync(
                        item.TargetUrl,
                        item.HttpMethod,
                        item.Payload,
                        item.Headers,
                        item.TransactionId,
                        ct);

                    if (result.IsSuccess)
                    {
                        acknowledgedIds.Add(item.Id);
                        processedCount++;
                    }
                    else
                    {
                        // Network or server error encountered; pause playback cycle to avoid server hammer
                        batchHadFailure = true;
                        break;
                    }
                }

                if (acknowledgedIds.Count > 0)
                {
                    await _queue.AcknowledgeBatchAsync(acknowledgedIds, ct);
                    ReplayProgress?.Invoke(this, processedCount);
                }

                if (batchHadFailure)
                {
                    break;
                }
            }
        }
        finally
        {
            IsReplaying = false;
        }

        return processedCount;
    }

    private async Task ReplayLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_pollInterval, ct);
                long queueCount = await _queue.GetQueueCountAsync(ct);
                if (queueCount > 0)
                {
                    await DrainQueueOnceAsync(ct);
                }
            }
            catch (OperationCanceledException) { break; }
            catch { }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) != 0) return;
        await StopAsync();
    }
}
