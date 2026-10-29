using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;
using LogistiFlow.Shipping.Application.UseCases.Shipments.Get.Contracts;
using LogistiFlow.Shipping.Application.UseCases.Shipments.Shared;

namespace LogistiFlow.Shipping.Application.UseCases.Shipments.Get.Interfaces;

public interface IGetShipmentUseCase : IUseCase<GetShipmentRequest, ShipmentDetails>;
