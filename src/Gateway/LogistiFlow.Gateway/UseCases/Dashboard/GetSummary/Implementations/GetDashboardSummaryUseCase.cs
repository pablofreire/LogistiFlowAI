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
    public const string ShippingService = "shipping";

    public async Task<Result<GetDashboardSummaryResponse>> ExecuteAsync(GetDashboardSummaryRequest request, CancellationToken cancellationToken)
    {
        var orders = Fetch(OrdersService, request, cancellationToken);
        var inventory = Fetch(InventoryService, request, cancellationToken);
        var fulfillment = Fetch(FulfillmentService, request, cancellationToken);
        var shipping = Fetch(ShippingService, request, cancellationToken);
        await Task.WhenAll(orders, inventory, fulfillment, shipping);

        var unavailable = new[] { (OrdersService, orders), (InventoryService, inventory), (FulfillmentService, fulfillment), (ShippingService, shipping) }
            .Where(source => source.Item2.Result is null)
            .Select(source => source.Item1)
            .ToList();

        return new GetDashboardSummaryResponse(
            orders.Result,
            inventory.Result,
            fulfillment.Result,
            shipping.Result,
            unavailable,
            timeProvider.GetUtcNow());
    }

    private Task<System.Text.Json.JsonElement?> Fetch(string service, GetDashboardSummaryRequest request, CancellationToken cancellationToken) =>
        metrics.GetAsync(service, $"/api/metrics/{service}", request.Authorization, request.CorrelationId, cancellationToken);
}
