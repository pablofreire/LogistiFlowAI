# Example run: test-quality-review

Recorded on 2026-11-03 with Claude Code 2.1.301 (`claude-opus-5-5`). The run was headless, from the repository root:

```bash
claude -p "/test-quality-review Fulfillment" --strict-mcp-config --allowedTools "Read,Grep,Glob,Skill,Bash(git diff:*),Bash(git log:*)" --disallowedTools "Edit,Write,NotebookEdit" --output-format stream-json --verbose
```

Claude Code found the project skill and called it through the `Skill` tool. The run could only read files: it took
49 turns and 4 min 35 s, and used these tools: Read 25, Grep 14, Bash 3, Glob 3, Skill 1, PowerShell 1.

Below is the skill's output, word for word. Only the heading levels changed, and a closing offer to fix the findings
was removed. Phase 5 did not act on these findings; `STATUS.md` lists them as follow-ups. A finding was confirmed by
running code only where the report says so.

---

### Test quality review: Fulfillment service

**Verdict: Changes required.** All three Fulfillment acceptance criteria (AC-17 to AC-19) are proven or partly proven. What blocks approval is a stock path with no test at all: cancelling the warehouse job when the order is cancelled. The skill's rule is that "a High-risk path (data loss, authorization, money or stock) with no negative test" means changes are required.

I didn't run the tests because the `dotnet test` command needed approval. The test counts come from `STATUS.md:10` (Fulfillment: 25 passing), which matches the 25 test cases I counted.

### 1. Criteria

| Criterion | Test(s) | Status |
|---|---|---|
| AC-17 Pack moves the job and the order to `Packed`; ship moves both to `Shipped` | `FulfillmentActionsTests.cs:13-27`; `e2e-005-fulfillment.spec.ts:8-38`, which also checks the job is `Shipped` and the reservation is `Consumed` | Proven |
| AC-18 Invalid floor actions return 409; viewers get 403 | API 409 with the error code: `FulfillmentActionsTests.cs:30-38`. Domain: `FulfillmentJobActionTests.cs:24-39` (5 combinations, state unchanged). 403 is tested for `pack` only (`FulfillmentActionsTests.cs:41-49`) | Partly proven |
| AC-19 A failed job can be reprocessed and every change is in the history | `FulfillmentActionsTests.cs:52-69` checks the exact history `Created, Picking, Failed, Picking` and that the order stays `Picking` | Proven |
| AC-06 / AC-07 A job is created in the same warehouse; no job when there is no stock | `OrderToFulfillmentFlowTests.cs:36-38` and `:66` | Proven |
| AC-31 The Fulfillment page fits a phone screen | `e2e-010-responsive.spec.ts:45-49` | Proven |

### 2. Findings

| Sev. | Where | Gap | Regression it lets through | Test to add |
|---|---|---|---|---|
| **High** | `CancelFulfillmentForOrderUseCase.cs:24-45`, `OrderCancelledEventHandler.cs:15-22` | No test anywhere for the cancellation path. Searching `tests/` for `OrderCancelledEvent` and `CancelFulfillment` finds nothing. Untested branches: no job, rejected transition (`Packed`), cancelled, and the transient exception on a version conflict. The handler being registered is also unproven. | `OrderStateMachine.cs:15` allows `Reserved → Cancelled`, and a job already exists by then. If the handler stops firing or cancels nothing, the job stays in `Picking` after Inventory has released the stock, and an operator can pack and ship a cancelled order. | 1. Unit tests for each branch, in the same style as `PerformFulfillmentJobActionUseCaseTests`. 2. An integration test that publishes an `OrderCancelledEvent` while the job is `Picking` and then expects the job `Cancelled` and `pack` → 409. `ShipmentFlowTests.cs:146-150` already publishes an event this way. |
| Medium | `FulfillmentJobQueries.cs:37-42` | Two operators acting on one job at the same time is only tested with a mocked repository (`PerformFulfillmentJobActionUseCaseTests.cs:84-93`). No integration test triggers `fulfillment.concurrent_modification`. | If the `version = @Version` check is dropped, both requests succeed and two `FulfillmentShipped` events are published. Every test still passes. | An integration test that sends two `ship` calls at once on a `Packed` job and expects one 200, one 409, and one `FulfillmentShipped` row in the outbox. |
| Medium | `FulfillmentJobsController.cs:58-87`; `FulfillmentActionsTests.cs:41-49` | The 403 for viewers is tested on `pack` only. Each action has its own `[Authorize]`, and the fallback policy only requires a logged-in user. | If `ship`, `report-issue` or `reprocess` loses its policy or gets the wrong one, a viewer can ship. | Turn the viewer test into a `[Theory]` covering all four actions. |
| Medium | `e2e-005-fulfillment.spec.ts:40-46` | The viewer test creates no data, so `toHaveCount(0)` also passes on an empty queue. | The pack button being shown to viewers would go unnoticed when the queue is empty. | Create an order through the API, check its row is visible, then check that row has no action buttons. |
| Medium | `FulfillmentJobQueries.cs:62-73`; `OrderToFulfillmentFlowTests.cs:85` | For metrics, only `totalJobs` is checked. `byStatus` and the per-warehouse open and failed counts are never checked. | A broken failed-jobs count gives the copilot a wrong answer to "Which warehouse has the highest number of fulfillment failures?" with every test still green. | Report an issue on a job, then expect that warehouse's `failed` count to go up by 1 and `open` to go down by 1. |
| Medium | `FulfillmentJobEntityTests.cs:41-52` | The state machine test covers 7 of the 36 status pairs. Orders checks all 81 of its pairs (AC-05). `Failed → Cancelled`, which the cancellation path relies on, isn't covered. | Adding a transition such as `Shipped → Failed` or `Packed → Picking`, or removing `Failed → Cancelled`, passes. | A test over all 36 pairs, checked against an explicit table. |
| Low | `PerformFulfillmentJobActionUseCaseTests.cs:38`, `:50-52` | Event checks are partial: pack checks only `OrderId`; ship doesn't check `Sku`, `WarehouseCode` or `OrderNumber`. `ShipmentFlowTests` partly covers the shipped event downstream. | A wrong warehouse in `FulfillmentPacked` goes unnoticed. | Compare the whole event against an expected one. |
| Low | `FulfillmentJobsController.cs:22-46` | Fulfillment endpoints have no 401 test (only the shared `FallbackPolicy_RejectsAnonymousUsers`). The 404 for an unknown job id is only checked at use-case level. Filtering the job list by status or warehouse, and paging, are untested. | A wrong error mapping, or a broken `ListFilter`. | One API test covering anonymous → 401, unknown id → 404 and `?status=Packed`. |
| Low | `PerformFulfillmentJobActionRequestValidator.cs:13-14` | There's no test for the validator on its own: `Reason` over 500 characters and `PerformedBy` over 120 aren't tested. | Loosened limits. | `PerformFulfillmentJobActionRequestValidatorTests`. |

Determinism is fine. Integration tests run one collection at a time (`AssemblyInfo.cs:5`) and Playwright uses one worker, so the "+1" count checks don't race. The `Task.Delay` calls only appear inside polling helpers. No assertion depends on the clock, so `TimeProvider.System` in the unit tests is not a problem.

### 3. Strengths worth keeping
- Status histories are checked against exact lists (`FulfillmentActionsTests.cs:63-64`, `OrderToFulfillmentFlowTests.cs:41-42`), so an extra or missing step fails the test.
- Rejected actions check both that state is unchanged and that no history was added (`FulfillmentJobActionTests.cs:36-38`), and that nothing was published (`:92`).
- 409 responses are checked by error code, not only by status.
- E2E-005 uses `data-testid` selectors, creates its own order, waits on what is visible, saves screenshots, and checks the effect in other services (reservation `Consumed`).

### 4. Most likely regressions
1. **Cancellation path breaks.** Nothing would fail; this is the High finding.
2. **The version check in `UpdateStatus` is lost in a refactor.** Nothing would fail; this is the Medium concurrency finding.
3. **A wrong transition is added to the state machine.** This is caught only if it is one of the 7 pairs the test covers.

Fixing the High finding plus the concurrency and 403 tests would move this to "Approved with follow-ups."
