using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Warehouses.List.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Warehouses.Shared;

namespace LogistiFlow.Inventory.Application.UseCases.Warehouses.List.Interfaces;

public interface IListWarehousesUseCase : IUseCase<ListWarehousesRequest, IReadOnlyList<WarehouseItem>>;
