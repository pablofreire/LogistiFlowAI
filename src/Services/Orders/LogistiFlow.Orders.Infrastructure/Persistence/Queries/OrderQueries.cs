namespace LogistiFlow.Orders.Infrastructure.Persistence.Queries;

/// <summary>SQL of the order aggregate. Every statement is parameterized.</summary>
internal static class OrderQueries
{
    private const string OrderColumns = """
        id, public_id, order_number, customer_name, destination_city, destination_country, promised_delivery_date,
        status, status_reason, idempotency_key, request_hash, version, is_active, created_at, updated_at
        """;

    public const string SelectByPublicId = $"SELECT {OrderColumns} FROM orders.orders WHERE public_id = @PublicId AND is_active";

    public const string SelectByIdempotencyKey = $"SELECT {OrderColumns} FROM orders.orders WHERE idempotency_key = @IdempotencyKey";

    public const string SelectLines = """
        SELECT line_number, sku, quantity FROM orders.order_lines WHERE order_id = @OrderId ORDER BY line_number
        """;

    // ON CONFLICT DO NOTHING keeps the transaction usable when a concurrent request already used the key:
    // no row is returned and the caller reads the winner instead.
    public const string Insert = """
        INSERT INTO orders.orders (public_id, customer_name, destination_city, destination_country, promised_delivery_date,
                                   status, status_reason, idempotency_key, request_hash, version, is_active, created_at, updated_at)
        VALUES (@PublicId, @CustomerName, @DestinationCity, @DestinationCountry, CAST(@PromisedDeliveryDate AS date),
                @Status, @StatusReason, @IdempotencyKey, @RequestHash, @Version, true, @CreatedAt, @UpdatedAt)
        ON CONFLICT (idempotency_key) DO NOTHING
        RETURNING id, order_number
        """;

    public const string InsertLine = """
        INSERT INTO orders.order_lines (order_id, line_number, sku, quantity)
        VALUES (@OrderId, @LineNumber, @Sku, @Quantity)
        """;

    public const string InsertStatusChange = """
        INSERT INTO orders.order_status_changes (order_id, from_status, to_status, reason, source, occurred_at)
        VALUES (@OrderId, @FromStatus, @ToStatus, @Reason, @Source, @OccurredAt)
        """;

    // Optimistic concurrency: the update only succeeds against the version that was read.
    public const string UpdateStatus = """
        UPDATE orders.orders
        SET status = @Status, status_reason = @StatusReason, updated_at = @UpdatedAt, version = version + 1
        WHERE id = @Id AND version = @Version
        RETURNING version
        """;
}
