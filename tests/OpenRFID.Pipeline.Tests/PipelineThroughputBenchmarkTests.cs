using System.Diagnostics;
using OpenRFID.Core.Abstractions;
using OpenRFID.Core.Pipeline;
using OpenRFID.Core.Pipeline.Filters;
using Xunit;
using Xunit.Abstractions;

namespace OpenRFID.Pipeline.Tests;

public class PipelineThroughputBenchmarkTests
{
    private readonly ITestOutputHelper _output;

    public PipelineThroughputBenchmarkTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Pipeline_ThroughputBenchmark_Exceeds5000TagsPerSecond()
    {
        var metadataFilter = new MetadataFilter(minRssiDbm: -70.0f, epcPrefix: "E280");
        var slidingWindow = new SlidingWindowFilter(windowSeconds: 15.0, scope: DeduplicationScope.PerAntenna);

        var pipeline = new TagFilterPipeline(new ITagFilter[] { metadataFilter, slidingWindow });

        const int totalTags = 50_000;
        var tags = new TagReadEvent[totalTags];
        for (int i = 0; i < totalTags; i++)
        {
            tags[i] = new TagReadEvent
            {
                EPC = $"E280{(i % 500):X8}",
                ReaderId = $"reader-{(i % 4) + 1}",
                AntennaPort = (i % 4) + 1,
                RSSI = -50.0f - (i % 15)
            };
        }

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < totalTags; i++)
        {
            pipeline.Process(tags[i]);
        }
        sw.Stop();

        double tagsPerSec = totalTags / sw.Elapsed.TotalSeconds;
        _output.WriteLine($"Processed {totalTags} tag evaluations in {sw.ElapsedMilliseconds} ms ({tagsPerSec:F0} tags/sec).");

        Assert.Equal(totalTags, pipeline.TotalEvaluated);
        Assert.True(tagsPerSec > 5000, $"Throughput {tagsPerSec:F0} tags/sec did not meet 5,000 tags/sec target.");
    }
}
