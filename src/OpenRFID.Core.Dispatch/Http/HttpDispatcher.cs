using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;

namespace OpenRFID.Core.Dispatch.Http;

/// <summary>
/// HTTP Method Adapter supporting POST, PUT, PATCH, and GET dispatch.
/// </summary>
public sealed class HttpDispatcher
{
    private readonly HttpClient _httpClient;

    public HttpDispatcher(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<DispatchResult> DispatchAsync(
        string targetUrl,
        string httpMethod,
        string payload,
        IDictionary<string, string>? headers = null,
        string? transactionId = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(targetUrl);
        ArgumentNullException.ThrowIfNull(httpMethod);

        string txId = transactionId ?? Guid.NewGuid().ToString("D");
        var processedHeaders = HeaderInjector.ProcessHeaders(headers, txId);
        var sw = Stopwatch.StartNew();

        try
        {
            HttpMethod method = new HttpMethod(httpMethod.ToUpperInvariant());
            Uri baseUri = new Uri(targetUrl);
            HttpRequestMessage request;

            if (method == HttpMethod.Get)
            {
                var uriBuilder = new UriBuilder(baseUri);
                string queryString = string.IsNullOrWhiteSpace(payload) ? "" : payload.TrimStart('?');
                uriBuilder.Query = string.IsNullOrEmpty(uriBuilder.Query)
                    ? queryString
                    : $"{uriBuilder.Query.TrimStart('?')}&{queryString}";

                request = new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri);
            }
            else
            {
                string contentType = processedHeaders.TryGetValue("Content-Type", out var ctHeader)
                    ? ctHeader
                    : "application/json";

                request = new HttpRequestMessage(method, baseUri)
                {
                    Content = new StringContent(payload ?? "", Encoding.UTF8, contentType)
                };
            }

            foreach (var kvp in processedHeaders)
            {
                if (string.Equals(kvp.Key, "Content-Type", StringComparison.OrdinalIgnoreCase)) continue;
                request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
            }

            using var response = await _httpClient.SendAsync(request, ct);
            sw.Stop();

            string responseBody = await response.Content.ReadAsStringAsync(ct);

            return new DispatchResult
            {
                IsSuccess = response.IsSuccessStatusCode,
                HttpStatusCode = (int)response.StatusCode,
                TargetUrl = targetUrl,
                ResponseContent = responseBody,
                ErrorMessage = response.IsSuccessStatusCode ? null : $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}",
                Elapsed = sw.Elapsed,
                TransactionId = txId
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new DispatchResult
            {
                IsSuccess = false,
                HttpStatusCode = 0,
                TargetUrl = targetUrl,
                ErrorMessage = ex.Message,
                Elapsed = sw.Elapsed,
                TransactionId = txId
            };
        }
    }
}
