using LogistiFlow.McpServer.Contracts.Views;
using LogistiFlow.McpServer.Models;

namespace LogistiFlow.McpServer.Interfaces;

/// <summary>The read-only LogistiFlow API calls the tools make, all through the gateway.</summary>
public interface IGatewayClient
{
    /// <summary>The order with exactly this number (the API search matches by prefix).</summary>
    Task<GatewayReply<OrderView>> FindOrderAsync(string orderNumber, CancellationToken cancellationToken);

    Task<GatewayReply<IReadOnlyList<StatusChangeView>>> GetOrderHistoryAsync(Guid orderId, CancellationToken cancellationToken);

    Task<GatewayReply<SkuAvailabilityView>> GetAvailabilityAsync(string sku, CancellationToken cancellationToken);

    Task<GatewayReply<ShipmentView>> GetShipmentByOrderAsync(Guid orderId, CancellationToken cancellationToken);

    Task<GatewayReply<ExceptionPage>> ListExceptionsAsync(string? status, string? type, int limit, CancellationToken cancellationToken);

    Task<GatewayReply<FulfillmentMetricsView>> GetFulfillmentMetricsAsync(CancellationToken cancellationToken);

    Task<GatewayReply<InventoryMetricsView>> GetInventoryMetricsAsync(CancellationToken cancellationToken);
}
