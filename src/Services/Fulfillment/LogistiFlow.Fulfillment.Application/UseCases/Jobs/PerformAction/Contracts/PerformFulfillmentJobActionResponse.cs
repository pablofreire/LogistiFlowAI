using LogistiFlow.Fulfillment.Domain.Enums;

namespace LogistiFlow.Fulfillment.Application.UseCases.Jobs.PerformAction.Contracts;

public sealed record PerformFulfillmentJobActionResponse(
    Guid JobId,
    Guid OrderId,
    string OrderNumber,
    FulfillmentJobStatus Status,
    string? StatusReason,
    int Version);
