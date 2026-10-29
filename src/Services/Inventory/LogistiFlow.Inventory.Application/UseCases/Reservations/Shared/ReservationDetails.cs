using LogistiFlow.Inventory.Domain.Enums;

namespace LogistiFlow.Inventory.Application.UseCases.Reservations.Shared;

public sealed record ReservationDetails(
    Guid Id,
    Guid OrderId,
    string OrderNumber,
    string? WarehouseCode,
    ReservationStatus Status,
    string? FailureReason,
    IReadOnlyList<ReservationLineItem> Lines,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
