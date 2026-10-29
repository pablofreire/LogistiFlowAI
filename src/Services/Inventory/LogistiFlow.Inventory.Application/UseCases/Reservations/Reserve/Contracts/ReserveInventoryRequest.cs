using LogistiFlow.Contracts.IntegrationEvents.Models;

namespace LogistiFlow.Inventory.Application.UseCases.Reservations.Reserve.Contracts;

public sealed record ReserveInventoryRequest(Guid OrderId, string OrderNumber, IReadOnlyList<OrderLineItem> Lines);
