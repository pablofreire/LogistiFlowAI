using LogistiFlow.Inventory.Domain.Enums;

namespace LogistiFlow.Inventory.Application.UseCases.Reservations.Release.Contracts;

/// <summary>Status of the order's reservation after the call; <c>UnitsReleased</c> is 0 when nothing was held.</summary>
public sealed record ReleaseInventoryResponse(ReservationStatus Status, int UnitsReleased);
