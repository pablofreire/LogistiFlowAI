using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Availability.GetBySku.Contracts;

namespace LogistiFlow.Inventory.Application.UseCases.Availability.GetBySku.Interfaces;

public interface IGetSkuAvailabilityUseCase : IUseCase<GetSkuAvailabilityRequest, GetSkuAvailabilityResponse>;
