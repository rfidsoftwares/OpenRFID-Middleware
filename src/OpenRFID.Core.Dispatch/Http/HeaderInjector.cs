namespace OpenRFID.Core.Dispatch.Http;

/// <summary>
/// Dynamic HTTP request header processor.
/// </summary>
public static class HeaderInjector
{
    public static Dictionary<string, string> ProcessHeaders(
        IDictionary<string, string>? headers,
        string transactionId)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (headers != null)
        {
            foreach (var kvp in headers)
            {
                string value = kvp.Value
                    .Replace("{{ timestamp_utc }}", DateTimeOffset.UtcNow.ToString("o"), StringComparison.OrdinalIgnoreCase)
                    .Replace("{{ transaction_id }}", transactionId, StringComparison.OrdinalIgnoreCase);

                result[kvp.Key] = value;
            }
        }

        if (!result.ContainsKey("X-Idempotency-Key"))
        {
            result["X-Idempotency-Key"] = transactionId;
        }

        return result;
    }
}
