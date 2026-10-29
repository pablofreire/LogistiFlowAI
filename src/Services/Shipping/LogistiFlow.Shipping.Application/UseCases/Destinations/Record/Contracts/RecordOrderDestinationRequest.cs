namespace LogistiFlow.Shipping.Application.UseCases.Destinations.Record.Contracts;

public sealed record RecordOrderDestinationRequest(
    Guid OrderId,
    string OrderNumber,
    string? CustomerName,
    string? DestinationCity,
    string? DestinationCountry,
    DateOnly? PromisedDeliveryDate);
