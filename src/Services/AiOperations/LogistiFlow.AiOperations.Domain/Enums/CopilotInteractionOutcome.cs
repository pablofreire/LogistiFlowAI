namespace LogistiFlow.AiOperations.Domain.Enums;

/// <summary>Whether the copilot produced an answer. A failed interaction is still audited.</summary>
public enum CopilotInteractionOutcome
{
    Answered = 0,
    Failed = 1,
}
