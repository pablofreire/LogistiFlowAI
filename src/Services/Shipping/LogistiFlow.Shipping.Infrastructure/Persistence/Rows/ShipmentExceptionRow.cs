namespace LogistiFlow.Shipping.Infrastructure.Persistence.Rows;

internal sealed class ShipmentExceptionRow
{
    public long Id { get; init; }

    public Guid PublicId { get; init; }

    public string Type { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string Details { get; init; } = string.Empty;

    public string? Resolution { get; init; }

    public string? ResolvedBy { get; init; }

    public DateTimeOffset? ResolvedAt { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}
