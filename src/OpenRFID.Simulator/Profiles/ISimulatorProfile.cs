using OpenRFID.Core.Abstractions;

namespace OpenRFID.Simulator.Profiles;

public enum SimulationMode
{
    StaticInventory,
    ConveyorBelt,
    TagStorm,
    FaultyNetwork
}

public interface ISimulatorProfile
{
    SimulationMode Mode { get; }
    IAsyncEnumerable<TagReadEvent> GenerateStreamAsync(CancellationToken ct);
}
