using Dapper;
using LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Interfaces;
using LogistiFlow.Fulfillment.Domain.Entities;
using LogistiFlow.Fulfillment.Domain.Enums;
using LogistiFlow.Fulfillment.Domain.Interfaces.Repositories;
using LogistiFlow.Fulfillment.Infrastructure.Persistence.Queries;
using LogistiFlow.Fulfillment.Infrastructure.Persistence.Rows;

namespace LogistiFlow.Fulfillment.Infrastructure.Persistence.Repositories;

public sealed class FulfillmentJobRepository(IDbSession session) : IFulfillmentJobRepository
{
    public Task<FulfillmentJobEntity?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken) =>
        GetAsync(FulfillmentJobQueries.SelectByOrderId, new { OrderId = orderId }, cancellationToken);

    public Task<FulfillmentJobEntity?> GetByPublicIdAsync(Guid jobId, CancellationToken cancellationToken) =>
        GetAsync(FulfillmentJobQueries.SelectByPublicId, new { PublicId = jobId }, cancellationToken);

    private async Task<FulfillmentJobEntity?> GetAsync(string sql, object parameters, CancellationToken cancellationToken)
    {
        var connection = await session.GetConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<FulfillmentJobRow>(new CommandDefinition(
            sql,
            parameters,
            session.Transaction,
            cancellationToken: cancellationToken));
        if (row is null)
        {
            return null;
        }

        var lines = await connection.QueryAsync<FulfillmentJobLineRow>(new CommandDefinition(
            FulfillmentJobQueries.SelectLines,
            new { JobId = row.Id },
            session.Transaction,
            cancellationToken: cancellationToken));

        return FulfillmentJobEntity.Restore(
            row.Id,
            row.PublicId,
            row.OrderId,
            row.OrderNumber,
            row.WarehouseCode,
            row.ReservationId,
            Enum.Parse<FulfillmentJobStatus>(row.Status),
            row.StatusReason,
            row.Version,
            row.CreatedAt,
            row.UpdatedAt,
            lines.Select(line => new FulfillmentJobLineEntity(line.Sku, line.Quantity)));
    }

    public async Task<bool> TryAddAsync(FulfillmentJobEntity job, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);

        var connection = await session.GetConnectionAsync(cancellationToken);
        var id = await connection.QuerySingleOrDefaultAsync<long?>(new CommandDefinition(
            FulfillmentJobQueries.Insert,
            new
            {
                job.PublicId,
                job.OrderId,
                job.OrderNumber,
                job.WarehouseCode,
                job.ReservationId,
                Status = job.Status.ToString(),
                job.StatusReason,
                job.Version,
                job.CreatedAt,
                job.UpdatedAt,
            },
            session.Transaction,
            cancellationToken: cancellationToken));

        if (id is null)
        {
            return false;
        }

        job.AssignId(id.Value);
        await connection.ExecuteAsync(new CommandDefinition(
            FulfillmentJobQueries.InsertLine,
            job.Lines.Select(line => new { JobId = id.Value, line.Sku, line.Quantity }),
            session.Transaction,
            cancellationToken: cancellationToken));

        await InsertPendingStatusChangesAsync(job, cancellationToken);
        job.MarkPersisted(job.Version);
        return true;
    }

    public async Task<bool> TryUpdateStatusAsync(FulfillmentJobEntity job, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);

        var connection = await session.GetConnectionAsync(cancellationToken);
        var version = await connection.QuerySingleOrDefaultAsync<int?>(new CommandDefinition(
            FulfillmentJobQueries.UpdateStatus,
            new { job.Id, job.Version, Status = job.Status.ToString(), job.StatusReason, job.UpdatedAt },
            session.Transaction,
            cancellationToken: cancellationToken));

        if (version is null)
        {
            return false;
        }

        await InsertPendingStatusChangesAsync(job, cancellationToken);
        job.MarkPersisted(version.Value);
        return true;
    }

    private async Task InsertPendingStatusChangesAsync(FulfillmentJobEntity job, CancellationToken cancellationToken)
    {
        var connection = await session.GetConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            FulfillmentJobQueries.InsertStatusChange,
            job.PendingStatusChanges.Select(change => new
            {
                JobId = job.Id,
                FromStatus = change.FromStatus?.ToString(),
                ToStatus = change.ToStatus.ToString(),
                change.Reason,
                change.Source,
                change.OccurredAt,
            }),
            session.Transaction,
            cancellationToken: cancellationToken));
    }
}
