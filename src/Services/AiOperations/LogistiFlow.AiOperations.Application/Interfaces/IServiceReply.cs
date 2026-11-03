using LogistiFlow.AiOperations.Domain.Enums;

namespace LogistiFlow.AiOperations.Application.Interfaces;

/// <summary>The outcome of a service call, whatever its data type.</summary>
public interface IServiceReply
{
    CopilotToolCallOutcome Outcome { get; }

    string? Message { get; }
}
