-- One row per question asked to the copilot, answered or failed. Written once; never stores the caller's token.
CREATE TABLE aiops.copilot_interactions (
    id                  bigint        GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    public_id           uuid          NOT NULL,
    conversation_id     uuid          NOT NULL,
    user_email          varchar(254)  NOT NULL,
    user_role           varchar(40)   NOT NULL,
    question            varchar(500)  NOT NULL,
    answer              varchar(8000) NOT NULL,
    outcome             varchar(20)   NOT NULL,
    failure_reason      varchar(500)  NULL,
    provider            varchar(40)   NOT NULL,
    model               varchar(100)  NOT NULL,
    simulated           boolean       NOT NULL,
    input_tokens        bigint        NULL,
    output_tokens       bigint        NULL,
    total_tokens        bigint        NULL,
    estimated_cost_usd  numeric(12,6) NOT NULL DEFAULT 0,
    duration_ms         bigint        NOT NULL,
    is_active           boolean       NOT NULL DEFAULT true,
    created_at          timestamptz   NOT NULL,
    updated_at          timestamptz   NOT NULL,
    CONSTRAINT ck_copilot_interactions_outcome CHECK (outcome IN ('Answered', 'Failed'))
);

CREATE UNIQUE INDEX ix_copilot_interactions_public_id ON aiops.copilot_interactions (public_id);
CREATE INDEX ix_copilot_interactions_is_active ON aiops.copilot_interactions (is_active) WHERE is_active = true;
CREATE INDEX ix_copilot_interactions_conversation ON aiops.copilot_interactions (conversation_id, user_email, id);
CREATE INDEX ix_copilot_interactions_created_at ON aiops.copilot_interactions (created_at DESC, id DESC);
