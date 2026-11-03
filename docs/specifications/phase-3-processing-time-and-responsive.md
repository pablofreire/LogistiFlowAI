# Spec — Phase 3: Processing Time KPI, Responsive Pages and the End-to-End Demo

Status: implemented on 2026-11-03. Every criterion below is covered by the named tests, which ran green locally
(see `STATUS.md`); GitHub Actions has not run them yet.

## Objective
Show operators how long an order takes from intake to delivery, check that the Phase 2 pages work on a phone, and
make the scripted demo walk an order all the way to the customer.

## Scope

| In | Out (later phase) |
|---|---|
| Orders metrics: mean time from creation to picking, to shipped and to delivered | Percentiles, per-warehouse or per-carrier breakdowns, time windows |
| Dashboard "Avg. processing time" card (created → delivered, with the shipped stage) | Charts of the trend over time |
| Phone-viewport E2E for the Fulfillment, Shipments, Shipment details and Exceptions pages | Visual regression snapshots |
| `scripts/demo-flow.ps1` from intake to delivery | AI Operations Copilot (Phase 4) |

## Definition of the KPI
For each stage, the mean of `first time the order reached the status - order creation`, in seconds, over the orders
that reached it (`orders.order_status_changes`, both timestamps from the Orders service clock). The stages are
averaged over different sets of orders: an order still in picking counts toward "to picking" but not "to delivered".
A stage is `null` until one order has reached it, and the card shows "—".

The card shows "to delivered" with "to ship" as its hint. "To picking" stays in the API but is not on the card: it
also counts orders that never shipped (the k6 backlog left ~29k in picking), so next to the other two stages it read
as "1.2 s to ship, 1.5 min to picking", which looks contradictory.

## Requirements and acceptance criteria

| Id | Acceptance criterion | Verified by |
|---|---|---|
| AC-29 | `GET /api/metrics/orders` returns `averageSecondsToPicking`, `averageSecondsToShipped` and `averageSecondsToDelivered`; once an order is delivered all three are positive | `ShipmentFlowTests.OrderMetrics_ReportProcessingTimes_OnceAnOrderIsDelivered` |
| AC-30 | After a delivery, the dashboard shows the processing time with a unit (s, min or h) | E2E-006 |
| AC-31 | On a phone viewport the dashboard, Fulfillment, Shipments, Shipment details and Exceptions pages render without horizontal page scrolling; wide tables scroll inside their container | E2E-010 |

## Found while verifying
A rebuilt compose stack failed E2E-005/006/007: the restarted carrier simulator reissued tracking numbers already
stored by Shipping. The simulator now seeds its sequence from the clock (ADR-0007).
