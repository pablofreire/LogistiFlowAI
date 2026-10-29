namespace LogistiFlow.Orders.Application.UseCases.Orders.Shared;

public sealed record OrderLineDetails(int LineNumber, string Sku, int Quantity);
