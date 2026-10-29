namespace LogistiFlow.Inventory.Application.UseCases.Products.Shared;

/// <summary>A product with its stock totals across all warehouses.</summary>
public sealed record ProductItem(Guid Id, string Sku, string Name, string? Description, int OnHand, int Reserved, int Available);
