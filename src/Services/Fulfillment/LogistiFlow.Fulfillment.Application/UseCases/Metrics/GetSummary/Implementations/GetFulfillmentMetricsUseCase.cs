using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Fulfillment.Application.Interfaces;
using LogistiFlow.Fulfillment.Application.UseCases.Metrics.GetSummary.Contracts;
using LogistiFlow.Fulfillment.Application.UseCases.Metrics.GetSummary.Interfaces;

namespace LogistiFlow.Fulfillment.Application.UseCases.Metrics.GetSummary.Implementations;

public sealed class GetFulfillmentMetricsUseCase(IFulfillmentQueries queries) : IGetFulfillmentMetricsUseCase
{
    public async Task<Result<GetFulfillmentMetricsResponse>> ExecuteAsync(GetFulfillmentMetricsRequest request, CancellationToken cancellationToken) =>
        await queries.GetMetricsAsync(cancellationToken);
}
