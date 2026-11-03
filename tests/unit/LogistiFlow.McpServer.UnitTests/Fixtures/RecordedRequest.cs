namespace LogistiFlow.McpServer.UnitTests.Fixtures;

/// <summary>A request the fake gateway received.</summary>
public sealed record RecordedRequest(string Method, string Path, string Query, string? Authorization, string? Body);
