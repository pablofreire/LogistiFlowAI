using Dapper;
using LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Interfaces;
using LogistiFlow.Shipping.Domain.Interfaces.Repositories;
using LogistiFlow.Shipping.Domain.Models;
using LogistiFlow.Shipping.Infrastructure.Persistence.Queries;
using LogistiFlow.Shipping.Infrastructure.Persistence.Rows;

namespace LogistiFlow.Shipping.Infrastructure.Persistence.Repositories;

public sealed class OrderDestinationRepository(IDbSession session) : IOrderDestinationRepository
{
    public async Task<OrderDestination?> GetAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var connection = await session.GetConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<OrderDestinationRow>(new CommandDefinition(
            OrderDestinationQueries.Select,
            new { OrderId = orderId },
            session.Transaction,
            cancellationToken: cancellationToken));

        return row is null
            ? null
            : new OrderDestination(
                row.OrderId,
                row.OrderNumber,
                row.CustomerName,
                row.DestinationCity,
                row.DestinationCountry.Trim(),
                row.PromisedDeliveryDate is { } date ? DateOnly.FromDateTime(date) : null);
    }

    public async Task AddIfMissingAsync(OrderDestination destination, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(destination);

        var connection = await session.GetConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            OrderDestinationQueries.InsertIfMissing,
            new
            {
                destination.OrderId,
                destination.OrderNumber,
                destination.CustomerName,
                destination.DestinationCity,
                destination.DestinationCountry,
                PromisedDeliveryDate = destination.PromisedDeliveryDate?.ToDateTime(TimeOnly.MinValue),
            },
            session.Transaction,
            cancellationToken: cancellationToken));
    }
}
