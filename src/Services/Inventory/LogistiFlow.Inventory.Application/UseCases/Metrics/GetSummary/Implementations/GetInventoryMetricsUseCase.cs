using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Inventory.Application.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Metrics.GetSummary.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Metrics.GetSummary.Interfaces;

namespace LogistiFlow.Inventory.Application.UseCases.Metrics.GetSummary.Implementations;

public sealed class GetInventoryMetricsUseCase(IInventoryQueries queries) : IGetInventoryMetricsUseCase
{
    public async Task<Result<GetInventoryMetricsResponse>> ExecuteAsync(GetInventoryMetricsRequest request, CancellationToken cancellationToken) =>
        await queries.GetMetricsAsync(
            Math.Clamp(request.LowStockThreshold, 0, 1000),
            Math.Clamp(request.RecentMovementLimit, 1, 50),
            cancellationToken);
}
