namespace LogistiFlow.Inventory.Domain.ReadModels;

/// <summary>Availability of a SKU: in the best single warehouse and across the network.</summary>
public sealed record SkuAvailability(string Sku, int BestWarehouseAvailable, int NetworkAvailable);
