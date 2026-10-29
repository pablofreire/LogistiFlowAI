using LogistiFlow.Fulfillment.Domain.Enums;

namespace LogistiFlow.Fulfillment.Application.UseCases.Jobs.Shared;

public sealed record FulfillmentJobSummary(
    Guid Id,
    Guid OrderId,
    string OrderNumber,
    string WarehouseCode,
    FulfillmentJobStatus Status,
    string? StatusReason,
    int TotalUnits,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
