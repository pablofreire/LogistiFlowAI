using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Orders.Application.Interfaces;
using LogistiFlow.Orders.Application.UseCases.Metrics.GetSummary.Contracts;
using LogistiFlow.Orders.Application.UseCases.Metrics.GetSummary.Interfaces;

namespace LogistiFlow.Orders.Application.UseCases.Metrics.GetSummary.Implementations;

public sealed class GetOrdersMetricsUseCase(IOrderQueries queries) : IGetOrdersMetricsUseCase
{
    private const int MaxRecentActivity = 50;

    public async Task<Result<GetOrdersMetricsResponse>> ExecuteAsync(GetOrdersMetricsRequest request, CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(request.RecentActivityLimit, 1, MaxRecentActivity);
        return await queries.GetMetricsAsync(limit, cancellationToken);
    }
}
