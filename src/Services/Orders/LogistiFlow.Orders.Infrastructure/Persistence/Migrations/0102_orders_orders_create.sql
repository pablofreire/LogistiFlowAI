CREATE TABLE orders.orders (
    id                      bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    public_id               uuid         NOT NULL,
    order_number            varchar(20)  NOT NULL DEFAULT ('ORD-' || nextval('orders.order_number_seq')),
    customer_name           varchar(120) NOT NULL,
    destination_city        varchar(80)  NOT NULL,
    destination_country     char(2)      NOT NULL,
    promised_delivery_date  date         NULL,
    status                  varchar(30)  NOT NULL,
    status_reason           varchar(500) NULL,
    idempotency_key         varchar(100) NOT NULL,
    request_hash            char(64)     NOT NULL,
    version                 integer      NOT NULL DEFAULT 1,
    is_active               boolean      NOT NULL DEFAULT true,
    created_at              timestamptz  NOT NULL,
    updated_at              timestamptz  NOT NULL,
    CONSTRAINT ck_orders_status CHECK (status IN
        ('Created', 'PendingInventory', 'Reserved', 'Picking', 'Packed', 'Shipped', 'Delivered', 'Cancelled', 'Failed')),
    CONSTRAINT ck_orders_version CHECK (version > 0)
);

CREATE UNIQUE INDEX ix_orders_public_id ON orders.orders (public_id);
CREATE INDEX ix_orders_is_active ON orders.orders (is_active) WHERE is_active = true;
CREATE UNIQUE INDEX ix_orders_order_number ON orders.orders (order_number);

-- Enforces one order per idempotency key; INSERT ... ON CONFLICT relies on it to resolve concurrent duplicates.
CREATE UNIQUE INDEX ix_orders_idempotency_key ON orders.orders (idempotency_key);

-- Order list: filter by status, newest first.
CREATE INDEX ix_orders_status_created_at ON orders.orders (status, created_at DESC);
CREATE INDEX ix_orders_created_at ON orders.orders (created_at DESC);
