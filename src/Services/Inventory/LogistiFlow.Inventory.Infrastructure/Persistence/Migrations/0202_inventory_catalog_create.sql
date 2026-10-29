CREATE TABLE inventory.products (
    id           bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    public_id    uuid          NOT NULL,
    sku          varchar(40)   NOT NULL,
    name         varchar(150)  NOT NULL,
    description  varchar(1000) NULL,
    is_active    boolean       NOT NULL DEFAULT true,
    created_at   timestamptz   NOT NULL,
    updated_at   timestamptz   NOT NULL
);

CREATE UNIQUE INDEX ix_products_public_id ON inventory.products (public_id);
CREATE INDEX ix_products_is_active ON inventory.products (is_active) WHERE is_active = true;
CREATE UNIQUE INDEX ix_products_sku ON inventory.products (sku);

CREATE TABLE inventory.warehouses (
    id          bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    public_id   uuid         NOT NULL,
    code        varchar(20)  NOT NULL,
    name        varchar(120) NOT NULL,
    city        varchar(80)  NOT NULL,
    country     char(2)      NOT NULL,
    is_active   boolean      NOT NULL DEFAULT true,
    created_at  timestamptz  NOT NULL,
    updated_at  timestamptz  NOT NULL
);

CREATE UNIQUE INDEX ix_warehouses_public_id ON inventory.warehouses (public_id);
CREATE INDEX ix_warehouses_is_active ON inventory.warehouses (is_active) WHERE is_active = true;
CREATE UNIQUE INDEX ix_warehouses_code ON inventory.warehouses (code);
