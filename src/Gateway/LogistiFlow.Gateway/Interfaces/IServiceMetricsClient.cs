using System.Text.Json;

namespace LogistiFlow.Gateway.Interfaces;

/// <summary>Reads one service's metrics on behalf of the caller (the caller's token is forwarded).</summary>
public interface IServiceMetricsClient
{
    /// <summary>Returns the <c>data</c> of the service's envelope, or <c>null</c> when the service is unavailable.</summary>
    Task<JsonElement?> GetAsync(string service, string path, string? authorization, string correlationId, CancellationToken cancellationToken);
}
