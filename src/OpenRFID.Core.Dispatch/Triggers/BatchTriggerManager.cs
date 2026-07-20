using System.Collections.Concurrent;
using OpenRFID.Core.Abstractions;

namespace OpenRFID.Core.Dispatch.Triggers;

public enum DispatchTriggerMode
{
    Instant,
    Periodic,
    BatchCount,
    Hybrid
}

/// <summary>
/// Manages tag accumulation and triggers batch flush callbacks based on Instant, Periodic, BatchCount, or Hybrid conditions.
/// </summary>
public sealed class BatchTriggerManager : IAsyncDisposable
{
    private readonly ConcurrentQueue<TagReadEvent> _buffer = new();
    private readonly SemaphoreSlim _flushLock = new(1, 1);
    private CancellationTokenSource? _cts;
    private Task? _periodicTask;
    private int _isDisposed;

    public DispatchTriggerMode Mode { get; }
    public double BatchIntervalSeconds { get; }
    public int MaxBatchSize { get; }

    public int BufferedCount => _buffer.Count;

    public event Func<IReadOnlyList<TagReadEvent>, Task>? BatchReady;

    public BatchTriggerManager(
        DispatchTriggerMode mode = DispatchTriggerMode.Instant,
        double batchIntervalSeconds = 5.0,
        int maxBatchSize = 50)
    {
        Mode = mode;
        BatchIntervalSeconds = Math.Max(0.1, batchIntervalSeconds);
        MaxBatchSize = Math.Max(1, maxBatchSize);

        if (Mode == DispatchTriggerMode.Periodic || Mode == DispatchTriggerMode.Hybrid)
        {
            _cts = new CancellationTokenSource();
            _periodicTask = Task.Run(() => PeriodicLoopAsync(_cts.Token));
        }
    }

    /// <summary>
    /// Enqueues an incoming tag and evaluates immediate flush triggers.
    /// </summary>
    public void Enqueue(TagReadEvent tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        _buffer.Enqueue(tag);

        if (Mode == DispatchTriggerMode.Instant)
        {
            _ = Task.Run(FlushAsync);
        }
        else if ((Mode == DispatchTriggerMode.BatchCount || Mode == DispatchTriggerMode.Hybrid) && _buffer.Count >= MaxBatchSize)
        {
            _ = Task.Run(FlushAsync);
        }
    }

    /// <summary>
    /// Flushes all currently accumulated tags to the BatchReady event handler.
    /// </summary>
    public async Task<int> FlushAsync()
    {
        if (_buffer.IsEmpty) return 0;

        await _flushLock.WaitAsync();
        try
        {
            var batch = new List<TagReadEvent>();
            while (_buffer.TryDequeue(out var tag))
            {
                batch.Add(tag);
                if (Mode != DispatchTriggerMode.Instant && batch.Count >= MaxBatchSize)
                {
                    break;
                }
            }

            if (batch.Count > 0 && BatchReady != null)
            {
                await BatchReady.Invoke(batch.AsReadOnly());
            }

            return batch.Count;
        }
        finally
        {
            _flushLock.Release();
        }
    }

    private async Task PeriodicLoopAsync(CancellationToken ct)
    {
        TimeSpan interval = TimeSpan.FromSeconds(BatchIntervalSeconds);
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, ct);
                if (!_buffer.IsEmpty)
                {
                    await FlushAsync();
                }
            }
            catch (OperationCanceledException) { break; }
            catch { }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) != 0) return;

        if (_cts != null)
        {
            _cts.Cancel();
            if (_periodicTask != null)
            {
                try { await _periodicTask; } catch (OperationCanceledException) { }
            }
            _cts.Dispose();
            _cts = null;
        }

        await FlushAsync();
        _flushLock.Dispose();
    }
}
