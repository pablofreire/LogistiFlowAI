using LogistiFlow.BuildingBlocks.Application.Correlation.Interfaces;

namespace LogistiFlow.BuildingBlocks.Application.Correlation.Implementations;

public sealed class CorrelationContext : ICorrelationContext
{
    public string CorrelationId { get; private set; } = Guid.NewGuid().ToString("N");

    public Guid? CausationId { get; private set; }

    public void Set(string correlationId, Guid? causationId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        CorrelationId = correlationId;
        CausationId = causationId;
    }
}
