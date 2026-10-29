-- Shipping's local copy of each order's destination, written from OrderCreated (event-carried state transfer).
-- order_id is the Orders service public id: a reference, not a foreign key (different database).
CREATE TABLE shipping.order_destinations (
    order_id                uuid         PRIMARY KEY,
    order_number            varchar(20)  NOT NULL,
    customer_name           varchar(120) NULL,
    destination_city        varchar(80)  NOT NULL,
    destination_country     char(2)      NOT NULL,
    promised_delivery_date  date         NULL,
    received_at             timestamptz  NOT NULL DEFAULT now()
);
