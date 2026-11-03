namespace LogistiFlow.AiOperations.Application.Models.Services;

/// <summary>Availability of a SKU across warehouses, from the Inventory service.</summary>
public sealed record SkuAvailabilityView(
    string Sku,
    string ProductName,
    int TotalAvailable,
    IReadOnlyList<WarehouseStockView> Warehouses);
