namespace LogistiFlow.Inventory.Infrastructure.Persistence.Queries;

internal static class StockQueries
{
    // Positions that individually have enough stock; the repository keeps only warehouses that cover every SKU.
    public const string CandidatePositions = """
        SELECT w.code AS warehouse_code, sp.id AS stock_position_id, p.sku, (sp.on_hand - sp.reserved) AS available
        FROM unnest(@Skus, @Quantities) AS r(sku, quantity)
        JOIN inventory.products p ON p.sku = r.sku AND p.is_active
        JOIN inventory.stock_positions sp ON sp.product_id = p.id AND sp.is_active
        JOIN inventory.warehouses w ON w.id = sp.warehouse_id AND w.is_active
        WHERE sp.on_hand - sp.reserved >= r.quantity
        ORDER BY w.code, sp.id
        """;

    // The whole concurrency guarantee: the availability check and the increment are one atomic statement.
    // Under READ COMMITTED a concurrent writer makes this statement wait for the row lock and then re-evaluate
    // the WHERE clause against the committed values, so two reservations can never both take the last unit.
    public const string TryReserve = """
        UPDATE inventory.stock_positions
        SET reserved = reserved + @Quantity, version = version + 1, updated_at = now()
        WHERE id = @Id AND on_hand - reserved >= @Quantity
        """;

    public const string Release = """
        UPDATE inventory.stock_positions
        SET reserved = reserved - @Quantity, version = version + 1, updated_at = now()
        WHERE id = @Id AND reserved >= @Quantity
        """;

    public const string Consume = """
        UPDATE inventory.stock_positions
        SET on_hand = on_hand - @Quantity, reserved = reserved - @Quantity, version = version + 1, updated_at = now()
        WHERE id = @Id AND reserved >= @Quantity AND on_hand >= @Quantity
        """;

    public const string Receive = """
        INSERT INTO inventory.stock_positions (public_id, product_id, warehouse_id, on_hand, reserved, created_at, updated_at)
        SELECT @PublicId, p.id, w.id, @Quantity, 0, now(), now()
        FROM inventory.products p
        CROSS JOIN inventory.warehouses w
        WHERE p.sku = @Sku AND p.is_active AND w.code = @WarehouseCode AND w.is_active
        ON CONFLICT (product_id, warehouse_id) DO UPDATE
        SET on_hand = inventory.stock_positions.on_hand + EXCLUDED.on_hand,
            version = inventory.stock_positions.version + 1,
            updated_at = now()
        RETURNING id
        """;

    public const string InsertMovement = """
        INSERT INTO inventory.stock_movements (stock_position_id, movement_type, quantity, reference_type, reference_id)
        VALUES (@StockPositionId, @MovementType, @Quantity, @ReferenceType, @ReferenceId)
        """;

    public const string Availability = """
        SELECT p.sku,
               COALESCE(max(sp.on_hand - sp.reserved), 0)::int AS best_warehouse_available,
               COALESCE(sum(sp.on_hand - sp.reserved), 0)::int AS network_available
        FROM inventory.products p
        LEFT JOIN inventory.stock_positions sp ON sp.product_id = p.id AND sp.is_active
        WHERE p.sku = ANY(@Skus)
        GROUP BY p.sku
        """;
}
