using FluentValidation;
using LogistiFlow.BuildingBlocks.Application.Persistence.Interfaces;
using LogistiFlow.BuildingBlocks.Application.Validation;
using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Inventory.Application.Interfaces;
using LogistiFlow.Inventory.Application.UseCases.Availability.GetBySku.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Stock.Receive.Contracts;
using LogistiFlow.Inventory.Application.UseCases.Stock.Receive.Interfaces;
using LogistiFlow.Inventory.Domain.Enums;
using LogistiFlow.Inventory.Domain.Errors;
using LogistiFlow.Inventory.Domain.Interfaces.Repositories;

namespace LogistiFlow.Inventory.Application.UseCases.Stock.Receive.Implementations;

/// <summary>Adds received units to on-hand stock and records the movement in the same transaction.</summary>
public sealed class ReceiveStockUseCase(
    IValidator<ReceiveStockRequest> validator,
    IStockRepository stock,
    IInventoryQueries queries,
    IUnitOfWork unitOfWork) : IReceiveStockUseCase
{
    public const string MovementReferenceType = "receipt";

    public async Task<Result<GetSkuAvailabilityResponse>> ExecuteAsync(ReceiveStockRequest request, CancellationToken cancellationToken)
    {
        var validationError = await validator.ValidateToErrorAsync(request, cancellationToken);
        if (validationError != Error.None)
        {
            return validationError;
        }

        var sku = request.Sku.Trim().ToUpperInvariant();
        var received = await unitOfWork.ExecuteAsync(
            async token =>
            {
                var positionId = await stock.ReceiveAsync(request.WarehouseCode.Trim().ToUpperInvariant(), sku, request.Quantity, token);
                if (positionId is null)
                {
                    return Result.Failure<long>(InventoryErrors.ProductNotFound with
                    {
                        Message = "The SKU or the warehouse does not exist.",
                    });
                }

                await stock.RecordMovementAsync(positionId.Value, StockMovementType.Receipt, request.Quantity, MovementReferenceType, request.Reference.Trim(), token);
                return Result.Success(positionId.Value);
            },
            cancellationToken);

        if (received.IsFailure)
        {
            return received.Error;
        }

        var availability = await queries.GetAvailabilityAsync(sku, cancellationToken);
        return availability is null ? InventoryErrors.ProductNotFound : availability;
    }
}
