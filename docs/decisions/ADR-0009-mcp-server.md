# ADR-0009: The LogistiFlow MCP server

Status: accepted (2026-11-03)

## Context
Phase 5 makes LogistiFlow data available to MCP clients such as Claude Code. The server must be read-only, honour the
platform's authorization, keep credentials out of the repository, and run locally next to the developer's client.
The in-product copilot (ADR-0008) already reads the services, but it is a service with its own users and audit.

## Decision
- **Separate executable, `src/Mcp/LogistiFlow.McpServer`**, built on the official C# SDK `ModelContextProtocol` 2.3.0
  (2.3.1 was published on the day of implementation and is not used yet).
- **stdio transport.** The client starts the server as a child process. Standard output carries only the protocol.
  Logs go to standard error as Serilog compact JSON, and the default console logger is removed.
- **A gateway client, not a service.** Every call goes through the gateway's public HTTP API with a bearer token. The
  server references no LogistiFlow project, which is enforced by `McpServer_ReferencesNoLogistiFlowAssembly`. Its
  contracts are its own copies of the API shapes, and the integration tests check them against the real services.
- **Authorization is the platform's.** The server signs in as a configured user, either with a pre-issued
  `LOGISTIFLOW_ACCESS_TOKEN` or with `LOGISTIFLOW_EMAIL` and `LOGISTIFLOW_PASSWORD` through `POST /api/auth/login`.
  The services apply that user's role policies. The default identity in `.mcp.json` is the Viewer, the
  least-privileged role, which can read everything the tools need. The token is cached until one minute before it
  expires and dropped on a 401. Neither the password nor the token is logged or echoed in an error.
- **Five read-only tools** with the names from the master prompt: `logistics_get_order`, `logistics_get_inventory`,
  `logistics_get_shipment`, `logistics_list_exceptions` and `logistics_get_warehouse_metrics`.
  - Every tool is annotated read-only, non-destructive, idempotent and closed-world.
  - Every tool returns structured content with an output schema.
  - Every tool validates its arguments before any HTTP call: order numbers `^ORD-[0-9]{1,10}$`, SKUs
    `^[A-Za-z0-9-]{1,40}$`, enum filters from a fixed list, limit 1 to 100.
- **Errors are tool errors.** Not found, forbidden, unauthorized, invalid and unavailable become `isError` results
  with a message the model can act on. A partial answer, such as metrics with one source missing, is an error rather
  than incomplete data.
- **Null properties are serialised.** The SDK omits them by default, while its generated output schema marks every
  property required. Claude Code validates structured content, so it rejected results with nulls until this was set
  (see `docs/evidence/mcp/claude-code-validation.md`).
- **Resilience.** The standard resilience handler is used with a 5 s attempt timeout and a 15 s total timeout.

## Alternatives considered
- **Streamable HTTP transport, hosted in compose.** This would need its own authentication between the MCP client and
  the server. It is not needed for a local developer tool. It remains an option for a shared deployment in Phase 7.
- **Reusing the AiOperations tool clients.** That would couple an external adapter to a service's internals and break
  the rule that nothing outside a service references its projects.
- **Write tools (cancel, resolve).** These are out of scope, as for the copilot. They would need preview,
  confirmation, idempotency and audit.

## Consequences
- The server works against any LogistiFlow gateway URL (local compose today, Azure later) with no code change.
- The tool contracts are copies of the API shapes. A drift shows up in the integration tests, which run the server
  against the real services.
- A client that does not validate output schemas would not have caught the null-property defect. The conformance test
  now does.
