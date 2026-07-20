using OpenRFID.Core.Abstractions;
using OpenRFID.Core.Engine.Watchdog;
using Xunit;

namespace OpenRFID.Core.Tests;

public class ReaderHealthWatchdogTests
{
    private class DummyConnection : IReaderConnection
    {
        public string ReaderId => "reader-watchdog-test";
        public ConnectionState State { get; set; } = ConnectionState.Disconnected;

        public event EventHandler<TagReadEventArgs>? TagRead { add { } remove { } }
        public event EventHandler<ReaderStatusEventArgs>? StatusChanged;

        public Task ConnectAsync(CancellationToken ct = default)
        {
            State = ConnectionState.Connected;
            StatusChanged?.Invoke(this, new ReaderStatusEventArgs(ReaderId, ConnectionState.Disconnected, ConnectionState.Connected));
            return Task.CompletedTask;
        }

        public Task DisconnectAsync(CancellationToken ct = default)
        {
            State = ConnectionState.Disconnected;
            return Task.CompletedTask;
        }

        public Task ApplyConfigAsync(ReaderConfig config, CancellationToken ct = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Watchdog_CalculateBackoff_IncreasesExponentiallyWithJitter()
    {
        var config = new ReaderConfig { ReaderId = "r1", ProviderId = "p1" };
        var conn = new DummyConnection();
        await using var watchdog = new ReaderHealthWatchdog(conn, config, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30));

        TimeSpan delay1 = watchdog.CalculateBackoff(1);
        TimeSpan delay2 = watchdog.CalculateBackoff(2);
        TimeSpan delay3 = watchdog.CalculateBackoff(3);

        Assert.True(delay1 >= TimeSpan.FromMilliseconds(500));
        Assert.True(delay2 > TimeSpan.FromMilliseconds(1000));
        Assert.True(delay3 > delay2 * 0.8);
    }
}
