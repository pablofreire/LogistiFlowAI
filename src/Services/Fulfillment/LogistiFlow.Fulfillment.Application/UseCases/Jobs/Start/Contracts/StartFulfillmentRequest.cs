using LogistiFlow.Contracts.IntegrationEvents.Models;

namespace LogistiFlow.Fulfillment.Application.UseCases.Jobs.Start.Contracts;

public sealed record StartFulfillmentRequest(
    Guid ReservationId,
    Guid OrderId,
    string OrderNumber,
    string WarehouseCode,
    IReadOnlyList<OrderLineItem> Lines,
    string Source);
