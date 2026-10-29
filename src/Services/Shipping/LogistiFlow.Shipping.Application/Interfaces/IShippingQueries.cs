using LogistiFlow.BuildingBlocks.Application.Paging.Models;
using LogistiFlow.Shipping.Application.UseCases.Exceptions.Shared;
using LogistiFlow.Shipping.Application.UseCases.Metrics.GetSummary.Contracts;
using LogistiFlow.Shipping.Application.UseCases.Shipments.Shared;
using LogistiFlow.Shipping.Domain.Enums;

namespace LogistiFlow.Shipping.Application.Interfaces;

/// <summary>Read side for the API: projections straight from SQL, no aggregates.</summary>
public interface IShippingQueries
{
    Task<PagedResult<ShipmentSummary>> ListShipmentsAsync(PageRequest page, ShipmentStatus? status, bool delayedOnly, CancellationToken cancellationToken);

    Task<ShipmentDetails?> GetShipmentAsync(Guid shipmentId, CancellationToken cancellationToken);

    Task<ShipmentDetails?> GetShipmentByOrderAsync(Guid orderId, CancellationToken cancellationToken);

    Task<PagedResult<ShipmentExceptionItem>> ListExceptionsAsync(
        PageRequest page,
        ShipmentExceptionStatus? status,
        ShipmentExceptionType? type,
        CancellationToken cancellationToken);

    Task<GetShippingMetricsResponse> GetMetricsAsync(CancellationToken cancellationToken);
}
