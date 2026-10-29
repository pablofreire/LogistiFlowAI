# ADR-0006: Shipping as its own service, with carrier calls outside message transactions

- Status: Accepted
- Date: 2026-10-29

## Context
After a parcel leaves the dock, the work changes nature: it depends on third-party carrier APIs that are slow,
rate-limited and sometimes down, and it lasts days instead of minutes. Phase 2 needs shipment creation, carrier
assignment, estimates, tracking, delay detection and operational exceptions (master prompt §4.4).

Two shortcuts were rejected:
- **Calling the carrier from the `FulfillmentShipped` handler.** The handler runs inside the inbox transaction. A
  carrier call there holds a database transaction open for seconds, and a timeout rolls back the inbox row, so the
  message is redelivered and the carrier may be booked twice.
- **HTTP-level retries (a Polly retry inside one call).** They hide carrier failures from operators and do not
  survive a restart: an hour-long outage would be invisible until the retries ran out in memory.

## Decision
- A **Shipping service** with its own database (`logistiflow_shipping`). It keeps a local copy of order destinations
  built from `OrderCreated` (event-carried state transfer); `OrderCreatedEvent` v1 gained nullable destination fields
  (additive change). A ship event that overtakes the destination is "early" and retried, as in ADR-0003.
- The `FulfillmentShipped` handler only inserts a `PendingBooking` shipment, due now.
- A **booking worker** claims due shipments (`FOR UPDATE SKIP LOCKED`, lease on the database clock), calls the carrier
  with a per-call timeout and no in-call retries, then records the result in a short transaction. Failures are
  retried with durable exponential backoff (`next_booking_at`) and surface as a `CarrierUnavailable` exception;
  after `BookingMaxAttempts` the shipment becomes `BookingFailed` and an operator can retry it.
- The shipment public id is sent as the carrier's **`Idempotency-Key`**. A call that timed out after the carrier
  created the booking is retried with the same key and gets the same tracking number back.
- A **tracking worker** polls carriers for active shipments. Events are stored with a unique
  `(shipment_id, carrier_event_id)`; only newly inserted events change the shipment, so reading the whole history on
  every poll is idempotent. Delivery and return are published (`ShipmentDelivered`, `ShipmentReturned`) through the
  outbox in the same transaction as the status change.
- **Exceptions** are rows, not log lines: `CarrierUnavailable`, `Delay`, `DeliveryAttemptFailed`, `ReturnedToSender`,
  at most one open per type and shipment (partial unique index). Delivery resolves delays and failed attempts
  automatically; an ops manager (`shipments.manage`) resolves the rest with a note.
- **Carrier assignment** is a pure domain policy: international → GlobalPost, domestic with a promised date within
  two days → SimExpress, other domestic → Roadrunner Ground.

## Consequences
- Carrier latency never blocks message consumption or holds database locks.
- Polling adds load proportional to active shipments; webhooks would replace it for carriers that offer them (the
  stored-event idempotency stays the same).
- Shipments are booked once per order. Multi-parcel shipments and rate shopping are out of scope.
- Proven by `ShipmentFlowTests`, `CarrierScenarioTests` and `ShippingApiTests` against the simulator (ADR-0007).
