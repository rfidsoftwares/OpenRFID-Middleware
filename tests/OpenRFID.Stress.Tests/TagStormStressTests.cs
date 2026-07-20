using System.Diagnostics;
using OpenRFID.Core.Pipeline;
using OpenRFID.Core.Pipeline.Filters;
using OpenRFID.Simulator.Generators;

namespace OpenRFID.Stress.Tests;

public class TagStormStressTests
{
    [Fact]
    public void TagFilterPipeline_Processes10000Tags_UnderHighThroughputThreshold()
    {
        var pipeline = new TagFilterPipeline(new ITagFilter[]
        {
            new SlidingWindowFilter(10.0),
            new MetadataFilter(minRssiDbm: -80.0f, regexPattern: "^E280.*")
        });

        int tagCount = 10000;
        var tags = TagGenerator.GenerateBatch(tagCount);

        var sw = Stopwatch.StartNew();
        int passedCount = 0;

        foreach (var tag in tags)
        {
            var result = pipeline.Process(tag);
            if (result.IsPassed) passedCount++;
        }

        sw.Stop();

        double tagsPerSec = tagCount / sw.Elapsed.TotalSeconds;

        Assert.Equal(tagCount, pipeline.TotalEvaluated);
        Assert.True(tagsPerSec > 1000.0, $"Expected throughput > 1000 tags/sec, got {tagsPerSec:F0} tags/sec.");
    }
}
