namespace LogistiFlow.Orders.Infrastructure.Persistence.Queries;

internal static class OrderReadQueries
{
    private const string ListFilter = """
        WHERE o.is_active
          AND (CAST(@Status AS varchar) IS NULL OR o.status = @Status)
          AND (CAST(@SearchPattern AS varchar) IS NULL
               OR o.order_number ILIKE @SearchPattern ESCAPE '\'
               OR o.customer_name ILIKE @SearchPattern ESCAPE '\')
        """;

    public const string Count = $"SELECT count(*) FROM orders.orders o {ListFilter}";

    public const string Page = $"""
        SELECT o.public_id AS id, o.order_number, o.customer_name, o.destination_city, o.status, o.status_reason,
               COALESCE(l.line_count, 0) AS line_count, COALESCE(l.total_units, 0) AS total_units,
               o.created_at, o.updated_at
        FROM orders.orders o
        LEFT JOIN LATERAL (
            SELECT count(*)::int AS line_count, sum(quantity)::int AS total_units
            FROM orders.order_lines
            WHERE order_id = o.id
        ) l ON true
        {ListFilter}
        ORDER BY o.created_at DESC, o.id DESC
        LIMIT @PageSize OFFSET @Offset
        """;

    public const string SelectOrderId = "SELECT id FROM orders.orders WHERE public_id = @OrderId AND is_active";

    public const string History = """
        SELECT o.public_id AS order_id, o.order_number, c.from_status, c.to_status, c.reason, c.source, c.occurred_at
        FROM orders.order_status_changes c
        JOIN orders.orders o ON o.id = c.order_id
        WHERE c.order_id = @Id
        ORDER BY c.id
        """;

    public const string CountByStatus = "SELECT status, count(*) AS count FROM orders.orders WHERE is_active GROUP BY status";

    public const string CreatedLast24Hours = "SELECT count(*) FROM orders.orders WHERE is_active AND created_at >= now() - interval '24 hours'";

    public const string AverageSecondsToPicking = """
        SELECT avg(extract(epoch FROM (c.occurred_at - o.created_at)))::float8
        FROM orders.order_status_changes c
        JOIN orders.orders o ON o.id = c.order_id
        WHERE c.to_status = 'Picking'
        """;

    public const string RecentActivity = """
        SELECT o.public_id AS order_id, o.order_number, c.from_status, c.to_status, c.reason, c.source, c.occurred_at
        FROM orders.order_status_changes c
        JOIN orders.orders o ON o.id = c.order_id
        ORDER BY c.id DESC
        LIMIT @Limit
        """;
}
