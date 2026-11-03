namespace LogistiFlow.AiOperations.Domain.Models;

/// <summary>
/// Tokens reported by the provider for one question (all model calls of the tool loop added up) and the cost
/// estimated from the configured prices. Null token counts mean the provider reported none.
/// </summary>
public sealed record CopilotUsage(long? InputTokens, long? OutputTokens, long? TotalTokens, decimal EstimatedCostUsd)
{
    public static readonly CopilotUsage None = new(null, null, null, 0m);
}
