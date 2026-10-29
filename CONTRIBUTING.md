# Contributing

1. Read [CLAUDE.md](CLAUDE.md) (conventions) and [STATUS.md](STATUS.md) (current state).
2. Create a branch from `main` (`feature/...`, `fix/...`, `docs/...`).
3. Keep the build at zero warnings and run the suites your change touches (see README → Testing).
4. If an API contract changes: restart the service, re-export `docs/api/*.openapi.json`, run `npm run generate:api`.
5. Use Conventional Commits (`feat:`, `fix:`, `test:`, `docs:`, `chore:`), and update docs and `STATUS.md`.
6. Open a pull request; CI must be green before merge.
