using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Shipping.Application.Interfaces;
using LogistiFlow.Shipping.Application.UseCases.Shipments.Get.Contracts;
using LogistiFlow.Shipping.Application.UseCases.Shipments.Get.Interfaces;
using LogistiFlow.Shipping.Application.UseCases.Shipments.Shared;
using LogistiFlow.Shipping.Domain.Errors;

namespace LogistiFlow.Shipping.Application.UseCases.Shipments.Get.Implementations;

public sealed class GetShipmentUseCase(IShippingQueries queries) : IGetShipmentUseCase
{
    public async Task<Result<ShipmentDetails>> ExecuteAsync(GetShipmentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var shipment = request switch
        {
            { ShipmentId: { } shipmentId, OrderId: null } => await queries.GetShipmentAsync(shipmentId, cancellationToken),
            { ShipmentId: null, OrderId: { } orderId } => await queries.GetShipmentByOrderAsync(orderId, cancellationToken),
            _ => null,
        };

        return shipment is null ? ShippingErrors.ShipmentNotFound : shipment;
    }
}
