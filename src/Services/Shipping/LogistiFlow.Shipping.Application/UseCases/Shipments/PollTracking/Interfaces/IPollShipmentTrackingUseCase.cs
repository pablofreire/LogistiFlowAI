using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;
using LogistiFlow.Shipping.Application.UseCases.Shipments.PollTracking.Contracts;

namespace LogistiFlow.Shipping.Application.UseCases.Shipments.PollTracking.Interfaces;

public interface IPollShipmentTrackingUseCase : IUseCase<PollShipmentTrackingRequest, PollShipmentTrackingResponse>;
