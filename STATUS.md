# LogistiFlow AI — Status

Single source of truth for progress and the resume point of every work session. Last updated: 2026-11-03.

## Verified (with evidence)

| Area | Evidence | Status |
|---|---|---|
| Build: .NET 10, warnings as errors, `latest-recommended` analyzers | `dotnet build LogistiFlow.slnx` → 0 warnings, 0 errors | Passing |
| Unit tests (BuildingBlocks 25, Orders 130, Inventory 15, Fulfillment 25, Shipping 26, AiOperations 49, McpServer 45) | `tests/unit` | 315 passing |
| Architecture tests (service isolation incl. Shipping, AiOperations, the carrier simulator and the MCP server, layers, folders, suffixes) | `tests/architecture` | 51 passing |
| Integration tests on PostgreSQL 17 + RabbitMQ 4.1 (Testcontainers), carrier simulator in-process | `tests/integration` | 56 passing (incl. 8 copilot and 4 MCP server tests against the real services) |
| Docker images (6 .NET services, carrier simulator, web) and compose `app` profile | `docker compose --profile app up -d --build --wait` | All healthy |
| Playwright E2E-001 to 010 against compose (E2E-008 copilot; E2E-010 covers every page on a phone viewport) | `tests/e2e`, screenshots in `docs/evidence/e2e` | 17 passing |
| k6 baseline 10 VUs / 1 min (Phase 1 intake only) | `docs/performance/baseline-2026-10-29.md` | Measured |
| Web: ESLint, strict typecheck | `src/Web/logistiflow-web` | Passing |
| Gateway smoke: login 401/200, proxy, dashboard, 401 anonymous, 429 after 10 logins | manual run 2026-10-29 (Phase 1) | Passing |
| Demo flow from intake to delivery (`scripts/demo-flow.ps1`) | run 2026-11-03: order Delivered via RDRN, 5 tracking events, out-of-stock order Failed | Passing |
| Platform keeps working with `aioperations-api` stopped: dashboard, orders, shipments 200; `/api/ai` 502; copilot page shows "Copilot unavailable" | manual run 2026-11-03, `docs/evidence/e2e/e2e-manual-copilot-unavailable.png` | Passing |
| MCP server used by Claude Code 2.1.301 through `.mcp.json` against compose: five tools, answers cross-checked with the gateway | `docs/evidence/mcp/claude-code-validation.md` (run 1 found a defect, run 2 after the fix) | Passing |
| Skills `dotnet-api-review`, `microservices-reliability-review`, `test-quality-review` found and run by Claude Code on this repository | `docs/evidence/skills/*.md` | Run, findings recorded |
| GitHub Actions on `main` and `develop` pushes: CI (build, tests, web, 8 Docker images, gitleaks, Playwright E2E on compose) and CodeQL | `github.com/pablofreire/LogistiFlowAI/actions`: runs 41707611529 (`main` 6e66428) and 41707650865 (`develop` 48f5e93), 2026-11-03 | Passing |
| Dashboard degrades with `shipping-api` stopped (`unavailableSources: ["shipping"]`, other sections present) | manual run 2026-10-29 | Passing |

## Repository and CI

Published to `github.com/pablofreire/LogistiFlowAI` on 2026-11-03 with git flow (`main` and `develop`), both protected
by 14 required checks. The first GitHub Actions run failed to build: the `[Rr]elease/` ignore rule had kept the
Inventory release use case out of git. The hotfix (#2 into `main`, #3 into `develop`) was merged and the push runs on
both branches are green, including the Docker image matrix and its `build-args` fix.

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

## Phase 4 (done): AI Operations Copilot

Spec with acceptance criteria AC-32 to AC-40: `docs/specifications/phase-4-ai-operations-copilot.md`. Decision: ADR-0008.
- New AiOperations service on Microsoft Agent Framework 1.26.0: nine read-only tools that read the services with the
  caller's token, a deterministic demo provider (labelled as simulated) that runs the same tool loop, an
  OpenAI-compatible provider by configuration, per-question audit (tool calls, tokens, cost; never the token).
- Copilot page in the console; Viewer has no copilot (UI and 403); the audit list is for Admin and OpsManager.
- Every verification used the demo provider. No real AI model has been called.

## Phase 5 (done): MCP server and Claude Code skills

Spec with acceptance criteria AC-41 to AC-50: `docs/specifications/phase-5-mcp-server-and-skills.md`. Decision: ADR-0009.
- `src/Mcp/LogistiFlow.McpServer` uses the official C# SDK `ModelContextProtocol` 2.3.0 over stdio. It has five
  read-only tools: `logistics_get_order`, `_get_inventory`, `_get_shipment`, `_list_exceptions` and
  `_get_warehouse_metrics`. They read through the gateway as a configured user: a token, or an e-mail and password
  from the environment, with Viewer as the default.
- **Defect found by the first real Claude Code session.** The SDK leaves out null properties, but its output schema marks
  them as required, so Claude Code rejected any result that contained a null. Nulls are now serialised. A new
  schema-conformance regression test fails without the fix. The other tests had passed because the SDK's C# client
  does not validate output schemas.
- **Skills** live in `.claude/skills`. Each one ran once, headless in Claude Code with read-only tools, and its report is in
  `docs/evidence/skills`. Their findings are follow-ups and were not fixed in this phase (see below).

### Follow-ups from the skill runs (not yet fixed, and not yet confirmed by running code)

- **Shipping API** (`dotnet-api-review`, verdict: changes required):
  - The list endpoints can answer 400 from model binding, but neither `[ProducesResponseType]` nor the OpenAPI
    document declares it.
  - `PageRequest` has no maximum for `page`. A very large value overflows the offset and the request returns 500.
  - There are minor validation and audit gaps.
  - The OpenAPI document declares no 401/403 responses and no security scheme.
- **Inventory** (`microservices-reliability-review`, verdict: approved with follow-ups):
  - Two races need fixing before Inventory is scaled out. They only happen with a second consumer instance: release
    against the reserve tombstone, and release against consume with no status guard.
  - A shipment for a reservation that was already released goes to the dead-letter queue without adjusting `on_hand`.
  - Stock receipts are not idempotent.
  - Permanent errors are retried as if they were transient.
- **Fulfillment tests** (`test-quality-review`, verdict: changes required):
  - The job cancellation path (`OrderCancelled`) has no tests.
  - No integration test covers two actions on the same job at once.
  - The Viewer 403 is tested on `pack` only.
  - The metrics `failed` and `open` counts are never asserted.
  - The state machine test covers 7 of the 36 transitions.

## Not yet verified

- No security review pass beyond the automated tests; no OWASP ZAP run.
- No load test of the Phase 2 paths (shipping, carrier polling) or of the copilot.
- The OpenAI-compatible provider path is wired but has never answered a real question (no credentials used).
- Copilot write actions (preview, confirmation, idempotency) are out of scope until their own spec (master prompt §5.3).
- Known limitation: a consumer added to a running system starts from "now"; events published before its first start
  need a manual backfill (ADR-0003). On a fresh start the readiness gate prevents the loss.
- Known limitation: the carrier simulator keeps bookings in memory; restarting it orphans the tracking of shipments in
  flight (ADR-0007).
- Known bottleneck: sequential consumers (~154 orders/s end to end vs 539 orders/s intake).

## Next (the roadmap in README.md)

1. The skill follow-ups above, starting with the Fulfillment cancellation tests and the Shipping 400 and overflow fixes.
2. Phase 6: observability stack, consumer concurrency (after the Inventory race fixes), full k6 suite.
3. UI polish (listed under Phase 3 in the README, not started).

## Resume checklist

```bash
docker compose --profile app up -d --build --wait
dotnet build LogistiFlow.slnx && dotnet test --project tests/integration/LogistiFlow.IntegrationTests
cd tests/e2e && npx playwright test
```
