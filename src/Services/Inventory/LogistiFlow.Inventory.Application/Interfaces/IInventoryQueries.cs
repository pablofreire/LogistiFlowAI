using LogistiFlow.BuildingBlocks.Application.Paging.Models;
using LogistiFlow.Inventory.Application.UseCases.Availability.GetBySku.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Metrics.GetSummary.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Products.Shared;
using LogistiFlow.Inventory.Application.UseCases.Reservations.Shared;
using LogistiFlow.Inventory.Application.UseCases.Stock.Shared;
using LogistiFlow.Inventory.Application.UseCases.Warehouses.Shared;

namespace LogistiFlow.Inventory.Application.Interfaces;

/// <summary>Read-side projections of the Inventory service.</summary>
public interface IInventoryQueries
{
    Task<PagedResult<ProductItem>> ListProductsAsync(PageRequest page, string? search, CancellationToken cancellationToken);

    Task<IReadOnlyList<WarehouseItem>> ListWarehousesAsync(CancellationToken cancellationToken);

    Task<PagedResult<StockPositionItem>> ListStockAsync(PageRequest page, string? warehouseCode, string? sku, bool lowStockOnly, int lowStockThreshold, CancellationToken cancellationToken);

    Task<GetSkuAvailabilityResponse?> GetAvailabilityAsync(string sku, CancellationToken cancellationToken);

    Task<ReservationDetails?> GetReservationByOrderAsync(Guid orderId, CancellationToken cancellationToken);

    Task<GetInventoryMetricsResponse> GetMetricsAsync(int lowStockThreshold, int recentMovementLimit, CancellationToken cancellationToken);
}
