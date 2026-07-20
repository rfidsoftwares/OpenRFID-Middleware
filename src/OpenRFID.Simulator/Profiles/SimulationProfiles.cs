using System.Runtime.CompilerServices;
using OpenRFID.Core.Abstractions;
using OpenRFID.Simulator.Generators;

namespace OpenRFID.Simulator.Profiles;

public sealed class StaticInventoryProfile : ISimulatorProfile
{
    private readonly List<string> _staticEpcs;
    private readonly int _intervalMs;
    private readonly string _readerId;

    public SimulationMode Mode => SimulationMode.StaticInventory;

    public StaticInventoryProfile(int uniqueTagCount = 10, int intervalMs = 200, string readerId = "Sim-Static-01")
    {
        _intervalMs = intervalMs;
        _readerId = readerId;
        _staticEpcs = Enumerable.Range(1, uniqueTagCount)
            .Select(i => $"E280119100000000{i:D8}")
            .ToList();
    }

    public async IAsyncEnumerable<TagReadEvent> GenerateStreamAsync([EnumeratorCancellation] CancellationToken ct)
    {
        int index = 0;
        while (!ct.IsCancellationRequested)
        {
            string epc = _staticEpcs[index % _staticEpcs.Count];
            yield return TagGenerator.GenerateTag(epc: epc, readerId: _readerId, antennaPort: (index % 4) + 1);
            index++;

            if (_intervalMs > 0)
            {
                await Task.Delay(_intervalMs, ct);
            }
        }
    }
}

public sealed class ConveyorBeltProfile : ISimulatorProfile
{
    private readonly int _itemDwellCount;
    private readonly int _itemIntervalMs;
    private readonly string _readerId;

    public SimulationMode Mode => SimulationMode.ConveyorBelt;

    public ConveyorBeltProfile(int itemDwellCount = 5, int itemIntervalMs = 500, string readerId = "Sim-Conveyor-01")
    {
        _itemDwellCount = itemDwellCount;
        _itemIntervalMs = itemIntervalMs;
        _readerId = readerId;
    }

    public async IAsyncEnumerable<TagReadEvent> GenerateStreamAsync([EnumeratorCancellation] CancellationToken ct)
    {
        int boxId = 1000;
        while (!ct.IsCancellationRequested)
        {
            string epc = $"E28055550000{boxId:D8}";
            for (int i = 0; i < _itemDwellCount; i++)
            {
                if (ct.IsCancellationRequested) yield break;
                yield return TagGenerator.GenerateTag(epc: epc, readerId: _readerId, antennaPort: 1, rssi: -45.0f + (i * 2.0f));
                await Task.Delay(100, ct);
            }

            boxId++;
            await Task.Delay(_itemIntervalMs, ct);
        }
    }
}

public sealed class TagStormProfile : ISimulatorProfile
{
    private readonly int _targetRatePerSec;
    private readonly string _readerId;

    public SimulationMode Mode => SimulationMode.TagStorm;

    public TagStormProfile(int targetRatePerSec = 2000, string readerId = "Sim-Storm-01")
    {
        _targetRatePerSec = targetRatePerSec;
        _readerId = readerId;
    }

    public async IAsyncEnumerable<TagReadEvent> GenerateStreamAsync([EnumeratorCancellation] CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            yield return TagGenerator.GenerateTag(readerId: _readerId);
            // Yield without delay for max throughput benchmarking
            await Task.Yield();
        }
    }
}

public sealed class FaultyNetworkProfile : ISimulatorProfile
{
    private readonly string _readerId;
    private readonly Random _rnd = new();

    public SimulationMode Mode => SimulationMode.FaultyNetwork;

    public FaultyNetworkProfile(string readerId = "Sim-Faulty-01")
    {
        _readerId = readerId;
    }

    public async IAsyncEnumerable<TagReadEvent> GenerateStreamAsync([EnumeratorCancellation] CancellationToken ct)
    {
        int count = 0;
        while (!ct.IsCancellationRequested)
        {
            count++;
            if (count % 20 == 0)
            {
                // Simulate network dropout exception
                throw new TimeoutException($"Simulated RFID reader '{_readerId}' connection timeout/dropped socket.");
            }

            yield return TagGenerator.GenerateTag(readerId: _readerId);
            await Task.Delay(150, ct);
        }
    }
}
