using Dapper;
using LogistiFlow.BuildingBlocks.Application.Paging.Models;
using LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Interfaces;
using LogistiFlow.Fulfillment.Application.Interfaces;
using LogistiFlow.Fulfillment.Application.UseCases.Jobs.Shared;
using LogistiFlow.Fulfillment.Application.UseCases.Metrics.GetSummary.Contracts;
using LogistiFlow.Fulfillment.Domain.Enums;
using LogistiFlow.Fulfillment.Infrastructure.Persistence.Queries;
using LogistiFlow.Fulfillment.Infrastructure.Persistence.Rows;

namespace LogistiFlow.Fulfillment.Infrastructure.Persistence.Readers;

public sealed class FulfillmentQueryReader(IDbSession session) : IFulfillmentQueries
{
    public async Task<PagedResult<FulfillmentJobSummary>> ListAsync(
        PageRequest page,
        FulfillmentJobStatus? status,
        string? warehouseCode,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(page);

        var parameters = new { Status = status?.ToString(), WarehouseCode = warehouseCode, page.PageSize, page.Offset };
        var connection = await session.GetConnectionAsync(cancellationToken);
        var total = await connection.ExecuteScalarAsync<long>(Command(FulfillmentJobQueries.Count, parameters, cancellationToken));
        var rows = total == 0 ? [] : await connection.QueryAsync<FulfillmentJobSummaryRow>(Command(FulfillmentJobQueries.Page, parameters, cancellationToken));

        var items = rows.Select(row => new FulfillmentJobSummary(
                row.Id,
                row.OrderId,
                row.OrderNumber,
                row.WarehouseCode,
                Enum.Parse<FulfillmentJobStatus>(row.Status),
                row.StatusReason,
                row.TotalUnits,
                row.CreatedAt,
                row.UpdatedAt))
            .ToList();
        return new PagedResult<FulfillmentJobSummary>(items, page.Page, page.PageSize, total);
    }

    public Task<FulfillmentJobDetails?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken) =>
        LoadDetailsAsync(FulfillmentJobQueries.SelectByPublicId, new { PublicId = jobId }, cancellationToken);

    public Task<FulfillmentJobDetails?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken) =>
        LoadDetailsAsync(FulfillmentJobQueries.SelectByOrderId, new { OrderId = orderId }, cancellationToken);

    public async Task<GetFulfillmentMetricsResponse> GetMetricsAsync(CancellationToken cancellationToken)
    {
        var connection = await session.GetConnectionAsync(cancellationToken);
        var counts = (await connection.QueryAsync<StatusCountRow>(Command(FulfillmentJobQueries.CountByStatus, null, cancellationToken)))
            .ToDictionary(row => row.Status, row => row.Count, StringComparer.Ordinal);
        var byStatus = Enum.GetValues<FulfillmentJobStatus>()
            .ToDictionary(status => status.ToString(), status => counts.GetValueOrDefault(status.ToString()), StringComparer.Ordinal);

        var byWarehouse = (await connection.QueryAsync<WarehouseJobCountRow>(Command(FulfillmentJobQueries.CountByWarehouse, null, cancellationToken)))
            .Select(row => new WarehouseJobCount(row.WarehouseCode, row.Open, row.Failed, row.Total))
            .ToList();

        return new GetFulfillmentMetricsResponse(byStatus.Values.Sum(), byStatus, byWarehouse);
    }

    private async Task<FulfillmentJobDetails?> LoadDetailsAsync(string sql, object parameters, CancellationToken cancellationToken)
    {
        var connection = await session.GetConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<FulfillmentJobRow>(Command(sql, parameters, cancellationToken));
        if (row is null)
        {
            return null;
        }

        var lines = await connection.QueryAsync<FulfillmentJobLineRow>(Command(FulfillmentJobQueries.SelectLines, new { JobId = row.Id }, cancellationToken));
        var history = await connection.QueryAsync<FulfillmentJobStatusChangeRow>(Command(FulfillmentJobQueries.SelectHistory, new { JobId = row.Id }, cancellationToken));

        return new FulfillmentJobDetails(
            row.PublicId,
            row.OrderId,
            row.OrderNumber,
            row.WarehouseCode,
            row.ReservationId,
            Enum.Parse<FulfillmentJobStatus>(row.Status),
            row.StatusReason,
            lines.Select(line => new FulfillmentJobLineItem(line.Sku, line.Quantity)).ToList(),
            history.Select(change => new FulfillmentJobStatusChangeItem(
                    change.FromStatus is null ? null : Enum.Parse<FulfillmentJobStatus>(change.FromStatus),
                    Enum.Parse<FulfillmentJobStatus>(change.ToStatus),
                    change.Reason,
                    change.Source,
                    change.OccurredAt))
                .ToList(),
            row.CreatedAt,
            row.UpdatedAt);
    }

    private CommandDefinition Command(string sql, object? parameters, CancellationToken cancellationToken) =>
        new(sql, parameters, session.Transaction, cancellationToken: cancellationToken);
}
