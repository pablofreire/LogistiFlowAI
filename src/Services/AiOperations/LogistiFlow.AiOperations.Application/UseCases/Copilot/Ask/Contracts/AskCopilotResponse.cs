namespace LogistiFlow.AiOperations.Application.UseCases.Copilot.Ask.Contracts;

/// <summary>The answer and how it was produced. <c>Simulated</c> is true for the deterministic demo provider.</summary>
public sealed record AskCopilotResponse(
    Guid InteractionId,
    Guid ConversationId,
    string Answer,
    string Provider,
    string Model,
    bool Simulated,
    IReadOnlyList<CopilotToolCallItem> ToolCalls,
    CopilotUsageItem Usage,
    long DurationMs,
    DateTimeOffset AnsweredAt);
