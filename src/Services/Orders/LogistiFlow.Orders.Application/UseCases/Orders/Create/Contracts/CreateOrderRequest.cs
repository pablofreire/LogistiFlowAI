namespace LogistiFlow.Orders.Application.UseCases.Orders.Create.Contracts;

public sealed record CreateOrderRequest(
    string IdempotencyKey,
    string CustomerName,
    string DestinationCity,
    string DestinationCountry,
    DateOnly? PromisedDeliveryDate,
    IReadOnlyList<CreateOrderLineRequest> Lines);
