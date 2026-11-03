namespace LogistiFlow.AiOperations.Application.Tools.Results;

/// <summary>A suggested next step for one problem. Suggestions only: the copilot executes nothing.</summary>
public sealed record RecoveryAction(
    string OrderNumber,
    string Problem,
    string Action,
    string Rationale,
    string WhereInConsole);
