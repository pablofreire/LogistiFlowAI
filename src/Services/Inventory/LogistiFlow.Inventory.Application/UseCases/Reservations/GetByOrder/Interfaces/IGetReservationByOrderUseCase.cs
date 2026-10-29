using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Reservations.GetByOrder.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Reservations.Shared;

namespace LogistiFlow.Inventory.Application.UseCases.Reservations.GetByOrder.Interfaces;

public interface IGetReservationByOrderUseCase : IUseCase<GetReservationByOrderRequest, ReservationDetails>;
