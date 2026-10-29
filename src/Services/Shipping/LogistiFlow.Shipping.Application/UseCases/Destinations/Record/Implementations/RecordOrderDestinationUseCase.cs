using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Shipping.Application.UseCases.Destinations.Record.Contracts;
using LogistiFlow.Shipping.Application.UseCases.Destinations.Record.Interfaces;
using LogistiFlow.Shipping.Domain.Interfaces.Repositories;
using LogistiFlow.Shipping.Domain.Models;

namespace LogistiFlow.Shipping.Application.UseCases.Destinations.Record.Implementations;

/// <summary>Keeps Shipping's local copy of the order destination (event-carried state transfer from Orders).</summary>
public sealed class RecordOrderDestinationUseCase(IOrderDestinationRepository destinations) : IRecordOrderDestinationUseCase
{
    public async Task<Result<RecordOrderDestinationResponse>> ExecuteAsync(RecordOrderDestinationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.DestinationCity) || string.IsNullOrWhiteSpace(request.DestinationCountry))
        {
            return new RecordOrderDestinationResponse(Recorded: false);
        }

        await destinations.AddIfMissingAsync(
            new OrderDestination(
                request.OrderId,
                request.OrderNumber,
                request.CustomerName,
                request.DestinationCity.Trim(),
                request.DestinationCountry.Trim().ToUpperInvariant(),
                request.PromisedDeliveryDate),
            cancellationToken);
        return new RecordOrderDestinationResponse(Recorded: true);
    }
}
