namespace LogistiFlow.McpServer.Contracts.Gateway;

/// <summary>A <c>PagedResult</c> as the APIs serialise it.</summary>
internal sealed record GatewayPage<T>(IReadOnlyList<T> Items, long TotalCount);
