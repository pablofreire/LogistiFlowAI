using LogistiFlow.AiOperations.Application.Models;
using LogistiFlow.AiOperations.Application.Models.Services;

namespace LogistiFlow.AiOperations.Application.Interfaces;

/// <summary>Read access to the Shipping service, as the calling user.</summary>
public interface IShippingApi
{
    Task<ServiceReply<ShipmentView>> GetShipmentByOrderAsync(Guid orderId, CancellationToken cancellationToken);

    Task<ServiceReply<ShipmentView>> GetShipmentAsync(Guid shipmentId, CancellationToken cancellationToken);

    /// <summary>One page of shipments, newest first; <paramref name="delayedOnly"/> keeps those with an open delay.</summary>
    Task<ServiceReply<PageView<ShipmentListItemView>>> ListShipmentsAsync(bool delayedOnly, int pageSize, CancellationToken cancellationToken);

    /// <summary>One page of exceptions, newest first.</summary>
    Task<ServiceReply<PageView<ShipmentExceptionView>>> ListExceptionsAsync(int pageSize, CancellationToken cancellationToken);
}
