using LogistiFlow.Shipping.Domain.Enums;

namespace LogistiFlow.Shipping.Application.UseCases.Shipments.Shared;

public sealed record ShipmentActionOutcome(Guid ShipmentId, string OrderNumber, ShipmentStatus Status, string? StatusReason, int Version);
