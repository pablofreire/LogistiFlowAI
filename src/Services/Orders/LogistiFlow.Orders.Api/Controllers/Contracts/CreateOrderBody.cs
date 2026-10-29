namespace LogistiFlow.Orders.Api.Controllers.Contracts;

/// <summary>Body of <c>POST /api/orders</c>. The idempotency key travels in the <c>Idempotency-Key</c> header.</summary>
public sealed record CreateOrderBody(
    string CustomerName,
    string DestinationCity,
    string DestinationCountry,
    DateOnly? PromisedDeliveryDate,
    IReadOnlyList<CreateOrderLineBody> Lines);
