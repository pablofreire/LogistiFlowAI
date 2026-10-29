using LogistiFlow.Shipping.Application.Interfaces;
using LogistiFlow.Shipping.Application.Options;
using LogistiFlow.Shipping.Application.UseCases.Shipments.PollTracking.Contracts;
using LogistiFlow.Shipping.Application.UseCases.Shipments.PollTracking.Interfaces;
using LogistiFlow.Shipping.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogistiFlow.Shipping.Infrastructure.Workers;

/// <summary>Claims shipments due for a tracking poll and reads the carrier for each one.</summary>
public sealed partial class TrackingPollingWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ShippingWorkerOptions> options,
    ILogger<TrackingPollingWorker> logger) : BackgroundService
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
        IReadOnlyList<Guid> claimed;
        await using (var claimScope = scopeFactory.CreateAsyncScope())
        {
            var queue = claimScope.ServiceProvider.GetRequiredService<IShipmentWorkQueue>();
            claimed = await queue.ClaimDueTrackingAsync(options.Value.BatchSize, options.Value.Lease, cancellationToken);
        }

        foreach (var shipmentId in claimed)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var useCase = scope.ServiceProvider.GetRequiredService<IPollShipmentTrackingUseCase>();
            var result = await useCase.ExecuteAsync(new PollShipmentTrackingRequest(shipmentId), cancellationToken);
            if (result.IsFailure)
            {
                LogPollNotStored(logger, shipmentId, result.Error.Code);
            }
            else if (result.Value.CarrierError is { } error)
            {
                LogCarrierFailed(logger, shipmentId, error);
            }
            else if (result.Value.NewEvents > 0)
            {
                LogTracked(logger, shipmentId, result.Value.NewEvents, result.Value.Status);
            }
        }

        return claimed.Count;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Shipment {ShipmentId}: {NewEvents} new carrier events, now {Status}")]
    private static partial void LogTracked(ILogger logger, Guid shipmentId, int newEvents, ShipmentStatus status);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tracking poll for shipment {ShipmentId} failed: {Error}")]
    private static partial void LogCarrierFailed(ILogger logger, Guid shipmentId, string error);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tracking result for shipment {ShipmentId} was not stored ({Code}); it is retried after the lease")]
    private static partial void LogPollNotStored(ILogger logger, Guid shipmentId, string code);

    [LoggerMessage(Level = LogLevel.Error, Message = "Tracking poll cycle failed")]
    private static partial void LogCycleFailed(ILogger logger, Exception exception);
}
