namespace LogistiFlow.BuildingBlocks.Messaging.Outbox;

internal static class OutboxQueries
{
    public const string Insert = """
        INSERT INTO messaging.outbox_messages (event_id, event_type, event_version, payload, correlation_id, causation_id, trace_parent)
        VALUES (@EventId, @EventType, @EventVersion, CAST(@Payload AS jsonb), @CorrelationId, @CausationId, @TraceParent)
        """;

    // Claims a batch with SKIP LOCKED so several relay instances never publish the same row concurrently.
    // The lease (locked_until) makes a row visible again if the relay crashes before marking it published.
    public const string Claim = """
        UPDATE messaging.outbox_messages AS m
        SET locked_until = now() + make_interval(secs => @LockSeconds),
            attempts = m.attempts + 1
        FROM (
            SELECT id
            FROM messaging.outbox_messages
            WHERE published_at IS NULL
              AND (locked_until IS NULL OR locked_until < now())
            ORDER BY id
            LIMIT @BatchSize
            FOR UPDATE SKIP LOCKED
        ) AS next
        WHERE m.id = next.id
        RETURNING m.id, m.event_id, m.event_type, m.event_version, m.payload::text AS payload,
                  m.correlation_id, m.causation_id, m.trace_parent, m.occurred_at, m.attempts
        """;

    public const string MarkPublished = """
        UPDATE messaging.outbox_messages
        SET published_at = now(), locked_until = NULL, last_error = NULL
        WHERE id = @Id
        """;

    public const string MarkFailed = """
        UPDATE messaging.outbox_messages
        SET last_error = @Error, locked_until = now() + make_interval(secs => @RetrySeconds)
        WHERE id = @Id
        """;

    public const string CountPending = "SELECT count(*) FROM messaging.outbox_messages WHERE published_at IS NULL";
}
