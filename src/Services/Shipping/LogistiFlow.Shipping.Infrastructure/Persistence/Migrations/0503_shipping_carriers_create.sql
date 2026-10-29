-- Reference data for the simulated carriers. Assignment rules live in CarrierSelectionPolicy; this table names them.
CREATE TABLE shipping.carriers (
    id             bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    code           varchar(10)  NOT NULL,
    name           varchar(80)  NOT NULL,
    service_level  varchar(20)  NOT NULL,
    is_active      boolean      NOT NULL DEFAULT true,
    created_at     timestamptz  NOT NULL DEFAULT now(),
    updated_at     timestamptz  NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX ix_carriers_code ON shipping.carriers (code);

INSERT INTO shipping.carriers (code, name, service_level) VALUES
    ('SIMX', 'SimExpress', 'Express'),
    ('RDRN', 'Roadrunner Ground', 'Ground'),
    ('GLBL', 'GlobalPost International', 'International');
