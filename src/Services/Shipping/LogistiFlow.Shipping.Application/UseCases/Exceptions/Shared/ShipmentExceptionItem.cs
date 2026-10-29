using LogistiFlow.Shipping.Domain.Enums;

namespace LogistiFlow.Shipping.Application.UseCases.Exceptions.Shared;

public sealed record ShipmentExceptionItem(
    Guid Id,
    Guid ShipmentId,
    string OrderNumber,
    string CarrierCode,
    string? TrackingNumber,
    ShipmentExceptionType Type,
    ShipmentExceptionStatus Status,
    string Details,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ResolvedAt,
    string? Resolution,
    string? ResolvedBy);
