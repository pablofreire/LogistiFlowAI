namespace LogistiFlow.Inventory.Infrastructure.Persistence.Rows;

internal sealed class ProductRow
{
    public Guid Id { get; init; }

    public string Sku { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public int OnHand { get; init; }

    public int Reserved { get; init; }

    public int Available { get; init; }
}
