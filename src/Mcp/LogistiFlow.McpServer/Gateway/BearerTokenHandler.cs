using System.Net;
using System.Net.Http.Headers;
using LogistiFlow.McpServer.Interfaces;

namespace LogistiFlow.McpServer.Gateway;

/// <summary>Adds the server's bearer token to every gateway call and drops it when the gateway rejects it.</summary>
public sealed class BearerTokenHandler(IAccessTokenProvider tokens) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await tokens.GetTokenAsync(cancellationToken));
        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            tokens.Invalidate();
        }

        return response;
    }
}
