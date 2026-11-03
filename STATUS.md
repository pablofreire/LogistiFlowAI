# LogistiFlow AI — Status

Single source of truth for progress and the resume point of every work session. Last updated: 2026-11-03.

## Verified (with evidence)

| Area | Evidence | Status |
|---|---|---|
| Build: .NET 10, warnings as errors, `latest-recommended` analyzers | `dotnet build LogistiFlow.slnx` → 0 warnings, 0 errors | Passing |
| Unit tests (BuildingBlocks 25, Orders 130, Inventory 15, Fulfillment 25, Shipping 26) | `tests/unit` | 221 passing |
| Architecture tests (service isolation incl. Shipping and the carrier simulator, layers, folders, suffixes) | `tests/architecture` | 39 passing |
| Integration tests on PostgreSQL 17 + RabbitMQ 4.1 (Testcontainers), carrier simulator in-process | `tests/integration` | 44 passing (3 consecutive runs on 2026-11-03) |
| Docker images (5 .NET services, carrier simulator, web) and compose `app` profile | `docker compose --profile app up -d --build --wait` | All healthy |
| Playwright E2E-001 to 007, 009, 010 against compose (E2E-010 now covers Fulfillment, Shipments, Shipment details, Exceptions) | `tests/e2e`, screenshots in `docs/evidence/e2e` | 15 passing |
| k6 baseline 10 VUs / 1 min (Phase 1 intake only) | `docs/performance/baseline-2026-10-29.md` | Measured |
| Web: ESLint, strict typecheck | `src/Web/logistiflow-web` | Passing |
| Gateway smoke: login 401/200, proxy, dashboard, 401 anonymous, 429 after 10 logins | manual run 2026-10-29 (Phase 1) | Passing |
| Demo flow from intake to delivery (`scripts/demo-flow.ps1`) | run 2026-11-03: order Delivered via RDRN, 5 tracking events, out-of-stock order Failed | Passing |
| Dashboard degrades with `shipping-api` stopped (`unavailableSources: ["shipping"]`, other sections present) | manual run 2026-10-29 | Passing |

## Phase 2 (done): pick, pack, ship → carrier tracking → delivery and exceptions

Spec with acceptance criteria AC-17 to AC-28: `docs/specifications/phase-2-fulfillment-to-delivery.md`.
Decisions: ADR-0006 (Shipping and carrier integration), ADR-0007 (carrier simulator), ADR-0003 follow-up (readiness).
The Fulfillment, Shipments and Exceptions pages were moved up from Phase 3 because E2E-005/006/007 need them.

## Phase 3 (done): processing time KPI, responsive pages, end-to-end demo

Spec with acceptance criteria AC-29 to AC-31: `docs/specifications/phase-3-processing-time-and-responsive.md`.
- Orders metrics add `averageSecondsToShipped` and `averageSecondsToDelivered`; the dashboard card "Avg. processing
  time" shows created → delivered with the shipped stage. OpenAPI and web contracts regenerated.
- Found while verifying: a restarted carrier simulator reissued tracking numbers already stored by Shipping, which
  blocked every booking on a rebuilt stack (E2E-005/006/007 failed). The sequence now starts from the clock (ADR-0007).
- One integration run on 2026-11-03 failed all 10 Shipping tests at 0 ms (fixture-level) right after a compose rebuild;
  the next 3 runs passed 44/44 and the cause was not captured. Watch for it on CI.

## Not yet verified

- GitHub Actions workflows are written but have never run (no remote; push not authorized). The Docker image matrix
  had a latent `build-args` bug that was fixed without a CI run to confirm it.
- No security review pass beyond the automated tests; no OWASP ZAP run.
- No load test of the Phase 2 paths (shipping, carrier polling).
- Known limitation: a consumer added to a running system starts from "now"; events published before its first start
  need a manual backfill (ADR-0003). On a fresh start the readiness gate prevents the loss.
- Known limitation: the carrier simulator keeps bookings in memory; restarting it orphans the tracking of shipments in
  flight (ADR-0007).
- Known bottleneck: sequential consumers (~154 orders/s end to end vs 539 orders/s intake).

## Next (the roadmap in README.md)

1. Phase 4: AI Operations Copilot.
2. UI polish (listed under Phase 3 in the README, not started).

## Resume checklist

```bash
docker compose --profile app up -d --build --wait
dotnet build LogistiFlow.slnx && dotnet test --project tests/integration/LogistiFlow.IntegrationTests
cd tests/e2e && npx playwright test
```
