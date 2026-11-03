namespace LogistiFlow.AiOperations.Application.UseCases.Interactions.List.Contracts;

/// <summary>One audited question.</summary>
public sealed record CopilotInteractionSummary(
    Guid Id,
    Guid ConversationId,
    string UserEmail,
    string UserRole,
    string Question,
    string Outcome,
    string? FailureReason,
    string Provider,
    string Model,
    bool Simulated,
    IReadOnlyList<string> Tools,
    long? TotalTokens,
    decimal EstimatedCostUsd,
    long DurationMs,
    DateTimeOffset CreatedAt);
