using Dapper;
using LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Interfaces;
using LogistiFlow.Inventory.Domain.Entities;
using LogistiFlow.Inventory.Domain.Enums;
using LogistiFlow.Inventory.Domain.Interfaces.Repositories;
using LogistiFlow.Inventory.Infrastructure.Persistence.Queries;
using LogistiFlow.Inventory.Infrastructure.Persistence.Rows;

namespace LogistiFlow.Inventory.Infrastructure.Persistence.Repositories;

public sealed class ReservationRepository(IDbSession session) : IReservationRepository
{
    public async Task<ReservationEntity?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var connection = await session.GetConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<ReservationRow>(new CommandDefinition(
            ReservationQueries.SelectByOrderId,
            new { OrderId = orderId },
            session.Transaction,
            cancellationToken: cancellationToken));
        if (row is null)
        {
            return null;
        }

        var lines = await connection.QueryAsync<ReservationLineRow>(new CommandDefinition(
            ReservationQueries.SelectLines,
            new { ReservationId = row.Id },
            session.Transaction,
            cancellationToken: cancellationToken));

        return ReservationEntity.Restore(
            row.Id,
            row.PublicId,
            row.OrderId,
            row.OrderNumber,
            row.WarehouseCode,
            Enum.Parse<ReservationStatus>(row.Status),
            row.FailureReason,
            row.CreatedAt,
            row.UpdatedAt,
            lines.Select(line => new ReservationLineEntity(line.StockPositionId, line.Sku, line.Quantity)));
    }

    public async Task<bool> TryAddAsync(ReservationEntity reservation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reservation);

        var connection = await session.GetConnectionAsync(cancellationToken);
        var id = await connection.QuerySingleOrDefaultAsync<long?>(new CommandDefinition(
            ReservationQueries.Insert,
            new
            {
                reservation.PublicId,
                reservation.OrderId,
                reservation.OrderNumber,
                reservation.WarehouseCode,
                Status = reservation.Status.ToString(),
                reservation.FailureReason,
                reservation.CreatedAt,
                reservation.UpdatedAt,
            },
            session.Transaction,
            cancellationToken: cancellationToken));

        if (id is null)
        {
            return false;
        }

        reservation.AssignId(id.Value);
        await connection.ExecuteAsync(new CommandDefinition(
            ReservationQueries.InsertLine,
            reservation.Lines.Select(line => new { ReservationId = id.Value, line.StockPositionId, line.Sku, line.Quantity }),
            session.Transaction,
            cancellationToken: cancellationToken));
        return true;
    }

    public async Task UpdateStatusAsync(ReservationEntity reservation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reservation);

        var connection = await session.GetConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            ReservationQueries.UpdateStatus,
            new { reservation.Id, Status = reservation.Status.ToString(), reservation.UpdatedAt },
            session.Transaction,
            cancellationToken: cancellationToken));
    }
}
