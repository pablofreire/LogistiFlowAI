namespace LogistiFlow.Inventory.Application.UseCases.Availability.GetBySku.Contracts;

public sealed record GetSkuAvailabilityResponse(string Sku, string ProductName, int TotalAvailable, IReadOnlyList<WarehouseAvailability> Warehouses);
