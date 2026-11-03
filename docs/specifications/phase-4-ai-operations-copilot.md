# Spec — Phase 4: AI Operations Copilot

Status: in progress (started 2026-11-03).

## Objective
Let operators ask questions about live operations ("Why is order ORD-1025 delayed?") and get answers built only from
data the platform returns. The copilot reads through the same APIs the console uses, as the signed-in user, and every
question, tool call and token count is audited. It works without any AI credentials, through a deterministic demo
provider that is labelled as simulated everywhere it appears.

## Scope

| In | Out (later phase) |
|---|---|
| New **AiOperations** service: agent orchestrator, tool registry, tool execution over HTTP with the caller's token, conversation context, structured responses, audit trail, token and cost tracking, timeouts and retries | Actions that change state (cancel, resolve, retry). The master prompt (§5.3) starts the copilot read-only; write actions need preview, explicit confirmation, idempotency and audit, and get their own spec |
| Model provider abstraction (`IChatClient`): an OpenAI-compatible provider (OpenAI or Azure AI Foundry's v1 endpoint) when configured, otherwise the deterministic demo provider | Streaming answers, multi-agent workflows, retrieval over documents |
| Nine read-only tools (below), each with a typed contract, validation, error handling and the service's own authorization | Tools over data no service exposes yet |
| Gateway route `/api/ai/{**rest}`; Copilot page in the web console | Voice, attachments |
| E2E-008; unit, architecture and integration tests | Evaluations against a real model (no credentials in CI) |

## How a question is answered

```
Web ─▶ Gateway (JWT) ─▶ AiOperations  POST /api/ai/copilot/ask
                          ├─ ChatClientAgent (Microsoft Agent Framework) over IChatClient
                          │    ├─ demo provider (deterministic) ── or ── OpenAI-compatible provider
                          │    └─ function invocation loop ─▶ tool registry
                          │                                    └─ typed HTTP clients ─▶ Orders / Inventory /
                          │                                       (caller's bearer token)  Fulfillment / Shipping
                          └─ audit: question, answer, provider, tool calls, tokens, cost ─▶ logistiflow_aiops
```

- The tools call the services directly with the caller's `Authorization` header, so a user can never learn through
  the copilot what the service would refuse them. The token is never stored.
- The **demo provider** is an `IChatClient`: it picks tools for the question with fixed rules, lets the agent
  framework run them, and writes the answer only from the tool results. A tool that fails is reported as failed; a
  question it does not recognise gets the list of questions it supports, never a guess.
- Every response carries `provider`, `model` and `simulated`; the UI shows a "Simulated" badge for the demo provider.

## Tools

| Tool | Reads | Used for |
|---|---|---|
| `GetOrderDetails(orderNumber)` | Orders: search by number, details, history | Status, reason and timeline of an order |
| `GetInventoryAvailability(sku)` | Inventory: availability by SKU | What stock exists where |
| `GetFulfillmentStatus(orderNumber)` | Fulfillment: job by order | Warehouse job status and history |
| `GetShipmentTracking(orderNumber)` | Shipping: shipment by order | Carrier, tracking events, estimates, exceptions |
| `AnalyzeShipmentDelay(orderNumber)` | Shipping: shipment by order | Promised vs. estimated date, days late, open delay exceptions |
| `ListShipmentsAtRisk()` | Shipping: undelivered shipments | Shipments with an open delay or an estimate after the promised date |
| `ListOperationalExceptions(sinceHours, status)` | Shipping: exceptions | Exceptions opened in a time window, by type |
| `GetWarehouseMetrics()` | Fulfillment and Inventory metrics | Open and failed jobs per warehouse, low stock |
| `SuggestRecoveryActions(orderNumber)` | Shipping: shipment by order | A fixed playbook per open exception type; suggestions only, nothing is executed |

## Supported demo questions
The demo provider recognises these (order numbers are taken from the question):
"Why is order ORD-1025 delayed?", "Show shipments at risk of missing their delivery deadline.", "Which warehouse has
the highest number of fulfillment failures?", "Summarize operational exceptions from the last 24 hours.", "What caused
the inventory reservation failure for ORD-1025?", "Suggest the next actions for delayed shipments."

## Cost and safety limits
Question length (500 characters), maximum tool rounds per question, maximum output tokens, a per-question timeout,
and a configured price per thousand input and output tokens for the cost estimate (the demo provider reports zero
tokens and zero cost). The copilot is available to Admin, OpsManager and WarehouseOperator; a Viewer gets 403.

## Requirements and acceptance criteria

| Id | Acceptance criterion | Verified by |
|---|---|---|
| AC-32 | Without AI configuration the service answers with the demo provider and marks every answer `simulated: true` | (pending) |
| AC-33 | "Why is order X delayed?" for a `Sandbox Delay` order runs `AnalyzeShipmentDelay` and answers with the delay exception's details and the promised and estimated dates | (pending) |
| AC-34 | Each of the six demo questions runs its tools and answers only from their results | (pending) |
| AC-35 | Tools call the services with the caller's token; a service's 403 or 404 comes back as a failed or empty tool result, not as invented data | (pending) |
| AC-36 | Every question is audited with the user, provider, model, answer, each tool call (arguments, outcome, duration) and token usage; the token is not stored | (pending) |
| AC-37 | A conversation id carries earlier turns into the next question | (pending) |
| AC-38 | Viewer gets 403; anonymous calls get 401 | (pending) |
| AC-39 | The copilot page shows the answer, the tools that ran and the "Simulated" badge (E2E-008) | (pending) |
| AC-40 | The rest of the platform keeps working when AiOperations is down; the console shows the copilot as unavailable | (pending) |

## Error scenarios
Validation (400: empty or too long question), unauthenticated (401), forbidden (403), provider or tool timeout
(answer says which tool failed; the question is still audited), AI provider unavailable (503 with a clear message;
the logistics flow is unaffected).

## Dependencies
Phase 1–3 services, PostgreSQL 17 (new database `logistiflow_aiops`), `Microsoft.Agents.AI` 1.26.0,
`Microsoft.Extensions.AI` 10.11.0, `Microsoft.Extensions.AI.OpenAI` 10.11.0. Decision record: ADR-0008.
