using LogistiFlow.Shipping.Domain.Enums;

namespace LogistiFlow.Shipping.Application.UseCases.Shipments.List.Contracts;

public sealed record ListShipmentsRequest(int? Page, int? PageSize, ShipmentStatus? Status, bool DelayedOnly);
