# Spec — Phase 1: Create Order → Reservation → Fulfillment Initiated → Dashboard

## Objective
Accept an order once, reserve its stock without overselling, start warehouse work, and show the result to operators.

## Requirements and acceptance criteria

| Id | Acceptance criterion | Verified by |
|---|---|---|
| AC-01 | `POST /api/orders` requires `Idempotency-Key`; same key + same body returns the original order (200, `Idempotent-Replayed: true`) | `OrdersApiTests.CreateOrder_SameKeyTwice_*`, `CreateOrder_WithoutIdempotencyKey_Returns400` |
| AC-02 | Ten concurrent requests with one key create exactly one order | `OrdersApiTests.CreateOrder_ConcurrentDuplicates_CreateExactlyOneOrder` |
| AC-03 | Same key + different body returns 409 `order.idempotency_key_reused` | `OrdersApiTests.CreateOrder_SameKeyDifferentBody_Returns409` |
| AC-04 | Invalid input returns 400 with field details; no data is written | `OrdersApiTests.CreateOrder_InvalidBody_*`, `CreateOrderRequestValidatorTests` |
| AC-05 | Order status changes only along the documented state machine; invalid transitions return 409 | `OrderStateMachineTests` (81 pairs), `OrderEventOrderingTests.CancelAfterPickingStarted_IsRejectedWith409` |
| AC-06 | An order with stock reaches `Picking`, with a reservation and a fulfillment job in the same warehouse | `OrderToFulfillmentFlowTests`, E2E-003 |
| AC-07 | An order without stock reaches `Failed` with the shortage as the reason; no job is created | `OrderToFulfillmentFlowTests`, E2E-004 |
| AC-08 | Concurrent reservations never oversell | `InventoryReservationTests.ConcurrentReservations_ForTheLastUnits_NeverOversell` |
| AC-09 | A redelivered event is applied once | `InventoryReservationTests.RedeliveredEvent_*`, `OrderEventOrderingTests.DuplicateReservationEvent_*` |
| AC-10 | Out-of-order events are ignored (stale) or retried (early) | `OrderEventOrderingTests` |
| AC-11 | Orders are accepted while the broker is down and published when it returns | `OutboxResilienceTests` |
| AC-12 | Failing messages are retried with backoff, then dead-lettered | `RetryAndDeadLetterTests` |
| AC-13 | Cancelling releases held stock; a cancellation that overtakes the order leaves a tombstone | `InventoryReservationTests.Release_*`, `CancellationThatOvertakesTheOrder_*` |
| AC-14 | Anonymous calls get 401; viewers cannot create or cancel (403) | `OrdersApiTests`, `AuthorizationPolicyTests`, E2E-001, E2E-009 |
| AC-15 | The dashboard aggregates live metrics and degrades to partial data when a service is down | `OrderToFulfillmentFlowTests.Metrics_*`, E2E-003, gateway smoke run |
| AC-16 | One correlation id follows the flow across services | `OrderToFulfillmentFlowTests.OrderWithStock_*` |

## Error scenarios
Validation (400), unauthenticated (401), forbidden (403), not found (404), conflict (409: idempotency key reuse,
invalid transition, concurrent modification), rate limited login (429), unexpected (500 with trace id, no internals).

## Dependencies
PostgreSQL 17, RabbitMQ 4.1, .NET 10, Node 24 (web build).
