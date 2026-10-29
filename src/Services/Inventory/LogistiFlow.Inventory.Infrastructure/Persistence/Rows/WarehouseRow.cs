namespace LogistiFlow.Inventory.Infrastructure.Persistence.Rows;

internal sealed class WarehouseRow
{
    public Guid Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;

    public int OnHand { get; init; }

    public int Reserved { get; init; }

    public int Available { get; init; }
}
