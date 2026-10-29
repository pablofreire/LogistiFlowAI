using LogistiFlow.Shipping.Domain.Enums;

namespace LogistiFlow.Shipping.Application.UseCases.Shipments.Book.Contracts;

public sealed record BookShipmentResponse(ShipmentStatus Status, bool Booked, string? CarrierError);
