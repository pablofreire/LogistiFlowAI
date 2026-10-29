namespace LogistiFlow.Shipping.Domain.Models;

/// <summary>Shipping's local copy of where an order goes, built from <c>OrderCreated</c>.</summary>
public sealed record OrderDestination(
    Guid OrderId,
    string OrderNumber,
    string? CustomerName,
    string DestinationCity,
    string DestinationCountry,
    DateOnly? PromisedDeliveryDate);
