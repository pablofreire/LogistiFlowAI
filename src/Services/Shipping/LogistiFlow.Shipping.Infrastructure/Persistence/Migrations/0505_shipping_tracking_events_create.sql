-- Carrier tracking history. The unique (shipment_id, carrier_event_id) makes re-reading the carrier's full history on
-- every poll idempotent: INSERT ... ON CONFLICT DO NOTHING tells the poller which events are new.
CREATE TABLE shipping.tracking_events (
    id                  bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    shipment_id         bigint       NOT NULL REFERENCES shipping.shipments (id),
    carrier_event_id    varchar(60)  NOT NULL,
    code                varchar(40)  NOT NULL,
    description         varchar(300) NOT NULL,
    location            varchar(120) NOT NULL,
    occurred_at         timestamptz  NOT NULL,
    estimated_delivery  date         NULL,
    received_at         timestamptz  NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX ix_tracking_events_shipment_carrier_event ON shipping.tracking_events (shipment_id, carrier_event_id);
