# Example run: microservices-reliability-review

Recorded on 2026-11-03 with Claude Code 2.1.301 (`claude-opus-5-5`). The run was headless, from the repository root:

```bash
claude -p "/microservices-reliability-review src/Services/Inventory" --strict-mcp-config --allowedTools "Read,Grep,Glob,Skill,Bash(git diff:*),Bash(git log:*)" --disallowedTools "Edit,Write,NotebookEdit" --output-format stream-json --verbose
```

Claude Code found the project skill and called it through the `Skill` tool. The run could only read files: it took
61 turns and 6 min 26 s, and used these tools: Read 38, Grep 13, Glob 7, Skill 1.

Below is the skill's output, word for word. Only the heading levels changed, and a closing offer to fix the findings
was removed. Phase 5 did not act on these findings; `STATUS.md` lists them as follow-ups. A finding was confirmed by
running code only where the report says so.

---

### Reliability review: `src/Services/Inventory`

**Verdict: Approved with follow-ups.** I found no High findings: four Medium and one Low. Two of the Medium ones are races that can't happen today, because only one Inventory instance runs and it handles one message at a time. They become real bugs as soon as a second instance is added.

### 1. Scope and flow map

| Flow | Trigger | State change | Publishes | Outbound calls |
|---|---|---|---|---|
| `ReserveInventoryUseCase` | `OrderCreated` | `stock_positions.reserved += q` (conditional), reservation row, ledger | `InventoryReserved` / `InventoryReservationFailed` | none |
| `ReleaseInventoryUseCase` | `OrderCancelled` | `reserved -= q`, status `Released`, or a `Cancelled` tombstone if no reservation exists yet | `InventoryReleased` | none |
| `ConsumeReservationUseCase` | `FulfillmentShipped` | `on_hand -= q`, `reserved -= q`, status `Consumed`, ledger | none | none |
| `ReceiveStockUseCase` | `POST /api/inventory/stock/receipts` | `on_hand += q` (upsert), ledger | none | none |
| `CreateProductUseCase` | HTTP POST | product row (unique SKU) | none | none |

**Concurrency today:** the consumer never sets `ConsumerDispatchConcurrency` (no match in `src/`; `RabbitMqConnectionProvider.cs:35-45`), so RabbitMQ.Client 7 handles one delivery at a time per channel. No compose file sets `replicas`. No HTTP route reaches Release or Consume: `ReservationsController.cs:14-19` is read-only.

### 2. Findings

| # | Sev | Where | Failure scenario | Consequence | Fix | Test that would prove it |
|---|---|---|---|---|---|---|
| 1 | **Medium** (needs 2+ consumers) | `ReleaseInventoryUseCase.cs:34-38` | 1. Instance A takes `OrderCreated` and instance B takes `OrderCancelled` for the same order. 2. Both read "no reservation". 3. A reserves stock and inserts the reservation. 4. B's tombstone insert waits on `ix_reservations_order_id`; once A commits, `ON CONFLICT DO NOTHING` makes `TryAddAsync` return `false`. 5. That `false` is ignored: B returns `Cancelled, 0` and its inbox row commits. Compare `ReserveInventoryUseCase.cs:66-69`, which does check it. | The cancelled order keeps its stock reserved forever and nothing releases it (a lost compensation). `InventoryReserved` is still published. | When `TryAddAsync` is `false`, throw `TransientIntegrationEventException` so the retry finds the `Reserved` row and releases it. | Run reserve and release for one order in parallel against `IsolatedInventory`; assert final status is `Released` or `Cancelled` and `reserved` is back to its starting value. |
| 2 | **Medium** (needs 2+ consumers) | `ReservationQueries.cs:27-29`, `ReservationRepository.cs:13-20`, `ConsumeReservationUseCase.cs:30-56`, `ReleaseInventoryUseCase.cs:41-52` | 1. Release and Consume for the same order run in parallel. 2. Both read `Reserved`; the read takes no row lock. 3. The per-row stock updates run one after the other. If other orders also hold units on that position, both `WHERE reserved >= @Quantity` checks pass. 4. `reserved` drops by 2q while `on_hand` drops by q. 5. `UpdateStatus` has no status guard, so the last writer wins. | The reservation is both released and consumed. Units held for other orders stop being counted as reserved, so later orders can oversell. `ck_stock_positions_quantities` doesn't catch it because `reserved >= 0` still holds. | Make the update conditional: `UPDATE … SET status=@Status WHERE id=@Id AND status='Reserved'`, run it first, and treat 0 rows as transient. Fulfillment already does this with `TryUpdateStatusAsync`. Alternatively, `SELECT … FOR UPDATE` in `SelectByOrderId`. | Run Release and Consume in parallel on an order whose stock position also holds another order's reservation; assert exactly one of them applies and `reserved` equals the other order's quantity. |
| 3 | **Medium** | `ConsumeReservationUseCase.cs:33-42`; root cause outside this scope at `CancelFulfillmentForOrderUseCase.cs:27-31` | 1. `InventoryReserved` fails once at Fulfillment and goes to a retry queue (1 s, 5 s or 15 s). 2. Meanwhile the order is cancelled; Orders allows `Reserved → Cancelled` (`OrderStateMachine.cs:15`). 3. Fulfillment handles `OrderCancelled` first, finds no job and writes no tombstone. 4. The retried `InventoryReserved` creates a job (`StartFulfillmentUseCase.cs:29-45`). 5. Inventory has already released the reservation. 6. The job ships, `FulfillmentShipped` hits a `Released` reservation, `InvalidOperationException` is thrown, and after 4 attempts the message is dead-lettered. | Inventory gave the reserved units back, but `on_hand` still counts units that physically left. Available stock is overstated by q, which can lead to overselling. The dead-letter reason is logged with the event id, so the problem is visible, but it is never recorded as inventory state. | Inventory: when a shipment arrives for a `Released` or `Cancelled` reservation, still subtract `on_hand`, record a `Shipment` movement and mark the reservation with a discrepancy status. Fulfillment (separate follow-up): write a cancellation tombstone when no job exists, like Inventory already does. | Dispatch `FulfillmentShipped` for an order whose reservation is `Released`; assert `on_hand` is reduced by q and the discrepancy is recorded. No test covers the `Released` branch today. |
| 4 | **Medium** | `StockController.cs:33-39`, `ReceiveStockUseCase.cs:33-48`, `0205_inventory_stock_movements_create.sql:15` (index is not unique) | 1. A client posts a receipt. 2. The commit succeeds but the response is lost. 3. The client retries with the same body. | `on_hand` goes up twice and the ledger gets two `Receipt` rows for one `Reference`. That stock then backs reservations. | Require `Idempotency-Key`, stored with a request hash (409 if the same key comes with a different body), as `POST /api/orders` already does. Or add a unique index on `(reference_type, reference_id)` for receipts. Caveat: the spec only requires the key for orders (`phase-1-order-to-fulfillment.md:10`), and nothing in the repo retries this POST automatically. | Post the same receipt twice; assert `on_hand` changes once and the replay returns the original result. |
| 5 | Low | `RabbitMqConsumerWorker.cs:156-167` (shared building blocks code) | `TransientIntegrationEventException` (an event that arrived early) and `InvalidOperationException` (a permanent error) go down the same path: 4 attempts, then the dead-letter queue. | A permanent error such as finding 3 is retried pointlessly for about 21 s before it is dead-lettered. | Send anything that isn't transient straight to the dead-letter queue. | Extend `RetryAndDeadLetterTests`: a permanent failure reaches `.dlq` on attempt 1. |

### 3. Guarantees confirmed

- **Publishing is atomic with the state change.**
  - `EnqueueAsync` refuses to run without a transaction and writes on `session.Transaction` (`OutboxIntegrationEventWriter.cs:20-39`).
  - Reserve and Release enqueue inside `unitOfWork.ExecuteAsync`.
  - Inventory makes no outbound HTTP calls, so no external call can happen inside a transaction (step 6 doesn't apply).
- **Inbox and effect commit together.** The inbox insert and the handler run in one transaction (`IntegrationEventDispatcher.cs:49-60`). A nested unit of work joins it (`DbUnitOfWork.cs:16-19`). All three handlers throw on failure (`OrderCreatedEventHandler.cs:19-22` and the equivalent lines in the other two), so the inbox row rolls back with the effect. Proven by `RedeliveredEvent_IsAppliedOnce_ThroughTheInbox`.
- **Each order has a second idempotency guard.**
  - The unique `order_id` index (`0204…sql:20`) catches a repeated `OrderCreated`, proven by `SameOrderPublishedTwiceWithDifferentEventIds_IsReservedOnce`.
  - The status check catches a repeated `FulfillmentShipped`, proven by `ShipmentFlowTests.ShippedOrder_ConsumesReservedStockOnce`.
- **No overselling on reservations.**
  - Reserving is one conditional `UPDATE` (`StockQueries.cs:19-23`), backed by the `CHECK` constraint (`0203…sql:15`).
  - Locks are taken in a fixed order: warehouse code, then stock position id (`StockRepository.cs:29`, `ReserveInventoryUseCase.cs:99`).
  - Proven by `ConcurrentReservations_ForTheLastUnits_NeverOversell` (50 parallel orders, 10 units).
- **Events that arrive out of order are classified.**
  - An early `FulfillmentShipped` is treated as transient and retried (`ConsumeReservationUseCase.cs:31`).
  - A cancellation that overtakes the order leaves a tombstone, proven by `CancellationThatOvertakesTheOrder_LeavesATombstone_…`.
  - A repeated release is a no-op, proven by `Release_ReturnsReservedUnits_AndIsIdempotent`.
- **Recovery after a crash.**
  - The consumer acknowledges only after the commit; a redelivery is caught by the inbox.
  - Outbox leases use the database clock (`OutboxQueries.cs:38`).
  - Broker outages are proven by `OutboxResilienceTests`; retries and dead-lettering by `RetryAndDeadLetterTests`.

### 4. Verdict

**Approved with follow-ups.** The rule is "only Medium or Low findings, each with a fix and a test", and that holds.

Findings 1 and 2 only rate Medium because Inventory runs one consumer instance that handles one message at a time. ADR-0003:38 names that sequential consumer as the throughput limit, so the obvious way to raise throughput is a second instance. Once a second instance runs, both findings become **High**, so fix them before scaling Inventory out.
