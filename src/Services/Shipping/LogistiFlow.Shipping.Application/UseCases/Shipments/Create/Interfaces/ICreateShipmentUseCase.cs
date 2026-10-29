using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;
using LogistiFlow.Shipping.Application.UseCases.Shipments.Create.Contracts;

namespace LogistiFlow.Shipping.Application.UseCases.Shipments.Create.Interfaces;

public interface ICreateShipmentUseCase : IUseCase<CreateShipmentRequest, CreateShipmentResponse>;
