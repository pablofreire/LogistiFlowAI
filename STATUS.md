# LogistiFlow AI — Status

Single source of truth for progress and the resume point of every work session. Last updated: 2026-10-29.

## Verified (with evidence)

| Area | Evidence | Status |
|---|---|---|
| Build: .NET 10, warnings as errors, `latest-recommended` analyzers | `dotnet build LogistiFlow.slnx` → 0 warnings, 0 errors | Passing |
| Unit tests (BuildingBlocks 25, Orders 130, Inventory 15, Fulfillment 25, Shipping 26) | `tests/unit` | 221 passing |
| Architecture tests (service isolation incl. Shipping and the carrier simulator, layers, folders, suffixes) | `tests/architecture` | 39 passing |
| Integration tests on PostgreSQL 17 + RabbitMQ 4.1 (Testcontainers), carrier simulator in-process | `tests/integration` | 43 passing |
| Docker images (5 .NET services, carrier simulator, web) and compose `app` profile | `docker compose --profile app up -d --build --wait` | All healthy |
| Playwright E2E-001 to 007, 009, 010 against compose | `tests/e2e`, screenshots in `docs/evidence/e2e` | 14 passing |
| k6 baseline 10 VUs / 1 min (Phase 1 intake only) | `docs/performance/baseline-2026-10-29.md` | Measured |
| Web: ESLint, strict typecheck | `src/Web/logistiflow-web` | Passing |
| Gateway smoke: login 401/200, proxy, dashboard, 401 anonymous, 429 after 10 logins | manual run 2026-10-29 (Phase 1) | Passing |
| Dashboard degrades with `shipping-api` stopped (`unavailableSources: ["shipping"]`, other sections present) | manual run 2026-10-29 | Passing |

## Phase 2 (done): pick, pack, ship → carrier tracking → delivery and exceptions

Spec with acceptance criteria AC-17 to AC-28: `docs/specifications/phase-2-fulfillment-to-delivery.md`.
Decisions: ADR-0006 (Shipping and carrier integration), ADR-0007 (carrier simulator), ADR-0003 follow-up (readiness).
The Fulfillment, Shipments and Exceptions pages were moved up from Phase 3 because E2E-005/006/007 need them.

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

## Next (Phase 3, then the roadmap in README.md)

1. "Average processing time" dashboard KPI (order created → shipped / delivered).
2. Responsive E2E for the new pages (E2E-010 covers the dashboard and orders only).
3. `scripts/demo-flow.ps1`: extend from Picking to delivery.
4. Phase 4: AI Operations Copilot.

## Resume checklist

```bash
docker compose --profile app up -d --build --wait
dotnet build LogistiFlow.slnx && dotnet test --project tests/integration/LogistiFlow.IntegrationTests
cd tests/e2e && npx playwright test
```
