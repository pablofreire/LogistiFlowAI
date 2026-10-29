using Dapper;
using LogistiFlow.BuildingBlocks.Application.Paging.Models;
using LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Interfaces;
using LogistiFlow.Inventory.Application.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Availability.GetBySku.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Metrics.GetSummary.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Products.Shared;
using LogistiFlow.Inventory.Application.UseCases.Reservations.Shared;
using LogistiFlow.Inventory.Application.UseCases.Stock.Shared;
using LogistiFlow.Inventory.Application.UseCases.Warehouses.Shared;
using LogistiFlow.Inventory.Domain.Enums;
using LogistiFlow.Inventory.Infrastructure.Persistence.Queries;
using LogistiFlow.Inventory.Infrastructure.Persistence.Rows;

namespace LogistiFlow.Inventory.Infrastructure.Persistence.Readers;

public sealed class InventoryQueryReader(IDbSession session) : IInventoryQueries
{
    public async Task<PagedResult<ProductItem>> ListProductsAsync(PageRequest page, string? search, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(page);

        var parameters = new { SearchPattern = search is null ? null : $"%{EscapeLike(search)}%", page.PageSize, page.Offset };
        var connection = await session.GetConnectionAsync(cancellationToken);
        var total = await connection.ExecuteScalarAsync<long>(Command(InventoryReadQueries.CountProducts, parameters, cancellationToken));
        var rows = total == 0 ? [] : await connection.QueryAsync<ProductRow>(Command(InventoryReadQueries.PageProducts, parameters, cancellationToken));

        var items = rows.Select(row => new ProductItem(row.Id, row.Sku, row.Name, row.Description, row.OnHand, row.Reserved, row.Available)).ToList();
        return new PagedResult<ProductItem>(items, page.Page, page.PageSize, total);
    }

    public async Task<IReadOnlyList<WarehouseItem>> ListWarehousesAsync(CancellationToken cancellationToken)
    {
        var connection = await session.GetConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<WarehouseRow>(Command(InventoryReadQueries.Warehouses, null, cancellationToken));
        return rows.Select(row => new WarehouseItem(row.Id, row.Code, row.Name, row.City, row.Country, row.OnHand, row.Reserved, row.Available)).ToList();
    }

    public async Task<PagedResult<StockPositionItem>> ListStockAsync(
        PageRequest page,
        string? warehouseCode,
        string? sku,
        bool lowStockOnly,
        int lowStockThreshold,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(page);

        var parameters = new
        {
            WarehouseCode = warehouseCode,
            Sku = sku,
            LowStockOnly = lowStockOnly,
            LowStockThreshold = lowStockThreshold,
            page.PageSize,
            page.Offset,
        };

        var connection = await session.GetConnectionAsync(cancellationToken);
        var total = await connection.ExecuteScalarAsync<long>(Command(InventoryReadQueries.CountStock, parameters, cancellationToken));
        var rows = total == 0 ? [] : await connection.QueryAsync<StockPositionRow>(Command(InventoryReadQueries.PageStock, parameters, cancellationToken));

        var items = rows
            .Select(row => new StockPositionItem(row.Id, row.Sku, row.ProductName, row.WarehouseCode, row.OnHand, row.Reserved, row.Available, row.UpdatedAt))
            .ToList();
        return new PagedResult<StockPositionItem>(items, page.Page, page.PageSize, total);
    }

    public async Task<GetSkuAvailabilityResponse?> GetAvailabilityAsync(string sku, CancellationToken cancellationToken)
    {
        var connection = await session.GetConnectionAsync(cancellationToken);
        var product = await connection.QuerySingleOrDefaultAsync<ProductNameRow>(Command(InventoryReadQueries.ProductBySku, new { Sku = sku }, cancellationToken));
        if (product is null)
        {
            return null;
        }

        var warehouses = (await connection.QueryAsync<WarehouseAvailabilityRow>(Command(InventoryReadQueries.AvailabilityBySku, new { Sku = sku }, cancellationToken)))
            .Select(row => new WarehouseAvailability(row.WarehouseCode, row.OnHand, row.Reserved, row.Available))
            .ToList();

        return new GetSkuAvailabilityResponse(product.Sku, product.Name, warehouses.Sum(warehouse => warehouse.Available), warehouses);
    }

    public async Task<ReservationDetails?> GetReservationByOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var connection = await session.GetConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<ReservationRow>(Command(InventoryReadQueries.ReservationByOrder, new { OrderId = orderId }, cancellationToken));
        if (row is null)
        {
            return null;
        }

        var lines = await connection.QueryAsync<ReservationLineRow>(Command(InventoryReadQueries.ReservationLines, new { ReservationId = row.Id }, cancellationToken));
        return new ReservationDetails(
            row.PublicId,
            row.OrderId,
            row.OrderNumber,
            row.WarehouseCode,
            Enum.Parse<ReservationStatus>(row.Status),
            row.FailureReason,
            lines.Select(line => new ReservationLineItem(line.Sku, line.Quantity)).ToList(),
            row.CreatedAt,
            row.UpdatedAt);
    }

    public async Task<GetInventoryMetricsResponse> GetMetricsAsync(int lowStockThreshold, int recentMovementLimit, CancellationToken cancellationToken)
    {
        var connection = await session.GetConnectionAsync(cancellationToken);
        var totals = await connection.QuerySingleAsync<TotalsRow>(Command(InventoryReadQueries.Totals, new { LowStockThreshold = lowStockThreshold }, cancellationToken));

        var counts = (await connection.QueryAsync<StatusCountRow>(Command(InventoryReadQueries.ReservationsByStatus, null, cancellationToken)))
            .ToDictionary(row => row.Status, row => row.Count, StringComparer.Ordinal);
        var byStatus = Enum.GetValues<ReservationStatus>()
            .ToDictionary(status => status.ToString(), status => counts.GetValueOrDefault(status.ToString()), StringComparer.Ordinal);

        var movements = (await connection.QueryAsync<MovementRow>(Command(InventoryReadQueries.RecentMovements, new { Limit = recentMovementLimit }, cancellationToken)))
            .Select(row => new RecentStockMovement(
                row.Sku,
                row.WarehouseCode,
                Enum.Parse<StockMovementType>(row.MovementType),
                row.Quantity,
                row.ReferenceType,
                row.ReferenceId,
                row.OccurredAt))
            .ToList();

        return new GetInventoryMetricsResponse(
            totals.Products,
            totals.Warehouses,
            totals.TotalOnHand,
            totals.TotalReserved,
            totals.TotalAvailable,
            totals.LowStockPositions,
            lowStockThreshold,
            byStatus,
            movements);
    }

    private CommandDefinition Command(string sql, object? parameters, CancellationToken cancellationToken) =>
        new(sql, parameters, session.Transaction, cancellationToken: cancellationToken);

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}
