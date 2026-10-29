import { apiGet, apiToken, createOrderThroughUi, evidence, expect, signIn, test, users } from '../support/fixtures';

interface Order {
  id: string;
  orderNumber: string;
  customerName: string;
  lines: { sku: string; quantity: number }[];
}

test.describe('E2E-002 Create order', () => {
  test('creates an order through the UI and persists it', async ({ page, request }) => {
    const customer = `E2E Create ${Date.now()}`;
    await signIn(page, users.opsManager);

    const orderId = await createOrderThroughUi(page, customer, 'SKU-1001', 2);

    const orderNumber = await page.getByTestId('order-number').textContent();
    expect(orderNumber).toMatch(/^ORD-\d+$/);
    await expect(page.getByTestId('order-lines')).toContainText('SKU-1001');
    await evidence(page, 'e2e-002-order-created');

    // Independent check through the API: the order exists with what the user typed.
    const token = await apiToken(request, users.opsManager);
    const stored = await apiGet<Order>(request, token, `/api/orders/${orderId}`);
    expect(stored.orderNumber).toBe(orderNumber);
    expect(stored.customerName).toBe(customer);
    expect(stored.lines).toEqual([expect.objectContaining({ sku: 'SKU-1001', quantity: 2 })]);

    // And it is listed.
    await page.getByTestId('nav-orders').click();
    await page.getByTestId('orders-search').fill(customer);
    await expect(page.getByTestId('order-row')).toHaveCount(1);
    await expect(page.getByTestId('order-row')).toHaveAttribute('data-order-number', orderNumber!);
  });
});
