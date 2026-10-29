# Job requirements mapping

Status legend: **Validated** = implemented and proven by an automated test or a recorded run · **Implemented** =
in the code, not yet proven end to end · **Planned** = on the roadmap, not started.

| Job requirement | Project evidence | Status |
|---|---|---|
| C# / .NET | .NET 10 services, warnings as errors, analyzers (`src/`) | Validated (local build with 0 warnings, Docker Release publish, 170 unit tests) |
| REST APIs | Orders, Inventory, Fulfillment, Gateway controllers; OpenAPI in `docs/api` | Validated (integration + E2E) |
| Microservices | 4 deployables, database per service, isolation enforced by `tests/architecture/ServiceBoundaryTests` | Validated |
| Event-driven integration | RabbitMQ, transactional outbox, inbox, retry tiers, DLQ (`src/BuildingBlocks/LogistiFlow.BuildingBlocks.Messaging`) | Validated (`tests/integration/Messaging`) |
| Reliability | Idempotency keys, optimistic concurrency, conditional stock UPDATE, compensation | Validated (`OrdersApiTests`, `InventoryReservationTests`, `OrderEventOrderingTests`) |
| SQL / PostgreSQL / Dapper | Hand-written SQL, DbUp migrations, `SKIP LOCKED`, `ON CONFLICT` | Validated |
| TypeScript / React | `src/Web/logistiflow-web` (strict TS, types generated from OpenAPI) | Validated (lint, typecheck, build, Playwright) |
| Unit testing | xUnit v3, NSubstitute, AwesomeAssertions — 170 tests | Validated |
| Integration testing | Testcontainers PostgreSQL + RabbitMQ — 25 tests | Validated |
| E2E testing | Playwright TS against Docker Compose — 10 tests (E2E-001..004, 009, 010) | Validated |
| Performance testing | k6 baseline, real numbers and a documented bottleneck (`docs/performance`) | Validated (baseline only) |
| DevOps / CI | GitHub Actions: build, tests, E2E on compose, gitleaks, CodeQL, Docker images | Implemented (not yet run on GitHub) |
| Containers | Multi-stage, non-root images; compose with init-container migrations | Validated (local) |
| Cloud (Azure) | Container Apps + PostgreSQL Flexible + Key Vault design | Planned (Phase 7) |
| Security | JWT, permission policies, rate limit, BCrypt, parameterized SQL, CSP | Validated (tests); security review pending |
| Observability | Serilog JSON, correlation ids, OpenTelemetry traces/metrics, health checks | Implemented (dashboards in Phase 6) |
| AI-powered applications | AI Operations Copilot on Microsoft Agent Framework with read-only tools | Planned (Phase 4) |
| Custom MCP server | `LogistiFlow.McpServer` | Planned (Phase 5) |
| AI development tools | Claude Code agents in `.claude/agents`; skills planned for Phase 5 | Implemented (agents) |
| Logistics domain | Orders, multi-warehouse reservation, fulfillment jobs, stock ledger | Validated |
| Technical communication | ADRs, spec with acceptance criteria, architecture overview | Implemented |
