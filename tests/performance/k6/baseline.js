// Baseline load: order intake plus the reads an operations team generates, through the gateway.
// Run against the compose stack (see docs/performance/README.md):
//   docker run --rm -i -e BASE_URL=http://host.docker.internal:8089 -v ${PWD}/tests/performance/k6:/scripts grafana/k6:1.3.0 run /scripts/baseline.js
import http from 'k6/http';
import { check } from 'k6';
import { Counter } from 'k6/metrics';

const baseUrl = __ENV.BASE_URL || 'http://localhost:8089';
const password = __ENV.DEMO_PASSWORD || 'Demo-Password-2026!';
const vus = Number(__ENV.VUS || 10);
const duration = __ENV.DURATION || '1m';

const ordersCreated = new Counter('orders_created');

export const options = {
  scenarios: {
    order_intake: { executor: 'constant-vus', exec: 'createOrder', vus: Math.max(1, Math.floor(vus / 2)), duration },
    operations_reads: { executor: 'constant-vus', exec: 'readOperations', vus: Math.max(1, Math.ceil(vus / 2)), duration },
  },
  thresholds: {
    http_req_failed: [`rate<${__ENV.MAX_ERROR_RATE || '0.01'}`],
    'http_req_duration{scenario:order_intake}': [`p(95)<${__ENV.P95_WRITE_MS || '300'}`],
    'http_req_duration{scenario:operations_reads}': [`p(95)<${__ENV.P95_READ_MS || '300'}`],
  },
  summaryTrendStats: ['avg', 'med', 'p(90)', 'p(95)', 'p(99)', 'max'],
};

export function setup() {
  const response = http.post(`${baseUrl}/api/auth/login`, JSON.stringify({ email: 'ops.manager@logistiflow.local', password }), {
    headers: { 'Content-Type': 'application/json' },
  });
  check(response, { 'login succeeded': (r) => r.status === 200 });
  return { token: response.json('data.accessToken') };
}

export function createOrder(data) {
  const key = `k6-${__VU}-${__ITER}-${Date.now()}`;
  const body = JSON.stringify({
    customerName: 'k6 Load Test',
    destinationCity: 'Dallas',
    destinationCountry: 'US',
    lines: [{ sku: 'SKU-BULK-001', quantity: 1 }],
  });
  const response = http.post(`${baseUrl}/api/orders`, body, {
    headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${data.token}`, 'Idempotency-Key': key },
    tags: { endpoint: 'POST /api/orders' },
  });
  if (check(response, { 'order accepted (201)': (r) => r.status === 201 })) {
    ordersCreated.add(1);
  }
}

export function readOperations(data) {
  const headers = { Authorization: `Bearer ${data.token}` };
  const list = http.get(`${baseUrl}/api/orders?pageSize=20`, { headers, tags: { endpoint: 'GET /api/orders' } });
  check(list, { 'orders listed (200)': (r) => r.status === 200 });
  const stock = http.get(`${baseUrl}/api/inventory/stock?sku=SKU-BULK-001`, { headers, tags: { endpoint: 'GET /api/inventory/stock' } });
  check(stock, { 'stock read (200)': (r) => r.status === 200 });
  const dashboard = http.get(`${baseUrl}/api/dashboard/summary`, { headers, tags: { endpoint: 'GET /api/dashboard/summary' } });
  check(dashboard, { 'dashboard read (200)': (r) => r.status === 200 });
}
