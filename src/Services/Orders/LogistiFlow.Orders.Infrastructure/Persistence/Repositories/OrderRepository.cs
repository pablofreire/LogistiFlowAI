using Dapper;
using LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Interfaces;
using LogistiFlow.Orders.Domain.Entities;
using LogistiFlow.Orders.Domain.Enums;
using LogistiFlow.Orders.Domain.Interfaces.Repositories;
using LogistiFlow.Orders.Infrastructure.Persistence.Queries;
using LogistiFlow.Orders.Infrastructure.Persistence.Rows;

namespace LogistiFlow.Orders.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository(IDbSession session) : IOrderRepository
{
    public Task<OrderEntity?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken) =>
        LoadAsync(OrderQueries.SelectByPublicId, new { PublicId = publicId }, cancellationToken);

    public Task<OrderEntity?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken) =>
        LoadAsync(OrderQueries.SelectByIdempotencyKey, new { IdempotencyKey = idempotencyKey }, cancellationToken);

    public async Task<bool> TryAddAsync(OrderEntity order, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(order);

        var connection = await session.GetConnectionAsync(cancellationToken);
        var inserted = await connection.QuerySingleOrDefaultAsync<InsertedOrderRow>(new CommandDefinition(
            OrderQueries.Insert,
            new
            {
                order.PublicId,
                order.CustomerName,
                order.DestinationCity,
                order.DestinationCountry,
                PromisedDeliveryDate = order.PromisedDeliveryDate?.ToDateTime(TimeOnly.MinValue),
                Status = order.Status.ToString(),
                order.StatusReason,
                order.IdempotencyKey,
                order.RequestHash,
                order.Version,
                order.CreatedAt,
                order.UpdatedAt,
            },
            session.Transaction,
            cancellationToken: cancellationToken));

        if (inserted is null)
        {
            return false;
        }

        order.AssignId(inserted.Id);
        order.AssignOrderNumber(inserted.OrderNumber);

        await connection.ExecuteAsync(new CommandDefinition(
            OrderQueries.InsertLine,
            order.Lines.Select(line => new { OrderId = order.Id, line.LineNumber, line.Sku, line.Quantity }),
            session.Transaction,
            cancellationToken: cancellationToken));

        await InsertPendingStatusChangesAsync(order, cancellationToken);
        order.MarkPersisted(order.Version);
        return true;
    }

    public async Task<bool> TryUpdateStatusAsync(OrderEntity order, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(order);

        var connection = await session.GetConnectionAsync(cancellationToken);
        var newVersion = await connection.QuerySingleOrDefaultAsync<int?>(new CommandDefinition(
            OrderQueries.UpdateStatus,
            new
            {
                order.Id,
                order.Version,
                Status = order.Status.ToString(),
                order.StatusReason,
                order.UpdatedAt,
            },
            session.Transaction,
            cancellationToken: cancellationToken));

        if (newVersion is null)
        {
            return false;
        }

        await InsertPendingStatusChangesAsync(order, cancellationToken);
        order.MarkPersisted(newVersion.Value);
        return true;
    }

    private async Task InsertPendingStatusChangesAsync(OrderEntity order, CancellationToken cancellationToken)
    {
        var connection = await session.GetConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            OrderQueries.InsertStatusChange,
            order.PendingStatusChanges.Select(change => new
            {
                OrderId = order.Id,
                FromStatus = change.FromStatus?.ToString(),
                ToStatus = change.ToStatus.ToString(),
                change.Reason,
                change.Source,
                change.OccurredAt,
            }),
            session.Transaction,
            cancellationToken: cancellationToken));
    }

    private async Task<OrderEntity?> LoadAsync(string sql, object parameters, CancellationToken cancellationToken)
    {
        var connection = await session.GetConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<OrderRow>(new CommandDefinition(sql, parameters, session.Transaction, cancellationToken: cancellationToken));
        if (row is null)
        {
            return null;
        }

        var lines = await connection.QueryAsync<OrderLineRow>(new CommandDefinition(
            OrderQueries.SelectLines,
            new { OrderId = row.Id },
            session.Transaction,
            cancellationToken: cancellationToken));

        return OrderEntity.Restore(
            row.Id,
            row.PublicId,
            row.OrderNumber,
            row.CustomerName,
            row.DestinationCity,
            row.DestinationCountry,
            row.PromisedDeliveryDate is { } date ? DateOnly.FromDateTime(date) : null,
            Enum.Parse<OrderStatus>(row.Status),
            row.StatusReason,
            row.Version,
            row.IdempotencyKey,
            row.RequestHash,
            row.IsActive,
            row.CreatedAt,
            row.UpdatedAt,
            lines.Select(line => new OrderLineEntity(line.LineNumber, line.Sku, line.Quantity)));
    }
}
