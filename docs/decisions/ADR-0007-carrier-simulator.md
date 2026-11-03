# ADR-0007: A deterministic carrier simulator as a separate deployable

- Status: Accepted
- Date: 2026-10-29

## Context
The master prompt asks for simulated carriers with deterministic behaviour (normal delivery, delay, temporary
failure, timeout, delivery not located, redelivery) and forbids paid or external APIs. The behaviours that matter
most, timeouts and 503s, only exist over a real network call.

## Decision
- `LogistiFlow.CarrierSimulator` is a small ASP.NET Core app with the API shape of a carrier: `POST /v1/shipments`
  (requires `Idempotency-Key`) and `GET /v1/shipments/{tracking}/events`. It references no LogistiFlow project, which an
  architecture test enforces, so Shipping integrates with it exactly as it would with a third party.
- **Scenario selection is deterministic** and keyed on the destination city, the way carrier sandboxes reserve test
  addresses: `Sandbox Delay`, `Sandbox Unavailable`, `Sandbox Timeout`, `Sandbox Redelivery`,
  `Sandbox Address Not Found`; every other city is the normal scenario. The scenarios are listed in the Phase 2 spec.
- "Delivery not located" is interpreted as **the carrier cannot locate the delivery address**: every attempt fails
  and the parcel is returned to sender. (The other reading, a tracking number the carrier cannot find, is what a
  simulator restart produces, and Shipping keeps polling with backoff in that case.)
- **Time is compressed and configurable:** one scripted tracking step per `Simulator:StepInterval` (3 s in compose,
  200 ms in integration tests), with `TimeProvider` as the clock. Bookings are kept in memory.
- In integration tests the simulator runs in-process (`WebApplicationFactory`) and Shipping's typed `HttpClient` uses
  its test-server handler: the timeout scenario still goes through the real client timeout and cancellation.

## Consequences
- Every scenario is reproducible in tests, in compose and in demos without external accounts or cost.
- Restarting the simulator forgets bookings; tracking for shipments booked before the restart fails until it is
  retried manually. Acceptable for a simulator; documented here.
- Tracking numbers never repeat across restarts: the sequence starts at the Unix time in milliseconds. Until
  2026-11-03 it started at a fixed value, so a restart against a persisted Shipping database reissued stored numbers,
  `ix_shipments_tracking_number` rejected the booking and new shipments stayed in "Awaiting booking" (found when a
  rebuilt compose stack failed E2E-005/006/007).
- The simulator is not a contract test of any real carrier API.
