namespace LogistiFlow.Inventory.Application.UseCases.Stock.List.Contracts;

/// <summary>Paged stock positions, optionally filtered by warehouse, SKU, or low availability.</summary>
public sealed record ListStockRequest(int? Page, int? PageSize, string? WarehouseCode, string? Sku, bool LowStockOnly);
