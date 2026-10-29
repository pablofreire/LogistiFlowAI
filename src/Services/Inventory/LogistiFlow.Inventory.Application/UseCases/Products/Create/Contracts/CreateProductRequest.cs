namespace LogistiFlow.Inventory.Application.UseCases.Products.Create.Contracts;

public sealed record CreateProductRequest(string Sku, string Name, string? Description);
