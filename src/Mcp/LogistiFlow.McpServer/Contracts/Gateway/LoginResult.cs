namespace LogistiFlow.McpServer.Contracts.Gateway;

/// <summary>The part of the login response the server needs: the token and when it expires.</summary>
internal sealed record LoginResult(string AccessToken, DateTimeOffset ExpiresAt);
