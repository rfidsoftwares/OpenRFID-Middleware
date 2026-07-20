using OpenRFID.Core.Abstractions;
using OpenRFID.Core.Engine.Configuration;
using OpenRFID.Core.Engine.Orchestration;
using OpenRFID.Simulator;
using OpenRFID.Simulator.Profiles;

namespace OpenRFID.E2E.Tests;

public class EndToEndPipelineTests : IDisposable
{
    private readonly string _tempConfigFile = Path.Combine(Path.GetTempPath(), $"openrfid_e2e_{Guid.NewGuid()}.json");

    [Fact]
    public async Task EndToEnd_SimulatorToOrchestratorPipeline_FlowsTagsSuccessfully()
    {
        var configService = new ConfigurationService(_tempConfigFile);
        await using var orchestrator = new MiddlewareOrchestrator(configService);

        int rawCount = 0;
        int filteredCount = 0;

        orchestrator.RawTagReceived += (_, _) => Interlocked.Increment(ref rawCount);
        orchestrator.FilteredTagDispatched += (_, _) => Interlocked.Increment(ref filteredCount);

        await orchestrator.StartAsync();

        // Create simulator connection with ConveyorBelt profile
        var profile = new ConveyorBeltProfile(itemDwellCount: 3, itemIntervalMs: 100, readerId: "E2E-Reader-1");
        await using var connection = new SimulatorReaderConnection("E2E-Reader-1", profile);

        var readerConfig = new ReaderConfig
        {
            ReaderId = "E2E-Reader-1",
            ProviderId = "Simulator",
            BrandName = "E2E Virtual Simulator"
        };

        orchestrator.RegisterReaderConnection(connection, readerConfig);
        await connection.ConnectAsync();

        // Allow stream to run briefly
        await Task.Delay(800);

        await connection.DisconnectAsync();
        await orchestrator.StopAsync();

        Assert.True(rawCount > 0, "Expected raw tags to be received from simulator.");
        Assert.True(filteredCount > 0, "Expected filtered tags to be dispatched by orchestrator.");
    }

    public void Dispose()
    {
        if (File.Exists(_tempConfigFile))
        {
            try { File.Delete(_tempConfigFile); } catch { }
        }
    }
}
