using LogistiFlow.BuildingBlocks.Domain.Entities;

namespace LogistiFlow.Inventory.Domain.Entities;

public sealed class ProductEntity : Entity
{
    private ProductEntity(string sku, string name, string? description)
    {
        Sku = sku;
        Name = name;
        Description = description;
    }

    public string Sku { get; }

    public string Name { get; }

    public string? Description { get; }

    public static ProductEntity Create(string sku, string name, string? description, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var product = new ProductEntity(sku.Trim().ToUpperInvariant(), name.Trim(), string.IsNullOrWhiteSpace(description) ? null : description.Trim());
        product.Initialize(now);
        return product;
    }
}
