namespace LogistiFlow.Shipping.Application.Models;

/// <param name="ShipmentId">Public id of the claimed shipment.</param>
/// <param name="Attempt">Booking attempt number, counted by the claim.</param>
public sealed record ClaimedBooking(Guid ShipmentId, int Attempt);
