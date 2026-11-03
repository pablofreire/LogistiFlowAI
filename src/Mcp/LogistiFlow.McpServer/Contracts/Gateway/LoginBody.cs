namespace LogistiFlow.McpServer.Contracts.Gateway;

/// <summary>Body of <c>POST /api/auth/login</c>.</summary>
internal sealed record LoginBody(string Email, string Password);
