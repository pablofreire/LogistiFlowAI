using LogistiFlow.BuildingBlocks.Application.Paging.Models;
using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Shipping.Application.Interfaces;
using LogistiFlow.Shipping.Application.UseCases.Exceptions.List.Contracts;
using LogistiFlow.Shipping.Application.UseCases.Exceptions.List.Interfaces;
using LogistiFlow.Shipping.Application.UseCases.Exceptions.Shared;

namespace LogistiFlow.Shipping.Application.UseCases.Exceptions.List.Implementations;

public sealed class ListShipmentExceptionsUseCase(IShippingQueries queries) : IListShipmentExceptionsUseCase
{
    public async Task<Result<PagedResult<ShipmentExceptionItem>>> ExecuteAsync(ListShipmentExceptionsRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if ((request.Status is { } status && !Enum.IsDefined(status)) || (request.Type is { } type && !Enum.IsDefined(type)))
        {
            return Error.Validation("request.invalid", "Unknown exception status or type.");
        }

        return await queries.ListExceptionsAsync(PageRequest.Normalize(request.Page, request.PageSize), request.Status, request.Type, cancellationToken);
    }
}
