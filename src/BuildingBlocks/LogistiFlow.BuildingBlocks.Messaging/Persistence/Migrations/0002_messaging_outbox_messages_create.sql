-- Transactional outbox: rows are inserted in the same transaction as the business change and published by
-- the relay. Append-only infrastructure table, so it has no is_active/updated_at columns (see ADR-0003).
-- All timestamps come from the database clock (now()), never from the application clock.
CREATE TABLE messaging.outbox_messages (
    id              bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    event_id        uuid         NOT NULL,
    event_type      varchar(150) NOT NULL,
    event_version   integer      NOT NULL,
    payload         jsonb        NOT NULL,
    correlation_id  varchar(64)  NOT NULL,
    causation_id    uuid         NULL,
    trace_parent    varchar(128) NULL,
    occurred_at     timestamptz  NOT NULL DEFAULT now(),
    attempts        integer      NOT NULL DEFAULT 0,
    locked_until    timestamptz  NULL,
    published_at    timestamptz  NULL,
    last_error      varchar(2000) NULL,
    CONSTRAINT ck_outbox_messages_event_version CHECK (event_version > 0)
);

CREATE UNIQUE INDEX ix_outbox_messages_event_id ON messaging.outbox_messages (event_id);

-- The relay only scans unpublished rows; the partial index keeps that scan small as the table grows.
CREATE INDEX ix_outbox_messages_pending ON messaging.outbox_messages (id) WHERE published_at IS NULL;
