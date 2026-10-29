namespace LogistiFlow.Shipping.Infrastructure.Persistence.Rows;

internal sealed class ClaimedBookingRow
{
    public Guid ShipmentId { get; init; }

    public int Attempt { get; init; }
}
