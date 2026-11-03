namespace LogistiFlow.AiOperations.Application.Tools.Results;

/// <summary>Suggested next steps for one order or for the shipments at risk.</summary>
public sealed record RecoveryActionsData(IReadOnlyList<RecoveryAction> Actions, int ShipmentsConsidered);
