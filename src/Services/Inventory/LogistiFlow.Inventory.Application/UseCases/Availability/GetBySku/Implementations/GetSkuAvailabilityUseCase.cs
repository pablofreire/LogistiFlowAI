using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Inventory.Application.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Availability.GetBySku.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Availability.GetBySku.Interfaces;
using LogistiFlow.Inventory.Domain.Errors;

namespace LogistiFlow.Inventory.Application.UseCases.Availability.GetBySku.Implementations;

public sealed class GetSkuAvailabilityUseCase(IInventoryQueries queries) : IGetSkuAvailabilityUseCase
{
    public async Task<Result<GetSkuAvailabilityResponse>> ExecuteAsync(GetSkuAvailabilityRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Sku) || request.Sku.Length > 40)
        {
            return InventoryErrors.ProductNotFound;
        }

        var availability = await queries.GetAvailabilityAsync(request.Sku.Trim().ToUpperInvariant(), cancellationToken);
        return availability is null ? InventoryErrors.ProductNotFound : availability;
    }
}
