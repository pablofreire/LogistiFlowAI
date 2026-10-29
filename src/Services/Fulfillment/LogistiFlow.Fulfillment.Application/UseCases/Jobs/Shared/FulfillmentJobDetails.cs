using LogistiFlow.Fulfillment.Domain.Enums;

namespace LogistiFlow.Fulfillment.Application.UseCases.Jobs.Shared;

public sealed record FulfillmentJobDetails(
    Guid Id,
    Guid OrderId,
    string OrderNumber,
    string WarehouseCode,
    Guid ReservationId,
    FulfillmentJobStatus Status,
    string? StatusReason,
    IReadOnlyList<FulfillmentJobLineItem> Lines,
    IReadOnlyList<FulfillmentJobStatusChangeItem> History,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
