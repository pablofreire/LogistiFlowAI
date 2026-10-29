-- One row per (product, warehouse). available = on_hand - reserved is derived, never stored.
-- The CHECK constraint is defence in depth: the conditional UPDATE used for reservations already refuses to
-- reserve more than is available, and the database rejects any write that would oversell regardless of the caller.
CREATE TABLE inventory.stock_positions (
    id            bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    public_id     uuid        NOT NULL,
    product_id    bigint      NOT NULL REFERENCES inventory.products (id),
    warehouse_id  bigint      NOT NULL REFERENCES inventory.warehouses (id),
    on_hand       integer     NOT NULL DEFAULT 0,
    reserved      integer     NOT NULL DEFAULT 0,
    version       integer     NOT NULL DEFAULT 1,
    is_active     boolean     NOT NULL DEFAULT true,
    created_at    timestamptz NOT NULL,
    updated_at    timestamptz NOT NULL,
    CONSTRAINT ck_stock_positions_quantities CHECK (on_hand >= 0 AND reserved >= 0 AND reserved <= on_hand)
);

CREATE UNIQUE INDEX ix_stock_positions_public_id ON inventory.stock_positions (public_id);
CREATE INDEX ix_stock_positions_is_active ON inventory.stock_positions (is_active) WHERE is_active = true;
CREATE UNIQUE INDEX ix_stock_positions_product_warehouse ON inventory.stock_positions (product_id, warehouse_id);
CREATE INDEX ix_stock_positions_warehouse_id ON inventory.stock_positions (warehouse_id);
