namespace LogistiFlow.Inventory.Application.UseCases.Warehouses.Shared;

public sealed record WarehouseItem(Guid Id, string Code, string Name, string City, string Country, int OnHand, int Reserved, int Available);
