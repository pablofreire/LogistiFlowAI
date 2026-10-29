namespace LogistiFlow.Shipping.Application.UseCases.Metrics.GetSummary.Contracts;

/// <summary>Shipping KPIs from the Shipping database only.</summary>
/// <param name="TotalShipments">All shipments.</param>
/// <param name="ByStatus">Count per <c>ShipmentStatus</c>, including zeros.</param>
/// <param name="InTransit">Shipments the carrier currently holds (label created up to a failed attempt).</param>
/// <param name="Delivered">Delivered shipments.</param>
/// <param name="DelayedShipments">Undelivered shipments with an open delay exception.</param>
/// <param name="OpenExceptions">Open exceptions of every type.</param>
/// <param name="OpenExceptionsByType">Open exceptions per <c>ShipmentExceptionType</c>, including zeros.</param>
public sealed record GetShippingMetricsResponse(
    long TotalShipments,
    IReadOnlyDictionary<string, long> ByStatus,
    long InTransit,
    long Delivered,
    long DelayedShipments,
    long OpenExceptions,
    IReadOnlyDictionary<string, long> OpenExceptionsByType);
