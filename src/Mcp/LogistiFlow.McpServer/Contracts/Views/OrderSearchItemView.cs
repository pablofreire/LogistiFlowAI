namespace LogistiFlow.McpServer.Contracts.Views;

/// <summary>An order row of the order search.</summary>
public sealed record OrderSearchItemView(
    Guid Id,
    string OrderNumber,
    string Status);
