namespace LogistiFlow.Inventory.Application.UseCases.Products.List.Contracts;

/// <summary>Paged product list. <c>Search</c> matches the SKU or the name.</summary>
public sealed record ListProductsRequest(int? Page, int? PageSize, string? Search);
