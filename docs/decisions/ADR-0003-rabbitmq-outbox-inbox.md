# ADR-0003: RabbitMQ with a transactional outbox and an idempotent inbox

- Status: Accepted
- Date: 2026-10-29

## Context
An order must trigger a reservation, which must trigger fulfillment, across three databases. Writing to a database
and publishing to a broker cannot be one transaction; dual writes lose or invent events on crashes.

## Decision
- **RabbitMQ 4** through `RabbitMQ.Client` 7 directly (no MassTransit: v9 is commercial, and the needed features are
  small). One durable topic exchange `logistiflow.events`; routing key `{eventType}.v{version}`.
- **Transactional outbox:** the state change and the event row commit together. A relay claims rows with
  `FOR UPDATE SKIP LOCKED` and a lease, publishes with publisher confirms, then marks them published. Broker down →
  rows stay pending with exponential backoff (proven by `OutboxResilienceTests`).
- **Inbox:** each consumer records `(consumer, event_id)` in the same transaction as the handler's changes. A
  redelivery is detected and skipped (`InventoryReservationTests`, `OrderEventOrderingTests`). Business keys add a
  second guard: one reservation per order, one fulfillment job per order.
- **Retries and dead letters:** per-service retry queues with fixed TTLs (1 s, 5 s, 15 s by default) that dead-letter
  back into the main queue — backoff without head-of-line blocking. After the last attempt, or when a message cannot
  be read, it goes to `{queue}.dlq` with the reason in a header (`RetryAndDeadLetterTests`).
- **Ordering:** not guaranteed across producers. Handlers classify each event as applied, stale (ignored) or early
  (transient → retried), e.g. `FulfillmentStarted` overtaking `InventoryReserved`.
- **Versioning:** contracts are additive within a version; a breaking change is a new record with `Version = 2`
  published alongside v1 until consumers move.

## Consequences
- At-least-once delivery with effectively-once effects.
- Known limitation: messages are published with `mandatory=false`, so an event published before any consumer has
  declared its queue is dropped. This happened once in local development (an order stuck in `PendingInventory`
  because Inventory had not started yet). Mitigation for real deployments: declare the topology as infrastructure
  before services start (Phase 7), or publish with `mandatory=true` to an alternate exchange.
- Follow-up (Phase 2): `/health/ready` of a consuming service stays unhealthy until its queues are declared and bound
  (`ConsumerReadinessHealthCheck`, proven by `ConsumerReadinessTests`). Container health checks and
  `docker compose --wait` use readiness, so on a fresh start no traffic reaches the system before every queue
  exists. A consumer added to a system that is already running still starts from "now": events published before
  its first start are not replayed automatically and need a manual backfill.
- Throughput is bounded by sequential consumers; see `docs/performance/baseline-2026-10-29.md`.
- Azure Service Bus is the managed alternative for Azure (Phase 7 ADR); the outbox/inbox design does not change.
