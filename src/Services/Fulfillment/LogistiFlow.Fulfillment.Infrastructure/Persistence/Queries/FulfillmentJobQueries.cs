namespace LogistiFlow.Fulfillment.Infrastructure.Persistence.Queries;

internal static class FulfillmentJobQueries
{
    private const string JobColumns = """
        id, public_id, order_id, order_number, warehouse_code, reservation_id, status, status_reason, version, created_at, updated_at
        """;

    public const string SelectByOrderId = $"SELECT {JobColumns} FROM fulfillment.fulfillment_jobs WHERE order_id = @OrderId AND is_active";

    public const string SelectByPublicId = $"SELECT {JobColumns} FROM fulfillment.fulfillment_jobs WHERE public_id = @PublicId AND is_active";

    public const string SelectLines = "SELECT sku, quantity FROM fulfillment.fulfillment_job_lines WHERE job_id = @JobId ORDER BY id";

    public const string SelectHistory = """
        SELECT from_status, to_status, reason, source, occurred_at
        FROM fulfillment.fulfillment_job_status_changes
        WHERE job_id = @JobId
        ORDER BY id
        """;

    public const string Insert = """
        INSERT INTO fulfillment.fulfillment_jobs (public_id, order_id, order_number, warehouse_code, reservation_id, status, status_reason,
                                                  version, is_active, created_at, updated_at)
        VALUES (@PublicId, @OrderId, @OrderNumber, @WarehouseCode, @ReservationId, @Status, @StatusReason, @Version, true, @CreatedAt, @UpdatedAt)
        ON CONFLICT (order_id) DO NOTHING
        RETURNING id
        """;

    public const string InsertLine = "INSERT INTO fulfillment.fulfillment_job_lines (job_id, sku, quantity) VALUES (@JobId, @Sku, @Quantity)";

    public const string InsertStatusChange = """
        INSERT INTO fulfillment.fulfillment_job_status_changes (job_id, from_status, to_status, reason, source, occurred_at)
        VALUES (@JobId, @FromStatus, @ToStatus, @Reason, @Source, @OccurredAt)
        """;

    public const string UpdateStatus = """
        UPDATE fulfillment.fulfillment_jobs
        SET status = @Status, status_reason = @StatusReason, updated_at = @UpdatedAt, version = version + 1
        WHERE id = @Id AND version = @Version
        RETURNING version
        """;

    private const string ListFilter = """
        WHERE j.is_active
          AND (CAST(@Status AS varchar) IS NULL OR j.status = @Status)
          AND (CAST(@WarehouseCode AS varchar) IS NULL OR j.warehouse_code = @WarehouseCode)
        """;

    public const string Count = $"SELECT count(*) FROM fulfillment.fulfillment_jobs j {ListFilter}";

    public const string Page = $"""
        SELECT j.public_id AS id, j.order_id, j.order_number, j.warehouse_code, j.status, j.status_reason,
               COALESCE((SELECT sum(quantity) FROM fulfillment.fulfillment_job_lines l WHERE l.job_id = j.id), 0)::int AS total_units,
               j.created_at, j.updated_at
        FROM fulfillment.fulfillment_jobs j
        {ListFilter}
        ORDER BY j.created_at DESC, j.id DESC
        LIMIT @PageSize OFFSET @Offset
        """;

    public const string CountByStatus = "SELECT status, count(*) AS count FROM fulfillment.fulfillment_jobs WHERE is_active GROUP BY status";

    public const string CountByWarehouse = """
        SELECT warehouse_code,
               count(*) FILTER (WHERE status IN ('Created', 'Picking', 'Packed')) AS open,
               count(*) FILTER (WHERE status = 'Failed') AS failed,
               count(*) AS total
        FROM fulfillment.fulfillment_jobs
        WHERE is_active
        GROUP BY warehouse_code
        ORDER BY warehouse_code
        """;
}
