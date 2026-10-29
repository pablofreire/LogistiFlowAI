using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Inventory.Application.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Warehouses.List.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Warehouses.List.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Warehouses.Shared;

namespace LogistiFlow.Inventory.Application.UseCases.Warehouses.List.Implementations;

public sealed class ListWarehousesUseCase(IInventoryQueries queries) : IListWarehousesUseCase
{
    public async Task<Result<IReadOnlyList<WarehouseItem>>> ExecuteAsync(ListWarehousesRequest request, CancellationToken cancellationToken) =>
        Result.Success(await queries.ListWarehousesAsync(cancellationToken));
}
