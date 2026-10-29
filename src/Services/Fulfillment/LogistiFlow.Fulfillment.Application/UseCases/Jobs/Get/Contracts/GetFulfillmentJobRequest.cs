namespace LogistiFlow.Fulfillment.Application.UseCases.Jobs.Get.Contracts;

/// <summary>Looks a job up by its own id or by the order id; exactly one must be set.</summary>
public sealed record GetFulfillmentJobRequest(Guid? JobId, Guid? OrderId);
