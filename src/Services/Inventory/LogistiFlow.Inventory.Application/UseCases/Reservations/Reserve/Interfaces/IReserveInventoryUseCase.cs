using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Reservations.Reserve.Contracts;

namespace LogistiFlow.Inventory.Application.UseCases.Reservations.Reserve.Interfaces;

public interface IReserveInventoryUseCase : IUseCase<ReserveInventoryRequest, ReserveInventoryResponse>;
