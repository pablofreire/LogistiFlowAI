using LogistiFlow.Fulfillment.Domain.Entities;

namespace LogistiFlow.Fulfillment.Domain.Interfaces.Repositories;

public interface IFulfillmentJobRepository
{
    Task<FulfillmentJobEntity?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);

    /// <summary>Returns <c>false</c> when a job already exists for the order.</summary>
    Task<bool> TryAddAsync(FulfillmentJobEntity job, CancellationToken cancellationToken);

    /// <summary>Optimistic update guarded by the job version; returns <c>false</c> on a concurrent change.</summary>
    Task<bool> TryUpdateStatusAsync(FulfillmentJobEntity job, CancellationToken cancellationToken);
}
