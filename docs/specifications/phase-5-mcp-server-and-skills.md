# Spec: Phase 5, MCP server and Claude Code skills

Status: implemented on 2026-11-03. Each criterion below names the tests or recorded runs that cover it. Those tests and
runs passed locally (see `STATUS.md`). The Claude Code integration was run for real against the local stack.

## Objective
Two separate things are built in this phase:
- **For MCP clients:** a server of our own that gives clients such as Claude Code read-only access to live
  LogistiFlow data. It applies the platform's authorization, keeps credentials out of the repository and runs locally.
- **For developers:** three Claude Code skills that review the codebase the way a senior engineer would, each proven
  by a real run on this repository.

These are development and integration tools, separate from the product's own AI copilot (Phase 4).

## Scope

| In | Out |
|---|---|
| `LogistiFlow.McpServer` (official C# SDK `ModelContextProtocol` 2.3.0), stdio transport, five read-only tools | Write tools (cancel, resolve, retry), MCP resources and prompts |
| Sign-in through the gateway with a token or e-mail and password from environment variables | Streamable HTTP transport and hosting the server in compose or Azure |
| `.mcp.json` for Claude Code, a recorded Claude Code session against the compose stack | Publishing the server as a NuGet tool |
| Skills `dotnet-api-review`, `microservices-reliability-review`, `test-quality-review` in `.claude/skills`, each run once on this repository | More development agents (the three in `.claude/agents` stay) |

## Tools

| Tool | Arguments | Reads (through the gateway) |
|---|---|---|
| `logistics_get_order` | `orderNumber` (required, `ORD-` + digits) | Order search (exact match), order details, status history |
| `logistics_get_inventory` | `sku` (required) | Availability per warehouse |
| `logistics_get_shipment` | `orderNumber` (required) | Order, then its shipment with tracking events and exceptions |
| `logistics_list_exceptions` | `status` (Open/Resolved), `type` (CarrierUnavailable/Delay/DeliveryAttemptFailed/ReturnedToSender), `limit` (1 to 100, default 20) | Shipment exceptions, newest first |
| `logistics_get_warehouse_metrics` | none | Fulfillment metrics per warehouse and status, inventory totals |

All five are annotated read-only, non-destructive, idempotent and closed-world, and all return structured content
with an output schema. Design and alternatives: ADR-0009.

## Configuration

| Variable | Meaning | Default in `.mcp.json` |
|---|---|---|
| `LOGISTIFLOW_GATEWAY_URL` | Gateway base URL | `http://localhost:8089` |
| `LOGISTIFLOW_ACCESS_TOKEN` | Pre-issued bearer token (takes precedence) | empty |
| `LOGISTIFLOW_EMAIL` / `LOGISTIFLOW_PASSWORD` | Sign-in through `POST /api/auth/login` | `viewer@logistiflow.local` / empty (must be set by the user) |

## Requirements and acceptance criteria

| Id | Acceptance criterion | Verified by |
|---|---|---|
| AC-41 | The server lists exactly the five tools. Each has a description, read-only and non-destructive annotations, an input schema that requires its identifier, and an output schema | `LogisticsToolsTests.ListTools_ExposesExactlyTheFiveReadOnlyTools`, `ListTools_DeclaresTheRequiredIdentifier` |
| AC-42 | Arguments are validated before any gateway call. A bad argument is a tool error that says what is expected | `LogisticsToolsTests.GetOrder_RejectsAMalformedNumber_*`, `GetInventory_RejectsASkuWithPathCharacters`, `ListExceptions_RejectsAnUnknownFilterOrLimit` |
| AC-43 | The tools return live data that matches the APIs | `McpServerFlowTests.ShippedOrder_IsReadThroughEveryTool_AsAViewer`, `CarrierDelay_IsListedByTheExceptionsTool` (real services, Testcontainers); Claude Code run 2 cross-checked against the gateway (`docs/evidence/mcp/claude-code-validation.md`) |
| AC-44 | Not found, forbidden, unauthorized, invalid and unavailable come back as tool errors with a reason, never as invented or partial data | `LogisticsToolsTests.GetOrder_UnknownNumber_*`, `GetOrder_UserTheGatewayForbids_*`, `GetShipment_OrderNotShippedYet_*`, `GetWarehouseMetrics_OneSourceUnavailable_*`; `GatewayClientTests`; `McpServerFlowTests.UnknownOrder_*`, `TokenTheServicesDoNotTrust_*` |
| AC-45 | Authorization is the platform's. Credentials come only from the environment. Sign-in happens once per token lifetime and again after a 401. No password or token appears in an error. The server refuses to start without credentials | `SignInTests` (5), `GatewayOptionsTests` (3); every integration test runs as Viewer |
| AC-46 | Structured content always satisfies the advertised output schema, including null values | `OutputSchemaConformanceTests` (fails without the fix); Claude Code run 2 |
| AC-47 | Over stdio, standard output carries only the protocol. Logs are JSON on standard error, and every tool call is logged with its outcome and duration but no token | Manual stdio checks on 2026-11-03, built server against compose. The run used `initialize`, one successful and one rejected tool call. Standard output had 3 lines, all JSON-RPC. Standard error had `Signed in to the gateway as viewer@logistiflow.local` (e-mail and expiry only), `Tool logistics_get_inventory (SKU-1003) completed in 242 ms` and `Tool logistics_get_order (1025) returned an error in 1 ms: orderNumber must look like ORD-1025...`. Both Claude Code sessions connected over stdio |
| AC-48 | Claude Code uses the server through the repository's `.mcp.json`: real sessions against the compose stack, recorded | `docs/evidence/mcp/claude-code-validation.md` (run 1 found the null-property defect, run 2 passed after the fix) |
| AC-49 | The server references no LogistiFlow assembly. It is a gateway client | `ServiceBoundaryTests.McpServer_ReferencesNoLogistiFlowAssembly`; the convention tests now include its assembly |
| AC-50 | Each of the three skills has an objective, input, procedure, approval criteria, output and example run, and was run in Claude Code on this repository with its output recorded | `.claude/skills/*/SKILL.md`; `docs/evidence/skills/*.md` |

## Error scenarios
- Malformed arguments: tool error with no HTTP call.
- Unknown order or SKU: tool error.
- User not allowed: tool error.
- Token rejected: tool error; the cached token is dropped and the next call signs in again.
- Credentials rejected: tool error naming the e-mail, never the password.
- Gateway down or timing out: tool error after the resilience policy.
- Missing configuration: the server refuses to start and names the variables.

## Dependencies
- Phases 1 to 4 and the gateway's `POST /api/auth/login`.
- `ModelContextProtocol` 2.3.0.
- Claude Code 2.1.301 for the recorded runs.
- Decision record: ADR-0009.
