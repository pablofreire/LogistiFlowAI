# ADR-0001: Microservices with explicit boundaries, not a modular monolith

- Status: Accepted
- Date: 2026-10-29

## Context
LogistiFlow models a fulfillment operator: orders arrive from merchants, stock is reserved, warehouse work starts.
The project is also a reference for service-based architecture, event-driven integration and cloud deployment.
A modular monolith (the approach of the author's DataForge project) would be simpler to run.

## Decision
Four deployables in Phase 1, each with a reason to be separate:

| Service | Owns | Why it is its own service |
|---|---|---|
| Orders | Order lifecycle, idempotent intake, order history | Customer-facing write path; must accept orders even when the warehouse side is slow or down |
| Inventory | Products, warehouses, stock positions, reservations, stock ledger | The concurrency hotspot; scales and is tuned independently (row contention, not request volume) |
| Fulfillment | Fulfillment jobs and their execution history | Warehouse-floor workflow with its own state machine; reacts to reservations asynchronously |
| Gateway | Identity, token issuance, routing, dashboard aggregation | One origin and one security edge for the SPA and API clients |

Services share only versioned integration-event contracts (`LogistiFlow.Contracts.IntegrationEvents`) and generic
building blocks. An architecture test fails if one service references another.

## Consequences
- Positive: independent scaling and deployment; failure isolation (Orders keeps accepting orders when Inventory is
  down); each boundary is visible in code, tests and compose.
- Negative: eventual consistency between services, more moving parts (broker, four databases), and distributed
  debugging. Mitigated by the outbox/inbox (ADR-0003), correlation ids and the order timeline.
- Not separated on purpose: building blocks are a shared library, not a service; the web app is static files.
