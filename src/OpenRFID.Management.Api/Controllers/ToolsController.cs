using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using OpenRFID.Core.Abstractions;
using OpenRFID.Core.Dispatch.Templating;
using OpenRFID.Core.Engine.Orchestration;

namespace OpenRFID.Management.Api.Controllers;

public sealed record TemplatePreviewRequest
{
    public required string Format { get; init; } // json, form, xml, csv
    public string? CustomTemplate { get; init; }
    public TagReadEvent? SampleTag { get; init; }
}

public sealed record RegexTestRequest
{
    public required string Pattern { get; init; }
    public required List<string> TestEpcs { get; init; }
}

public sealed record RegexTestItemResult
{
    public required string Epc { get; init; }
    public required bool IsMatch { get; init; }
}

[ApiController]
[Route("api/v1")]
[Produces("application/json")]
public class ToolsController : ControllerBase
{
    private readonly PayloadTemplateEngine _templateEngine = new();
    private readonly MiddlewareOrchestrator _orchestrator;

    public ToolsController(MiddlewareOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    /// <summary>
    /// Renders a sample payload template for interactive UI testing.
    /// </summary>
    [HttpPost("templates/preview")]
    public IActionResult PreviewTemplate([FromBody] TemplatePreviewRequest request)
    {
        try
        {
            var tag = request.SampleTag ?? new TagReadEvent
            {
                EPC = "E28011912000000000001234",
                TID = "E20034120123456789",
                UserMemory = "0000",
                RSSI = -55.5f,
                AntennaPort = 1,
                ReadCount = 42,
                FirstSeenTime = DateTimeOffset.UtcNow,
                LastSeenTime = DateTimeOffset.UtcNow,
                ReaderId = "Simulator-01",
                ExtraMetadata = new Dictionary<string, string> { { "Zone", "Gate-A" } }
            };

            var dummyConfig = new ReaderConfig
            {
                ReaderId = tag.ReaderId,
                ProviderId = "Simulator",
                BrandName = "OpenRFID Virtual Simulator"
            };

            var templateSrc = !string.IsNullOrWhiteSpace(request.CustomTemplate)
                ? request.CustomTemplate
                : PayloadTemplateEngine.DefaultJsonArrayTemplate;

            var rendered = _templateEngine.Render(templateSrc, new[] { tag }, dummyConfig);
            return Ok(new { rendered, format = request.Format });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Template rendering failed: {ex.Message}" });
        }
    }

    /// <summary>
    /// Tests an EPC Regex pattern against a list of sample EPCs.
    /// </summary>
    [HttpPost("filters/test-regex")]
    public IActionResult TestRegex([FromBody] RegexTestRequest request)
    {
        try
        {
            var regex = new Regex(request.Pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(500));
            var results = request.TestEpcs.Select(epc => new RegexTestItemResult
            {
                Epc = epc,
                IsMatch = regex.IsMatch(epc)
            }).ToList();

            return Ok(new { pattern = request.Pattern, results });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Invalid regular expression pattern: {ex.Message}" });
        }
    }

    /// <summary>
    /// Injects a simulated tag read into the orchestrator pipeline for UI live streaming tests.
    /// </summary>
    [HttpPost("simulate/tag")]
    public IActionResult SimulateTag([FromBody] TagReadEvent? tag)
    {
        var sample = tag ?? new TagReadEvent
        {
            EPC = $"E280{Random.Shared.Next(10000000, 99999999)}",
            TID = $"TID{Random.Shared.Next(10000, 99999)}",
            RSSI = -40f - Random.Shared.NextSingle() * 40f,
            AntennaPort = Random.Shared.Next(1, 5),
            ReadCount = 1,
            FirstSeenTime = DateTimeOffset.UtcNow,
            LastSeenTime = DateTimeOffset.UtcNow,
            ReaderId = "Simulator-01"
        };

        _orchestrator.InjectRawTag(sample);
        return Ok(new { message = "Simulated tag injected into orchestrator stream.", tag = sample });
    }
}
