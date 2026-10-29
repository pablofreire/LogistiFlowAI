-- Inbox: one row per (consumer, event) processed. Inserted in the same transaction as the handler's changes,
-- so a redelivered or duplicated event is detected and skipped (idempotent consumer).
CREATE TABLE messaging.inbox_messages (
    consumer      varchar(100) NOT NULL,
    event_id      uuid         NOT NULL,
    event_type    varchar(150) NOT NULL,
    processed_at  timestamptz  NOT NULL DEFAULT now(),
    CONSTRAINT pk_inbox_messages PRIMARY KEY (consumer, event_id)
);

CREATE INDEX ix_inbox_messages_processed_at ON messaging.inbox_messages (processed_at);
