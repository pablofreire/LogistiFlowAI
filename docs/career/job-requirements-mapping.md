# Job requirements mapping

Status legend: **Validated** = implemented and proven by an automated test or a recorded run · **Implemented** =
in the code, not yet proven end to end · **Planned** = on the roadmap, not started.

| Job requirement | Project evidence | Status |
|---|---|---|
| C# / .NET | .NET 10 services, warnings as errors, analyzers (`src/`) | Validated (local build with 0 warnings, Docker Release publish, 315 unit tests) |
| REST APIs | Orders, Inventory, Fulfillment, Gateway controllers; OpenAPI in `docs/api` | Validated (integration + E2E) |
| Microservices | 5 services plus a carrier simulator, database per service, isolation enforced by `tests/architecture/ServiceBoundaryTests` | Validated |
| Event-driven integration | RabbitMQ, transactional outbox, inbox, retry tiers, DLQ (`src/BuildingBlocks/LogistiFlow.BuildingBlocks.Messaging`) | Validated (`tests/integration/Messaging`) |
| Reliability | Idempotency keys, optimistic concurrency, conditional stock UPDATE, compensation | Validated (`OrdersApiTests`, `InventoryReservationTests`, `OrderEventOrderingTests`) |
| Third-party integration resilience | Carrier calls outside message transactions, per-call timeout, durable backoff, idempotent booking, tracking de-duplication | Validated (`CarrierScenarioTests`: 503, timeout, delay, redelivery, return) |
| SQL / PostgreSQL / Dapper | Hand-written SQL, DbUp migrations, `SKIP LOCKED`, `ON CONFLICT` | Validated |
| TypeScript / React | `src/Web/logistiflow-web` (strict TS, types generated from OpenAPI) | Validated (lint, typecheck, build, Playwright) |
| Unit testing | xUnit v3, NSubstitute, AwesomeAssertions — 315 tests | Validated |
| Integration testing | Testcontainers PostgreSQL + RabbitMQ, carrier simulator in-process — 56 tests | Validated |
| E2E testing | Playwright TS against Docker Compose — 17 tests (E2E-001..010) | Validated |
| Performance testing | k6 baseline, real numbers and a documented bottleneck (`docs/performance`) | Validated (baseline only; Phase 2 paths not load tested) |
| DevOps / CI | GitHub Actions: build, tests, E2E on compose, gitleaks, CodeQL, Docker images | Validated (green on GitHub Actions for the hotfix pull requests, 2026-11-03) |
| Containers | Multi-stage, non-root images; compose with init-container migrations | Validated (local) |
| Cloud (Azure) | Container Apps + PostgreSQL Flexible + Key Vault design | Planned (Phase 7) |
| Security | JWT, permission policies, rate limit, BCrypt, parameterized SQL, CSP | Validated (tests); security review pending |
| Observability | Serilog JSON, correlation ids, OpenTelemetry traces/metrics, health checks | Implemented (dashboards in Phase 6) |
| AI-powered applications | AI Operations Copilot on Microsoft Agent Framework: read-only tools over the services with the caller's token, deterministic demo provider, audit, token and cost tracking | Validated (demo provider; no real model run) |
| Custom MCP server | `LogistiFlow.McpServer`: official C# SDK, stdio, five read-only tools through the gateway, structured results checked against their output schemas | Validated (unit, integration and a recorded Claude Code session, `docs/evidence/mcp`) |
| AI development tools | Claude Code agents in `.claude/agents`; skills `dotnet-api-review`, `microservices-reliability-review`, `test-quality-review` in `.claude/skills` | Validated (each run in Claude Code on this repository, `docs/evidence/skills`) |
| Logistics domain | Orders, multi-warehouse reservation, pick/pack/ship, carrier assignment, tracking, delivery estimates, delays and exceptions, stock ledger | Validated |
| Technical communication | ADRs, spec with acceptance criteria, architecture overview | Implemented |
