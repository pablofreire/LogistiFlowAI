namespace LogistiFlow.Orders.Application.UseCases.Orders.Create.Contracts;

public sealed record CreateOrderLineRequest(string Sku, int Quantity);
