using LogistiFlow.BuildingBlocks.Messaging.Options;
using LogistiFlow.BuildingBlocks.Messaging.Outbox;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogistiFlow.BuildingBlocks.Messaging.Workers;

/// <summary>Polls the outbox and drains it while there is work; sleeps for the polling interval when it is empty.</summary>
public sealed partial class OutboxRelayWorker(OutboxRelay relay, IOptions<OutboxOptions> options, ILogger<OutboxRelayWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogStarted(logger, options.Value.PollingInterval.TotalMilliseconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var published = await relay.PublishPendingAsync(stoppingToken);
                if (published > 0)
                {
                    continue;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
#pragma warning disable CA1031 // The relay must survive database outages and keep polling.
            catch (Exception exception)
#pragma warning restore CA1031
            {
                LogCycleFailed(logger, exception);
            }

            try
            {
                await Task.Delay(options.Value.PollingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Outbox relay started (polling every {IntervalMs} ms)")]
    private static partial void LogStarted(ILogger logger, double intervalMs);

    [LoggerMessage(Level = LogLevel.Error, Message = "Outbox relay cycle failed")]
    private static partial void LogCycleFailed(ILogger logger, Exception exception);
}
