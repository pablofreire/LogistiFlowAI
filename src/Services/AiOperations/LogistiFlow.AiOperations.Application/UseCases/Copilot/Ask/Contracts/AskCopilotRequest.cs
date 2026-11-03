namespace LogistiFlow.AiOperations.Application.UseCases.Copilot.Ask.Contracts;

/// <summary>A question from a signed-in user. <c>ConversationId</c> continues an earlier conversation.</summary>
public sealed record AskCopilotRequest(
    string Question,
    Guid? ConversationId,
    string UserEmail,
    string UserRole);
