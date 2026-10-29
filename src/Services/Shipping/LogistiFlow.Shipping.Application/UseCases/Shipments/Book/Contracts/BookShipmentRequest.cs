namespace LogistiFlow.Shipping.Application.UseCases.Shipments.Book.Contracts;

/// <param name="ShipmentId">Public id of the claimed shipment.</param>
/// <param name="Attempt">Attempt number counted by the claim (1-based).</param>
public sealed record BookShipmentRequest(Guid ShipmentId, int Attempt);
