namespace LogistiFlow.AiOperations.Application.Tools.Results;

/// <summary>Shipments at risk, out of the undelivered shipments scanned.</summary>
public sealed record AtRiskShipmentsData(IReadOnlyList<AtRiskShipment> Shipments, int Scanned, bool Truncated);
