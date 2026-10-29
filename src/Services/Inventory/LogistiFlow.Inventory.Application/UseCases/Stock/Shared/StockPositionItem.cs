namespace LogistiFlow.Inventory.Application.UseCases.Stock.Shared;

public sealed record StockPositionItem(
    Guid Id,
    string Sku,
    string ProductName,
    string WarehouseCode,
    int OnHand,
    int Reserved,
    int Available,
    DateTimeOffset UpdatedAt);
