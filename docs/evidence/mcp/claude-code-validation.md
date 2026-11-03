# MCP server: Claude Code validation (2026-11-03)

Two real Claude Code sessions used `LogistiFlow.McpServer` against the local Docker Compose stack. The first one found
a defect that the automated tests had missed. The second one ran after the fix.

## Setup

| Item | Value |
|---|---|
| Claude Code | 2.1.301, headless (`claude -p`), model `claude-opus-5-5` |
| MCP configuration | the repository's `.mcp.json` (`--mcp-config .mcp.json --strict-mcp-config`, so no other server was loaded) |
| Allowed tools | the five `mcp__logistiflow__logistics_*` tools only |
| Server | `src/Mcp/LogistiFlow.McpServer/bin/Release/net10.0/LogistiFlow.McpServer.dll` over stdio |
| Identity | `viewer@logistiflow.local` (Viewer, the least-privileged role), signed in with the local demo password through `LOGISTIFLOW_PASSWORD` |
| Platform | `docker compose --profile app` on this machine, gateway `http://localhost:8089` |

Command (PowerShell, password set in the session only):

```powershell
$env:LOGISTIFLOW_PASSWORD = '<local demo password>'
claude -p "<prompt>" --mcp-config .mcp.json --strict-mcp-config `
  --allowedTools "mcp__logistiflow__logistics_get_order,mcp__logistiflow__logistics_get_inventory,mcp__logistiflow__logistics_get_shipment,mcp__logistiflow__logistics_list_exceptions,mcp__logistiflow__logistics_get_warehouse_metrics" `
  --output-format stream-json --verbose
```

The session's `init` event reported `{"name": "logistiflow", "status": "connected"}` and listed the five tools.

## Run 1: a defect found

Prompt: list the open Delay exceptions (limit 5), pick the most recent, report that order's status, carrier,
tracking number and dates, the stock of its SKU, and the warehouse with the most failed fulfillment jobs.

| Call | Arguments | Result |
|---|---|---|
| `logistics_list_exceptions` | `status=Open, type=Delay, limit=5` | ok: `totalCount 0` (every Delay exception in this database had been resolved) |
| `logistics_get_warehouse_metrics` | none | ok: 29,558 jobs; WH-CA1 18 open / 0 failed / 45 total; WH-NJ1 29,513 open / 0 failed |
| `logistics_list_exceptions` | `type=Delay, limit=5` | **error**: `Structured content does not match the tool's output schema: data must have required property 'status'` |

Claude reported that it could not pick an order and why. It did not make up an order. It also identified the likely cause:
with no status filter, `status` was null and was left out of the result, while the output schema lists it as required.

**Cause.** The SDK serialises tool results with options that omit null properties, but the output schema it generates
lists every property as required. Claude Code validates structured content against the schema and rejected the result.
The SDK's C# client does not validate it, so the unit and integration tests passed. Any null field triggered the error,
including an unresolved exception's `resolvedAt`, an order's `statusReason` and an undelivered shipment's `deliveredAt`.

**Fix.** Tool results are serialised with null properties kept
(`McpServerServiceCollectionExtensions.ToolJson`). The new regression test
`OutputSchemaConformanceTests` checks every tool's structured content against its advertised output schema, using
sample data that contains nulls. With the fix removed it fails four ways: `$: required property 'status' is missing`,
`$.order: required property 'statusReason' is missing`, `$: required property 'statusReason' is missing` and
`$.items[0]: required property 'resolvedAt' is missing`. With the fix in place it passes.

## Run 2: after the fix

Prompt: list the 5 most recent Delay exceptions of any status, pick the most recent, report that order's status,
carrier, tracking number and dates, the stock of its SKU, and the warehouse with the most open fulfillment jobs.

| Call | Arguments | Result |
|---|---|---|
| `logistics_list_exceptions` | `type=Delay, limit=5` | ok: 11 match; the most recent is ORD-30568 (Resolved) |
| `logistics_get_warehouse_metrics` | none | ok |
| `logistics_get_order` | `orderNumber=ORD-30568` | ok: Delivered, promised 2026-12-03, line SKU-1003 × 1, full history |
| `logistics_get_shipment` | `orderNumber=ORD-30568` | ok: Roadrunner Ground, RDRN1793717643313, estimate 2026-11-11 (originally 2026-11-09) |
| `logistics_get_inventory` | `sku=SKU-1003` | ok: 4,968 available (WH-CA1 1,468, WH-NJ1 2,000, WH-TX1 1,500) |

Excerpt of Claude's answer:

> **2) ORD-30568**
> - **Order status:** Delivered ("Delivered by RDRN (RDRN1793717643313)")
> - **Carrier:** Roadrunner Ground (RDRN), shipped from WH-CA1
> - **Tracking number:** RDRN1793717643313
> - **Promised delivery date:** 2026-12-03
> - **Current estimated delivery:** 2026-11-11. The original estimate was 2026-11-09.
>
> **3) Stock for SKU-1003 (Shipping Label Roll 4x6)**
> - **Total available: 4,968**
> - WH-CA1: 1,468 · WH-NJ1: 2,000 · WH-TX1: 1,500. Nothing is reserved in any warehouse.
>
> **4) Warehouse with the most open fulfillment jobs**
> - **WH-NJ1, with 29,513 open jobs.** All 29,513 of its jobs are open and none have failed.

The answer also pointed out that the customer name shows ORD-30568 came from the E2E suite, and that WH-NJ1's backlog
is stuck in Picking. That backlog is the roughly 29k k6 load-test orders already described in `STATUS.md`.

### Cross-check against the gateway API

The values were read again directly from the gateway as the same Viewer user, and all of them match:

| Endpoint | Value |
|---|---|
| `GET /api/shipping/shipments/by-order/{id}` | `RDRN1793717643313`, Roadrunner Ground, Delivered, promised 2026-12-03, original 2026-11-09, estimated 2026-11-11 |
| `GET /api/inventory/availability/SKU-1003` | 4,968: WH-CA1 1,468, WH-NJ1 2,000, WH-TX1 1,500 |
| `GET /api/metrics/fulfillment` | 29,558 jobs; WH-CA1 18 open / 45 total; WH-NJ1 29,513 open / 29,513 total |

The raw session transcripts are not committed, because they contain local paths and session ids.
