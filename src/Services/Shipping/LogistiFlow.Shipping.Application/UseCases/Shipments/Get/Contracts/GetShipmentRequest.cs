namespace LogistiFlow.Shipping.Application.UseCases.Shipments.Get.Contracts;

/// <summary>Exactly one of <paramref name="ShipmentId"/> and <paramref name="OrderId"/> is set.</summary>
public sealed record GetShipmentRequest(Guid? ShipmentId, Guid? OrderId);
