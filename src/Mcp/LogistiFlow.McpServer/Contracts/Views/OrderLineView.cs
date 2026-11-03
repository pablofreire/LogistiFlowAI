namespace LogistiFlow.McpServer.Contracts.Views;

/// <summary>A line of an order.</summary>
public sealed record OrderLineView(
    int LineNumber,
    string Sku,
    int Quantity);
