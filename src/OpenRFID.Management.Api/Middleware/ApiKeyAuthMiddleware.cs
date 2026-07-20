using System.Text.Json;
using OpenRFID.Core.Engine.Configuration;

namespace OpenRFID.Management.Api.Middleware;

/// <summary>
/// Configurable API Key Authentication Middleware for OpenRFID Management API endpoints.
/// Security can be dynamically enabled or disabled via settings (`Security.Enabled`).
/// </summary>
public sealed class ApiKeyAuthMiddleware
{
    private const string ApiKeyHeaderName = "X-API-Key";
    private const string ApiKeyQueryParam = "apiKey";
    private readonly RequestDelegate _next;

    public ApiKeyAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ConfigurationService configService)
    {
        var config = configService.Current;

        // If security is disabled, pass request through
        if (!config.Security.Enabled)
        {
            await _next(context);
            return;
        }

        string path = context.Request.Path.Value ?? string.Empty;

        // Bypass security for static Web Dashboard UI files and Swagger documentation
        if (IsPublicPath(path))
        {
            await _next(context);
            return;
        }

        // Extract API Key from Header or Query String (for WebSockets)
        string? extractedKey = null;

        if (context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var headerValue))
        {
            extractedKey = headerValue.FirstOrDefault();
        }
        else if (context.Request.Query.TryGetValue(ApiKeyQueryParam, out var queryValue))
        {
            extractedKey = queryValue.FirstOrDefault();
        }

        if (string.IsNullOrWhiteSpace(extractedKey) || !string.Equals(extractedKey, config.Security.ApiKey, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            var responsePayload = new { error = "Unauthorized. Missing or invalid X-API-Key header or apiKey parameter." };
            await context.Response.WriteAsync(JsonSerializer.Serialize(responsePayload));
            return;
        }

        await _next(context);
    }

    private static bool IsPublicPath(string path)
    {
        if (string.IsNullOrEmpty(path) || path == "/") return true;

        string lowerPath = path.ToLowerInvariant();
        return lowerPath.StartsWith("/swagger") ||
               lowerPath == "/index.html" ||
               lowerPath == "/app.css" ||
               lowerPath == "/app.js" ||
               lowerPath == "/favicon.ico";
    }
}
