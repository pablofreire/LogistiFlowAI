# ADR-0002: PostgreSQL with one database per service, Dapper and DbUp

- Status: Accepted
- Date: 2026-10-29

## Context
Services must own their data. Locally we want one PostgreSQL container; in Azure, one Flexible Server.

## Decision
- One PostgreSQL instance, **one database per service** (`logistiflow_orders`, `_inventory`, `_fulfillment`,
  `_identity`). No cross-database joins or foreign keys; services reference each other's entities by public id.
- **Dapper** with hand-written, parameterized SQL. The hot paths (conditional stock UPDATE, `SKIP LOCKED` outbox
  claim, `ON CONFLICT` idempotency) are explicit SQL anyway.
- **DbUp** scripts embedded per service, named `{NNNN}_{schema}_{entity}_{action}.sql` (00xx shared messaging
  tables, 01xx Orders, 02xx Inventory, 03xx Fulfillment, 04xx identity). Each service migrates its own database as
  a container init step (`--migrate`), never at web start-up.
- Conventions: `id bigint identity` (internal), `public_id uuid` v7 (exposed), `is_active`, `created_at`,
  `updated_at` (timestamptz), unique `ix_{table}_public_id`, partial `ix_{table}_is_active`.

## Exceptions to the conventions (deliberate)
- Child rows (`order_lines`, `reservation_lines`, `fulfillment_job_lines`) and append-only ledgers
  (`order_status_changes`, `stock_movements`, `outbox_messages`, `inbox_messages`) have no `public_id` or
  `is_active`: they are never addressed on their own and are never soft-deleted.

## Consequences
- Data ownership is enforced physically, and a service can move to its own server without code changes.
- Reporting across services needs events or the gateway's aggregation, not SQL joins.
- Queue and lease timestamps use the database clock (`now()`), never the application clock.
