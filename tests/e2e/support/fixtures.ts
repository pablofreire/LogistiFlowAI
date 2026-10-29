import { expect, test as base, type APIRequestContext, type Page } from '@playwright/test';
import { mkdirSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

export const demoPassword = process.env.LOGISTIFLOW_DEMO_PASSWORD ?? 'Demo-Password-2026!';

export const users = {
  opsManager: { email: 'ops.manager@logistiflow.local', name: 'Morgan Ops', role: 'OpsManager' },
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

export const test = base;
export { expect };
