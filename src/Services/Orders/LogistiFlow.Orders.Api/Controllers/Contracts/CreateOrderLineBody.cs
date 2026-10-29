namespace LogistiFlow.Orders.Api.Controllers.Contracts;

public sealed record CreateOrderLineBody(string Sku, int Quantity);
