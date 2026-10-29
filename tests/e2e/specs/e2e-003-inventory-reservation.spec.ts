import { apiGet, apiToken, createOrderThroughUi, evidence, expect, signIn, test, users } from '../support/fixtures';

interface DashboardSummary {
  orders: { totalOrders: number } | null;
  fulfillment: { totalJobs: number } | null;
}

interface Reservation {
  status: string;
  warehouseCode: string;
  lines: { sku: string; quantity: number }[];
}

test.describe('E2E-003 Inventory reservation', () => {
  test('reserves stock, starts fulfillment and updates the dashboard', async ({ page, request }) => {
    const token = await apiToken(request, users.opsManager);
    const before = await apiGet<DashboardSummary>(request, token, '/api/dashboard/summary');
    await signIn(page, users.opsManager);

    const orderId = await createOrderThroughUi(page, `E2E Reserve ${Date.now()}`, 'SKU-1002', 1);

    // The order moves on its own as Inventory and Fulfillment react to the events.
    await expect(page.getByTestId('order-status').getByTestId('status-badge')).toHaveAttribute('data-status', 'Picking');
    await expect(page.getByTestId('reservation-status').getByTestId('status-badge')).toHaveAttribute('data-status', 'Reserved');
    await expect(page.getByTestId('fulfillment-status').getByTestId('status-badge')).toHaveAttribute('data-status', 'Picking');
    const timeline = page.getByTestId('timeline-entry');
    await expect(timeline).toHaveCount(4);
    await expect(timeline.nth(2)).toHaveAttribute('data-status', 'Reserved');
    await expect(timeline.nth(3)).toHaveAttribute('data-status', 'Picking');
    await evidence(page, 'e2e-003-order-reserved-and-picking');

    const reservation = await apiGet<Reservation>(request, token, `/api/inventory/reservations/by-order/${orderId}`);
    expect(reservation.status).toBe('Reserved');
    expect(reservation.lines).toEqual([{ sku: 'SKU-1002', quantity: 1 }]);
    await expect(page.getByTestId('reservation-warehouse')).toHaveText(reservation.warehouseCode);

    const after = await apiGet<DashboardSummary>(request, token, '/api/dashboard/summary');
    expect(after.orders!.totalOrders).toBe(before.orders!.totalOrders + 1);
    expect(after.fulfillment!.totalJobs).toBe(before.fulfillment!.totalJobs + 1);

    await page.getByTestId('nav-dashboard').click();
    await expect(page.getByTestId('kpi-total-orders-value')).toHaveText(after.orders!.totalOrders.toLocaleString('en-US'));
    await evidence(page, 'e2e-003-dashboard-updated');
  });
});
