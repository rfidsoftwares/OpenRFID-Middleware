using System.Net;
using Microsoft.AspNetCore.Http;
using OpenRFID.Core.Engine.Configuration;
using OpenRFID.Management.Api.Middleware;

namespace OpenRFID.Management.Tests;

public class ApiKeyAuthMiddlewareTests : IDisposable
{
    private readonly string _tempConfigFile = Path.Combine(Path.GetTempPath(), $"openrfid_auth_test_{Guid.NewGuid()}.json");

    [Fact]
    public async Task InvokeAsync_SecurityDisabled_AllowsAllRequests()
    {
        var configService = new ConfigurationService(_tempConfigFile);
        var middleware = new ApiKeyAuthMiddleware(next: (innerContext) =>
        {
            innerContext.Response.StatusCode = 200;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/status";

        await middleware.InvokeAsync(context, configService);

        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_SecurityEnabled_BlocksWithoutApiKey_AndAllowsWithApiKey()
    {
        var configService = new ConfigurationService(_tempConfigFile);
        var config = configService.Current;
        var updatedConfig = config with
        {
            Security = new SecurityConfig
            {
                Enabled = true,
                ApiKey = "secret-key-999"
            }
        };
        configService.SaveConfig(updatedConfig);

        var middleware = new ApiKeyAuthMiddleware(next: (innerContext) =>
        {
            innerContext.Response.StatusCode = 200;
            return Task.CompletedTask;
        });

        // 1. Request without API Key -> 401 Unauthorized
        var unauthContext = new DefaultHttpContext();
        unauthContext.Request.Path = "/api/v1/status";
        await middleware.InvokeAsync(unauthContext, configService);
        Assert.Equal(401, unauthContext.Response.StatusCode);

        // 2. Request with valid API Key header -> 200 OK
        var authContext = new DefaultHttpContext();
        authContext.Request.Path = "/api/v1/status";
        authContext.Request.Headers["X-API-Key"] = "secret-key-999";
        await middleware.InvokeAsync(authContext, configService);
        Assert.Equal(200, authContext.Response.StatusCode);
    }

    public void Dispose()
    {
        if (File.Exists(_tempConfigFile))
        {
            File.Delete(_tempConfigFile);
        }
    }
}
