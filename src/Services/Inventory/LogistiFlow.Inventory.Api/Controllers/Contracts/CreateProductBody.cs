namespace LogistiFlow.Inventory.Api.Controllers.Contracts;

public sealed record CreateProductBody(string Sku, string Name, string? Description);
