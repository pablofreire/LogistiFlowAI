using LogistiFlow.BuildingBlocks.Application.Messaging.Interfaces;
using LogistiFlow.BuildingBlocks.Application.Persistence.Interfaces;
using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Contracts.IntegrationEvents.Inventory;
using LogistiFlow.Contracts.IntegrationEvents.Models;
using LogistiFlow.Inventory.Application.UseCases.Reservations.Reserve.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Reservations.Reserve.Interfaces;
using LogistiFlow.Inventory.Domain.Entities;
using LogistiFlow.Inventory.Domain.Enums;
using LogistiFlow.Inventory.Domain.Errors;
using LogistiFlow.Inventory.Domain.Interfaces.Repositories;
using LogistiFlow.Inventory.Domain.ReadModels;
using Microsoft.Extensions.Logging;

namespace LogistiFlow.Inventory.Application.UseCases.Reservations.Reserve.Implementations;

/// <summary>
/// Reserves every line of an order in a single warehouse, all or nothing.
/// <para>
/// Overselling is prevented by the database, not by the read: each line is reserved with a conditional
/// <c>UPDATE ... WHERE on_hand - reserved &gt;= qty</c>. If a concurrent order took the stock between the read and
/// the update, the lines already reserved in this transaction are given back and the next candidate warehouse is
/// tried. Lines are reserved in stock-position order so two orders never lock the same rows in opposite order.
/// </para>
/// </summary>
public sealed partial class ReserveInventoryUseCase(
    IStockRepository stock,
    IReservationRepository reservations,
    IIntegrationEventOutbox outbox,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<ReserveInventoryUseCase> logger) : IReserveInventoryUseCase
{
    public const string MovementReferenceType = "order";

    public Task<Result<ReserveInventoryResponse>> ExecuteAsync(ReserveInventoryRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return unitOfWork.ExecuteAsync(token => ReserveAsync(request, token), cancellationToken);
    }

    private async Task<Result<ReserveInventoryResponse>> ReserveAsync(ReserveInventoryRequest request, CancellationToken cancellationToken)
    {
        var existing = await reservations.GetByOrderIdAsync(request.OrderId, cancellationToken);
        if (existing is not null)
        {
            return new ReserveInventoryResponse(existing.Status, existing.WarehouseCode, existing.FailureReason, AlreadyProcessed: true);
        }

        var requested = request.Lines
            .GroupBy(line => line.Sku.ToUpperInvariant(), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Sum(line => line.Quantity), StringComparer.Ordinal);

        var now = timeProvider.GetUtcNow();
        var candidates = await stock.FindCandidateWarehousesAsync(requested, cancellationToken);
        foreach (var candidate in candidates)
        {
            var reservedLines = await TryReserveInWarehouseAsync(candidate, requested, cancellationToken);
            if (reservedLines is null)
            {
                LogContention(logger, request.OrderNumber, candidate.WarehouseCode);
                continue;
            }

            var reservation = ReservationEntity.Reserved(request.OrderId, request.OrderNumber, candidate.WarehouseCode, reservedLines, now);
            if (!await reservations.TryAddAsync(reservation, cancellationToken))
            {
                throw new InvalidOperationException($"A reservation for order {request.OrderNumber} was created concurrently.");
            }

            foreach (var line in reservedLines)
            {
                await stock.RecordMovementAsync(line.StockPositionId, StockMovementType.Reservation, line.Quantity, MovementReferenceType, request.OrderNumber, cancellationToken);
            }

            await outbox.EnqueueAsync(
                new InventoryReservedEvent(
                    reservation.PublicId,
                    request.OrderId,
                    request.OrderNumber,
                    candidate.WarehouseCode,
                    reservedLines.Select(line => new OrderLineItem(line.Sku, line.Quantity)).ToList(),
                    now),
                cancellationToken);

            return new ReserveInventoryResponse(ReservationStatus.Reserved, candidate.WarehouseCode, null, AlreadyProcessed: false);
        }

        return await FailAsync(request, requested, now, cancellationToken);
    }

    /// <summary>Returns the reserved lines, or <c>null</c> after undoing partial work when any line lost a race.</summary>
    private async Task<List<ReservationLineEntity>?> TryReserveInWarehouseAsync(
        WarehouseStockCandidate candidate,
        Dictionary<string, int> requested,
        CancellationToken cancellationToken)
    {
        var reserved = new List<ReservationLineEntity>();
        foreach (var position in candidate.Positions.OrderBy(position => position.StockPositionId))
        {
            var quantity = requested[position.Sku];
            if (await stock.TryReserveAsync(position.StockPositionId, quantity, cancellationToken))
            {
                reserved.Add(new ReservationLineEntity(position.StockPositionId, position.Sku, quantity));
                continue;
            }

            // This transaction still holds the row locks of the lines it reserved, so undoing them is safe.
            foreach (var line in reserved)
            {
                await stock.ReleaseAsync(line.StockPositionId, line.Quantity, cancellationToken);
            }

            return null;
        }

        return reserved;
    }

    private async Task<Result<ReserveInventoryResponse>> FailAsync(
        ReserveInventoryRequest request,
        Dictionary<string, int> requested,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var availability = (await stock.GetAvailabilityAsync(requested.Keys.ToList(), cancellationToken))
            .ToDictionary(item => item.Sku, StringComparer.Ordinal);

        var shortages = requested
            .Select(pair => new InventoryShortage(pair.Key, pair.Value, availability.TryGetValue(pair.Key, out var item) ? item.BestWarehouseAvailable : 0))
            .Where(shortage => shortage.Available < shortage.Requested)
            .ToList();

        var reason = shortages.Count > 0 ? InventoryErrors.InsufficientInventoryReason : InventoryErrors.SplitAcrossWarehousesReason;
        var reservation = ReservationEntity.Failed(request.OrderId, request.OrderNumber, reason, now);
        if (!await reservations.TryAddAsync(reservation, cancellationToken))
        {
            throw new InvalidOperationException($"A reservation for order {request.OrderNumber} was created concurrently.");
        }

        await outbox.EnqueueAsync(
            new InventoryReservationFailedEvent(request.OrderId, request.OrderNumber, reason, shortages, now),
            cancellationToken);

        LogFailed(logger, request.OrderNumber, reason);
        return new ReserveInventoryResponse(ReservationStatus.Failed, null, reason, AlreadyProcessed: false);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Order {OrderNumber} lost a stock race in {WarehouseCode}; trying the next warehouse")]
    private static partial void LogContention(ILogger logger, string orderNumber, string warehouseCode);

    [LoggerMessage(Level = LogLevel.Information, Message = "Reservation failed for order {OrderNumber}: {Reason}")]
    private static partial void LogFailed(ILogger logger, string orderNumber, string reason);
}
