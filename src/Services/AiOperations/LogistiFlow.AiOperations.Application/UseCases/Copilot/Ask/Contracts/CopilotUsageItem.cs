namespace LogistiFlow.AiOperations.Application.UseCases.Copilot.Ask.Contracts;

/// <summary>Tokens and estimated cost of the answer. The demo provider reports none.</summary>
public sealed record CopilotUsageItem(
    long? InputTokens,
    long? OutputTokens,
    long? TotalTokens,
    decimal EstimatedCostUsd);
