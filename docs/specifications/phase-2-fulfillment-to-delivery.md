# Spec — Phase 2: Pick, Pack, Ship → Carrier Tracking → Delivery and Exceptions

Status: in progress. This document is the plan for Phase 2; the "Verified by" column names the test that will prove
each criterion, and `STATUS.md` records which of them have actually run.

## Objective
Take a reserved order through the warehouse floor (pick, pack, ship), hand it to a simulated carrier, follow it
until it is delivered or returned, and make delays and failures visible to operators as exceptions.

## Scope

| In | Out (later phase) |
|---|---|
| Fulfillment actions: pack, ship, report an issue, reprocess a failed job | Wave picking, bin locations, partial shipments |
| Inventory consumes the reservation when the job ships | Returns restocking |
| New **Shipping** service: shipments, carrier assignment, estimates, tracking, delays, exceptions | Rate shopping, label PDFs, customs |
| New **carrier simulator** with deterministic scenarios over real HTTP | Any real carrier API (none is called) |
| Orders reach `Packed`, `Shipped`, `Delivered` (or `Failed` when returned to sender) | Customer notifications |
| UI: Fulfillment page (actions), Shipments list and details (tracking timeline), Exceptions page, shipment card on the order, dashboard KPIs for shipments, delays and exceptions | "Average processing time" KPI (Phase 3) |
| E2E-005, E2E-006, E2E-007 | E2E-008 (Copilot, Phase 4) |

**Rescoping note:** the Phase 1 plan put the Fulfillment, Shipments and Exceptions pages in Phase 3. E2E-005/006/007
drive those flows through the UI, so the minimum of each page moves into Phase 2.

## Flow

```
Fulfillment job (Picking)
  └─ POST /api/fulfillment/jobs/{id}/pack ─▶ Packed ── FulfillmentPacked ─▶ Orders: Packed
  └─ POST /api/fulfillment/jobs/{id}/ship ─▶ Shipped ─ FulfillmentShipped ─┬▶ Orders: Shipped
                                                                          ├▶ Inventory: consume reservation
                                                                          └▶ Shipping: shipment PendingBooking
Shipping booking worker ── POST carrier /shipments (Idempotency-Key) ─▶ LabelCreated (tracking number, ETA)
Shipping tracking worker ── GET carrier /shipments/{tracking}/events ─▶ InTransit ─▶ OutForDelivery ─▶ Delivered
                                                                      ShipmentDelivered ─▶ Orders: Delivered
                                                                      ShipmentReturned  ─▶ Orders: Failed
```

## Decisions

- **Shipping is its own service** (ADR-0006): it owns carrier integration, which has a different failure profile
  (slow, flaky third parties) from the warehouse. It keeps its own projection of order destinations, built from
  `OrderCreated` (event-carried state transfer). `OrderCreatedEvent` v1 gains nullable, additive fields
  (`CustomerName`, `DestinationCity`, `DestinationCountry`, `PromisedDeliveryDate`).
- **No HTTP call inside a message transaction.** The `FulfillmentShipped` handler only inserts a `PendingBooking`
  shipment. A booking worker claims due shipments (`FOR UPDATE SKIP LOCKED`, lease, database clock), calls the
  carrier with a timeout, and retries with exponential backoff. The shipment public id is sent as the carrier
  `Idempotency-Key`, so a booking that timed out but succeeded is never booked twice.
- **Tracking by polling.** A tracking worker polls active shipments. Carrier events are stored with a unique
  `(shipment_id, carrier_event_id)`; only newly inserted events change the shipment, so re-reading the whole
  history on every poll is harmless.
- **Ship before the destination is known:** if `FulfillmentShipped` arrives before Shipping has seen
  `OrderCreated`, the handler throws a transient exception (retry with backoff, then DLQ). This is the same
  "early event" rule as Phase 1.
- **Broker start-up race (ADR-0003 follow-up):** `/health/ready` reports unhealthy until the service's consumer
  queues are declared and bound. Compose `--wait` and the container health checks therefore guarantee that every
  queue exists before traffic arrives. A consumer added to a running system still starts from "now"; backfilling it
  is a manual replay, documented in ADR-0003.
- **Permissions:** pack, ship, report issue and reprocess use `warehouse.operate`. A new `shipments.manage`
  permission (Admin, OpsManager) resolves exceptions and retries a failed booking. Reads use `operations.read`.

## Carriers and estimates

| Code | Name | Used when | Transit (business days) |
|---|---|---|---|
| `GLBL` | GlobalPost International | destination country is not `US` | 6 |
| `SIMX` | SimExpress | `US`, promised date within 2 days of shipping | 1 |
| `RDRN` | Roadrunner Ground | every other `US` shipment | 4 |

The carrier quotes the estimated delivery date when it books; a `DELAYED` tracking event carries a new estimate.

## Carrier simulator scenarios

The simulator is a separate deployable (ADR-0007) so that timeouts and 503s are real HTTP behaviour. The scenario is
chosen deterministically from the destination city, the way carrier sandboxes use reserved test addresses. Every
other city gets the normal scenario. Time is compressed: one tracking step every `Simulator:StepInterval`.

| Destination city | Scenario | Expected outcome in LogistiFlow |
|---|---|---|
| any other | Normal: label, picked up, in transit, out for delivery, delivered | Shipment and order `Delivered` |
| `Sandbox Delay` | Delay: in transit, then `DELAYED` with an estimate 2 days later, then delivered | Open `Delay` exception while undelivered; resolved on delivery |
| `Sandbox Unavailable` | Temporary failure: the first 2 booking calls return 503 | `CarrierUnavailable` exception opened, booking retried, exception resolved |
| `Sandbox Timeout` | Timeout: the first booking call answers after the client timeout, but the booking is created | Retried with the same `Idempotency-Key`; exactly one carrier booking |
| `Sandbox Redelivery` | First delivery attempt fails (recipient unavailable), second attempt delivers | `DeliveryAttemptFailed` exception, resolved on delivery |
| `Sandbox Address Not Found` | "Delivery not located": every attempt fails because the address cannot be located; returned to sender | Shipment `ReturnedToSender`, `AddressNotLocated` exception open, order `Failed` |

## Requirements and acceptance criteria

| Id | Acceptance criterion | Verified by |
|---|---|---|
| AC-17 | Pack moves a `Picking` job to `Packed` and the order to `Packed`; ship moves it to `Shipped` and the order to `Shipped` | `FulfillmentActionsTests`, E2E-005 |
| AC-18 | Invalid floor actions return 409 (for example, ship before pack); viewers get 403 | `FulfillmentActionsTests`, `FulfillmentJobEntityTests` |
| AC-19 | A failed job can be reprocessed (`Failed` → `Picking`), and every change is in the job history | `FulfillmentActionsTests`, `FulfillmentJobEntityTests` |
| AC-20 | Shipping consumes the reservation exactly once: `on_hand` and `reserved` drop by the shipped quantity, with a `Shipment` ledger row | `ShipmentFlowTests.ShippedOrder_ConsumesReservedStockOnce` |
| AC-21 | A shipped order gets one shipment with a carrier chosen by the rules above, a tracking number and an estimate | `ShipmentFlowTests`, `CarrierSelectionPolicyTests` |
| AC-22 | Tracking events move the shipment and the order to `Delivered`; a repeated poll adds no duplicate events | `ShipmentFlowTests.NormalScenario_*`, `ShipmentEntityTests`, E2E-006 |
| AC-23 | A carrier delay opens a `Delay` exception and counts as a delayed shipment until delivery | `CarrierScenarioTests.Delay_*`, E2E-007 |
| AC-24 | Carrier 503s and timeouts are retried with backoff; the timeout case books exactly once | `CarrierScenarioTests.TemporaryFailure_*`, `CarrierScenarioTests.Timeout_*` |
| AC-25 | A failed attempt followed by redelivery ends `Delivered`; an unlocatable address ends `ReturnedToSender` and the order `Failed` | `CarrierScenarioTests.Redelivery_*`, `CarrierScenarioTests.AddressNotFound_*` |
| AC-26 | An ops manager can resolve an open exception and retry a failed booking; other roles get 403 | `ShippingApiTests` |
| AC-27 | The dashboard shows shipments by status, delayed shipments and open exceptions, and degrades when Shipping is down | `ShipmentFlowTests.Metrics_*`, E2E-006, E2E-007 |
| AC-28 | `/health/ready` stays unhealthy until the consumer topology is declared | `ConsumerReadinessTests` |

## Error scenarios
Validation (400), unauthenticated (401), forbidden (403), not found (404), invalid floor action or concurrent change
(409), carrier unavailable or slow (retried in the background, surfaced as an exception, never as an HTTP error to the
operator).

## Dependencies
Phase 1 services, PostgreSQL 17 (new database `logistiflow_shipping`), RabbitMQ 4.1.
