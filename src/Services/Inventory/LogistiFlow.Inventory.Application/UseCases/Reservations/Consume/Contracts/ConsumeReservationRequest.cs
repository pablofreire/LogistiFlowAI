namespace LogistiFlow.Inventory.Application.UseCases.Reservations.Consume.Contracts;

public sealed record ConsumeReservationRequest(Guid OrderId, string OrderNumber);
