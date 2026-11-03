namespace LogistiFlow.McpServer.Contracts.Gateway;

/// <summary>The <c>ApiResponse</c> envelope every LogistiFlow API answers with.</summary>
internal sealed record GatewayEnvelope<T>(T? Data, GatewayError? Error);
