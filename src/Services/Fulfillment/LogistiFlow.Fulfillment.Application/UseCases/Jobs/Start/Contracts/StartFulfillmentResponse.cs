using LogistiFlow.Fulfillment.Domain.Enums;

namespace LogistiFlow.Fulfillment.Application.UseCases.Jobs.Start.Contracts;

/// <summary><c>AlreadyExisted</c> is true when the order already had a job; nothing changed.</summary>
public sealed record StartFulfillmentResponse(Guid JobId, FulfillmentJobStatus Status, bool AlreadyExisted);
