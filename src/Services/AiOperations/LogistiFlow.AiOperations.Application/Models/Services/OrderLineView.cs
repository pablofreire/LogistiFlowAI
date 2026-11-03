namespace LogistiFlow.AiOperations.Application.Models.Services;

/// <summary>A line of an order as the Orders service returns it.</summary>
public sealed record OrderLineView(int LineNumber, string Sku, int Quantity);
