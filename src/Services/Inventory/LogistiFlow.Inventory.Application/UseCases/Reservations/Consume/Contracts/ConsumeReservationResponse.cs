using LogistiFlow.Inventory.Domain.Enums;

namespace LogistiFlow.Inventory.Application.UseCases.Reservations.Consume.Contracts;

/// <summary>Status of the reservation after the call; <c>UnitsConsumed</c> is 0 when it had already been consumed.</summary>
public sealed record ConsumeReservationResponse(ReservationStatus Status, int UnitsConsumed);
