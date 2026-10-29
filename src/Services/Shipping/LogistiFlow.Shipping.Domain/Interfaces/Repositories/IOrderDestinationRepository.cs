using LogistiFlow.Shipping.Domain.Models;

namespace LogistiFlow.Shipping.Domain.Interfaces.Repositories;

public interface IOrderDestinationRepository
{
    Task<OrderDestination?> GetAsync(Guid orderId, CancellationToken cancellationToken);

    /// <summary>Stores the destination once per order; a redelivered <c>OrderCreated</c> changes nothing.</summary>
    Task AddIfMissingAsync(OrderDestination destination, CancellationToken cancellationToken);
}
