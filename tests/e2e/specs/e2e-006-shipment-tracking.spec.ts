import { apiCreateOrder, apiGet, apiPackAndShip, apiToken, evidence, expect, signIn, test, users, waitForOrderStatus } from '../support/fixtures';

interface ShippingMetrics {
  delivered: number;
}

interface OrdersMetrics {
  averageSecondsToDelivered: number | null;
}

interface DashboardSummary {
  orders: OrdersMetrics | null;
  shipping: ShippingMetrics | null;
}

test.describe('E2E-006 Shipment tracking', () => {
  test('carrier events move the shipment to delivered and the dashboard follows', async ({ page, request }) => {
    const managerToken = await apiToken(request, users.opsManager);
    const operatorToken = await apiToken(request, users.warehouseOperator);
    const before = await apiGet<DashboardSummary>(request, managerToken, '/api/dashboard/summary');

    const order = await apiCreateOrder(request, managerToken, { customer: `E2E Tracking ${Date.now()}`, sku: 'SKU-1003', quantity: 1 });
    await waitForOrderStatus(request, managerToken, order.id, 'Picking');
    await apiPackAndShip(request, operatorToken, order.id);

    await signIn(page, users.opsManager);
    await page.goto(`/orders/${order.id}`);
    await page.getByTestId('order-shipment-link').click();

    // The simulated carrier reports one step every few seconds; the page refreshes on its own.
    const events = page.getByTestId('tracking-event');
    await expect(events.first()).toHaveAttribute('data-code', 'LABEL_CREATED');
    await expect(page.getByTestId('shipment-status').getByTestId('status-badge')).toHaveAttribute('data-status', 'InTransit', { timeout: 30_000 });
    await evidence(page, 'e2e-006-shipment-in-transit');

    await expect(page.getByTestId('shipment-status').getByTestId('status-badge')).toHaveAttribute('data-status', 'Delivered', { timeout: 45_000 });
    await expect(events).toHaveCount(5);
    await expect(events.last()).toHaveAttribute('data-code', 'DELIVERED');
    await evidence(page, 'e2e-006-shipment-delivered');

    await waitForOrderStatus(request, managerToken, order.id, 'Delivered');

    const after = await apiGet<DashboardSummary>(request, managerToken, '/api/dashboard/summary');
    expect(after.shipping!.delivered).toBeGreaterThanOrEqual(before.shipping!.delivered + 1);
    await page.getByTestId('nav-dashboard').click();
    await expect(page.getByTestId('kpi-delivered-value')).not.toHaveText(before.shipping!.delivered.toLocaleString('en-US'));
    // At least one order is delivered now, so the end-to-end processing time has a value.
    expect(after.orders!.averageSecondsToDelivered).toBeGreaterThan(0);
    await expect(page.getByTestId('kpi-processing-time-value')).toHaveText(/^\d+(\.\d)? (s|min|h)$/);
    await evidence(page, 'e2e-006-dashboard-delivered');
  });
});
