using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;
using LogistiFlow.Shipping.Application.UseCases.Destinations.Record.Contracts;

namespace LogistiFlow.Shipping.Application.UseCases.Destinations.Record.Interfaces;

public interface IRecordOrderDestinationUseCase : IUseCase<RecordOrderDestinationRequest, RecordOrderDestinationResponse>;
