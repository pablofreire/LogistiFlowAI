---
name: dotnet-api-review
description: Reviews a LogistiFlow HTTP API surface (controllers, request/response contracts, validators, error mapping, authorization, logging) against the repository's REST conventions and reports findings with file:line evidence and a verdict. Use when a change adds or modifies an endpoint, a contract or a validator, or before publishing an OpenAPI document.
argument-hint: "<path to an Api project, a controller, or a use-case folder>"
---

# dotnet-api-review

## Objective
Find API defects before clients do: wrong status codes, contracts that leak internals or drift from the OpenAPI
document, input that reaches a use case unvalidated, endpoints with the wrong policy, and failures that are not
observable. Every finding is backed by code, never by assumption.

## Input
`$ARGUMENTS`: a path such as `src/Services/Shipping/LogistiFlow.Shipping.Api`, one controller file, or a use-case
folder. With no argument, review the API projects changed on the current branch (`git diff --name-only develop...`).

## Procedure
1. **Inventory the surface.** List every action in the target controllers: HTTP verb, route, policy
   (`[Authorize(Policy = ...)]` or the controller default), the use case it calls, and its `[ProducesResponseType]`
   attributes. Read `src/BuildingBlocks/LogistiFlow.BuildingBlocks.Web/Api/BaseApiController.cs` for the
   `ErrorType` → status mapping (Validation 400, NotFound 404, Conflict 409, Forbidden 403, Unauthorized 401,
   Unavailable 503).
2. **Status codes.** For each action, trace every `Error.*` the use case can return and check the mapped status is
   declared in `[ProducesResponseType]`, and that success codes are right (201 + location for creation, 200 for
   reads and state changes, never 200 with an error body).
3. **Contracts.** Check the request and response records in `UseCases/.../Contracts`: only `public_id` GUIDs are
   exposed (no internal `long` ids), enums serialise as strings, dates are `DateOnly`/`DateTimeOffset`, and every
   response is wrapped in `ApiResponse<T>`. Compare with `docs/api/<service>.openapi.json`; a drift means the
   document was not re-exported.
4. **Validation.** Every request with user input has a `*RequestValidator` in `Validators` and the use case runs it
   before touching data. Look for unbounded inputs: page sizes without a maximum, free-text without a length limit,
   enum values not checked with `IsInEnum`, ids parsed without a `Guid` constraint in the route.
5. **Errors.** Error codes and messages are stable, specific and leak no internals (no exception text, SQL or stack
   traces). Unexpected exceptions reach the global handler and become a 500 envelope.
6. **Authorization.** The policy matches the operation: reads use `operations.read`, writes use the narrowest
   manage/operate policy in `AuthorizationPolicies`. Anonymous access is explicit (`[AllowAnonymous]`) and justified.
7. **REST shape.** Nouns in routes, actions as sub-resources only for state transitions
   (`/jobs/{id}/pack`), idempotency for creation where a client can retry (`Idempotency-Key`), filtering and paging
   through query parameters with defaults.
8. **Observability.** Failures that matter to operators are logged with structured properties (`[LoggerMessage]`),
   no personal data or tokens in logs, and the request is traceable (OpenTelemetry is configured by
   `ServiceDefaultsExtensions`).
9. Confirm or discard each suspected finding by reading the code it depends on. Drop anything you cannot cite.

## Approval criteria
- **Approved**: no findings of severity High or Medium.
- **Approved with follow-ups**: only Medium or Low findings, each with a concrete fix.
- **Changes required**: any High finding: a wrong or undeclared status code on a reachable path, an internal id or
  secret in a contract or log, an unvalidated input that reaches SQL or an external call, or a write endpoint with a
  read policy.

## Output
A Markdown report:
1. Scope: files reviewed and the endpoint inventory table (verb, route, policy, statuses declared).
2. Findings table: severity (High / Medium / Low), `file:line`, what is wrong, the evidence, the fix.
3. Checks that passed, one line each, so the reader knows what was covered.
4. Verdict with the approval criterion that decided it.

## Example run
A real run on `src/Services/Shipping/LogistiFlow.Shipping.Api` is recorded in
`docs/evidence/skills/dotnet-api-review.md`.
