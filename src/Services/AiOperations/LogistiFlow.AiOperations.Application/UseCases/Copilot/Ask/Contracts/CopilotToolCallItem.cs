namespace LogistiFlow.AiOperations.Application.UseCases.Copilot.Ask.Contracts;

/// <summary>A tool the copilot ran for this answer, in order.</summary>
public sealed record CopilotToolCallItem(
    int Sequence,
    string Name,
    string Arguments,
    string Outcome,
    string Summary,
    long DurationMs);
