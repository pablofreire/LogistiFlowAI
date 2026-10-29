using Dapper;
using LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Interfaces;
using LogistiFlow.Inventory.Domain.Enums;
using LogistiFlow.Inventory.Domain.Interfaces.Repositories;
using LogistiFlow.Inventory.Domain.ReadModels;
using LogistiFlow.Inventory.Infrastructure.Persistence.Queries;
using LogistiFlow.Inventory.Infrastructure.Persistence.Rows;

namespace LogistiFlow.Inventory.Infrastructure.Persistence.Repositories;

public sealed class StockRepository(IDbSession session) : IStockRepository
{
    public async Task<IReadOnlyList<WarehouseStockCandidate>> FindCandidateWarehousesAsync(
        IReadOnlyDictionary<string, int> requestedBySku,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requestedBySku);

        var connection = await session.GetConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<CandidatePositionRow>(new CommandDefinition(
            StockQueries.CandidatePositions,
            new { Skus = requestedBySku.Keys.ToArray(), Quantities = requestedBySku.Values.ToArray() },
            session.Transaction,
            cancellationToken: cancellationToken));

        return rows
            .GroupBy(row => row.WarehouseCode, StringComparer.Ordinal)
            .Where(group => group.Select(row => row.Sku).Distinct(StringComparer.Ordinal).Count() == requestedBySku.Count)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new WarehouseStockCandidate(
                group.Key,
                group.Select(row => new CandidateStockPosition(row.StockPositionId, row.Sku, row.Available)).ToList()))
            .ToList();
    }

    public async Task<bool> TryReserveAsync(long stockPositionId, int quantity, CancellationToken cancellationToken)
    {
        var connection = await session.GetConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(new CommandDefinition(
            StockQueries.TryReserve,
            new { Id = stockPositionId, Quantity = quantity },
            session.Transaction,
            cancellationToken: cancellationToken));
        return affected == 1;
    }

    public async Task ConsumeAsync(long stockPositionId, int quantity, CancellationToken cancellationToken)
    {
        var connection = await session.GetConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(new CommandDefinition(
            StockQueries.Consume,
            new { Id = stockPositionId, Quantity = quantity },
            session.Transaction,
            cancellationToken: cancellationToken));

        if (affected != 1)
        {
            throw new InvalidOperationException($"Stock position {stockPositionId} holds fewer than {quantity} reserved units to ship.");
        }
    }

    public async Task ReleaseAsync(long stockPositionId, int quantity, CancellationToken cancellationToken)
    {
        var connection = await session.GetConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(new CommandDefinition(
            StockQueries.Release,
            new { Id = stockPositionId, Quantity = quantity },
            session.Transaction,
            cancellationToken: cancellationToken));

        if (affected != 1)
        {
            throw new InvalidOperationException($"Stock position {stockPositionId} holds fewer than {quantity} reserved units.");
        }
    }

    public async Task<long?> ReceiveAsync(string warehouseCode, string sku, int quantity, CancellationToken cancellationToken)
    {
        var connection = await session.GetConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<long?>(new CommandDefinition(
            StockQueries.Receive,
            new { PublicId = Guid.CreateVersion7(), WarehouseCode = warehouseCode, Sku = sku, Quantity = quantity },
            session.Transaction,
            cancellationToken: cancellationToken));
    }

    public async Task RecordMovementAsync(
        long stockPositionId,
        StockMovementType type,
        int quantity,
        string referenceType,
        string referenceId,
        CancellationToken cancellationToken)
    {
        var connection = await session.GetConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            StockQueries.InsertMovement,
            new
            {
                StockPositionId = stockPositionId,
                MovementType = type.ToString(),
                Quantity = quantity,
                ReferenceType = referenceType,
                ReferenceId = referenceId,
            },
            session.Transaction,
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<SkuAvailability>> GetAvailabilityAsync(IReadOnlyCollection<string> skus, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(skus);

        var connection = await session.GetConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<SkuAvailabilityRow>(new CommandDefinition(
            StockQueries.Availability,
            new { Skus = skus.ToArray() },
            session.Transaction,
            cancellationToken: cancellationToken));
        return rows.Select(row => new SkuAvailability(row.Sku, row.BestWarehouseAvailable, row.NetworkAvailable)).ToList();
    }
}
