using LogistiFlow.Inventory.Domain.Enums;

namespace LogistiFlow.Inventory.Application.UseCases.Reservations.Reserve.Contracts;

/// <summary>
/// The reservation outcome. <c>AlreadyProcessed</c> is true when a reservation (or a cancellation tombstone)
/// already existed for the order, in which case nothing changed.
/// </summary>
public sealed record ReserveInventoryResponse(ReservationStatus Status, string? WarehouseCode, string? FailureReason, bool AlreadyProcessed);
