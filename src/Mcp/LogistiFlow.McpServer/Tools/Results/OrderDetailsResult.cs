using LogistiFlow.McpServer.Contracts.Views;

namespace LogistiFlow.McpServer.Tools.Results;

/// <summary>An order with its full status history.</summary>
public sealed record OrderDetailsResult(OrderView Order, IReadOnlyList<StatusChangeView> History);
