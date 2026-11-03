-- The tools the copilot ran for an interaction, in order: arguments, outcome and duration (results are not stored).
CREATE TABLE aiops.copilot_tool_calls (
    id              bigint        GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    public_id       uuid          NOT NULL,
    interaction_id  bigint        NOT NULL REFERENCES aiops.copilot_interactions (id),
    sequence        integer       NOT NULL,
    tool_name       varchar(60)   NOT NULL,
    arguments_json  varchar(1000) NOT NULL,
    outcome         varchar(20)   NOT NULL,
    summary         varchar(300)  NOT NULL,
    duration_ms     bigint        NOT NULL,
    is_active       boolean       NOT NULL DEFAULT true,
    created_at      timestamptz   NOT NULL,
    updated_at      timestamptz   NOT NULL,
    CONSTRAINT ck_copilot_tool_calls_outcome CHECK (outcome IN ('Ok', 'NotFound', 'Forbidden', 'Invalid', 'Unavailable')),
    CONSTRAINT uq_copilot_tool_calls_sequence UNIQUE (interaction_id, sequence)
);

CREATE UNIQUE INDEX ix_copilot_tool_calls_public_id ON aiops.copilot_tool_calls (public_id);
CREATE INDEX ix_copilot_tool_calls_is_active ON aiops.copilot_tool_calls (is_active) WHERE is_active = true;
