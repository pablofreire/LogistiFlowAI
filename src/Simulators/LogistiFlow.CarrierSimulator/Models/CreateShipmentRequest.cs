namespace LogistiFlow.CarrierSimulator.Models;

/// <param name="CarrierCode">One of the simulated carriers: <c>SIMX</c>, <c>RDRN</c>, <c>GLBL</c>.</param>
/// <param name="Reference">The shipper's reference, usually the order number.</param>
/// <param name="Origin">Origin facility code.</param>
/// <param name="DestinationCity">Destination city; sandbox cities select a scenario.</param>
/// <param name="DestinationCountry">ISO 3166-1 alpha-2 code.</param>
public sealed record CreateShipmentRequest(
    string CarrierCode,
    string Reference,
    string Origin,
    string DestinationCity,
    string DestinationCountry);
