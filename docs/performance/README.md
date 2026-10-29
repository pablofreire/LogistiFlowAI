# Performance testing

k6 scripts live in `tests/performance/k6`. Run them only against a local or isolated environment.

```bash
docker compose --profile app up -d --build --wait
docker run --rm -e BASE_URL=http://host.docker.internal:8089 -e VUS=10 -e DURATION=1m \
  -v "$PWD/tests/performance/k6:/scripts" grafana/k6:1.3.0 \
  run --summary-export /scripts/results/baseline-10vus.json /scripts/baseline.js
```

| Variable | Default | Meaning |
|---|---|---|
| `BASE_URL` | `http://localhost:8089` | Gateway address |
| `VUS` / `DURATION` | `10` / `1m` | Virtual users (half write, half read) and run length |
| `P95_WRITE_MS` / `P95_READ_MS` | `300` | p95 thresholds per scenario |
| `MAX_ERROR_RATE` | `0.01` | Failed-request threshold |
| `DEMO_PASSWORD` | `Demo-Password-2026!` | Password of the seeded demo users |

Recorded runs: [baseline-2026-10-29.md](baseline-2026-10-29.md).
