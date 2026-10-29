namespace LogistiFlow.Inventory.Infrastructure.Persistence.Queries;

internal static class ReservationQueries
{
    public const string SelectByOrderId = """
        SELECT id, public_id, order_id, order_number, warehouse_code, status, failure_reason, created_at, updated_at
        FROM inventory.reservations
        WHERE order_id = @OrderId
        """;

    public const string SelectLines = """
        SELECT stock_position_id, sku, quantity FROM inventory.reservation_lines WHERE reservation_id = @ReservationId ORDER BY id
        """;

    public const string Insert = """
        INSERT INTO inventory.reservations (public_id, order_id, order_number, warehouse_code, status, failure_reason, is_active, created_at, updated_at)
        VALUES (@PublicId, @OrderId, @OrderNumber, @WarehouseCode, @Status, @FailureReason, true, @CreatedAt, @UpdatedAt)
        ON CONFLICT (order_id) DO NOTHING
        RETURNING id
        """;

    public const string InsertLine = """
        INSERT INTO inventory.reservation_lines (reservation_id, stock_position_id, sku, quantity)
        VALUES (@ReservationId, @StockPositionId, @Sku, @Quantity)
        """;

    public const string UpdateStatus = """
        UPDATE inventory.reservations SET status = @Status, updated_at = @UpdatedAt WHERE id = @Id
        """;
}
