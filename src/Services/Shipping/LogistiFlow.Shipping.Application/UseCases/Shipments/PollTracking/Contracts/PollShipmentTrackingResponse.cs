using LogistiFlow.Shipping.Domain.Enums;

namespace LogistiFlow.Shipping.Application.UseCases.Shipments.PollTracking.Contracts;

/// <param name="Status">Shipment status after the poll.</param>
/// <param name="NewEvents">Carrier events seen for the first time; a repeated poll returns 0.</param>
/// <param name="CarrierError">Why the carrier could not be read, if it could not.</param>
public sealed record PollShipmentTrackingResponse(ShipmentStatus Status, int NewEvents, string? CarrierError);
