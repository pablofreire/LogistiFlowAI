import { expect, test as base, type APIRequestContext, type Page } from '@playwright/test';
import { mkdirSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

export const demoPassword = process.env.LOGISTIFLOW_DEMO_PASSWORD ?? 'Demo-Password-2026!';

export const users = {
  opsManager: { email: 'ops.manager@logistiflow.local', name: 'Morgan Ops', role: 'OpsManager' },
  warehouseOperator: { email: 'warehouse.operator@logistiflow.local', name: 'Riley Floor', role: 'WarehouseOperator' },
  viewer: { email: 'viewer@logistiflow.local', name: 'Casey Viewer', role: 'Viewer' },
} as const;

type User = (typeof users)[keyof typeof users];

const evidenceDir = resolve(dirname(fileURLToPath(import.meta.url)), '../../../docs/evidence/e2e');

/** Saves a named screenshot to docs/evidence/e2e so each run leaves reviewable evidence. */
export async function evidence(page: Page, name: string): Promise<void> {
  mkdirSync(evidenceDir, { recursive: true });
  await page.screenshot({ path: resolve(evidenceDir, `${name}.png`), fullPage: true });
}

export async function signIn(page: Page, user: User): Promise<void> {
  await page.goto('/login');
  await page.getByTestId('login-email').fill(user.email);
  await page.getByTestId('login-password').fill(demoPassword);
  await page.getByTestId('login-submit').click();
  await expect(page.getByTestId('current-user')).toHaveText(user.name);
}

/** Calls the gateway directly, as an independent check that the UI shows persisted state. */
export async function apiToken(request: APIRequestContext, user: User): Promise<string> {
  const response = await request.post('/api/auth/login', { data: { email: user.email, password: demoPassword } });
  expect(response.ok()).toBeTruthy();
  return (await response.json()).data.accessToken as string;
}

export async function apiGet<T>(request: APIRequestContext, token: string, path: string): Promise<T> {
  const response = await request.get(path, { headers: { Authorization: `Bearer ${token}` } });
  expect(response.ok(), `${path} -> ${response.status()}`).toBeTruthy();
  return (await response.json()).data as T;
}

/** Fills the order form and returns the order id from the details page URL. */
export async function createOrderThroughUi(page: Page, customer: string, sku: string, quantity: number): Promise<string> {
  await page.getByTestId('nav-orders').click();
  await page.getByTestId('new-order').click();
  await page.getByTestId('order-customer').fill(customer);
  await page.getByTestId('order-city').fill('Austin');
  await page.getByTestId('order-country').fill('US');
  await page.getByTestId('order-line-sku-0').fill(sku);
  await page.getByTestId('order-line-quantity-0').fill(String(quantity));
  await page.getByTestId('submit-order').click();
  await page.waitForURL(/\/orders\/[0-9a-f-]{36}$/);
  return page.url().split('/').pop()!;
}

export interface OrderSeed {
  customer: string;
  sku: string;
  quantity: number;
  city?: string;
  country?: string;
}

/** Creates an order through the gateway API; used when the order itself is not what the test is about. */
export async function apiCreateOrder(request: APIRequestContext, token: string, seed: OrderSeed): Promise<{ id: string; orderNumber: string }> {
  const promised = new Date(Date.now() + 30 * 24 * 3600 * 1000).toISOString().slice(0, 10);
  const response = await request.post('/api/orders', {
    headers: { Authorization: `Bearer ${token}`, 'Idempotency-Key': `e2e-${Date.now()}-${Math.random().toString(36).slice(2)}` },
    data: {
      customerName: seed.customer,
      destinationCity: seed.city ?? 'Austin',
      destinationCountry: seed.country ?? 'US',
      promisedDeliveryDate: promised,
      lines: [{ sku: seed.sku, quantity: seed.quantity }],
    },
  });
  expect(response.status()).toBe(201);
  const order = (await response.json()).data;
  return { id: order.id as string, orderNumber: order.orderNumber as string };
}

/** Waits, through the API, until the order reaches a status. */
export async function waitForOrderStatus(request: APIRequestContext, token: string, orderId: string, status: string, timeout = 30_000): Promise<void> {
  await expect
    .poll(async () => (await apiGet<{ status: string }>(request, token, `/api/orders/${orderId}`)).status, { timeout, message: `order ${orderId} to reach ${status}` })
    .toBe(status);
}

/** Packs and ships the order's job through the API, as the warehouse floor would. */
export async function apiPackAndShip(request: APIRequestContext, operatorToken: string, orderId: string): Promise<void> {
  const job = await apiGet<{ id: string }>(request, operatorToken, `/api/fulfillment/jobs/by-order/${orderId}`);
  for (const action of ['pack', 'ship']) {
    const response = await request.post(`/api/fulfillment/jobs/${job.id}/${action}`, { headers: { Authorization: `Bearer ${operatorToken}` } });
    expect(response.ok(), `${action} -> ${response.status()}`).toBeTruthy();
  }
}

export const test = base;
export { expect };
