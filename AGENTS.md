# AGENTS.md

Instructions for coding agents working in this repository (product AI agents are a separate concern, Phase 4).

1. **Orient.** Read `STATUS.md`, the relevant spec in `docs/specifications` and the ADRs before changing code.
2. **Place code by layer.** Domain rules in `Domain` (state machines in `Domain/Services`), orchestration in
   `Application/UseCases`, SQL in `Infrastructure/Persistence`, HTTP in `Api/Controllers`.
3. **Add a use case.** Create `Contracts/`, `Interfaces/`, `Implementations/`, `Validators/` under
   `UseCases/{Aggregate}/{Action}`, register it in the service's `DependencyInjection` extension, expose it from a thin
   controller.
4. **Publish an event.** Add a versioned record in `src/Contracts/LogistiFlow.Contracts.IntegrationEvents`, enqueue it
   through the outbox inside the unit of work, register handlers with `AddLogistiFlowMessaging(..., handlers => ...)`.
5. **Change the database.** Add a new DbUp script in the service's number range; never edit an applied script.
6. **Verify.** Build with zero warnings; run unit, architecture and integration tests; run Playwright for UI-visible
   changes.
7. **Report honestly.** Record evidence in `STATUS.md`; mark anything unverified as such.

Project agents for Claude Code live in `.claude/agents`: `architecture-reviewer`, `reliability-reviewer`,
`test-automation`.

## Quality gates (CI)
.NET build and tests (unit, architecture, integration), web lint/typecheck/build, Playwright on Docker Compose,
gitleaks, CodeQL, Docker image builds.
