using LogistiFlow.BuildingBlocks.Application.Paging.Models;
using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Fulfillment.Application.Interfaces;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.List.Contracts;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.List.Interfaces;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.Shared;

namespace LogistiFlow.Fulfillment.Application.UseCases.Jobs.List.Implementations;

public sealed class ListFulfillmentJobsUseCase(IFulfillmentQueries queries) : IListFulfillmentJobsUseCase
{
    public async Task<Result<PagedResult<FulfillmentJobSummary>>> ExecuteAsync(ListFulfillmentJobsRequest request, CancellationToken cancellationToken)
    {
        if (request.Status is { } status && !Enum.IsDefined(status))
        {
            return Error.Validation("request.invalid", "Unknown fulfillment job status.");
        }

        var warehouse = string.IsNullOrWhiteSpace(request.WarehouseCode) || request.WarehouseCode.Length > 20
            ? null
            : request.WarehouseCode.Trim().ToUpperInvariant();
        return await queries.ListAsync(PageRequest.Normalize(request.Page, request.PageSize), request.Status, warehouse, cancellationToken);
    }
}
