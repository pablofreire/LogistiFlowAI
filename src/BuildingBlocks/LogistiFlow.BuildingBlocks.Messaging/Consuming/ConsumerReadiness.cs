namespace LogistiFlow.BuildingBlocks.Messaging.Consuming;

/// <summary>
/// Set once the consumer has declared and bound its queues. Durable queues outlive the process, so after the first
/// declaration events are kept for this service even while it is down or reconnecting.
/// </summary>
public sealed class ConsumerReadiness
{
    private volatile bool _topologyDeclared;

    public bool TopologyDeclared => _topologyDeclared;

    public void MarkTopologyDeclared() => _topologyDeclared = true;
}
