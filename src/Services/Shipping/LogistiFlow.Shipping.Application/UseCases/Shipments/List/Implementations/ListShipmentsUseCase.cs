using LogistiFlow.BuildingBlocks.Application.Paging.Models;
using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Shipping.Application.Interfaces;
using LogistiFlow.Shipping.Application.UseCases.Shipments.List.Contracts;
using LogistiFlow.Shipping.Application.UseCases.Shipments.List.Interfaces;
using LogistiFlow.Shipping.Application.UseCases.Shipments.Shared;

namespace LogistiFlow.Shipping.Application.UseCases.Shipments.List.Implementations;

public sealed class ListShipmentsUseCase(IShippingQueries queries) : IListShipmentsUseCase
{
    public async Task<Result<PagedResult<ShipmentSummary>>> ExecuteAsync(ListShipmentsRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Status is { } status && !Enum.IsDefined(status))
        {
            return Error.Validation("request.invalid", "Unknown shipment status.");
        }

        return await queries.ListShipmentsAsync(PageRequest.Normalize(request.Page, request.PageSize), request.Status, request.DelayedOnly, cancellationToken);
    }
}
