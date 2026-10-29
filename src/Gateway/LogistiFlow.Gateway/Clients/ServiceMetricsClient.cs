using System.Net.Http.Headers;
using System.Text.Json;
using LogistiFlow.BuildingBlocks.Web.Middleware;
using LogistiFlow.Gateway.Interfaces;

namespace LogistiFlow.Gateway.Clients;

/// <summary>
/// Calls a service through a named <see cref="HttpClient"/> that carries the standard resilience pipeline
/// (timeout, retry with jittered backoff, circuit breaker). A failing service degrades the dashboard instead of
/// failing it.
/// </summary>
public sealed partial class ServiceMetricsClient(IHttpClientFactory httpClientFactory, ILogger<ServiceMetricsClient> logger) : IServiceMetricsClient
{
    public async Task<JsonElement?> GetAsync(string service, string path, string? authorization, string correlationId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (!string.IsNullOrWhiteSpace(authorization) && AuthenticationHeaderValue.TryParse(authorization, out var header))
        {
            request.Headers.Authorization = header;
        }

        request.Headers.TryAddWithoutValidation(CorrelationIdMiddleware.HeaderName, correlationId);

        try
        {
            using var response = await httpClientFactory.CreateClient(service).SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                LogUnavailable(logger, service, (int)response.StatusCode);
                return null;
            }

            using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            return document.RootElement.GetProperty("data").Clone();
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException or Polly.CircuitBreaker.BrokenCircuitException or Polly.Timeout.TimeoutRejectedException)
        {
            LogFailed(logger, exception, service);
            return null;
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Metrics from {Service} unavailable (HTTP {StatusCode})")]
    private static partial void LogUnavailable(ILogger logger, string service, int statusCode);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Metrics from {Service} failed")]
    private static partial void LogFailed(ILogger logger, Exception exception, string service);
}
