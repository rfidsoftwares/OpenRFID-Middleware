using OpenRFID.Core.Abstractions;

namespace OpenRFID.Simulator.Generators;

public static class TagGenerator
{
    private static readonly Random Rnd = new();

    public static TagReadEvent GenerateTag(
        string? epc = null,
        string? readerId = "Simulator-01",
        int antennaPort = 1,
        float? rssi = null)
    {
        string epcVal = epc ?? $"E2801191{Rnd.Next(10000000, 99999999):X8}";
        string tidVal = $"E2003412{Rnd.Next(100000, 999999):X6}";
        float rssiVal = rssi ?? (-40.0f - Rnd.NextSingle() * 40.0f);

        return new TagReadEvent
        {
            EPC = epcVal,
            TID = tidVal,
            UserMemory = "0000",
            AntennaPort = antennaPort,
            RSSI = rssiVal,
            ReadCount = 1,
            FirstSeenTime = DateTimeOffset.UtcNow,
            LastSeenTime = DateTimeOffset.UtcNow,
            ReaderId = readerId ?? "Simulator-01"
        };
    }

    public static List<TagReadEvent> GenerateBatch(int count, string? readerId = "Simulator-01")
    {
        var batch = new List<TagReadEvent>(count);
        for (int i = 0; i < count; i++)
        {
            batch.Add(GenerateTag(readerId: readerId, antennaPort: (i % 4) + 1));
        }
        return batch;
    }
}
