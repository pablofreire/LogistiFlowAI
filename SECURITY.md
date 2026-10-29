# Security

## Reporting
Please report vulnerabilities privately through GitHub's "Report a vulnerability" (security advisories) rather than
a public issue.

## What is in place
- JWT bearer authentication on every endpoint by default (fallback policy), re-validated by each service.
- Permission policies per role; UI hiding is never the control (E2E-009 checks the API returns 403).
- BCrypt (enhanced, work factor 11) password hashes; timing-equalized login; per-IP login rate limit.
- Parameterized SQL only (Dapper); input validation with FluentValidation; no internals in error responses.
- Secrets come from configuration/environment; `.env` is git-ignored and `.env.example` holds placeholders.
- Non-root containers; nginx sends CSP, `X-Frame-Options: DENY`, `nosniff`; services send baseline security headers.
- CI: gitleaks over the full history and CodeQL (C#, TypeScript); Dependabot for NuGet, npm, Actions and Docker.

## Local development defaults
`docker-compose.yml` and `appsettings.Development.json` contain development-only defaults (database password,
JWT signing key, demo password). They are not secrets and must be overridden outside a local machine.
