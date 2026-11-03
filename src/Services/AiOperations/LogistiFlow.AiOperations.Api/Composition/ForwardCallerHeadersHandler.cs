using System.Net.Http.Headers;
using LogistiFlow.BuildingBlocks.Web.Middleware;

namespace LogistiFlow.AiOperations.Api.Composition;

/// <summary>
/// Sends the caller's bearer token and correlation id with every tool call, so each service authorises the user
/// itself and the calls join the request's trace. The token is only forwarded, never stored or logged.
/// </summary>
public sealed class ForwardCallerHeadersHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var incoming = httpContextAccessor.HttpContext?.Request.Headers;
        if (incoming is not null)
        {
            if (AuthenticationHeaderValue.TryParse(incoming.Authorization.ToString(), out var authorization))
            {
                request.Headers.Authorization = authorization;
            }

            var correlationId = incoming[CorrelationIdMiddleware.HeaderName].ToString();
            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                request.Headers.TryAddWithoutValidation(CorrelationIdMiddleware.HeaderName, correlationId);
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}
