using LogistiFlow.McpServer.Contracts.Views;

namespace LogistiFlow.McpServer.Tools.Results;

/// <summary>Shipment exceptions matching the filters, newest first, and how many match in total.</summary>
public sealed record ExceptionListResult(string? Status, string? Type, long TotalCount, IReadOnlyList<ShipmentExceptionView> Items);
