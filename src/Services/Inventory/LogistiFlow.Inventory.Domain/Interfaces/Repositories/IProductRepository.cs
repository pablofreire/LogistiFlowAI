using LogistiFlow.Inventory.Domain.Entities;

namespace LogistiFlow.Inventory.Domain.Interfaces.Repositories;

public interface IProductRepository
{
    /// <summary>Returns <c>false</c> when the SKU is already registered.</summary>
    Task<bool> TryAddAsync(ProductEntity product, CancellationToken cancellationToken);
}
