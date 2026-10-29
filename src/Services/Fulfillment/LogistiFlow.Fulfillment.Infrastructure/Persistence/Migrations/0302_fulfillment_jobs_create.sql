-- One job per order. order_id and reservation_id are public ids owned by other services (no foreign keys).
CREATE TABLE fulfillment.fulfillment_jobs (
    id              bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    public_id       uuid         NOT NULL,
    order_id        uuid         NOT NULL,
    order_number    varchar(20)  NOT NULL,
    warehouse_code  varchar(20)  NOT NULL,
    reservation_id  uuid         NOT NULL,
    status          varchar(20)  NOT NULL,
    status_reason   varchar(500) NULL,
    version         integer      NOT NULL DEFAULT 1,
    is_active       boolean      NOT NULL DEFAULT true,
    created_at      timestamptz  NOT NULL,
    updated_at      timestamptz  NOT NULL,
    CONSTRAINT ck_fulfillment_jobs_status CHECK (status IN ('Created', 'Picking', 'Packed', 'Shipped', 'Cancelled', 'Failed'))
);

CREATE UNIQUE INDEX ix_fulfillment_jobs_public_id ON fulfillment.fulfillment_jobs (public_id);
CREATE INDEX ix_fulfillment_jobs_is_active ON fulfillment.fulfillment_jobs (is_active) WHERE is_active = true;
CREATE UNIQUE INDEX ix_fulfillment_jobs_order_id ON fulfillment.fulfillment_jobs (order_id);
CREATE INDEX ix_fulfillment_jobs_status_created_at ON fulfillment.fulfillment_jobs (status, created_at DESC);
CREATE INDEX ix_fulfillment_jobs_warehouse_code ON fulfillment.fulfillment_jobs (warehouse_code);

CREATE TABLE fulfillment.fulfillment_job_lines (
    id        bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    job_id    bigint      NOT NULL REFERENCES fulfillment.fulfillment_jobs (id),
    sku       varchar(40) NOT NULL,
    quantity  integer     NOT NULL,
    CONSTRAINT ck_fulfillment_job_lines_quantity CHECK (quantity > 0)
);

CREATE INDEX ix_fulfillment_job_lines_job_id ON fulfillment.fulfillment_job_lines (job_id);

-- Append-only execution history.
CREATE TABLE fulfillment.fulfillment_job_status_changes (
    id           bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    job_id       bigint       NOT NULL REFERENCES fulfillment.fulfillment_jobs (id),
    from_status  varchar(20)  NULL,
    to_status    varchar(20)  NOT NULL,
    reason       varchar(500) NULL,
    source       varchar(100) NOT NULL,
    occurred_at  timestamptz  NOT NULL
);

CREATE INDEX ix_fulfillment_job_status_changes_job_id ON fulfillment.fulfillment_job_status_changes (job_id, id);
