namespace LogistiFlow.Shipping.Infrastructure.Persistence.Queries;

internal static class ShippingReadQueries
{
    private const string OpenDelay = """
        EXISTS (SELECT 1 FROM shipping.shipment_exceptions e
                WHERE e.shipment_id = s.id AND e.type = 'Delay' AND e.status = 'Open')
        """;

    private const string ShipmentFilter = $"""
        WHERE s.is_active
          AND (CAST(@Status AS varchar) IS NULL OR s.status = @Status)
          AND (NOT @DelayedOnly OR {OpenDelay})
        """;

    public const string CountShipments = $"SELECT count(*) FROM shipping.shipments s {ShipmentFilter}";

    public const string PageShipments = $"""
        SELECT s.public_id AS id, s.order_id, s.order_number, s.carrier_code, c.name AS carrier_name, s.tracking_number, s.status,
               s.destination_city, s.destination_country, s.promised_delivery_date, s.estimated_delivery,
               {OpenDelay} AS is_delayed,
               (SELECT count(*) FROM shipping.shipment_exceptions e WHERE e.shipment_id = s.id AND e.status = 'Open')::int AS open_exceptions,
               s.created_at, s.updated_at
        FROM shipping.shipments s
        JOIN shipping.carriers c ON c.code = s.carrier_code
        {ShipmentFilter}
        ORDER BY s.created_at DESC, s.id DESC
        LIMIT @PageSize OFFSET @Offset
        """;

    private const string DetailsSelect = $"""
        SELECT s.id AS internal_id, s.public_id AS id, s.order_id, s.order_number, s.fulfillment_job_id, s.warehouse_code,
               s.carrier_code, c.name AS carrier_name, s.tracking_number, s.status, s.status_reason, s.destination_city,
               s.destination_country, s.promised_delivery_date, s.original_estimated_delivery, s.estimated_delivery,
               s.delivered_at, s.booking_attempts, {OpenDelay} AS is_delayed, s.created_at, s.updated_at
        FROM shipping.shipments s
        JOIN shipping.carriers c ON c.code = s.carrier_code
        """;

    public const string DetailsByPublicId = $"{DetailsSelect} WHERE s.public_id = @ShipmentId AND s.is_active";

    public const string DetailsByOrderId = $"{DetailsSelect} WHERE s.order_id = @OrderId AND s.is_active";

    public const string TrackingEvents = """
        SELECT code, description, location, occurred_at, estimated_delivery, received_at
        FROM shipping.tracking_events
        WHERE shipment_id = @InternalId
        ORDER BY occurred_at, id
        """;

    private const string ExceptionSelect = """
        SELECT e.public_id AS id, s.public_id AS shipment_id, s.order_number, s.carrier_code, s.tracking_number, e.type, e.status,
               e.details, e.created_at AS opened_at, e.resolved_at, e.resolution, e.resolved_by
        FROM shipping.shipment_exceptions e
        JOIN shipping.shipments s ON s.id = e.shipment_id
        """;

    public const string ExceptionsForShipment = $"{ExceptionSelect} WHERE e.shipment_id = @InternalId AND e.is_active ORDER BY e.id";

    private const string ExceptionFilter = """
        WHERE e.is_active
          AND (CAST(@Status AS varchar) IS NULL OR e.status = @Status)
          AND (CAST(@Type AS varchar) IS NULL OR e.type = @Type)
        """;

    public const string CountExceptions = $"SELECT count(*) FROM shipping.shipment_exceptions e {ExceptionFilter}";

    public const string PageExceptions = $"""
        {ExceptionSelect}
        {ExceptionFilter}
        ORDER BY e.created_at DESC, e.id DESC
        LIMIT @PageSize OFFSET @Offset
        """;

    public const string CountByStatus = "SELECT status, count(*) AS count FROM shipping.shipments WHERE is_active GROUP BY status";

    public const string CountDelayed = $"""
        SELECT count(*) FROM shipping.shipments s
        WHERE s.is_active AND s.status NOT IN ('Delivered', 'ReturnedToSender') AND {OpenDelay}
        """;

    public const string CountOpenExceptionsByType = """
        SELECT type AS status, count(*) AS count FROM shipping.shipment_exceptions WHERE is_active AND status = 'Open' GROUP BY type
        """;
}
