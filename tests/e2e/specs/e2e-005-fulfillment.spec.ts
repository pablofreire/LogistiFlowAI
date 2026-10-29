import { apiCreateOrder, apiGet, apiToken, evidence, expect, signIn, test, users, waitForOrderStatus } from '../support/fixtures';

interface Reservation {
  status: string;
}

test.describe('E2E-005 Fulfillment', () => {
  test('a warehouse operator picks, packs and ships an order', async ({ page, request }) => {
    const managerToken = await apiToken(request, users.opsManager);
    const order = await apiCreateOrder(request, managerToken, { customer: `E2E Floor ${Date.now()}`, sku: 'SKU-1003', quantity: 2 });
    await waitForOrderStatus(request, managerToken, order.id, 'Picking');

    await signIn(page, users.warehouseOperator);

    // The job is waiting on the floor, newest first.
    await page.getByTestId('nav-fulfillment').click();
    const row = page.locator(`[data-testid="job-row"][data-order-number="${order.orderNumber}"]`);
    await expect(row.getByTestId('status-badge')).toHaveAttribute('data-status', 'Picking');
    await evidence(page, 'e2e-005-fulfillment-queue');

    await row.getByTestId('job-action-pack').click();
    await expect(row.getByTestId('status-badge')).toHaveAttribute('data-status', 'Packed');

    // Shipping from the order page: the operator sees the order and its shipment move together.
    await row.getByRole('link', { name: order.orderNumber }).click();
    await expect(page.getByTestId('order-status').getByTestId('status-badge')).toHaveAttribute('data-status', 'Packed');
    await page.getByTestId('job-action-ship').click();
    await expect(page.getByTestId('fulfillment-status').getByTestId('status-badge')).toHaveAttribute('data-status', 'Shipped');
    await expect(page.getByTestId('order-status').getByTestId('status-badge')).toHaveAttribute('data-status', /Shipped|Delivered/);
    await expect(page.getByTestId('order-shipment-tracking')).toHaveText(/^RDRN\d+$/);
    await evidence(page, 'e2e-005-order-shipped');

    const history = await apiGet<{ toStatus: string }[]>(request, managerToken, `/api/orders/${order.id}/history`);
    expect(history.map((change) => change.toStatus).slice(0, 6)).toEqual(['Created', 'PendingInventory', 'Reserved', 'Picking', 'Packed', 'Shipped']);
    await expect
      .poll(async () => (await apiGet<Reservation>(request, managerToken, `/api/inventory/reservations/by-order/${order.id}`)).status)
      .toBe('Consumed');
  });

  test('a viewer sees the queue but no floor actions', async ({ page }) => {
    await signIn(page, users.viewer);
    await page.getByTestId('nav-fulfillment').click();

    await expect(page.getByTestId('jobs-table')).toBeVisible();
    await expect(page.getByTestId('job-action-pack')).toHaveCount(0);
  });
});
