namespace LogistiFlow.Fulfillment.Application.UseCases.Jobs.CancelForOrder.Contracts;

public sealed record CancelFulfillmentForOrderRequest(Guid OrderId, string Reason, string Source);
