import { createOrderThroughUi, evidence, expect, signIn, test, users } from '../support/fixtures';

test.describe('E2E-004 Insufficient inventory', () => {
  test('fails the order with the shortage and starts no fulfillment', async ({ page }) => {
    await signIn(page, users.opsManager);

    await createOrderThroughUi(page, `E2E Shortage ${Date.now()}`, 'SKU-OOS-001', 2);

    await expect(page.getByTestId('order-status').getByTestId('status-badge')).toHaveAttribute('data-status', 'Failed');
    await expect(page.getByTestId('order-status-reason')).toHaveText('Insufficient inventory: SKU-OOS-001 requested 2, available 0');
    await expect(page.getByTestId('reservation-status').getByTestId('status-badge')).toHaveAttribute('data-status', 'Failed');
    await expect(page.getByTestId('fulfillment-status')).toHaveText('Not started');
    await expect(page.getByTestId('cancel-order')).toHaveCount(0);
    await evidence(page, 'e2e-004-insufficient-inventory');
  });
});
