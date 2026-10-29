namespace LogistiFlow.Shipping.Infrastructure.Persistence.Queries;

internal static class ShipmentQueries
{
    private const string ShipmentColumns = """
        id, public_id, order_id, order_number, fulfillment_job_id, warehouse_code, destination_city, destination_country,
        promised_delivery_date, carrier_code, tracking_number, original_estimated_delivery, estimated_delivery, status,
        status_reason, delivered_at, booking_attempts, version, created_at, updated_at
        """;

    public const string SelectByPublicId = $"SELECT {ShipmentColumns} FROM shipping.shipments WHERE public_id = @PublicId AND is_active";

    public const string SelectByOrderId = $"SELECT {ShipmentColumns} FROM shipping.shipments WHERE order_id = @OrderId AND is_active";

    public const string SelectByExceptionId = $"""
        SELECT {ShipmentColumns} FROM shipping.shipments
        WHERE id = (SELECT shipment_id FROM shipping.shipment_exceptions WHERE public_id = @ExceptionId) AND is_active
        """;

    public const string SelectExceptions = """
        SELECT id, public_id, type, status, details, resolution, resolved_by, resolved_at, created_at, updated_at
        FROM shipping.shipment_exceptions
        WHERE shipment_id = @ShipmentId AND is_active
        ORDER BY id
        """;

    public const string Insert = """
        INSERT INTO shipping.shipments (public_id, order_id, order_number, fulfillment_job_id, warehouse_code, destination_city,
                                        destination_country, promised_delivery_date, carrier_code, status, status_reason, version,
                                        next_booking_at, next_poll_at, is_active, created_at, updated_at)
        VALUES (@PublicId, @OrderId, @OrderNumber, @FulfillmentJobId, @WarehouseCode, @DestinationCity, @DestinationCountry,
                CAST(@PromisedDeliveryDate AS date), @CarrierCode, @Status, @StatusReason, @Version,
                CASE WHEN CAST(@NextBookingSeconds AS double precision) IS NULL THEN NULL
                     ELSE now() + make_interval(secs => @NextBookingSeconds) END,
                CASE WHEN CAST(@NextPollSeconds AS double precision) IS NULL THEN NULL
                     ELSE now() + make_interval(secs => @NextPollSeconds) END,
                true, @CreatedAt, @UpdatedAt)
        ON CONFLICT (order_id) DO NOTHING
        RETURNING id
        """;

    // Optimistic update. Also writes the next worker schedule (on the database clock) and releases the lease.
    // Leaving BookingFailed through a retry resets the attempt budget.
    public const string Update = """
        UPDATE shipping.shipments
        SET tracking_number = @TrackingNumber,
            original_estimated_delivery = CAST(@OriginalEstimatedDelivery AS date),
            estimated_delivery = CAST(@EstimatedDelivery AS date),
            booking_attempts = CASE WHEN status = 'BookingFailed' AND @Status = 'PendingBooking' THEN 0 ELSE booking_attempts END,
            status = @Status,
            status_reason = @StatusReason,
            delivered_at = @DeliveredAt,
            next_booking_at = CASE WHEN CAST(@NextBookingSeconds AS double precision) IS NULL THEN NULL
                                   ELSE now() + make_interval(secs => @NextBookingSeconds) END,
            next_poll_at = CASE WHEN CAST(@NextPollSeconds AS double precision) IS NULL THEN NULL
                                ELSE now() + make_interval(secs => @NextPollSeconds) END,
            locked_until = NULL,
            updated_at = @UpdatedAt,
            version = version + 1
        WHERE id = @Id AND version = @Version
        RETURNING version
        """;

    public const string InsertException = """
        INSERT INTO shipping.shipment_exceptions (public_id, shipment_id, type, status, details, resolution, resolved_by, resolved_at,
                                                  is_active, created_at, updated_at)
        VALUES (@PublicId, @ShipmentId, @Type, @Status, @Details, @Resolution, @ResolvedBy, @ResolvedAt, true, @CreatedAt, @UpdatedAt)
        RETURNING id
        """;

    public const string UpdateException = """
        UPDATE shipping.shipment_exceptions
        SET status = @Status, details = @Details, resolution = @Resolution, resolved_by = @ResolvedBy, resolved_at = @ResolvedAt,
            updated_at = @UpdatedAt
        WHERE id = @Id
        """;

    public const string InsertTrackingEvent = """
        INSERT INTO shipping.tracking_events (shipment_id, carrier_event_id, code, description, location, occurred_at, estimated_delivery)
        VALUES (@ShipmentId, @CarrierEventId, @Code, @Description, @Location, @OccurredAt, CAST(@EstimatedDelivery AS date))
        ON CONFLICT (shipment_id, carrier_event_id) DO NOTHING
        """;

    // Claims due bookings. SKIP LOCKED keeps concurrent workers apart; the lease frees rows of a crashed worker.
    public const string ClaimDueBookings = """
        UPDATE shipping.shipments AS s
        SET locked_until = now() + make_interval(secs => @LeaseSeconds),
            booking_attempts = s.booking_attempts + 1
        FROM (
            SELECT id
            FROM shipping.shipments
            WHERE status = 'PendingBooking'
              AND next_booking_at <= now()
              AND (locked_until IS NULL OR locked_until < now())
            ORDER BY next_booking_at
            LIMIT @BatchSize
            FOR UPDATE SKIP LOCKED
        ) AS due
        WHERE s.id = due.id
        RETURNING s.public_id AS shipment_id, s.booking_attempts AS attempt
        """;

    public const string ClaimDueTracking = """
        UPDATE shipping.shipments AS s
        SET locked_until = now() + make_interval(secs => @LeaseSeconds)
        FROM (
            SELECT id
            FROM shipping.shipments
            WHERE status IN ('LabelCreated', 'InTransit', 'OutForDelivery', 'DeliveryAttemptFailed')
              AND next_poll_at <= now()
              AND (locked_until IS NULL OR locked_until < now())
            ORDER BY next_poll_at
            LIMIT @BatchSize
            FOR UPDATE SKIP LOCKED
        ) AS due
        WHERE s.id = due.id
        RETURNING s.public_id
        """;

    public const string Reschedule = """
        UPDATE shipping.shipments
        SET next_booking_at = CASE WHEN CAST(@NextBookingSeconds AS double precision) IS NULL THEN NULL
                                   ELSE now() + make_interval(secs => @NextBookingSeconds) END,
            next_poll_at = CASE WHEN CAST(@NextPollSeconds AS double precision) IS NULL THEN NULL
                                ELSE now() + make_interval(secs => @NextPollSeconds) END,
            locked_until = NULL
        WHERE public_id = @PublicId
        """;
}
