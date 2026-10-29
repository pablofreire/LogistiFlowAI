using Dapper;
using LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Interfaces;
using LogistiFlow.Shipping.Domain.Entities;
using LogistiFlow.Shipping.Domain.Enums;
using LogistiFlow.Shipping.Domain.Interfaces.Repositories;
using LogistiFlow.Shipping.Domain.Models;
using LogistiFlow.Shipping.Infrastructure.Persistence.Queries;
using LogistiFlow.Shipping.Infrastructure.Persistence.Rows;

namespace LogistiFlow.Shipping.Infrastructure.Persistence.Repositories;

public sealed class ShipmentRepository(IDbSession session) : IShipmentRepository
{
    public Task<ShipmentEntity?> GetByPublicIdAsync(Guid shipmentId, CancellationToken cancellationToken) =>
        GetAsync(ShipmentQueries.SelectByPublicId, new { PublicId = shipmentId }, cancellationToken);

    public Task<ShipmentEntity?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken) =>
        GetAsync(ShipmentQueries.SelectByOrderId, new { OrderId = orderId }, cancellationToken);

    public Task<ShipmentEntity?> GetByExceptionIdAsync(Guid exceptionId, CancellationToken cancellationToken) =>
        GetAsync(ShipmentQueries.SelectByExceptionId, new { ExceptionId = exceptionId }, cancellationToken);

    public async Task<bool> TryAddAsync(ShipmentEntity shipment, ShipmentSchedule schedule, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(shipment);
        ArgumentNullException.ThrowIfNull(schedule);

        var connection = await session.GetConnectionAsync(cancellationToken);
        var id = await connection.QuerySingleOrDefaultAsync<long?>(new CommandDefinition(
            ShipmentQueries.Insert,
            new
            {
                shipment.PublicId,
                shipment.OrderId,
                shipment.OrderNumber,
                shipment.FulfillmentJobId,
                shipment.WarehouseCode,
                shipment.DestinationCity,
                shipment.DestinationCountry,
                PromisedDeliveryDate = ToDateTime(shipment.PromisedDeliveryDate),
                shipment.CarrierCode,
                Status = shipment.Status.ToString(),
                shipment.StatusReason,
                shipment.Version,
                NextBookingSeconds = schedule.NextBookingIn?.TotalSeconds,
                NextPollSeconds = schedule.NextPollIn?.TotalSeconds,
                shipment.CreatedAt,
                shipment.UpdatedAt,
            },
            session.Transaction,
            cancellationToken: cancellationToken));

        if (id is null)
        {
            return false;
        }

        shipment.AssignId(id.Value);
        await SaveExceptionsAsync(shipment, cancellationToken);
        shipment.MarkPersisted(shipment.Version);
        return true;
    }

    public async Task<bool> TryUpdateAsync(ShipmentEntity shipment, ShipmentSchedule schedule, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(shipment);
        ArgumentNullException.ThrowIfNull(schedule);

        var connection = await session.GetConnectionAsync(cancellationToken);
        var version = await connection.QuerySingleOrDefaultAsync<int?>(new CommandDefinition(
            ShipmentQueries.Update,
            new
            {
                shipment.Id,
                shipment.Version,
                shipment.TrackingNumber,
                OriginalEstimatedDelivery = ToDateTime(shipment.OriginalEstimatedDelivery),
                EstimatedDelivery = ToDateTime(shipment.EstimatedDelivery),
                Status = shipment.Status.ToString(),
                shipment.StatusReason,
                shipment.DeliveredAt,
                NextBookingSeconds = schedule.NextBookingIn?.TotalSeconds,
                NextPollSeconds = schedule.NextPollIn?.TotalSeconds,
                shipment.UpdatedAt,
            },
            session.Transaction,
            cancellationToken: cancellationToken));

        if (version is null)
        {
            return false;
        }

        await SaveExceptionsAsync(shipment, cancellationToken);
        shipment.MarkPersisted(version.Value);
        return true;
    }

    public async Task<bool> TryAddTrackingEventAsync(long shipmentId, CarrierTrackingEvent trackingEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(trackingEvent);

        var connection = await session.GetConnectionAsync(cancellationToken);
        var inserted = await connection.ExecuteAsync(new CommandDefinition(
            ShipmentQueries.InsertTrackingEvent,
            new
            {
                ShipmentId = shipmentId,
                trackingEvent.CarrierEventId,
                trackingEvent.Code,
                Description = Truncate(trackingEvent.Description, 300),
                Location = Truncate(trackingEvent.Location, 120),
                trackingEvent.OccurredAt,
                EstimatedDelivery = ToDateTime(trackingEvent.EstimatedDelivery),
            },
            session.Transaction,
            cancellationToken: cancellationToken));
        return inserted == 1;
    }

    private async Task<ShipmentEntity?> GetAsync(string sql, object parameters, CancellationToken cancellationToken)
    {
        var connection = await session.GetConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<ShipmentRow>(new CommandDefinition(sql, parameters, session.Transaction, cancellationToken: cancellationToken));
        if (row is null)
        {
            return null;
        }

        var exceptions = await connection.QueryAsync<ShipmentExceptionRow>(new CommandDefinition(
            ShipmentQueries.SelectExceptions,
            new { ShipmentId = row.Id },
            session.Transaction,
            cancellationToken: cancellationToken));

        return ShipmentEntity.Restore(
            row.Id,
            row.PublicId,
            row.OrderId,
            row.OrderNumber,
            row.FulfillmentJobId,
            row.WarehouseCode,
            row.DestinationCity,
            row.DestinationCountry,
            ToDateOnly(row.PromisedDeliveryDate),
            row.CarrierCode,
            row.TrackingNumber,
            ToDateOnly(row.OriginalEstimatedDelivery),
            ToDateOnly(row.EstimatedDelivery),
            Enum.Parse<ShipmentStatus>(row.Status),
            row.StatusReason,
            row.DeliveredAt,
            row.Version,
            row.CreatedAt,
            row.UpdatedAt,
            exceptions.Select(exception => ShipmentExceptionEntity.Restore(
                exception.Id,
                exception.PublicId,
                Enum.Parse<ShipmentExceptionType>(exception.Type),
                Enum.Parse<ShipmentExceptionStatus>(exception.Status),
                exception.Details,
                exception.ResolvedAt,
                exception.Resolution,
                exception.ResolvedBy,
                exception.CreatedAt,
                exception.UpdatedAt)));
    }

    /// <summary>Updates before inserts: a resolved exception must leave the "one open per type" index before a new one opens.</summary>
    private async Task SaveExceptionsAsync(ShipmentEntity shipment, CancellationToken cancellationToken)
    {
        var connection = await session.GetConnectionAsync(cancellationToken);
        foreach (var exception in shipment.Exceptions.Where(exception => exception.Id != 0 && exception.IsModified))
        {
            await connection.ExecuteAsync(new CommandDefinition(
                ShipmentQueries.UpdateException,
                new
                {
                    exception.Id,
                    Status = exception.Status.ToString(),
                    exception.Details,
                    exception.Resolution,
                    exception.ResolvedBy,
                    exception.ResolvedAt,
                    exception.UpdatedAt,
                },
                session.Transaction,
                cancellationToken: cancellationToken));
        }

        foreach (var exception in shipment.Exceptions.Where(exception => exception.Id == 0))
        {
            var id = await connection.QuerySingleAsync<long>(new CommandDefinition(
                ShipmentQueries.InsertException,
                new
                {
                    exception.PublicId,
                    ShipmentId = shipment.Id,
                    Type = exception.Type.ToString(),
                    Status = exception.Status.ToString(),
                    exception.Details,
                    exception.Resolution,
                    exception.ResolvedBy,
                    exception.ResolvedAt,
                    exception.CreatedAt,
                    exception.UpdatedAt,
                },
                session.Transaction,
                cancellationToken: cancellationToken));
            exception.AssignId(id);
        }
    }

    private static DateTime? ToDateTime(DateOnly? date) => date?.ToDateTime(TimeOnly.MinValue);

    private static DateOnly? ToDateOnly(DateTime? date) => date is { } value ? DateOnly.FromDateTime(value) : null;

    private static string Truncate(string text, int length) => text.Length <= length ? text : text[..length];
}
