namespace LogistiFlow.Shipping.Application.UseCases.Shipments.Create.Contracts;

public sealed record CreateShipmentRequest(
    Guid FulfillmentJobId,
    Guid OrderId,
    string OrderNumber,
    string WarehouseCode,
    DateTimeOffset ShippedAt);
