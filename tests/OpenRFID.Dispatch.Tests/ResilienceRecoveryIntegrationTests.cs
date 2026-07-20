using System.Net;
using OpenRFID.Core.Dispatch.Http;
using OpenRFID.Core.Storage;
using Xunit;

namespace OpenRFID.Dispatch.Tests;

public class ResilienceRecoveryIntegrationTests
{
    private class FlakyHttpMessageHandler : HttpMessageHandler
    {
        public bool IsServerOnline { get; set; } = false;
        public List<string> ReceivedPayloads { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!IsServerOnline)
            {
                throw new HttpRequestException("Server connection failed / network unreachable.");
            }

            if (request.Content != null)
            {
                string content = request.Content.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
                ReceivedPayloads.Add(content);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"status\":\"success\"}")
            });
        }
    }

    [Fact]
    public async Task NetworkOutage_BuffersToSqlite_AndReplaysOnRecoveryZeroTagLoss()
    {
        string tempDb = Path.Combine(Path.GetTempPath(), $"resilience_test_{Guid.NewGuid():N}.db");
        var flakyHandler = new FlakyHttpMessageHandler();
        using var client = new HttpClient(flakyHandler);
        var dispatcher = new HttpDispatcher(client);

        try
        {
            using var queue = new SqliteOfflineQueue(tempDb);

            // Step 1: Simulate network outage - dispatch attempts fail and spill over to SQLite queue
            flakyHandler.IsServerOnline = false;

            for (int i = 1; i <= 5; i++)
            {
                string payload = $"{{\"batch\":{i}}}";
                string txId = $"tx-batch-{i}";

                var result = await dispatcher.DispatchAsync("http://api.company.com/tags", "POST", payload, transactionId: txId);
                Assert.False(result.IsSuccess);

                // Spills to SQLite WAL Queue
                await queue.EnqueueAsync(txId, "http://api.company.com/tags", "POST", payload, tagCount: 1);
            }

            Assert.Equal(5, await queue.GetQueueCountAsync());
            Assert.Empty(flakyHandler.ReceivedPayloads);

            // Step 2: Simulate Network Recovery!
            flakyHandler.IsServerOnline = true;

            await using var replayController = new ReplayController(queue, dispatcher);
            long replayedCount = await replayController.DrainQueueOnceAsync();

            // Step 3: Verify zero tag/batch loss!
            Assert.Equal(5, replayedCount);
            Assert.Equal(0, await queue.GetQueueCountAsync());
            Assert.Equal(5, flakyHandler.ReceivedPayloads.Count);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(tempDb)) File.Delete(tempDb);
        }
    }
}
