using Dapper;
using LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Interfaces;
using LogistiFlow.Shipping.Application.Interfaces;
using LogistiFlow.Shipping.Application.Models;
using LogistiFlow.Shipping.Domain.Models;
using LogistiFlow.Shipping.Infrastructure.Persistence.Queries;
using LogistiFlow.Shipping.Infrastructure.Persistence.Rows;

namespace LogistiFlow.Shipping.Infrastructure.Persistence.WorkQueues;

public sealed class ShipmentWorkQueue(IDbSession session) : IShipmentWorkQueue
{
    public async Task<IReadOnlyList<ClaimedBooking>> ClaimDueBookingsAsync(int batchSize, TimeSpan lease, CancellationToken cancellationToken)
    {
        var connection = await session.GetConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<ClaimedBookingRow>(new CommandDefinition(
            ShipmentQueries.ClaimDueBookings,
            new { BatchSize = batchSize, LeaseSeconds = lease.TotalSeconds },
            session.Transaction,
            cancellationToken: cancellationToken));
        return rows.Select(row => new ClaimedBooking(row.ShipmentId, row.Attempt)).ToList();
    }

    public async Task<IReadOnlyList<Guid>> ClaimDueTrackingAsync(int batchSize, TimeSpan lease, CancellationToken cancellationToken)
    {
        var connection = await session.GetConnectionAsync(cancellationToken);
        var ids = await connection.QueryAsync<Guid>(new CommandDefinition(
            ShipmentQueries.ClaimDueTracking,
            new { BatchSize = batchSize, LeaseSeconds = lease.TotalSeconds },
            session.Transaction,
            cancellationToken: cancellationToken));
        return ids.ToList();
    }

    public async Task RescheduleAsync(Guid shipmentId, ShipmentSchedule schedule, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        var connection = await session.GetConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            ShipmentQueries.Reschedule,
            new
            {
                PublicId = shipmentId,
                NextBookingSeconds = schedule.NextBookingIn?.TotalSeconds,
                NextPollSeconds = schedule.NextPollIn?.TotalSeconds,
            },
            session.Transaction,
            cancellationToken: cancellationToken));
    }
}
