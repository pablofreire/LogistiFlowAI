namespace LogistiFlow.Orders.Infrastructure.Persistence.Rows;

internal sealed class OrderRow
{
    public long Id { get; init; }

    public Guid PublicId { get; init; }

    public string OrderNumber { get; init; } = string.Empty;

    public string CustomerName { get; init; } = string.Empty;

    public string DestinationCity { get; init; } = string.Empty;

    public string DestinationCountry { get; init; } = string.Empty;

    public DateTime? PromisedDeliveryDate { get; init; }

    public string Status { get; init; } = string.Empty;

    public string? StatusReason { get; init; }

    public string IdempotencyKey { get; init; } = string.Empty;

    public string RequestHash { get; init; } = string.Empty;

    public int Version { get; init; }

    public bool IsActive { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}
