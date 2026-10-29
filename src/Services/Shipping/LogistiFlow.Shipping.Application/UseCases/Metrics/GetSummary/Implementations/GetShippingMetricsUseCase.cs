using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Shipping.Application.Interfaces;
using LogistiFlow.Shipping.Application.UseCases.Metrics.GetSummary.Contracts;
using LogistiFlow.Shipping.Application.UseCases.Metrics.GetSummary.Interfaces;

namespace LogistiFlow.Shipping.Application.UseCases.Metrics.GetSummary.Implementations;

public sealed class GetShippingMetricsUseCase(IShippingQueries queries) : IGetShippingMetricsUseCase
{
    public async Task<Result<GetShippingMetricsResponse>> ExecuteAsync(GetShippingMetricsRequest request, CancellationToken cancellationToken) =>
        await queries.GetMetricsAsync(cancellationToken);
}
