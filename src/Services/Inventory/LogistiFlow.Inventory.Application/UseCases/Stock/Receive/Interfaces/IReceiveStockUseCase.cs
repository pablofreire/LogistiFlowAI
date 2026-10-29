using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Availability.GetBySku.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Stock.Receive.Contracts;

namespace LogistiFlow.Inventory.Application.UseCases.Stock.Receive.Interfaces;

public interface IReceiveStockUseCase : IUseCase<ReceiveStockRequest, GetSkuAvailabilityResponse>;
