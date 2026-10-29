namespace LogistiFlow.Orders.Application.UseCases.Orders.Cancel.Contracts;

public sealed record CancelOrderRequest(Guid OrderId, string Reason);
