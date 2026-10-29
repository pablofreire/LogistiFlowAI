using FluentValidation;
using LogistiFlow.BuildingBlocks.Application.Persistence.Interfaces;
using LogistiFlow.BuildingBlocks.Application.Validation;
using LogistiFlow.BuildingBlocks.Domain.Results;
using LogistiFlow.Shipping.Application.UseCases.Exceptions.Resolve.Contracts;
using LogistiFlow.Shipping.Application.UseCases.Exceptions.Resolve.Interfaces;
using LogistiFlow.Shipping.Application.UseCases.Shipments.Shared;
using LogistiFlow.Shipping.Domain.Enums;
using LogistiFlow.Shipping.Domain.Errors;
using LogistiFlow.Shipping.Domain.Interfaces.Repositories;
using LogistiFlow.Shipping.Domain.Models;
using LogistiFlow.Shipping.Domain.Services;

namespace LogistiFlow.Shipping.Application.UseCases.Exceptions.Resolve.Implementations;

/// <summary>An operator closes an exception with a note, for example after contacting the customer about a return.</summary>
public sealed class ResolveShipmentExceptionUseCase(
    IValidator<ResolveShipmentExceptionRequest> validator,
    IShipmentRepository shipments,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IResolveShipmentExceptionUseCase
{
    public async Task<Result<ShipmentActionOutcome>> ExecuteAsync(ResolveShipmentExceptionRequest request, CancellationToken cancellationToken)
    {
        var validationError = await validator.ValidateToErrorAsync(request, cancellationToken);
        if (validationError != Error.None)
        {
            return validationError;
        }

        return await unitOfWork.ExecuteAsync<ShipmentActionOutcome>(
            async token =>
            {
                var shipment = await shipments.GetByExceptionIdAsync(request.ExceptionId, token);
                if (shipment is null)
                {
                    return ShippingErrors.ExceptionNotFound;
                }

                var resolved = shipment.ResolveException(request.ExceptionId, request.Resolution.Trim(), request.ResolvedBy, timeProvider.GetUtcNow());
                if (resolved.IsFailure)
                {
                    return resolved.Error;
                }

                // Resolving an exception does not stop the work in progress: booking or tracking continues right away.
                var schedule = shipment.Status switch
                {
                    ShipmentStatus.PendingBooking => ShipmentSchedule.BookIn(TimeSpan.Zero),
                    var status when ShipmentStateMachine.IsTracked(status) => ShipmentSchedule.PollIn(TimeSpan.Zero),
                    _ => ShipmentSchedule.None,
                };
                if (!await shipments.TryUpdateAsync(shipment, schedule, token))
                {
                    return ShippingErrors.ConcurrentModification;
                }

                return new ShipmentActionOutcome(shipment.PublicId, shipment.OrderNumber, shipment.Status, shipment.StatusReason, shipment.Version);
            },
            cancellationToken);
    }
}
