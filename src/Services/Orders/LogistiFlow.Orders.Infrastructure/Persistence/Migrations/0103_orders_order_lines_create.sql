-- Child rows of the order aggregate. Never addressed on their own, so no public_id (see ADR-0002).
CREATE TABLE orders.order_lines (
    id           bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    order_id     bigint      NOT NULL REFERENCES orders.orders (id),
    line_number  integer     NOT NULL,
    sku          varchar(40) NOT NULL,
    quantity     integer     NOT NULL,
    CONSTRAINT ck_order_lines_quantity CHECK (quantity > 0),
    CONSTRAINT ck_order_lines_line_number CHECK (line_number > 0)
);

CREATE UNIQUE INDEX ix_order_lines_order_line ON orders.order_lines (order_id, line_number);
