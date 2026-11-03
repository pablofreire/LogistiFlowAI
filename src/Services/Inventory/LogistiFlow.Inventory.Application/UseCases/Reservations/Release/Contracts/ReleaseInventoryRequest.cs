namespace LogistiFlow.Inventory.Application.UseCases.Reservations.Release.Contracts;

public sealed record ReleaseInventoryRequest(Guid OrderId, string OrderNumber);
