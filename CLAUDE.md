# CLAUDE.md — LogistiFlow AI

## Ground rules
- Facts over assumptions: base every change on the code, the spec (`docs/specifications`) and the ADRs (`docs/decisions`).
- English for code, comments, commits and docs.
- Never claim something works without a test or a recorded run; update `STATUS.md` with evidence.
- Never push, publish or create cloud resources without explicit authorization.

## Conventions
- Services: `src/Services/{Service}/LogistiFlow.{Service}.{Domain,Application,Infrastructure,Api}`. A service never
  references another service; they share only `LogistiFlow.Contracts.IntegrationEvents` (versioned records).
- Use cases: `UseCases/{Aggregate}/{Action}/{Contracts,Interfaces,Implementations,Validators}`. One artifact kind per
  folder; an interface never shares a folder with a class. Suffixes: `Entity`, `UseCase`, `RequestValidator`,
  `Repository`, `EventHandler`, `Options`, `Controller`.
- Data: Dapper with parameterized SQL in `Infrastructure/Persistence/Queries`; DbUp scripts
  `{NNNN}_{schema}_{entity}_{action}.sql`; `id` internal, `public_id` exposed; timestamps from the database clock
  for queues and leases.
- State changes that publish events: `IUnitOfWork.ExecuteAsync` + `IIntegrationEventOutbox.EnqueueAsync` in the same
  transaction. Event handlers must be idempotent and classify events as applied / stale / early.
- API: controllers inherit `BaseApiController`, return the `ApiResponse<T>` envelope, depend on permission policies.
- Architecture rules are tests (`tests/architecture`); keep them green rather than weakening them.

## Commands
```bash
dotnet build LogistiFlow.slnx
dotnet test --project tests/unit/LogistiFlow.Orders.UnitTests        # Microsoft Testing Platform runner
dotnet test --project tests/integration/LogistiFlow.IntegrationTests # needs Docker
docker compose --profile app up -d --build --wait                    # full stack: web :8088, gateway :8089
cd tests/e2e && npx playwright test
cd src/Web/logistiflow-web && npm run lint && npm run typecheck && npm run generate:api
```

## Definition of done
Build with 0 warnings · relevant unit, architecture, integration and E2E tests green · contracts regenerated if an API
changed (`docs/api`, `npm run generate:api`) · docs and `STATUS.md` updated with evidence.
