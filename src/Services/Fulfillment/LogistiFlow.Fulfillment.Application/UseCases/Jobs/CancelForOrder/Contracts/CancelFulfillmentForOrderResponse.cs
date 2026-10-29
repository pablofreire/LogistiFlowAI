using LogistiFlow.Fulfillment.Domain.Enums;

namespace LogistiFlow.Fulfillment.Application.UseCases.Jobs.CancelForOrder.Contracts;

/// <summary><c>Status</c> is null when the order never had a job.</summary>
public sealed record CancelFulfillmentForOrderResponse(bool Cancelled, FulfillmentJobStatus? Status);
