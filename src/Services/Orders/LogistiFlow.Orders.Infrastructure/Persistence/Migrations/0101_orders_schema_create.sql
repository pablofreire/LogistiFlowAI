CREATE SCHEMA IF NOT EXISTS orders;

-- Human-readable order numbers (ORD-1001, ORD-1002, ...). Gaps are acceptable: a rolled-back insert consumes a value.
CREATE SEQUENCE orders.order_number_seq START WITH 1001;
