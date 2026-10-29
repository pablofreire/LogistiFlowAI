using LogistiFlow.BuildingBlocks.Application.Paging.Models;
using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Inventory.Application.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Products.List.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Products.List.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Products.Shared;

namespace LogistiFlow.Inventory.Application.UseCases.Products.List.Implementations;

public sealed class ListProductsUseCase(IInventoryQueries queries) : IListProductsUseCase
{
    private const int MaxSearchLength = 60;

    public async Task<Result<PagedResult<ProductItem>>> ExecuteAsync(ListProductsRequest request, CancellationToken cancellationToken)
    {
        var search = request.Search?.Trim();
        if (search is { Length: > MaxSearchLength })
        {
            return Error.Validation("request.invalid", $"Search must be at most {MaxSearchLength} characters.");
        }

        var page = PageRequest.Normalize(request.Page, request.PageSize);
        return await queries.ListProductsAsync(page, string.IsNullOrEmpty(search) ? null : search, cancellationToken);
    }
}
