using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Gateway.Interfaces;
using LogistiFlow.Gateway.UseCases.Dashboard.GetSummary.Contracts;
using LogistiFlow.Gateway.UseCases.Dashboard.GetSummary.Interfaces;

namespace LogistiFlow.Gateway.UseCases.Dashboard.GetSummary.Implementations;

public sealed class GetDashboardSummaryUseCase(IServiceMetricsClient metrics, TimeProvider timeProvider) : IGetDashboardSummaryUseCase
{
    public const string OrdersService = "orders";
    public const string InventoryService = "inventory";
    public const string FulfillmentService = "fulfillment";

    public async Task<Result<GetDashboardSummaryResponse>> ExecuteAsync(GetDashboardSummaryRequest request, CancellationToken cancellationToken)
    {
        var orders = metrics.GetAsync(OrdersService, "/api/metrics/orders", request.Authorization, request.CorrelationId, cancellationToken);
        var inventory = metrics.GetAsync(InventoryService, "/api/metrics/inventory", request.Authorization, request.CorrelationId, cancellationToken);
        var fulfillment = metrics.GetAsync(FulfillmentService, "/api/metrics/fulfillment", request.Authorization, request.CorrelationId, cancellationToken);
        await Task.WhenAll(orders, inventory, fulfillment);

        var unavailable = new List<string>();
        if (orders.Result is null)
        {
            unavailable.Add(OrdersService);
        }

        if (inventory.Result is null)
        {
            unavailable.Add(InventoryService);
        }

        if (fulfillment.Result is null)
        {
            unavailable.Add(FulfillmentService);
        }

        return new GetDashboardSummaryResponse(orders.Result, inventory.Result, fulfillment.Result, unavailable, timeProvider.GetUtcNow());
    }
}
