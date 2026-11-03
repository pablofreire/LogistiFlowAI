# Example run: dotnet-api-review

Recorded on 2026-11-03 with Claude Code 2.1.301 (`claude-opus-5-5`). The run was headless, from the repository root:

```bash
claude -p "/dotnet-api-review src/Services/Shipping/LogistiFlow.Shipping.Api" --strict-mcp-config --allowedTools "Read,Grep,Glob,Skill,Bash(git diff:*),Bash(git log:*)" --disallowedTools "Edit,Write,NotebookEdit" --output-format stream-json --verbose
```

Claude Code found the project skill and called it through the `Skill` tool. The run could only read files: it took
51 turns and 3 min 54 s, and used these tools: Read 30, Grep 11, Glob 6, Skill 1, Bash 1.

Below is the skill's output, word for word. Only the heading levels changed, and a closing offer to fix the findings
was removed. Phase 5 did not act on these findings; `STATUS.md` lists them as follow-ups. A finding was confirmed by
running code only where the report says so.

---

### API review: `src/Services/Shipping/LogistiFlow.Shipping.Api`

**Verdict: Changes required.** Two list endpoints can return a 400 that their declarations and the OpenAPI document don't mention. The skill's rule treats an undeclared status code on a reachable path as High, and any High means changes required. The fix is small, and the other findings are Medium or Low. This was a review only: I didn't run any tests and didn't change any code.

### 1. Scope

I read the 3 controllers, `Program.cs` and `ResolveShipmentExceptionBody`, plus everything they depend on: the use cases, the validator, the shared contracts, `ShippingErrors`, `ShipmentEntity`, `ShipmentStateMachine`, `ShippingQueryReader`, `PageRequest`, `DbUnitOfWork`, `BaseApiController`, `ServiceDefaultsExtensions`, `JwtAuthenticationServiceCollectionExtensions`, `GlobalExceptionHandler` and `docs/api/shipping.openapi.json`.

| Verb | Route | Policy | Use case | Statuses declared |
|---|---|---|---|---|
| GET | `/api/shipping/shipments` | `operations.read` | ListShipments | 200 |
| GET | `/api/shipping/shipments/{shipmentId:guid}` | `operations.read` | GetShipment | 200, 404 |
| GET | `/api/shipping/shipments/by-order/{orderId:guid}` | `operations.read` | GetShipment | 200, 404 |
| POST | `/api/shipping/shipments/{shipmentId:guid}/retry-booking` | `shipments.manage` | RetryShipmentBooking | 200, 404, 409 |
| GET | `/api/shipping/exceptions` | `operations.read` | ListShipmentExceptions | 200 |
| POST | `/api/shipping/exceptions/{exceptionId:guid}/resolve` | `shipments.manage` | ResolveShipmentException | 200, 400, 404, 409 |
| GET | `/api/metrics/shipping` | `operations.read` | GetShippingMetrics | 200 |

### 2. Findings

| Sev | Location | Problem | Evidence | Fix |
|---|---|---|---|---|
| **High** | `ShipmentsController.cs:20-23`, `ShipmentExceptionsController.cs:21-24` | Both list endpoints can return 400, but only 200 is declared, and the OpenAPI document matches that (`shipping.openapi.json:50-61`, `181-193`). | Requests like `?status=Bogus`, `?status=999` or `?page=abc` fail model binding. They go to `InvalidModelStateResponseFactory` (`ServiceDefaultsExtensions.cs:40-50`), which returns a 400 envelope. The `Enum.IsDefined` checks at `ListShipmentsUseCase.cs:16` and `ListShipmentExceptionsUseCase.cs:16` are defensive only: the binder rejects undefined enum values before the use case runs, so they can't be reached over HTTP. | Add `[ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]` to both actions. Then re-export `docs/api/shipping.openapi.json` and run `npm run generate:api`. |
| **Medium** | `BuildingBlocks/.../Paging/Models/PageRequest.cs:9,12-13` | A large `page` value makes the offset overflow, and the request fails with a 500. | `Normalize` limits `pageSize` but not `page`, and `Offset = (Page - 1) * PageSize` uses unchecked `int` arithmetic. With `?page=21474838&pageSize=100` the result wraps to about -2.1 billion. That value reaches `OFFSET @Offset` (`ShippingReadQueries.cs:28,72`), Postgres rejects a negative OFFSET, and `GlobalExceptionHandler` returns a 500. This only happens when there is data: `ShippingQueryReader.cs:28,67` skips the page query when the count is 0. **I confirmed this by reading the code, not by running it.** | Put a maximum on `page` in `Normalize`, or compute `Offset` as a `long`. `PageRequest` is in BuildingBlocks, so the fix applies to every service. Add a unit test for the boundary value. |
| Low | `ResolveShipmentExceptionRequestValidator.cs:11` / `ResolveShipmentExceptionUseCase.cs:40` | The minimum length is checked before the text is trimmed. | The validator runs `Length(3, 500)` on the raw text, and the use case trims afterwards. `"  a  "` passes validation and is saved as `"a"`. | Validate the trimmed value, e.g. `.Must(r => r.Trim().Length >= 3)`, or trim before validating. |
| Low | `ShipmentExceptionsController.cs:45` / `ResolveShipmentExceptionRequestValidator.cs:12` | `ResolvedBy` comes from the server, but a too-long value is reported as the client's fault. | The value is the JWT `email` claim, limited to 120 characters by the validator and by the `resolved_by varchar(120)` column (`0506_shipping_exceptions_create.sql:10`). A user with a longer email gets a 400 they can't fix. | Make the limit match the Gateway's user email length, or truncate server-side and stop validating it as user input. |
| Low | `ShipmentsController.cs:52-53`, `RetryShipmentBookingUseCase.cs:31` | There's no record of who asked for a booking retry. | Resolving an exception stores `ResolvedBy`, but a retry only sets `StatusReason = "Booking retry requested for {CarrierCode}"` (`ShipmentEntity.cs:203`). The controller and use case log nothing. | Pass the operator into the use case and add it to the status reason, or log it with a `[LoggerMessage]` that includes the shipment id. |
| Low | All controllers (this applies to the whole repo) | 401 and 403 aren't declared anywhere, and the OpenAPI document has no `securitySchemes`. | No controller under `src/Services` declares `Status401`/`Status403`. Every endpoint sits behind the fallback policy (`JwtAuthenticationServiceCollectionExtensions.cs:50`), so the document under-describes them. | Add an OpenAPI transformer that adds the bearer scheme plus 401/403 responses to authorized operations. |

### 3. Checks that passed
- **Status codes on the POSTs:** every `Error.*` they can return is declared. Not found → 404; `InvalidTransition`, `ExceptionAlreadyResolved` and `ConcurrentModification` → 409; validation → 400 on resolve.
- **Success codes:** 200 for reads and state changes. Nothing is created over HTTP, so no 201 is needed.
- **Repeated actions:** calling retry twice gives 409, because `PendingBooking → PendingBooking` isn't an allowed transition (`ShipmentStateMachine.cs:13`). Resolving twice gives `ExceptionAlreadyResolved`.
- **Contracts:** only `Guid` ids are exposed. The internal `InternalId` is used only inside queries (`ShippingQueryReader.cs:106-107`). The `long` fields in the metrics response are counts, not ids.
- **Enums:** serialised as strings in both the MVC and HTTP JSON options (`ServiceDefaultsExtensions.cs:33,56`).
- **Dates:** `DateOnly` for calendar dates, `DateTimeOffset` for points in time.
- **Envelope:** every response is wrapped in `ApiResponse<T>`, including model-binding errors and unhandled exceptions.
- **OpenAPI paths and schemas:** paths, parameters and status codes match the controllers. The `ShipmentDetails`, `ShipmentSummary` and `TrackingEventItem` fields are all present.
- **Validation:** route ids use `:guid` constraints, `pageSize` is capped at 100, and the resolve text is limited to 500 characters to match the column.
- **SQL:** queries use Dapper parameters only. Status and type filters are passed as parameters, never concatenated into the query.
- **Authorization:** reads use `operations.read` and writes use `shipments.manage`. There is no `[AllowAnonymous]`, and the fallback policy requires an authenticated user.
- **Operator email:** `MapInboundClaims = false` (`JwtAuthenticationServiceCollectionExtensions.cs:33`), so the `email` claim is read correctly, the same way as in the Fulfillment and AiOperations controllers.
- **Error messages:** stable codes and no internal details. Unexpected errors become a generic `server.error` 500 and are logged with `[LoggerMessage]`.
- **Observability:** OpenTelemetry tracing and metrics come from `AddLogistiFlowServiceDefaults`. The workers log failures with structured properties, and no tokens or personal data appear in log templates.
