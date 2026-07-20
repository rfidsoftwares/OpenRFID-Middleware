using OpenRFID.Core.Abstractions;
using OpenRFID.Core.Dispatch.Triggers;
using Xunit;

namespace OpenRFID.Dispatch.Tests;

public class BatchTriggerManagerTests
{
    [Fact]
    public async Task BatchTrigger_BatchCountMode_FlushesWhenSizeReached()
    {
        await using var manager = new BatchTriggerManager(DispatchTriggerMode.BatchCount, maxBatchSize: 3);
        IReadOnlyList<TagReadEvent>? flushedBatch = null;

        manager.BatchReady += batch =>
        {
            flushedBatch = batch;
            return Task.CompletedTask;
        };

        manager.Enqueue(new TagReadEvent { EPC = "E1", ReaderId = "r1" });
        manager.Enqueue(new TagReadEvent { EPC = "E2", ReaderId = "r1" });
        Assert.Null(flushedBatch);

        manager.Enqueue(new TagReadEvent { EPC = "E3", ReaderId = "r1" });
        
        // Wait briefly for Task.Run trigger
        await Task.Delay(100);

        Assert.NotNull(flushedBatch);
        Assert.Equal(3, flushedBatch.Count);
    }
}
