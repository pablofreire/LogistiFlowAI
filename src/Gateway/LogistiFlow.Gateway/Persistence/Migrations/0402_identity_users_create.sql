CREATE TABLE identity.users (
    id             bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    public_id      uuid         NOT NULL,
    email          varchar(254) NOT NULL,
    display_name   varchar(120) NOT NULL,
    role           varchar(30)  NOT NULL,
    password_hash  varchar(100) NOT NULL,
    is_active      boolean      NOT NULL DEFAULT true,
    created_at     timestamptz  NOT NULL,
    updated_at     timestamptz  NOT NULL,
    CONSTRAINT ck_users_role CHECK (role IN ('Admin', 'OpsManager', 'WarehouseOperator', 'Viewer'))
);

CREATE UNIQUE INDEX ix_users_public_id ON identity.users (public_id);
CREATE INDEX ix_users_is_active ON identity.users (is_active) WHERE is_active = true;
CREATE UNIQUE INDEX ix_users_email ON identity.users (lower(email));
