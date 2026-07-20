using Microsoft.AspNetCore.Mvc;
using OpenRFID.Core.Engine.Configuration;

namespace OpenRFID.Management.Api.Controllers;

[ApiController]
[Route("api/v1/config")]
[Produces("application/json")]
public class ConfigController : ControllerBase
{
    private readonly ConfigurationService _configService;

    public ConfigController(ConfigurationService configService)
    {
        _configService = configService;
    }

    /// <summary>
    /// Fetches the currently active middleware configuration.
    /// </summary>
    [HttpGet]
    public ActionResult<OpenRFIDConfig> GetConfig()
    {
        return Ok(_configService.Current);
    }

    /// <summary>
    /// Updates and saves the active middleware configuration.
    /// </summary>
    [HttpPost]
    public IActionResult UpdateConfig([FromBody] OpenRFIDConfig config)
    {
        try
        {
            _configService.SaveConfig(config);
            return Ok(new { message = "Configuration updated and applied successfully.", config });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Failed to save configuration: {ex.Message}" });
        }
    }

    /// <summary>
    /// Triggers a manual hot-reload of the configuration file from disk.
    /// </summary>
    [HttpPost("reload")]
    public IActionResult ReloadConfig()
    {
        try
        {
            var config = _configService.LoadOrCreateDefault();
            return Ok(new { message = "Configuration hot-reloaded successfully from disk.", config });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Failed to reload configuration: {ex.Message}" });
        }
    }
}
