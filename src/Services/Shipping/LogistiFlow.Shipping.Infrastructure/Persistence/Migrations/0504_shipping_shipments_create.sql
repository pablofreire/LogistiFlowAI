-- One shipment per order. The worker columns (next_booking_at, next_poll_at, locked_until, booking_attempts) are a
-- work queue on the same row: workers claim due rows with FOR UPDATE SKIP LOCKED and a lease on the database clock.
CREATE TABLE shipping.shipments (
    id                           bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    public_id                    uuid         NOT NULL,
    order_id                     uuid         NOT NULL,
    order_number                 varchar(20)  NOT NULL,
    fulfillment_job_id           uuid         NOT NULL,
    warehouse_code               varchar(20)  NOT NULL,
    destination_city             varchar(80)  NOT NULL,
    destination_country          char(2)      NOT NULL,
    promised_delivery_date       date         NULL,
    carrier_code                 varchar(10)  NOT NULL REFERENCES shipping.carriers (code),
    tracking_number              varchar(40)  NULL,
    original_estimated_delivery  date         NULL,
    estimated_delivery           date         NULL,
    status                       varchar(30)  NOT NULL,
    status_reason                varchar(500) NULL,
    delivered_at                 timestamptz  NULL,
    booking_attempts             integer      NOT NULL DEFAULT 0,
    next_booking_at              timestamptz  NULL,
    next_poll_at                 timestamptz  NULL,
    locked_until                 timestamptz  NULL,
    version                      integer      NOT NULL DEFAULT 1,
    is_active                    boolean      NOT NULL DEFAULT true,
    created_at                   timestamptz  NOT NULL,
    updated_at                   timestamptz  NOT NULL,
    CONSTRAINT ck_shipments_status CHECK (status IN ('PendingBooking', 'BookingFailed', 'LabelCreated', 'InTransit',
        'OutForDelivery', 'DeliveryAttemptFailed', 'Delivered', 'ReturnedToSender')),
    CONSTRAINT ck_shipments_version CHECK (version > 0)
);

CREATE UNIQUE INDEX ix_shipments_public_id ON shipping.shipments (public_id);
CREATE INDEX ix_shipments_is_active ON shipping.shipments (is_active) WHERE is_active = true;
CREATE UNIQUE INDEX ix_shipments_order_id ON shipping.shipments (order_id);
CREATE UNIQUE INDEX ix_shipments_tracking_number ON shipping.shipments (carrier_code, tracking_number) WHERE tracking_number IS NOT NULL;
CREATE INDEX ix_shipments_status_created_at ON shipping.shipments (status, created_at DESC);

-- Worker claims: only rows that are due are indexed.
CREATE INDEX ix_shipments_next_booking_at ON shipping.shipments (next_booking_at) WHERE next_booking_at IS NOT NULL;
CREATE INDEX ix_shipments_next_poll_at ON shipping.shipments (next_poll_at) WHERE next_poll_at IS NOT NULL;
