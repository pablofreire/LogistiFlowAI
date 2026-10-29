using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Reservations.Consume.Contracts;

namespace LogistiFlow.Inventory.Application.UseCases.Reservations.Consume.Interfaces;

public interface IConsumeReservationUseCase : IUseCase<ConsumeReservationRequest, ConsumeReservationResponse>;
