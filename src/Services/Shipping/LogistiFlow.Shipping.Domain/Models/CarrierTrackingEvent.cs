namespace LogistiFlow.Shipping.Domain.Models;

/// <param name="CarrierEventId">The carrier's id for the event; unique per shipment.</param>
/// <param name="Code">Carrier event code such as <c>OUT_FOR_DELIVERY</c>.</param>
/// <param name="Description">The carrier's text.</param>
/// <param name="Location">Where it happened.</param>
/// <param name="OccurredAt">When it happened, by the carrier's clock.</param>
/// <param name="EstimatedDelivery">The carrier's delivery estimate after the event.</param>
public sealed record CarrierTrackingEvent(
    string CarrierEventId,
    string Code,
    string Description,
    string Location,
    DateTimeOffset OccurredAt,
    DateOnly? EstimatedDelivery);
