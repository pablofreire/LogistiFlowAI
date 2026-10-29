using FluentValidation;
using LogistiFlow.BuildingBlocks.Application.Paging.Models;
using LogistiFlow.BuildingBlocks.Application.Validation;
using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Orders.Application.Interfaces;
using LogistiFlow.Orders.Application.UseCases.Orders.List.Contracts;
using LogistiFlow.Orders.Application.UseCases.Orders.List.Interfaces;
using LogistiFlow.Orders.Application.UseCases.Orders.Shared;

namespace LogistiFlow.Orders.Application.UseCases.Orders.List.Implementations;

public sealed class ListOrdersUseCase(IValidator<ListOrdersRequest> validator, IOrderQueries queries) : IListOrdersUseCase
{
    public async Task<Result<PagedResult<OrderSummary>>> ExecuteAsync(ListOrdersRequest request, CancellationToken cancellationToken)
    {
        var validationError = await validator.ValidateToErrorAsync(request, cancellationToken);
        if (validationError != Error.None)
        {
            return validationError;
        }

        var page = PageRequest.Normalize(request.Page, request.PageSize);
        var search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
        return await queries.ListAsync(page, request.Status, search, cancellationToken);
    }
}
