using OpenRFID.Core.Abstractions;
using OpenRFID.Core.Engine.Configuration;
using OpenRFID.Core.Engine.Orchestration;

namespace OpenRFID.Management.Tests;

public class MiddlewareOrchestratorTests : IDisposable
{
    private readonly string _tempConfigFile = Path.Combine(Path.GetTempPath(), $"openrfid_orch_test_{Guid.NewGuid()}.json");

    [Fact]
    public async Task StartAsync_InitializesStatus_AndProcessesInjectedTags()
    {
        var configService = new ConfigurationService(_tempConfigFile);
        await using var orchestrator = new MiddlewareOrchestrator(configService);

        TagReadEvent? receivedRawTag = null;
        TagReadEvent? receivedFilteredTag = null;

        orchestrator.RawTagReceived += (_, tag) => receivedRawTag = tag;
        orchestrator.FilteredTagDispatched += (_, tag) => receivedFilteredTag = tag;

        await orchestrator.StartAsync();
        var health = await orchestrator.GetHealthStatusAsync();

        Assert.True(health.IsRunning);
        Assert.Equal(0, health.TotalRawTagsCount);

        // Inject sample tag
        var sampleTag = new TagReadEvent
        {
            EPC = "E28011912000000000009999",
            RSSI = -50f,
            AntennaPort = 1,
            FirstSeenTime = DateTimeOffset.UtcNow,
            LastSeenTime = DateTimeOffset.UtcNow,
            ReaderId = "TestReader"
        };

        orchestrator.InjectRawTag(sampleTag);

        Assert.NotNull(receivedRawTag);
        Assert.NotNull(receivedFilteredTag);
        Assert.Equal("E28011912000000000009999", receivedRawTag.EPC);

        var updatedHealth = await orchestrator.GetHealthStatusAsync();
        Assert.Equal(1, updatedHealth.TotalRawTagsCount);
        Assert.Equal(1, updatedHealth.TotalFilteredTagsCount);
    }

    public void Dispose()
    {
        if (File.Exists(_tempConfigFile))
        {
            File.Delete(_tempConfigFile);
        }
    }
}
