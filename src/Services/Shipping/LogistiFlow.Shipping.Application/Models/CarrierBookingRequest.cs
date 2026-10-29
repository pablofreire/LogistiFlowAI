namespace LogistiFlow.Shipping.Application.Models;

/// <param name="IdempotencyKey">The shipment public id: a retried booking must never create a second carrier shipment.</param>
/// <param name="CarrierCode">The assigned carrier.</param>
/// <param name="Reference">Our reference for the carrier (the order number).</param>
/// <param name="Origin">Origin warehouse code.</param>
/// <param name="DestinationCity">Destination city.</param>
/// <param name="DestinationCountry">Destination country (ISO alpha-2).</param>
public sealed record CarrierBookingRequest(
    string IdempotencyKey,
    string CarrierCode,
    string Reference,
    string Origin,
    string DestinationCity,
    string DestinationCountry);
