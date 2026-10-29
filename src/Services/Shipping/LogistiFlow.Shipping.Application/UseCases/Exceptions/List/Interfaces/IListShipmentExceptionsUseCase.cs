using LogistiFlow.BuildingBlocks.Application.Paging.Models;
using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;
using LogistiFlow.Shipping.Application.UseCases.Exceptions.List.Contracts;
using LogistiFlow.Shipping.Application.UseCases.Exceptions.Shared;

namespace LogistiFlow.Shipping.Application.UseCases.Exceptions.List.Interfaces;

public interface IListShipmentExceptionsUseCase : IUseCase<ListShipmentExceptionsRequest, PagedResult<ShipmentExceptionItem>>;
