namespace LogistiFlow.AiOperations.Domain.Models;

/// <summary>Which model answered. <c>Simulated</c> is true for the deterministic demo provider, never for a model.</summary>
public sealed record CopilotProvider(string Name, string Model, bool Simulated);
