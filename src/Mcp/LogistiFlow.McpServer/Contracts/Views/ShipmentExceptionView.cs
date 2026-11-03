namespace LogistiFlow.McpServer.Contracts.Views;

/// <summary>An operational exception on a shipment.</summary>
public sealed record ShipmentExceptionView(
    Guid Id,
    string OrderNumber,
    string CarrierCode,
    string? TrackingNumber,
    string Type,
    string Status,
    string Details,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ResolvedAt);
