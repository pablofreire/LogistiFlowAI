using LogistiFlow.Shipping.Application.Models;
using LogistiFlow.Shipping.Domain.Models;

namespace LogistiFlow.Shipping.Application.Interfaces;

/// <summary>
/// Claims shipments that are due for a booking call or a tracking poll. A claim takes a lease (database clock), so
/// several instances never work on the same shipment and a crashed worker's shipments become due again.
/// </summary>
public interface IShipmentWorkQueue
{
    Task<IReadOnlyList<ClaimedBooking>> ClaimDueBookingsAsync(int batchSize, TimeSpan lease, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> ClaimDueTrackingAsync(int batchSize, TimeSpan lease, CancellationToken cancellationToken);

    /// <summary>Sets the next schedule and releases the lease without changing the shipment itself.</summary>
    Task RescheduleAsync(Guid shipmentId, ShipmentSchedule schedule, CancellationToken cancellationToken);
}
