using LogistiFlow.Shipping.Application.Interfaces;
using LogistiFlow.Shipping.Application.Options;
using LogistiFlow.Shipping.Application.UseCases.Shipments.Book.Contracts;
using LogistiFlow.Shipping.Application.UseCases.Shipments.Book.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogistiFlow.Shipping.Infrastructure.Workers;

/// <summary>Claims shipments due for a carrier booking and books them one by one, each in its own scope.</summary>
public sealed partial class CarrierBookingWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ShippingWorkerOptions> options,
    ILogger<CarrierBookingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var processed = 0;
            try
            {
                processed = await ProcessDueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
#pragma warning disable CA1031 // The worker must survive database outages and keep polling.
            catch (Exception exception)
#pragma warning restore CA1031
            {
                LogCycleFailed(logger, exception);
            }

            if (processed > 0)
            {
                continue;
            }

            try
            {
                await Task.Delay(options.Value.IdleDelay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task<int> ProcessDueAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Application.Models.ClaimedBooking> claimed;
        await using (var claimScope = scopeFactory.CreateAsyncScope())
        {
            var queue = claimScope.ServiceProvider.GetRequiredService<IShipmentWorkQueue>();
            claimed = await queue.ClaimDueBookingsAsync(options.Value.BatchSize, options.Value.Lease, cancellationToken);
        }

        foreach (var booking in claimed)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var useCase = scope.ServiceProvider.GetRequiredService<IBookShipmentUseCase>();
            var result = await useCase.ExecuteAsync(new BookShipmentRequest(booking.ShipmentId, booking.Attempt), cancellationToken);
            if (result.IsFailure)
            {
                LogBookingNotStored(logger, booking.ShipmentId, result.Error.Code);
            }
            else if (result.Value.CarrierError is { } error)
            {
                LogCarrierFailed(logger, booking.ShipmentId, booking.Attempt, error);
            }
        }

        return claimed.Count;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Carrier booking for shipment {ShipmentId} failed on attempt {Attempt}: {Error}")]
    private static partial void LogCarrierFailed(ILogger logger, Guid shipmentId, int attempt, string error);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Booking result for shipment {ShipmentId} was not stored ({Code}); the lease will expire and it is retried")]
    private static partial void LogBookingNotStored(ILogger logger, Guid shipmentId, string code);

    [LoggerMessage(Level = LogLevel.Error, Message = "Carrier booking cycle failed")]
    private static partial void LogCycleFailed(ILogger logger, Exception exception);
}
