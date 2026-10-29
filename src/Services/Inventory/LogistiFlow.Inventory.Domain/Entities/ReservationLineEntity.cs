namespace LogistiFlow.Inventory.Domain.Entities;

/// <summary>Quantity held on one stock position for one order line.</summary>
public sealed record ReservationLineEntity(long StockPositionId, string Sku, int Quantity);
