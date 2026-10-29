# ADR-0004: JWT issued by the gateway, validated by every service

- Status: Accepted
- Date: 2026-10-29

## Context
The SPA, API clients, k6 and (later) the MCP server and the AI copilot need to call the platform as a user, with
role-based permissions enforced by each service.

## Decision
- The gateway owns identity: users with BCrypt hashes in `logistiflow_identity`, `POST /api/auth/login` issues an
  HS256 access token (1 h) with `sub`, `name`, `email` and `role`. Login is rate limited per client IP and returns
  the same error for an unknown e-mail and a wrong password (with equal BCrypt cost).
- The gateway rejects anonymous calls before proxying; **every service validates the token again** (issuer,
  audience, signature, lifetime) and authorizes with permission policies (`orders.manage`, `inventory.manage`,
  `warehouse.operate`, `operations.read`). The fallback policy denies anonymous access by default.
- Roles: Admin, OpsManager, WarehouseOperator, Viewer. Endpoints depend on policies, never on role names.
- The SPA keeps the token in `sessionStorage` and sends it as a bearer header.

## Consequences
- Services stay stateless and can be called directly in tests and by internal clients with the same rules.
- A shared symmetric key must be distributed to every service (Key Vault in Azure). Moving to Entra ID (OIDC,
  asymmetric keys, JWKS) replaces issuance only; validation and policies stay.
- `sessionStorage` is readable by scripts on the page. The mitigations are a strict CSP from nginx and short token
  lifetime; a BFF with an HttpOnly cookie is the stronger option if the threat model requires it.
