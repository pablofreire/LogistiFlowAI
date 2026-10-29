using LogistiFlow.BuildingBlocks.Application.Correlation.Interfaces;
using LogistiFlow.BuildingBlocks.Application.Messaging.Models;
using LogistiFlow.BuildingBlocks.Application.Persistence.Interfaces;
using LogistiFlow.BuildingBlocks.Messaging.Enums;
using LogistiFlow.BuildingBlocks.Messaging.Inbox;
using LogistiFlow.BuildingBlocks.Messaging.Models;
using LogistiFlow.BuildingBlocks.Messaging.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LogistiFlow.BuildingBlocks.Messaging.Consuming;

/// <summary>
/// Applies one message exactly once per consumer: in a new DI scope and a single transaction it records the
/// event in the inbox and runs the handler. A duplicate delivery finds the inbox row and changes nothing;
/// a handler exception rolls back both, so the retry starts from a clean state.
/// </summary>
public sealed class IntegrationEventDispatcher(
    IServiceScopeFactory scopeFactory,
    IntegrationEventHandlerRegistry registry,
    IOptions<MessagingOptions> options)
{
    public async Task<DispatchOutcome> DispatchAsync(MessageEnvelope envelope, int attempt, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (!registry.TryGet(envelope.EventType, envelope.Version, out var registration))
        {
            return DispatchOutcome.Unhandled;
        }

        var payload = IntegrationEventHandlerRegistry.Deserialize(envelope.Payload, registration.EventClrType);
        var context = new IntegrationEventContext(
            envelope.EventId,
            envelope.EventType,
            envelope.Version,
            envelope.OccurredAt,
            envelope.CorrelationId,
            attempt);

        await using var scope = scopeFactory.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        provider.GetRequiredService<ICorrelationContext>().Set(envelope.CorrelationId, envelope.EventId);

        var inbox = provider.GetRequiredService<InboxStore>();
        var unitOfWork = provider.GetRequiredService<IUnitOfWork>();
        var outcome = DispatchOutcome.Handled;

        await unitOfWork.ExecuteAsync(
            async ct =>
            {
                if (!await inbox.TryMarkProcessedAsync(options.Value.ServiceName, envelope.EventId, envelope.EventType, ct))
                {
                    outcome = DispatchOutcome.Duplicate;
                    return;
                }

                await registration.Invoke(provider, payload, context, ct);
            },
            cancellationToken);

        return outcome;
    }
}
