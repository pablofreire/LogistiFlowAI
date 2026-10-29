using LogistiFlow.Shipping.Domain.Enums;

namespace LogistiFlow.Shipping.Application.UseCases.Exceptions.List.Contracts;

public sealed record ListShipmentExceptionsRequest(int? Page, int? PageSize, ShipmentExceptionStatus? Status, ShipmentExceptionType? Type);
