namespace LogistiFlow.McpServer.Contracts.Views;

/// <summary>Availability of a SKU across warehouses.</summary>
public sealed record SkuAvailabilityView(
    string Sku,
    string ProductName,
    int TotalAvailable,
    IReadOnlyList<WarehouseStockView> Warehouses);
