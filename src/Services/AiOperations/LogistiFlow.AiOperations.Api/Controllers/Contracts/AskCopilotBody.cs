namespace LogistiFlow.AiOperations.Api.Controllers.Contracts;

/// <summary>A question for the copilot. Send the <c>conversationId</c> of a previous answer to continue it.</summary>
public sealed record AskCopilotBody(string Question, Guid? ConversationId);
