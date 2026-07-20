using OpenRFID.Core.Engine.Configuration;
using OpenRFID.Core.Engine.Orchestration;
using OpenRFID.Management.Api.Middleware;
using OpenRFID.Management.Api.WebSockets;

var builder = WebApplication.CreateBuilder(args);

// Add Services
builder.Services.AddSingleton<ConfigurationService>();
builder.Services.AddSingleton<MiddlewareOrchestrator>();
builder.Services.AddSingleton<TagWebSocketHandler>();
builder.Services.AddSingleton<LogWebSocketHandler>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "OpenRFID Middleware Management API",
        Version = "v1.0.0",
        Description = "Enterprise REST and WebSockets management endpoints for OpenRFID Middleware."
    });
});

var app = builder.Build();

// Enable WebSockets
app.UseWebSockets();

// Enable Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "OpenRFID Management API v1");
});

// Serve Dashboard UI static files
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();

// Register Configurable API Key Authentication Middleware
app.UseMiddleware<ApiKeyAuthMiddleware>();

app.MapControllers();

// WebSocket endpoints
app.Map("/ws/tags", async (HttpContext context, TagWebSocketHandler handler) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await handler.HandleWebSocketAsync(context, webSocket);
    }
    else
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
    }
});

app.Map("/ws/logs", async (HttpContext context, LogWebSocketHandler handler) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await handler.HandleWebSocketAsync(context, webSocket);
    }
    else
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
    }
});

// Start Middleware Orchestrator
var orchestrator = app.Services.GetRequiredService<MiddlewareOrchestrator>();
await orchestrator.StartAsync();

app.Run();

// Partial class for WebApplicationFactory integration testing
public partial class Program { }
