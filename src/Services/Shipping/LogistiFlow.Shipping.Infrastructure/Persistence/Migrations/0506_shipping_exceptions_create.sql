-- Operational exceptions. At most one open exception per (shipment, type); the partial unique index enforces it.
CREATE TABLE shipping.shipment_exceptions (
    id           bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    public_id    uuid         NOT NULL,
    shipment_id  bigint       NOT NULL REFERENCES shipping.shipments (id),
    type         varchar(30)  NOT NULL,
    status       varchar(20)  NOT NULL,
    details      varchar(500) NOT NULL,
    resolution   varchar(500) NULL,
    resolved_by  varchar(120) NULL,
    resolved_at  timestamptz  NULL,
    is_active    boolean      NOT NULL DEFAULT true,
    created_at   timestamptz  NOT NULL,
    updated_at   timestamptz  NOT NULL,
    CONSTRAINT ck_shipment_exceptions_type CHECK (type IN ('CarrierUnavailable', 'Delay', 'DeliveryAttemptFailed', 'ReturnedToSender')),
    CONSTRAINT ck_shipment_exceptions_status CHECK (status IN ('Open', 'Resolved'))
);

CREATE UNIQUE INDEX ix_shipment_exceptions_public_id ON shipping.shipment_exceptions (public_id);
CREATE INDEX ix_shipment_exceptions_is_active ON shipping.shipment_exceptions (is_active) WHERE is_active = true;
CREATE UNIQUE INDEX ix_shipment_exceptions_open_per_type ON shipping.shipment_exceptions (shipment_id, type) WHERE status = 'Open';
CREATE INDEX ix_shipment_exceptions_status_created_at ON shipping.shipment_exceptions (status, created_at DESC);
