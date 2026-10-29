using System.Collections.Concurrent;

namespace LogistiFlow.IntegrationTests.Messaging.Probes;

public sealed class ProbeAttempts
{
    private readonly ConcurrentQueue<int> _attempts = new();

    public IReadOnlyList<int> Seen => [.. _attempts];

    public void Record(int attempt) => _attempts.Enqueue(attempt);
}
