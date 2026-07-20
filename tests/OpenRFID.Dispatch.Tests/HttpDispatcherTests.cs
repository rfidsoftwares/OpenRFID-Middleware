using System.Net;
using OpenRFID.Core.Dispatch.Http;
using Xunit;

namespace OpenRFID.Dispatch.Tests;

public class HttpDispatcherTests
{
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastContent { get; private set; }
        public HttpStatusCode StatusCodeToReturn { get; set; } = HttpStatusCode.OK;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content != null)
            {
                LastContent = await request.Content.ReadAsStringAsync(cancellationToken);
            }
            return new HttpResponseMessage(StatusCodeToReturn)
            {
                Content = new StringContent("{\"status\":\"success\"}")
            };
        }
    }

    [Fact]
    public async Task HttpDispatcher_PostMethod_SendsPayloadAndHeaders()
    {
        var mockHandler = new MockHttpMessageHandler();
        using var client = new HttpClient(mockHandler);
        var dispatcher = new HttpDispatcher(client);

        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer token-123",
            ["X-Org-Key"] = "KEY-999"
        };

        var result = await dispatcher.DispatchAsync(
            "http://localhost:8080/ingest",
            "POST",
            "{\"data\":\"test\"}",
            headers,
            "tx-uuid-101");

        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.HttpStatusCode);
        Assert.NotNull(mockHandler.LastRequest);
        Assert.Equal(HttpMethod.Post, mockHandler.LastRequest.Method);
        Assert.Equal("Bearer token-123", mockHandler.LastRequest.Headers.Authorization?.ToString());
        Assert.Equal("tx-uuid-101", mockHandler.LastRequest.Headers.GetValues("X-Idempotency-Key").First());
        Assert.Equal("{\"data\":\"test\"}", mockHandler.LastContent);
    }

    [Fact]
    public async Task HttpDispatcher_GetMethod_AppendsQueryStringToUrl()
    {
        var mockHandler = new MockHttpMessageHandler();
        using var client = new HttpClient(mockHandler);
        var dispatcher = new HttpDispatcher(client);

        var result = await dispatcher.DispatchAsync(
            "http://localhost:8080/ingest",
            "GET",
            "epc=E2801111&rssi=-50",
            null,
            "tx-uuid-102");

        Assert.True(result.IsSuccess);
        Assert.NotNull(mockHandler.LastRequest);
        Assert.Equal(HttpMethod.Get, mockHandler.LastRequest.Method);
        Assert.Equal("http://localhost:8080/ingest?epc=E2801111&rssi=-50", mockHandler.LastRequest.RequestUri?.ToString());
    }
}
