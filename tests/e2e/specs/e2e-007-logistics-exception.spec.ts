import { apiCreateOrder, apiPackAndShip, apiToken, evidence, expect, signIn, test, users, waitForOrderStatus } from '../support/fixtures';

test.describe('E2E-007 Logistics exception', () => {
  test('a carrier delay is identified as an exception and flagged on the shipment', async ({ page, request }) => {
    test.setTimeout(90_000);
    const managerToken = await apiToken(request, users.opsManager);
    const operatorToken = await apiToken(request, users.warehouseOperator);

    // "Sandbox Delay" is a reserved test destination: the simulated carrier reports a weather delay in transit.
    const order = await apiCreateOrder(request, managerToken, { customer: `E2E Delay ${Date.now()}`, sku: 'SKU-1003', quantity: 1, city: 'Sandbox Delay' });
    await waitForOrderStatus(request, managerToken, order.id, 'Picking');
    await apiPackAndShip(request, operatorToken, order.id);

    await signIn(page, users.opsManager);
    await page.getByTestId('nav-exceptions').click();
    const exception = page.locator(`[data-testid="exception-row"][data-order-number="${order.orderNumber}"][data-type="Delay"]`);
    await expect(exception).toHaveAttribute('data-status', 'Open', { timeout: 30_000 });
    await expect(exception).toContainText('severe weather');
    await evidence(page, 'e2e-007-exceptions-delay');

    await exception.getByRole('link', { name: order.orderNumber }).click();
    await expect(page.getByTestId('shipment-delayed-alert')).toBeVisible();
    await expect(page.locator('[data-testid="shipment-exception"][data-type="Delay"]')).toHaveAttribute('data-status', 'Open');
    await evidence(page, 'e2e-007-shipment-delayed');

    await page.getByTestId('nav-shipments').click();
    // The switch input is visually hidden (Mantine); operators click its label.
    await page.getByText('Delayed only').click();
    await expect(page.getByTestId('shipments-delayed-only')).toBeChecked();
    await expect(page.locator(`[data-testid="shipment-row"][data-order-number="${order.orderNumber}"]`).getByTestId('delayed-badge')).toBeVisible();

    await page.getByTestId('nav-dashboard').click();
    await expect(page.getByTestId('kpi-delayed-shipments-value')).not.toHaveText('0');
  });
});
