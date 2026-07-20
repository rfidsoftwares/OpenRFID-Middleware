using OpenRFID.Core.Abstractions;
using OpenRFID.Core.Engine.Configuration;
using OpenRFID.Core.Engine.Orchestration;
using OpenRFID.Simulator.Generators;

namespace OpenRFID.E2E.Tests;

public class NetworkFailureResilienceTests : IDisposable
{
    private readonly string _tempConfigFile = Path.Combine(Path.GetTempPath(), $"openrfid_net_fail_{Guid.NewGuid()}.json");

    [Fact]
    public async Task NetworkFailure_BuffersTagsToOfflineQueue_AndTracksStatus()
    {
        var configService = new ConfigurationService(_tempConfigFile);

        // Point config to unreachable target endpoint to force network failure
        var unreachConfig = configService.Current with
        {
            Dispatch = configService.Current.Dispatch with
            {
                TargetUrl = "http://127.0.0.1:59999/unreachable/endpoint"
            }
        };
        configService.SaveConfig(unreachConfig);

        await using var orchestrator = new MiddlewareOrchestrator(configService);
        await orchestrator.StartAsync();

        // Inject 5 tag reads
        for (int i = 0; i < 5; i++)
        {
            var tag = TagGenerator.GenerateTag(epc: $"E28099990000{i:D8}", readerId: "FailoverReader");
            orchestrator.InjectRawTag(tag);
        }

        // Allow background async dispatch/queue to complete (2.5s for HttpClient 2s timeout)
        await Task.Delay(2500);

        var status = await orchestrator.GetHealthStatusAsync();

        Assert.Equal(5, status.TotalRawTagsCount);
        Assert.True(status.OfflineQueueCount > 0, $"Expected offline queue count > 0, got {status.OfflineQueueCount}");
    }

    public void Dispose()
    {
        if (File.Exists(_tempConfigFile))
        {
            try { File.Delete(_tempConfigFile); } catch { }
        }
    }
}
