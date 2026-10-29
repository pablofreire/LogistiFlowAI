using LogistiFlow.Inventory.Domain.Enums;

namespace LogistiFlow.Inventory.Application.UseCases.Metrics.GetSummary.Contracts;

public sealed record RecentStockMovement(string Sku, string WarehouseCode, StockMovementType Type, int Quantity, string ReferenceType, string ReferenceId, DateTimeOffset OccurredAt);
