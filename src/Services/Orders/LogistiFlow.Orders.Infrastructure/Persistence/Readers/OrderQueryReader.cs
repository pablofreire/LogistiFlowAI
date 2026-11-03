using Dapper;
using LogistiFlow.BuildingBlocks.Application.Paging.Models;
using LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Interfaces;
using LogistiFlow.Orders.Application.Interfaces;
using LogistiFlow.Orders.Application.UseCases.Metrics.GetSummary.Contracts;
using LogistiFlow.Orders.Application.UseCases.Orders.Shared;
using LogistiFlow.Orders.Domain.Enums;
using LogistiFlow.Orders.Infrastructure.Persistence.Queries;
using LogistiFlow.Orders.Infrastructure.Persistence.Rows;
using Npgsql;

namespace LogistiFlow.Orders.Infrastructure.Persistence.Readers;

/// <summary>Read side of the Orders service: projections straight from SQL into API models.</summary>
public sealed class OrderQueryReader(IDbSession session) : IOrderQueries
{
    public async Task<PagedResult<OrderSummary>> ListAsync(PageRequest page, OrderStatus? status, string? search, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(page);

        var parameters = new
        {
            Status = status?.ToString(),
            SearchPattern = search is null ? null : $"%{EscapeLike(search)}%",
            page.PageSize,
            page.Offset,
        };

        var connection = await session.GetConnectionAsync(cancellationToken);
        var total = await connection.ExecuteScalarAsync<long>(new CommandDefinition(OrderReadQueries.Count, parameters, session.Transaction, cancellationToken: cancellationToken));
        var rows = total == 0
            ? []
            : await connection.QueryAsync<OrderSummaryRow>(new CommandDefinition(OrderReadQueries.Page, parameters, session.Transaction, cancellationToken: cancellationToken));

        var items = rows.Select(row => new OrderSummary(
                row.Id,
                row.OrderNumber,
                row.CustomerName,
                row.DestinationCity,
                Enum.Parse<OrderStatus>(row.Status),
                row.StatusReason,
                row.LineCount,
                row.TotalUnits,
                row.CreatedAt,
                row.UpdatedAt))
            .ToList();

        return new PagedResult<OrderSummary>(items, page.Page, page.PageSize, total);
    }

    public async Task<IReadOnlyList<OrderStatusChangeItem>?> GetHistoryAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var connection = await session.GetConnectionAsync(cancellationToken);
        var id = await connection.ExecuteScalarAsync<long?>(new CommandDefinition(OrderReadQueries.SelectOrderId, new { OrderId = orderId }, session.Transaction, cancellationToken: cancellationToken));
        if (id is null)
        {
            return null;
        }

        var rows = await connection.QueryAsync<OrderStatusChangeRow>(new CommandDefinition(OrderReadQueries.History, new { Id = id }, session.Transaction, cancellationToken: cancellationToken));
        return rows.Select(row => new OrderStatusChangeItem(ParseStatus(row.FromStatus), Enum.Parse<OrderStatus>(row.ToStatus), row.Reason, row.Source, row.OccurredAt)).ToList();
    }

    public async Task<GetOrdersMetricsResponse> GetMetricsAsync(int recentActivityLimit, CancellationToken cancellationToken)
    {
        var connection = await session.GetConnectionAsync(cancellationToken);

        var counts = (await connection.QueryAsync<StatusCountRow>(new CommandDefinition(OrderReadQueries.CountByStatus, transaction: session.Transaction, cancellationToken: cancellationToken)))
            .ToDictionary(row => row.Status, row => row.Count, StringComparer.Ordinal);
        var byStatus = Enum.GetValues<OrderStatus>()
            .ToDictionary(status => status.ToString(), status => counts.GetValueOrDefault(status.ToString()), StringComparer.Ordinal);

        var last24Hours = await connection.ExecuteScalarAsync<long>(new CommandDefinition(OrderReadQueries.CreatedLast24Hours, transaction: session.Transaction, cancellationToken: cancellationToken));
        var averageToPicking = await AverageSecondsToAsync(connection, OrderStatus.Picking, cancellationToken);
        var averageToShipped = await AverageSecondsToAsync(connection, OrderStatus.Shipped, cancellationToken);
        var averageToDelivered = await AverageSecondsToAsync(connection, OrderStatus.Delivered, cancellationToken);
        var recent = await connection.QueryAsync<OrderStatusChangeRow>(new CommandDefinition(OrderReadQueries.RecentActivity, new { Limit = recentActivityLimit }, session.Transaction, cancellationToken: cancellationToken));

        return new GetOrdersMetricsResponse(
            byStatus.Values.Sum(),
            byStatus,
            last24Hours,
            averageToPicking,
            averageToShipped,
            averageToDelivered,
            recent.Select(row => new RecentOrderActivity(
                    row.OrderId,
                    row.OrderNumber,
                    ParseStatus(row.FromStatus),
                    Enum.Parse<OrderStatus>(row.ToStatus),
                    row.Reason,
                    row.Source,
                    row.OccurredAt))
                .ToList());
    }

    private async Task<double?> AverageSecondsToAsync(NpgsqlConnection connection, OrderStatus status, CancellationToken cancellationToken)
    {
        var average = await connection.ExecuteScalarAsync<double?>(new CommandDefinition(OrderReadQueries.AverageSecondsToStatus, new { Status = status.ToString() }, session.Transaction, cancellationToken: cancellationToken));
        return average is null ? null : Math.Round(average.Value, 2);
    }

    private static OrderStatus? ParseStatus(string? value) => value is null ? null : Enum.Parse<OrderStatus>(value);

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}
