namespace LogistiFlow.Inventory.Domain.ReadModels;

public sealed record CandidateStockPosition(long StockPositionId, string Sku, int Available);
