namespace LogistiFlow.Shipping.Application.UseCases.Shipments.Create.Contracts;

public sealed record CreateShipmentResponse(Guid ShipmentId, string CarrierCode, bool AlreadyExisted);
