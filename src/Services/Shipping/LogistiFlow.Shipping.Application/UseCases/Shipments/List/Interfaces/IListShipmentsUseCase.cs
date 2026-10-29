using LogistiFlow.BuildingBlocks.Application.Paging.Models;
using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;
using LogistiFlow.Shipping.Application.UseCases.Shipments.List.Contracts;
using LogistiFlow.Shipping.Application.UseCases.Shipments.Shared;

namespace LogistiFlow.Shipping.Application.UseCases.Shipments.List.Interfaces;

public interface IListShipmentsUseCase : IUseCase<ListShipmentsRequest, PagedResult<ShipmentSummary>>;
