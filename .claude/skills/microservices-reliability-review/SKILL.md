---
name: microservices-reliability-review
description: Reviews LogistiFlow messaging and consistency code (outbox/inbox, idempotency, event ordering, retries and dead-lettering, timeouts, concurrency, recovery) and reports failure scenarios with file:line evidence and a verdict. Use when a change touches a use case that publishes events, an integration event handler, a background worker, stock or status writes, or an outbound HTTP client.
argument-hint: "<path to a service, a handler, a worker or a use-case folder>"
---

# microservices-reliability-review

## Objective
Prove, for the code under review, what happens when things go wrong: a message delivered twice or out of order, a
crash between a write and a publish, two requests racing for the same stock, a downstream that is slow or down. Each
finding is a concrete failure scenario with the code that allows it.

## Input
`$ARGUMENTS`: a path such as `src/Services/Inventory`, one event handler, a worker, or a use-case folder. With no
argument, review the files changed on the current branch (`git diff --name-only develop...`).

## Procedure
1. **Map the flows.** For each use case, handler and worker in scope, write one line: trigger (HTTP, event, timer),
   state it changes, events it publishes, external calls it makes. Read ADR-0003 (outbox/inbox) and ADR-0005
   (inventory concurrency) in `docs/decisions` for the intended design.
2. **Atomicity.** Every state change that publishes an event runs inside `IUnitOfWork.ExecuteAsync` and enqueues the
   event with `IIntegrationEventOutbox.EnqueueAsync` in the same transaction. Flag any direct publish, any event
   enqueued outside the transaction, and any external call made inside it.
3. **Idempotency.** For each handler: is the inbox record written in the same transaction as the effect? Is there a
   business-key guard where a duplicate would double-count (stock movements, shipments, jobs)? For HTTP creation: is
   the `Idempotency-Key` stored with a request hash so a replay with a different body is a 409?
4. **Ordering.** Can the event arrive before its prerequisite or after a later state? Check the handler classifies it
   as applied, stale (ignored, logged) or early (`TransientIntegrationEventException`, retried). Look for state
   machines that would accept a backwards transition.
5. **Retries and poison messages.** What is retried, how many times, with what delay, and where a message goes after
   the last attempt (dead-letter queue)? Is the reason logged with the event id? Is a permanent error retried as if it
   were transient?
6. **Timeouts and downstream failure.** Every outbound `HttpClient` has a resilience handler with attempt and total
   timeouts; retries apply only to idempotent calls; a slow or failing downstream produces a recorded state (for
   example a carrier exception), not a stuck row.
7. **Concurrency.** Writes on contended rows use a conditional `UPDATE ... WHERE` or `SELECT ... FOR UPDATE` in a
   fixed lock order; invariants such as `reserved <= on_hand` hold under parallel requests; leases and queues use the
   database clock (`now()`), not the application clock.
8. **Recovery.** After a crash at each step, does the system converge? (Outbox relay resumes, leases expire, polling
   picks up stuck work.) Is there a test that proves it (integration tests in `tests/integration`)?
9. Confirm or discard each suspected scenario by reading the code and any test that covers it. Drop what you cannot cite.

## Approval criteria
- **Approved**: no High or Medium findings.
- **Approved with follow-ups**: only Medium or Low findings, each with a fix and the test that would prove it.
- **Changes required**: any High finding: a lost or duplicated business effect on a realistic failure (crash,
  redelivery, race), a publish outside the transaction, or an invariant that parallel requests can break.

## Output
A Markdown report:
1. Scope and the flow map.
2. Findings table: severity, `file:line`, failure scenario (step by step), consequence, fix, test that would prove it.
3. Guarantees confirmed, each with the code or test that provides it.
4. Verdict with the approval criterion that decided it.

## Example run
A real run on `src/Services/Inventory` is recorded in `docs/evidence/skills/microservices-reliability-review.md`.
