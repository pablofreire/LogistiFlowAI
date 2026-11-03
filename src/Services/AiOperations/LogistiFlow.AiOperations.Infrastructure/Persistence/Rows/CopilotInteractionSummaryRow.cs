namespace LogistiFlow.AiOperations.Infrastructure.Persistence.Rows;

internal sealed class CopilotInteractionSummaryRow
{
    public Guid Id { get; init; }

    public Guid ConversationId { get; init; }

    public string UserEmail { get; init; } = string.Empty;

    public string UserRole { get; init; } = string.Empty;

    public string Question { get; init; } = string.Empty;

    public string Outcome { get; init; } = string.Empty;

    public string? FailureReason { get; init; }

    public string Provider { get; init; } = string.Empty;

    public string Model { get; init; } = string.Empty;

    public bool Simulated { get; init; }

    public long? TotalTokens { get; init; }

    public decimal EstimatedCostUsd { get; init; }

    public long DurationMs { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Tool names in call order, comma-separated.</summary>
    public string Tools { get; init; } = string.Empty;
}
