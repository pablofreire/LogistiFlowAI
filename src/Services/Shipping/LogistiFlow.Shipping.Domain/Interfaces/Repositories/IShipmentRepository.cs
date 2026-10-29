using LogistiFlow.Shipping.Domain.Entities;
using LogistiFlow.Shipping.Domain.Models;

namespace LogistiFlow.Shipping.Domain.Interfaces.Repositories;

public interface IShipmentRepository
{
    Task<ShipmentEntity?> GetByPublicIdAsync(Guid shipmentId, CancellationToken cancellationToken);

    Task<ShipmentEntity?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);

    Task<ShipmentEntity?> GetByExceptionIdAsync(Guid exceptionId, CancellationToken cancellationToken);

    /// <summary>Returns <c>false</c> when the order already has a shipment.</summary>
    Task<bool> TryAddAsync(ShipmentEntity shipment, ShipmentSchedule schedule, CancellationToken cancellationToken);

    /// <summary>
    /// Optimistic update guarded by the version; also writes new and changed exceptions, sets the next worker
    /// schedule and releases the worker lease. Returns <c>false</c> on a concurrent change.
    /// </summary>
    Task<bool> TryUpdateAsync(ShipmentEntity shipment, ShipmentSchedule schedule, CancellationToken cancellationToken);

    /// <summary>Stores a carrier event once; returns <c>false</c> when the event was already recorded.</summary>
    Task<bool> TryAddTrackingEventAsync(long shipmentId, CarrierTrackingEvent trackingEvent, CancellationToken cancellationToken);
}
