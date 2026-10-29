using LogistiFlow.BuildingBlocks.Application.Paging.Models;
using LogistiFlow.Orders.Application.UseCases.Metrics.GetSummary.Contracts;
using LogistiFlow.Orders.Application.UseCases.Orders.Shared;
using LogistiFlow.Orders.Domain.Enums;

namespace LogistiFlow.Orders.Application.Interfaces;

/// <summary>Read-side queries that project straight into API models, bypassing the aggregate.</summary>
public interface IOrderQueries
{
    Task<PagedResult<OrderSummary>> ListAsync(PageRequest page, OrderStatus? status, string? search, CancellationToken cancellationToken);

    /// <summary>Returns <c>null</c> when the order does not exist.</summary>
    Task<IReadOnlyList<OrderStatusChangeItem>?> GetHistoryAsync(Guid orderId, CancellationToken cancellationToken);

    Task<GetOrdersMetricsResponse> GetMetricsAsync(int recentActivityLimit, CancellationToken cancellationToken);
}
