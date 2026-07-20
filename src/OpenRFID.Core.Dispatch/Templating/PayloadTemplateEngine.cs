using Fluid;
using OpenRFID.Core.Abstractions;

namespace OpenRFID.Core.Dispatch.Templating;

/// <summary>
/// Liquid template engine powered by Fluid for transforming TagReadEvent batches into user-configured JSON, Form-Data, CSV, or XML payloads.
/// </summary>
public sealed class PayloadTemplateEngine
{
    private static readonly FluidParser Parser = new();
    private static readonly TemplateOptions Options = new();

    static PayloadTemplateEngine()
    {
        Options.MemberAccessStrategy.Register<TagReadEvent>();
        Options.MemberAccessStrategy.Register<ReaderConfig>();
    }

    public const string DefaultJsonArrayTemplate = @"{
  ""deviceId"": ""{{ config.ReaderId }}"",
  ""location"": ""{{ config.BrandName }}"",
  ""transactionId"": ""{{ transaction_id }}"",
  ""timestamp"": ""{{ current_utc_iso }}"",
  ""tagCount"": {{ tags.size }},
  ""tags"": [
    {% for tag in tags %}
    {
      ""epc"": ""{{ tag.EPC }}"",
      ""tid"": ""{{ tag.TID }}"",
      ""antenna"": {{ tag.AntennaPort }},
      ""rssi"": {{ tag.RSSI }},
      ""readCount"": {{ tag.ReadCount }},
      ""firstSeen"": ""{{ tag.FirstSeenTime | date: '%Y-%m-%dT%H:%M:%SZ' }}""
    }{% unless forloop.last %},{% endunless %}
    {% endfor %}
  ]
}";

    public const string FormUrlEncodedTemplate = "deviceId={{ config.ReaderId }}&transactionId={{ transaction_id }}&epc={{ tags[0].EPC }}&rssi={{ tags[0].RSSI }}";

    public const string CsvTemplate = @"EPC,TID,AntennaPort,RSSI,FirstSeenTime
{% for tag in tags %}{{ tag.EPC }},{{ tag.TID }},{{ tag.AntennaPort }},{{ tag.RSSI }},{{ tag.FirstSeenTime }}
{% endfor %}";

    /// <summary>
    /// Renders a batch of tags using the specified Liquid template string and metadata context.
    /// </summary>
    public string Render(
        string templateSource,
        IReadOnlyList<TagReadEvent> tags,
        ReaderConfig config,
        string? transactionId = null)
    {
        ArgumentNullException.ThrowIfNull(tags);
        ArgumentNullException.ThrowIfNull(config);

        if (string.IsNullOrWhiteSpace(templateSource))
        {
            templateSource = DefaultJsonArrayTemplate;
        }

        if (!Parser.TryParse(templateSource, out IFluidTemplate? template, out string? error))
        {
            throw new FormatException($"Failed to parse Liquid payload template: {error}");
        }

        var context = new TemplateContext(Options);
        context.SetValue("config", config);
        context.SetValue("tags", tags);
        context.SetValue("current_utc_iso", DateTimeOffset.UtcNow.ToString("o"));
        context.SetValue("transaction_id", transactionId ?? Guid.NewGuid().ToString("D"));

        return template.Render(context);
    }
}
