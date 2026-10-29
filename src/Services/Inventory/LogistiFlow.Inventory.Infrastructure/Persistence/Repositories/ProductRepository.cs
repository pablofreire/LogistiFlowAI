using Dapper;
using LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Interfaces;
using LogistiFlow.Inventory.Domain.Entities;
using LogistiFlow.Inventory.Domain.Interfaces.Repositories;
using LogistiFlow.Inventory.Infrastructure.Persistence.Queries;

namespace LogistiFlow.Inventory.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository(IDbSession session) : IProductRepository
{
    public async Task<bool> TryAddAsync(ProductEntity product, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(product);

        var connection = await session.GetConnectionAsync(cancellationToken);
        var id = await connection.QuerySingleOrDefaultAsync<long?>(new CommandDefinition(
            ProductQueries.Insert,
            new { product.PublicId, product.Sku, product.Name, product.Description, product.CreatedAt, product.UpdatedAt },
            session.Transaction,
            cancellationToken: cancellationToken));

        if (id is null)
        {
            return false;
        }

        product.AssignId(id.Value);
        return true;
    }
}
