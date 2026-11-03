import type { Page } from '@playwright/test';
import { apiCreateOrder, apiPackAndShip, apiToken, evidence, expect, signIn, test, users, waitForOrderStatus } from '../support/fixtures';

// Wide tables scroll inside their own container; the page itself must never scroll sideways.
async function expectNoHorizontalOverflow(page: Page) {
  const scrollWidth = await page.evaluate(() => document.documentElement.scrollWidth);
  expect(scrollWidth).toBeLessThanOrEqual(page.viewportSize()!.width);
}

async function navigate(page: Page, testId: string) {
  await page.getByRole('button', { name: 'Toggle navigation' }).click();
  await page.getByTestId(testId).click();
  // The drawer closes on navigation with a slide-out; wait until it has left the screen so evidence shows the page.
  await expect
    .poll(async () => {
      const box = await page.locator('.mantine-AppShell-navbar').boundingBox();
      return box === null || box.x + box.width <= 0;
    })
    .toBe(true);
}

test.describe('E2E-010 Responsive UI', () => {
  test('dashboard and navigation work on a phone viewport', async ({ page }) => {
    await signIn(page, users.opsManager);

    await expect(page.getByTestId('kpi-total-orders')).toBeVisible();
    await expect(page.getByTestId('kpi-processing-time')).toBeVisible();
    await expectNoHorizontalOverflow(page);
    await evidence(page, 'e2e-010-mobile-dashboard');

    await navigate(page, 'nav-orders');
    await expect(page.getByRole('heading', { name: 'Orders' })).toBeVisible();
    await evidence(page, 'e2e-010-mobile-orders');
  });

  test('fulfillment, shipments, shipment details, exceptions and the copilot fit a phone viewport', async ({ page, request }) => {
    const managerToken = await apiToken(request, users.opsManager);
    const operatorToken = await apiToken(request, users.warehouseOperator);
    const order = await apiCreateOrder(request, managerToken, { customer: `E2E Responsive ${Date.now()}`, sku: 'SKU-1003', quantity: 1 });
    await waitForOrderStatus(request, managerToken, order.id, 'Picking');
    await apiPackAndShip(request, operatorToken, order.id);

    await signIn(page, users.opsManager);

    await navigate(page, 'nav-fulfillment');
    await expect(page.getByRole('heading', { name: 'Fulfillment' })).toBeVisible();
    await expect(page.getByTestId('jobs-table')).toBeVisible();
    await expectNoHorizontalOverflow(page);
    await evidence(page, 'e2e-010-mobile-fulfillment');

    await navigate(page, 'nav-shipments');
    await expect(page.getByRole('heading', { name: 'Shipments' })).toBeVisible();
    await expect(page.getByTestId('shipments-table')).toBeVisible();
    await expectNoHorizontalOverflow(page);
    await evidence(page, 'e2e-010-mobile-shipments');

    await page.goto(`/orders/${order.id}`);
    await page.getByTestId('order-shipment-link').click();
    await expect(page.getByTestId('shipment-tracking-number')).toBeVisible();
    await expect(page.getByTestId('tracking-timeline')).toBeVisible();
    await expectNoHorizontalOverflow(page);
    await evidence(page, 'e2e-010-mobile-shipment-details');

    await navigate(page, 'nav-exceptions');
    await expect(page.getByRole('heading', { name: 'Logistics exceptions' })).toBeVisible();
    await expectNoHorizontalOverflow(page);
    await evidence(page, 'e2e-010-mobile-exceptions');

    await navigate(page, 'nav-copilot');
    await expect(page.getByRole('heading', { name: 'Operations copilot' })).toBeVisible();
    await page.getByTestId('copilot-question').fill('Which warehouse has the highest number of fulfillment failures?');
    await page.getByTestId('copilot-ask').click();
    await expect(page.getByTestId('copilot-answer')).toContainText('Low-stock positions');
    await expectNoHorizontalOverflow(page);
    await evidence(page, 'e2e-010-mobile-copilot');
  });
});
