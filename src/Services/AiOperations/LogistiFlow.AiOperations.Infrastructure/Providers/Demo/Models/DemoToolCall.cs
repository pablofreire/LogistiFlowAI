namespace LogistiFlow.AiOperations.Infrastructure.Providers.Demo.Models;

/// <summary>A tool the demo provider asks the agent framework to run.</summary>
public sealed record DemoToolCall(string Name, IReadOnlyDictionary<string, object?> Arguments);
