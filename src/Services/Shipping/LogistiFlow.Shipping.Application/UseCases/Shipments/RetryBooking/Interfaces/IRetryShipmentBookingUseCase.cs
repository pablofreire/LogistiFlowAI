using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;
using LogistiFlow.Shipping.Application.UseCases.Shipments.RetryBooking.Contracts;
using LogistiFlow.Shipping.Application.UseCases.Shipments.Shared;

namespace LogistiFlow.Shipping.Application.UseCases.Shipments.RetryBooking.Interfaces;

public interface IRetryShipmentBookingUseCase : IUseCase<RetryShipmentBookingRequest, ShipmentActionOutcome>;
