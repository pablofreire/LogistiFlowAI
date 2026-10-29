namespace LogistiFlow.Inventory.Domain.ReadModels;

/// <summary>A warehouse that, at read time, holds enough available stock for every requested SKU.</summary>
public sealed record WarehouseStockCandidate(string WarehouseCode, IReadOnlyList<CandidateStockPosition> Positions);
