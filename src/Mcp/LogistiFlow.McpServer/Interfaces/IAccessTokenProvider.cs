namespace LogistiFlow.McpServer.Interfaces;

/// <summary>Supplies the bearer token the server sends to the gateway.</summary>
public interface IAccessTokenProvider
{
    /// <exception cref="Exceptions.GatewaySignInException">The gateway did not accept the configured credentials.</exception>
    ValueTask<string> GetTokenAsync(CancellationToken cancellationToken);

    /// <summary>Forgets a cached token the gateway has rejected, so the next call signs in again.</summary>
    void Invalidate();
}
