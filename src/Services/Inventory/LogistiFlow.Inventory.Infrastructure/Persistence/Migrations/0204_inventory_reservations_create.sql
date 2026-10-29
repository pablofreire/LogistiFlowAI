-- At most one reservation per order: the unique order_id makes reserving idempotent per order and lets a
-- cancellation tombstone (status Cancelled) block a late OrderCreated.
-- order_id is the order's public id in the Orders service: a reference, not a foreign key (different database).
CREATE TABLE inventory.reservations (
    id              bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    public_id       uuid         NOT NULL,
    order_id        uuid         NOT NULL,
    order_number    varchar(20)  NOT NULL,
    warehouse_code  varchar(20)  NULL,
    status          varchar(20)  NOT NULL,
    failure_reason  varchar(500) NULL,
    is_active       boolean      NOT NULL DEFAULT true,
    created_at      timestamptz  NOT NULL,
    updated_at      timestamptz  NOT NULL,
    CONSTRAINT ck_reservations_status CHECK (status IN ('Reserved', 'Released', 'Failed', 'Cancelled'))
);

CREATE UNIQUE INDEX ix_reservations_public_id ON inventory.reservations (public_id);
CREATE INDEX ix_reservations_is_active ON inventory.reservations (is_active) WHERE is_active = true;
CREATE UNIQUE INDEX ix_reservations_order_id ON inventory.reservations (order_id);
CREATE INDEX ix_reservations_status ON inventory.reservations (status);

CREATE TABLE inventory.reservation_lines (
    id                 bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    reservation_id     bigint      NOT NULL REFERENCES inventory.reservations (id),
    stock_position_id  bigint      NOT NULL REFERENCES inventory.stock_positions (id),
    sku                varchar(40) NOT NULL,
    quantity           integer     NOT NULL,
    CONSTRAINT ck_reservation_lines_quantity CHECK (quantity > 0)
);

CREATE INDEX ix_reservation_lines_reservation_id ON inventory.reservation_lines (reservation_id);
