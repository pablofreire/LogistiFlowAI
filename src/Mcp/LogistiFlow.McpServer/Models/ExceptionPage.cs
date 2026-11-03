using LogistiFlow.McpServer.Contracts.Views;

namespace LogistiFlow.McpServer.Models;

/// <summary>One page of shipment exceptions and how many match in total.</summary>
public sealed record ExceptionPage(long TotalCount, IReadOnlyList<ShipmentExceptionView> Items);
