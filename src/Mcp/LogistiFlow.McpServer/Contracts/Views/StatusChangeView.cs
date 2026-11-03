namespace LogistiFlow.McpServer.Contracts.Views;

/// <summary>A status change in an order history.</summary>
public sealed record StatusChangeView(
    string? FromStatus,
    string ToStatus,
    string? Reason,
    string Source,
    DateTimeOffset OccurredAt);
