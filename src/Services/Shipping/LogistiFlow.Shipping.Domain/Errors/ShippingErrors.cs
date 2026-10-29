using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Shipping.Domain.Enums;

namespace LogistiFlow.Shipping.Domain.Errors;

public static class ShippingErrors
{
    public static readonly Error ShipmentNotFound = Error.NotFound("shipping.shipment_not_found", "The shipment does not exist.");

    public static readonly Error ExceptionNotFound = Error.NotFound("shipping.exception_not_found", "The shipment exception does not exist.");

    public static readonly Error ExceptionAlreadyResolved = Error.Conflict(
        "shipping.exception_already_resolved",
        "The shipment exception is already resolved.");

    public static readonly Error ConcurrentModification = Error.Conflict(
        "shipping.concurrent_modification",
        "The shipment was changed by someone else. Reload it and try again.");

    public static Error InvalidTransition(ShipmentStatus from, ShipmentStatus to) => Error.Conflict(
        "shipping.invalid_transition",
        $"A shipment in status {from} cannot move to {to}.");
}
