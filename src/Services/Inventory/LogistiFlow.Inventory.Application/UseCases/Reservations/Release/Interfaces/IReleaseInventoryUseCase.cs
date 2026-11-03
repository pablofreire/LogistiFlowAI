using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Reservations.Release.Contracts;

namespace LogistiFlow.Inventory.Application.UseCases.Reservations.Release.Interfaces;

public interface IReleaseInventoryUseCase : IUseCase<ReleaseInventoryRequest, ReleaseInventoryResponse>;
