using LogistiFlow.BuildingBlocks.Application.Paging.Models;
using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Inventory.Application.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Stock.List.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Stock.List.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Stock.Shared;

namespace LogistiFlow.Inventory.Application.UseCases.Stock.List.Implementations;

public sealed class ListStockUseCase(IInventoryQueries queries) : IListStockUseCase
{
    public const int LowStockThreshold = 10;

    public async Task<Result<PagedResult<StockPositionItem>>> ExecuteAsync(ListStockRequest request, CancellationToken cancellationToken)
    {
        var page = PageRequest.Normalize(request.Page, request.PageSize);
        return await queries.ListStockAsync(
            page,
            Normalize(request.WarehouseCode),
            Normalize(request.Sku),
            request.LowStockOnly,
            LowStockThreshold,
            cancellationToken);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant()[..Math.Min(value.Trim().Length, 40)];
}
