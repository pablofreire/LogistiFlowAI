namespace LogistiFlow.AiOperations.Application.UseCases.Interactions.List.Contracts;

/// <summary>A page of the audit trail, newest first.</summary>
public sealed record ListCopilotInteractionsRequest(int? Page, int? PageSize);
