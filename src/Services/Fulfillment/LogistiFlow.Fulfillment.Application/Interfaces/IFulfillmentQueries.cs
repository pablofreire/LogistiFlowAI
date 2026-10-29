using LogistiFlow.BuildingBlocks.Application.Paging.Models;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.Shared;
using LogistiFlow.Fulfillment.Application.UseCases.Metrics.GetSummary.Contracts;
using LogistiFlow.Fulfillment.Domain.Enums;

namespace LogistiFlow.Fulfillment.Application.Interfaces;

public interface IFulfillmentQueries
{
    Task<PagedResult<FulfillmentJobSummary>> ListAsync(PageRequest page, FulfillmentJobStatus? status, string? warehouseCode, CancellationToken cancellationToken);

    Task<FulfillmentJobDetails?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken);

    Task<FulfillmentJobDetails?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);

    Task<GetFulfillmentMetricsResponse> GetMetricsAsync(CancellationToken cancellationToken);
}
