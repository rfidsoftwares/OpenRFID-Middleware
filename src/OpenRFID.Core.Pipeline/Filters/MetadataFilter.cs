using System.Text.RegularExpressions;
using OpenRFID.Core.Abstractions;

namespace OpenRFID.Core.Pipeline.Filters;

/// <summary>
/// Filter that evaluates tag hardware metadata (RSSI, antenna port mask) and EPC pattern matching (prefix, suffix, length, regex).
/// Protected against ReDoS via a strict 500ms matching timeout.
/// </summary>
public sealed class MetadataFilter : ITagFilter
{
    private readonly Regex? _compiledRegex;

    public string Name => "MetadataFilter";

    public float? MinRssiDbm { get; }
    public ushort? AntennaMask { get; }
    public string? EpcPrefix { get; }
    public string? EpcSuffix { get; }
    public int? EpcMinLength { get; }
    public int? EpcMaxLength { get; }
    public string? RegexPattern { get; }

    public MetadataFilter(
        float? minRssiDbm = null,
        ushort? antennaMask = null,
        string? epcPrefix = null,
        string? epcSuffix = null,
        int? epcMinLength = null,
        int? epcMaxLength = null,
        string? regexPattern = null)
    {
        MinRssiDbm = minRssiDbm;
        AntennaMask = antennaMask;
        EpcPrefix = epcPrefix;
        EpcSuffix = epcSuffix;
        EpcMinLength = epcMinLength;
        EpcMaxLength = epcMaxLength;
        RegexPattern = regexPattern;

        if (!string.IsNullOrWhiteSpace(regexPattern))
        {
            _compiledRegex = new Regex(regexPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500));
        }
    }

    public FilterResult Evaluate(TagReadEvent tag, DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(tag);

        if (MinRssiDbm.HasValue && tag.RSSI < MinRssiDbm.Value)
        {
            return FilterResult.Drop($"RSSI {tag.RSSI:F1} dBm is below threshold {MinRssiDbm.Value:F1} dBm.");
        }

        if (AntennaMask.HasValue)
        {
            int bitPosition = tag.AntennaPort - 1;
            if (bitPosition < 0 || bitPosition >= 16 || (AntennaMask.Value & (1 << bitPosition)) == 0)
            {
                return FilterResult.Drop($"Antenna port {tag.AntennaPort} disabled by bitmask 0x{AntennaMask.Value:X}.");
            }
        }

        if (EpcMinLength.HasValue && tag.EPC.Length < EpcMinLength.Value)
        {
            return FilterResult.Drop($"EPC length {tag.EPC.Length} is below minimum {EpcMinLength.Value}.");
        }

        if (EpcMaxLength.HasValue && tag.EPC.Length > EpcMaxLength.Value)
        {
            return FilterResult.Drop($"EPC length {tag.EPC.Length} exceeds maximum {EpcMaxLength.Value}.");
        }

        if (!string.IsNullOrEmpty(EpcPrefix) && !tag.EPC.StartsWith(EpcPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return FilterResult.Drop($"EPC '{tag.EPC}' does not start with required prefix '{EpcPrefix}'.");
        }

        if (!string.IsNullOrEmpty(EpcSuffix) && !tag.EPC.EndsWith(EpcSuffix, StringComparison.OrdinalIgnoreCase))
        {
            return FilterResult.Drop($"EPC '{tag.EPC}' does not end with required suffix '{EpcSuffix}'.");
        }

        if (_compiledRegex != null)
        {
            try
            {
                if (!_compiledRegex.IsMatch(tag.EPC))
                {
                    return FilterResult.Drop($"EPC '{tag.EPC}' does not match regex pattern '{RegexPattern}'.");
                }
            }
            catch (RegexMatchTimeoutException)
            {
                return FilterResult.Drop($"EPC '{tag.EPC}' regex evaluation timed out after 500ms.");
            }
        }

        return FilterResult.Pass();
    }
}
