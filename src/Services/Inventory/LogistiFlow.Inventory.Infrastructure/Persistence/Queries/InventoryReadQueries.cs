namespace LogistiFlow.Inventory.Infrastructure.Persistence.Queries;

internal static class InventoryReadQueries
{
    private const string ProductFilter = """
        WHERE p.is_active
          AND (CAST(@SearchPattern AS varchar) IS NULL OR p.sku ILIKE @SearchPattern ESCAPE '\' OR p.name ILIKE @SearchPattern ESCAPE '\')
        """;

    public const string CountProducts = $"SELECT count(*) FROM inventory.products p {ProductFilter}";

    public const string PageProducts = $"""
        SELECT p.public_id AS id, p.sku, p.name, p.description,
               COALESCE(sum(sp.on_hand), 0)::int AS on_hand,
               COALESCE(sum(sp.reserved), 0)::int AS reserved,
               COALESCE(sum(sp.on_hand - sp.reserved), 0)::int AS available
        FROM inventory.products p
        LEFT JOIN inventory.stock_positions sp ON sp.product_id = p.id AND sp.is_active
        {ProductFilter}
        GROUP BY p.id
        ORDER BY p.sku
        LIMIT @PageSize OFFSET @Offset
        """;

    public const string Warehouses = """
        SELECT w.public_id AS id, w.code, w.name, w.city, w.country,
               COALESCE(sum(sp.on_hand), 0)::int AS on_hand,
               COALESCE(sum(sp.reserved), 0)::int AS reserved,
               COALESCE(sum(sp.on_hand - sp.reserved), 0)::int AS available
        FROM inventory.warehouses w
        LEFT JOIN inventory.stock_positions sp ON sp.warehouse_id = w.id AND sp.is_active
        WHERE w.is_active
        GROUP BY w.id
        ORDER BY w.code
        """;

    private const string StockFilter = """
        WHERE sp.is_active
          AND (CAST(@WarehouseCode AS varchar) IS NULL OR w.code = @WarehouseCode)
          AND (CAST(@Sku AS varchar) IS NULL OR p.sku = @Sku)
          AND (NOT @LowStockOnly OR sp.on_hand - sp.reserved <= @LowStockThreshold)
        """;

    private const string StockFrom = """
        FROM inventory.stock_positions sp
        JOIN inventory.products p ON p.id = sp.product_id
        JOIN inventory.warehouses w ON w.id = sp.warehouse_id
        """;

    public const string CountStock = $"SELECT count(*) {StockFrom} {StockFilter}";

    public const string PageStock = $"""
        SELECT sp.public_id AS id, p.sku, p.name AS product_name, w.code AS warehouse_code,
               sp.on_hand, sp.reserved, (sp.on_hand - sp.reserved) AS available, sp.updated_at
        {StockFrom}
        {StockFilter}
        ORDER BY p.sku, w.code
        LIMIT @PageSize OFFSET @Offset
        """;

    public const string ProductBySku = "SELECT sku, name FROM inventory.products WHERE sku = @Sku AND is_active";

    public const string AvailabilityBySku = """
        SELECT w.code AS warehouse_code, sp.on_hand, sp.reserved, (sp.on_hand - sp.reserved) AS available
        FROM inventory.stock_positions sp
        JOIN inventory.products p ON p.id = sp.product_id
        JOIN inventory.warehouses w ON w.id = sp.warehouse_id
        WHERE p.sku = @Sku AND sp.is_active
        ORDER BY w.code
        """;

    public const string ReservationByOrder = """
        SELECT id, public_id, order_id, order_number, warehouse_code, status, failure_reason, created_at, updated_at
        FROM inventory.reservations
        WHERE order_id = @OrderId AND is_active
        """;

    public const string ReservationLines = "SELECT sku, quantity FROM inventory.reservation_lines WHERE reservation_id = @ReservationId ORDER BY id";

    public const string Totals = """
        SELECT (SELECT count(*) FROM inventory.products WHERE is_active)::int AS products,
               (SELECT count(*) FROM inventory.warehouses WHERE is_active)::int AS warehouses,
               COALESCE(sum(on_hand), 0)::bigint AS total_on_hand,
               COALESCE(sum(reserved), 0)::bigint AS total_reserved,
               COALESCE(sum(on_hand - reserved), 0)::bigint AS total_available,
               count(*) FILTER (WHERE on_hand - reserved <= @LowStockThreshold)::int AS low_stock_positions
        FROM inventory.stock_positions
        WHERE is_active
        """;

    public const string ReservationsByStatus = "SELECT status, count(*) AS count FROM inventory.reservations WHERE is_active GROUP BY status";

    public const string RecentMovements = """
        SELECT p.sku, w.code AS warehouse_code, m.movement_type, m.quantity, m.reference_type, m.reference_id, m.occurred_at
        FROM inventory.stock_movements m
        JOIN inventory.stock_positions sp ON sp.id = m.stock_position_id
        JOIN inventory.products p ON p.id = sp.product_id
        JOIN inventory.warehouses w ON w.id = sp.warehouse_id
        ORDER BY m.id DESC
        LIMIT @Limit
        """;
}
