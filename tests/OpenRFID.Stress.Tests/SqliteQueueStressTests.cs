using OpenRFID.Core.Storage;

namespace OpenRFID.Stress.Tests;

public class SqliteQueueStressTests : IDisposable
{
    private readonly string _tempDb = Path.Combine(Path.GetTempPath(), $"openrfid_stress_{Guid.NewGuid()}.db");

    [Fact]
    public async Task SqliteOfflineQueue_HandlesConcurrentEnqueuesAndBatchPeeks()
    {
        using var queue = new SqliteOfflineQueue(_tempDb);
        int totalItems = 1000;

        // Perform concurrent insertions
        var tasks = Enumerable.Range(1, totalItems).Select(i => queue.EnqueueAsync(
            transactionId: Guid.NewGuid().ToString("D"),
            targetUrl: "http://localhost:8080/api/v1/tags",
            httpMethod: "POST",
            payload: $"{{\"epc\":\"E280{i:D8}\"}}",
            tagCount: 1
        ));

        await Task.WhenAll(tasks);

        long count = await queue.GetQueueCountAsync();
        Assert.Equal(totalItems, count);

        // Peek batch
        var items = await queue.PeekBatchAsync(count: 100);
        Assert.Equal(100, items.Count);

        // Acknowledge batch
        await queue.AcknowledgeBatchAsync(items.Select(x => x.Id));
        long remaining = await queue.GetQueueCountAsync();
        Assert.Equal(totalItems - 100, remaining);
    }

    public void Dispose()
    {
        if (File.Exists(_tempDb))
        {
            try { File.Delete(_tempDb); } catch { }
        }
    }
}
