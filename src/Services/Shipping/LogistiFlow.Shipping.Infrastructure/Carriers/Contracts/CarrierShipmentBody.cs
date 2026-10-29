namespace LogistiFlow.Shipping.Infrastructure.Carriers.Contracts;

/// <summary>Wire format of the carrier's create-shipment request.</summary>
internal sealed record CarrierShipmentBody(
    string CarrierCode,
    string Reference,
    string Origin,
    string DestinationCity,
    string DestinationCountry);
