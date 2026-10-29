-- Append-only ledger of every stock change (receipts, reservations, releases).
CREATE TABLE inventory.stock_movements (
    id                 bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    stock_position_id  bigint      NOT NULL REFERENCES inventory.stock_positions (id),
    movement_type      varchar(20) NOT NULL,
    quantity           integer     NOT NULL,
    reference_type     varchar(20) NOT NULL,
    reference_id       varchar(60) NOT NULL,
    occurred_at        timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_stock_movements_type CHECK (movement_type IN ('Receipt', 'Reservation', 'Release')),
    CONSTRAINT ck_stock_movements_quantity CHECK (quantity > 0)
);

CREATE INDEX ix_stock_movements_stock_position_id ON inventory.stock_movements (stock_position_id, id);
CREATE INDEX ix_stock_movements_reference ON inventory.stock_movements (reference_type, reference_id);
