using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using OpenRFID.Core.Engine.Configuration;

namespace OpenRFID.Management.Tests;

public class ManagementApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ManagementApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetStatus_Returns200OK_WithHealthPayload()
    {
        var response = await _client.GetAsync("/api/v1/status");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("isRunning", content);
        Assert.Contains("totalRawTagsCount", content);
    }

    [Fact]
    public async Task GetConfig_Returns200OK_WithConfigPayload()
    {
        var response = await _client.GetAsync("/api/v1/config");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var config = await response.Content.ReadFromJsonAsync<OpenRFIDConfig>();
        Assert.NotNull(config);
        Assert.NotNull(config.Filter);
    }

    [Fact]
    public async Task PostTemplatePreview_ReturnsRenderedPayload()
    {
        var request = new
        {
            format = "json"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/templates/preview", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("rendered", content);
        Assert.Contains("E28011912000000000001234", content);
    }

    [Fact]
    public async Task PostTestRegex_ReturnsMatchResults()
    {
        var request = new
        {
            pattern = "^E280.*",
            testEpcs = new[] { "E2801234", "ABCD5678" }
        };

        var response = await _client.PostAsJsonAsync("/api/v1/filters/test-regex", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"isMatch\":true", content);
        Assert.Contains("\"isMatch\":false", content);
    }

    [Fact]
    public async Task PostSimulateTag_InjectsTagSuccessfully()
    {
        var response = await _client.PostAsync("/api/v1/simulate/tag", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
