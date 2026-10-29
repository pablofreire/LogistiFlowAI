-- Append-only history of every status change; source is 'api' or the integration event type that caused it.
CREATE TABLE orders.order_status_changes (
    id           bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    order_id     bigint       NOT NULL REFERENCES orders.orders (id),
    from_status  varchar(30)  NULL,
    to_status    varchar(30)  NOT NULL,
    reason       varchar(500) NULL,
    source       varchar(100) NOT NULL,
    occurred_at  timestamptz  NOT NULL
);

CREATE INDEX ix_order_status_changes_order_id ON orders.order_status_changes (order_id, id);
CREATE INDEX ix_order_status_changes_occurred_at ON orders.order_status_changes (occurred_at DESC);
