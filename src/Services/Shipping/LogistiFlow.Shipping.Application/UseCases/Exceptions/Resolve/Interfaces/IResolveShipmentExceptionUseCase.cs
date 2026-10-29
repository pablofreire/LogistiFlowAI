using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;
using LogistiFlow.Shipping.Application.UseCases.Exceptions.Resolve.Contracts;
using LogistiFlow.Shipping.Application.UseCases.Shipments.Shared;

namespace LogistiFlow.Shipping.Application.UseCases.Exceptions.Resolve.Interfaces;

public interface IResolveShipmentExceptionUseCase : IUseCase<ResolveShipmentExceptionRequest, ShipmentActionOutcome>;
