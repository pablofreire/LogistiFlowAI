namespace LogistiFlow.Inventory.Application.UseCases.Availability.GetBySku.Contracts;

public sealed record WarehouseAvailability(string WarehouseCode, int OnHand, int Reserved, int Available);
