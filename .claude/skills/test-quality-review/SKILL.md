---
name: test-quality-review
description: Reviews the quality of LogistiFlow tests for a feature or service (unit, integration, E2E): whether they prove the acceptance criteria, cover negative and failure paths, assert outcomes rather than calls, and stay deterministic. Reports gaps and regression risks with file:line evidence and a verdict. Use before closing a phase or a PR, or when a test is flaky.
argument-hint: "<feature, service or spec to assess, e.g. Fulfillment or docs/specifications/phase-2-fulfillment-to-delivery.md>"
---

# test-quality-review

## Objective
Decide whether the tests would catch a regression in the area under review, and say exactly which change would slip
through. The point is the risk left uncovered, not the test count.

## Input
`$ARGUMENTS`: a service name (`Fulfillment`), a spec in `docs/specifications`, or test paths. With no argument, review
the tests for the code changed on the current branch.

## Procedure
1. **Map criteria to tests.** From the relevant spec, list each acceptance criterion and its "Verified by" column.
   Open every named test and confirm it exists and asserts the criterion, not just something near it. Mark each
   criterion as proven, partly proven or unproven.
2. **Inventory the tests.** For the area, list unit tests (`tests/unit`), integration tests (`tests/integration`,
   Testcontainers PostgreSQL and RabbitMQ, real APIs) and E2E specs (`tests/e2e/specs`). Note which layer proves what.
3. **Negative and failure paths.** For each use case and endpoint: invalid input (400), not found (404), conflict
   (409), unauthenticated (401), forbidden (403), and the domain's own failures (insufficient stock, invalid state
   transition, carrier errors). For messaging: duplicate delivery, out-of-order delivery, poison message.
4. **Assertion strength.** Prefer asserted outcomes (persisted state, response body, emitted event) over interaction
   checks alone. Flag assertions that would pass on a wrong result (`NotBeNull`, `BeGreaterThan(0)` where an exact
   value is knowable, status-only checks on a body that matters).
5. **Determinism.** Flag fixed sleeps (`Task.Delay`, `waitForTimeout`) used instead of polling a condition, tests that
   depend on wall-clock time instead of `FakeTimeProvider`, tests that depend on data other tests create or on
   execution order, and shared state without isolation.
6. **E2E quality.** Stable selectors (`data-testid`), each spec creates its own data, waits on visible state, and
   saves evidence; the spec covers the user-visible promise of the feature, not only that a page loads.
7. **Regression risk.** Name the two or three most likely regressions in this area (from recent changes, complex
   logic or past failures recorded in `STATUS.md`) and whether a test would fail for each.
8. Confirm each gap by searching the test projects for a test you may have missed before reporting it.

## Approval criteria
- **Approved**: every acceptance criterion proven, and no High findings.
- **Approved with follow-ups**: all criteria proven or partly proven, only Medium or Low findings.
- **Changes required**: an acceptance criterion with no test, a High-risk path (data loss, authorization, money or
  stock) with no negative test, or a test that cannot fail.

## Output
A Markdown report:
1. Criteria table: criterion, test(s), proven / partly / unproven.
2. Findings table: severity, `file:line`, the gap or weakness, the regression it would let through, the test to add.
3. Strengths worth keeping (short).
4. Verdict with the approval criterion that decided it.

## Example run
A real run on the Fulfillment service is recorded in `docs/evidence/skills/test-quality-review.md`.
