using Microsoft.AspNetCore.Mvc;
using OpenRFID.Core.Engine.Orchestration;

namespace OpenRFID.Management.Api.Controllers;

[ApiController]
[Route("api/v1/status")]
[Produces("application/json")]
public class StatusController : ControllerBase
{
    private readonly MiddlewareOrchestrator _orchestrator;

    public StatusController(MiddlewareOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    /// <summary>
    /// Gets overall system health status, connected readers, and queue metrics asynchronously.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<SystemHealthStatus>> GetStatus()
    {
        var status = await _orchestrator.GetHealthStatusAsync();
        return Ok(status);
    }
}
