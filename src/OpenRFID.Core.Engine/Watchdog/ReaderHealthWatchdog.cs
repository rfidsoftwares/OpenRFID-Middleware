using OpenRFID.Core.Abstractions;

namespace OpenRFID.Core.Engine.Watchdog;

/// <summary>
/// Monitors RFID reader connection health, detects state dropouts, and automatically executes reconnect attempts with exponential backoff.
/// </summary>
public sealed class ReaderHealthWatchdog : IAsyncDisposable
{
    private readonly IReaderConnection _connection;
    private readonly ReaderConfig _config;
    private readonly TimeSpan _baseDelay;
    private readonly TimeSpan _maxDelay;
    private readonly Random _jitter = new();

    private CancellationTokenSource? _cts;
    private Task? _monitoringTask;
    private int _isDisposed;

    public string ReaderId => _connection.ReaderId;
    public ConnectionState CurrentState => _connection.State;

    public event EventHandler<ReaderStatusEventArgs>? StatusChanged;

    public ReaderHealthWatchdog(
        IReaderConnection connection,
        ReaderConfig config,
        TimeSpan? baseDelay = null,
        TimeSpan? maxDelay = null)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _baseDelay = baseDelay ?? TimeSpan.FromSeconds(1);
        _maxDelay = maxDelay ?? TimeSpan.FromSeconds(60);

        _connection.StatusChanged += OnConnectionStatusChanged;
    }

    /// <summary>
    /// Starts the background watchdog monitoring loop.
    /// </summary>
    public void Start()
    {
        if (_cts != null) return;
        _cts = new CancellationTokenSource();
        _monitoringTask = Task.Run(() => MonitorLoopAsync(_cts.Token));
    }

    /// <summary>
    /// Stops the background watchdog monitoring loop.
    /// </summary>
    public async Task StopAsync()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            if (_monitoringTask != null)
            {
                try
                {
                    await _monitoringTask;
                }
                catch (OperationCanceledException) { }
            }
            _cts.Dispose();
            _cts = null;
        }
    }

    private void OnConnectionStatusChanged(object? sender, ReaderStatusEventArgs e)
    {
        StatusChanged?.Invoke(this, e);
    }

    private async Task MonitorLoopAsync(CancellationToken ct)
    {
        int retryCount = 0;
        int intervalMs = Math.Max(1000, _config.HealthCheckIntervalMs);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(intervalMs, ct);

                if (_connection.State == ConnectionState.Disconnected || _connection.State == ConnectionState.Faulted)
                {
                    retryCount++;
                    TimeSpan backoff = CalculateBackoff(retryCount);

                    StatusChanged?.Invoke(this, new ReaderStatusEventArgs(
                        ReaderId,
                        _connection.State,
                        ConnectionState.Reconnecting,
                        $"Initiating auto-reconnect attempt #{retryCount} after {backoff.TotalSeconds:F1}s delay."
                    ));

                    await Task.Delay(backoff, ct);

                    try
                    {
                        await _connection.ConnectAsync(ct);
                        if (_connection.State == ConnectionState.Connected)
                        {
                            retryCount = 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        StatusChanged?.Invoke(this, new ReaderStatusEventArgs(
                            ReaderId,
                            _connection.State,
                            ConnectionState.Faulted,
                            $"Reconnect attempt #{retryCount} failed: {ex.Message}",
                            ex
                        ));
                    }
                }
                else if (_connection.State == ConnectionState.Connected)
                {
                    retryCount = 0;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, new ReaderStatusEventArgs(
                    ReaderId,
                    _connection.State,
                    ConnectionState.Faulted,
                    $"Error in watchdog loop: {ex.Message}",
                    ex
                ));
            }
        }
    }

    /// <summary>
    /// Calculates exponential backoff with jitter.
    /// </summary>
    public TimeSpan CalculateBackoff(int retryCount)
    {
        double factor = Math.Pow(2, Math.Min(retryCount - 1, 10));
        double delayMs = _baseDelay.TotalMilliseconds * factor;
        double maxMs = _maxDelay.TotalMilliseconds;
        
        // Cap at max delay
        delayMs = Math.Min(delayMs, maxMs);

        // Apply +/- 20% jitter
        lock (_jitter)
        {
            double jitterPercentage = (_jitter.NextDouble() * 0.4) - 0.2; // -0.2 to +0.2
            delayMs += delayMs * jitterPercentage;
        }

        return TimeSpan.FromMilliseconds(Math.Max(500, delayMs));
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) != 0) return;

        _connection.StatusChanged -= OnConnectionStatusChanged;
        await StopAsync();
    }
}
