namespace LogistiFlow.AiOperations.Domain.Models;

/// <summary>An earlier answered question of a conversation, replayed as context for the next one.</summary>
public sealed record CopilotTurn(string Question, string Answer);
