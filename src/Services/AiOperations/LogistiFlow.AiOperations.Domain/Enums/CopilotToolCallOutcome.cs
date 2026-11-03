namespace LogistiFlow.AiOperations.Domain.Enums;

/// <summary>What a tool call returned. Anything but <c>Ok</c> means the answer cannot rely on that tool's data.</summary>
public enum CopilotToolCallOutcome
{
    Ok = 0,
    NotFound = 1,
    Forbidden = 2,
    Invalid = 3,
    Unavailable = 4,
}
