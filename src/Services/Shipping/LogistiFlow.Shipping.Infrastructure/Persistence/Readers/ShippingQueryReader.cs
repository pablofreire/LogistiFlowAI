using Dapper;
using LogistiFlow.BuildingBlocks.Application.Paging.Models;
using LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Interfaces;
using LogistiFlow.Shipping.Application.Interfaces;
using LogistiFlow.Shipping.Application.UseCases.Exceptions.Shared;
using LogistiFlow.Shipping.Application.UseCases.Metrics.GetSummary.Contracts;
using LogistiFlow.Shipping.Application.UseCases.Shipments.Shared;
using LogistiFlow.Shipping.Domain.Enums;
using LogistiFlow.Shipping.Domain.Services;
using LogistiFlow.Shipping.Infrastructure.Persistence.Queries;
using LogistiFlow.Shipping.Infrastructure.Persistence.Rows;

namespace LogistiFlow.Shipping.Infrastructure.Persistence.Readers;

public sealed class ShippingQueryReader(IDbSession session) : IShippingQueries
{
    public async Task<PagedResult<ShipmentSummary>> ListShipmentsAsync(
        PageRequest page,
        ShipmentStatus? status,
        bool delayedOnly,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(page);

        var parameters = new { Status = status?.ToString(), DelayedOnly = delayedOnly, page.PageSize, page.Offset };
        var connection = await session.GetConnectionAsync(cancellationToken);
        var total = await connection.ExecuteScalarAsync<long>(Command(ShippingReadQueries.CountShipments, parameters, cancellationToken));
        var rows = total == 0 ? [] : await connection.QueryAsync<ShipmentSummaryRow>(Command(ShippingReadQueries.PageShipments, parameters, cancellationToken));

        var items = rows.Select(row => new ShipmentSummary(
                row.Id,
                row.OrderId,
                row.OrderNumber,
                row.CarrierCode,
                row.CarrierName,
                row.TrackingNumber,
                Enum.Parse<ShipmentStatus>(row.Status),
                row.DestinationCity,
                row.DestinationCountry.Trim(),
                ToDateOnly(row.PromisedDeliveryDate),
                ToDateOnly(row.EstimatedDelivery),
                row.IsDelayed,
                row.OpenExceptions,
                row.CreatedAt,
                row.UpdatedAt))
            .ToList();
        return new PagedResult<ShipmentSummary>(items, page.Page, page.PageSize, total);
    }

    public Task<ShipmentDetails?> GetShipmentAsync(Guid shipmentId, CancellationToken cancellationToken) =>
        LoadDetailsAsync(ShippingReadQueries.DetailsByPublicId, new { ShipmentId = shipmentId }, cancellationToken);

    public Task<ShipmentDetails?> GetShipmentByOrderAsync(Guid orderId, CancellationToken cancellationToken) =>
        LoadDetailsAsync(ShippingReadQueries.DetailsByOrderId, new { OrderId = orderId }, cancellationToken);

    public async Task<PagedResult<ShipmentExceptionItem>> ListExceptionsAsync(
        PageRequest page,
        ShipmentExceptionStatus? status,
        ShipmentExceptionType? type,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(page);

        var parameters = new { Status = status?.ToString(), Type = type?.ToString(), page.PageSize, page.Offset };
        var connection = await session.GetConnectionAsync(cancellationToken);
        var total = await connection.ExecuteScalarAsync<long>(Command(ShippingReadQueries.CountExceptions, parameters, cancellationToken));
        var rows = total == 0 ? [] : await connection.QueryAsync<ShipmentExceptionItemRow>(Command(ShippingReadQueries.PageExceptions, parameters, cancellationToken));
        return new PagedResult<ShipmentExceptionItem>(rows.Select(ToItem).ToList(), page.Page, page.PageSize, total);
    }

    public async Task<GetShippingMetricsResponse> GetMetricsAsync(CancellationToken cancellationToken)
    {
        var connection = await session.GetConnectionAsync(cancellationToken);
        var statusCounts = (await connection.QueryAsync<StatusCountRow>(Command(ShippingReadQueries.CountByStatus, null, cancellationToken)))
            .ToDictionary(row => row.Status, row => row.Count, StringComparer.Ordinal);
        var byStatus = Enum.GetValues<ShipmentStatus>()
            .ToDictionary(status => status.ToString(), status => statusCounts.GetValueOrDefault(status.ToString()), StringComparer.Ordinal);

        var exceptionCounts = (await connection.QueryAsync<StatusCountRow>(Command(ShippingReadQueries.CountOpenExceptionsByType, null, cancellationToken)))
            .ToDictionary(row => row.Status, row => row.Count, StringComparer.Ordinal);
        var openByType = Enum.GetValues<ShipmentExceptionType>()
            .ToDictionary(type => type.ToString(), type => exceptionCounts.GetValueOrDefault(type.ToString()), StringComparer.Ordinal);

        var delayed = await connection.ExecuteScalarAsync<long>(Command(ShippingReadQueries.CountDelayed, null, cancellationToken));
        var inTransit = Enum.GetValues<ShipmentStatus>().Where(ShipmentStateMachine.IsTracked).Sum(status => byStatus[status.ToString()]);

        return new GetShippingMetricsResponse(
            byStatus.Values.Sum(),
            byStatus,
            inTransit,
            byStatus[nameof(ShipmentStatus.Delivered)],
            delayed,
            openByType.Values.Sum(),
            openByType);
    }

    private async Task<ShipmentDetails?> LoadDetailsAsync(string sql, object parameters, CancellationToken cancellationToken)
    {
        var connection = await session.GetConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<ShipmentDetailsRow>(Command(sql, parameters, cancellationToken));
        if (row is null)
        {
            return null;
        }

        var events = await connection.QueryAsync<TrackingEventRow>(Command(ShippingReadQueries.TrackingEvents, new { row.InternalId }, cancellationToken));
        var exceptions = await connection.QueryAsync<ShipmentExceptionItemRow>(Command(ShippingReadQueries.ExceptionsForShipment, new { row.InternalId }, cancellationToken));

        return new ShipmentDetails(
            row.Id,
            row.OrderId,
            row.OrderNumber,
            row.FulfillmentJobId,
            row.WarehouseCode,
            row.CarrierCode,
            row.CarrierName,
            row.TrackingNumber,
            Enum.Parse<ShipmentStatus>(row.Status),
            row.StatusReason,
            row.DestinationCity,
            row.DestinationCountry.Trim(),
            ToDateOnly(row.PromisedDeliveryDate),
            ToDateOnly(row.OriginalEstimatedDelivery),
            ToDateOnly(row.EstimatedDelivery),
            row.DeliveredAt,
            row.BookingAttempts,
            row.IsDelayed,
            row.CreatedAt,
            row.UpdatedAt,
            events.Select(e => new TrackingEventItem(e.Code, e.Description, e.Location, e.OccurredAt, ToDateOnly(e.EstimatedDelivery), e.ReceivedAt)).ToList(),
            exceptions.Select(ToItem).ToList());
    }

    private static ShipmentExceptionItem ToItem(ShipmentExceptionItemRow row) => new(
        row.Id,
        row.ShipmentId,
        row.OrderNumber,
        row.CarrierCode,
        row.TrackingNumber,
        Enum.Parse<ShipmentExceptionType>(row.Type),
        Enum.Parse<ShipmentExceptionStatus>(row.Status),
        row.Details,
        row.OpenedAt,
        row.ResolvedAt,
        row.Resolution,
        row.ResolvedBy);

    private static DateOnly? ToDateOnly(DateTime? date) => date is { } value ? DateOnly.FromDateTime(value) : null;

    private CommandDefinition Command(string sql, object? parameters, CancellationToken cancellationToken) =>
        new(sql, parameters, session.Transaction, cancellationToken: cancellationToken);
}
