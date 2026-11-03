import { apiCreateOrder, apiGet, apiPackAndShip, apiToken, evidence, expect, signIn, test, users, waitForOrderStatus } from '../support/fixtures';

interface ShipmentDetails {
  trackingNumber: string | null;
  isDelayed: boolean;
}

test.describe('E2E-008 AI Copilot', () => {
  test('a delayed order is explained from live data by read-only tools, labelled as simulated', async ({ page, request }) => {
    test.setTimeout(120_000);
    const managerToken = await apiToken(request, users.opsManager);
    const operatorToken = await apiToken(request, users.warehouseOperator);

    // "Sandbox Delay" makes the simulated carrier report a weather delay in transit.
    const order = await apiCreateOrder(request, managerToken, { customer: `E2E Copilot ${Date.now()}`, sku: 'SKU-1003', quantity: 1, city: 'Sandbox Delay' });
    await waitForOrderStatus(request, managerToken, order.id, 'Picking');
    await apiPackAndShip(request, operatorToken, order.id);
    await expect
      .poll(async () => (await apiGet<ShipmentDetails>(request, managerToken, `/api/shipping/shipments/by-order/${order.id}`).catch(() => null))?.isDelayed ?? false, {
        timeout: 60_000,
        message: 'the carrier delay to be reported',
      })
      .toBe(true);
    const shipment = await apiGet<ShipmentDetails>(request, managerToken, `/api/shipping/shipments/by-order/${order.id}`);

    await signIn(page, users.opsManager);
    await page.getByTestId('nav-copilot').click();
    await expect(page.getByRole('heading', { name: 'Operations copilot' })).toBeVisible();
    // The deterministic demo provider is never presented as a model.
    await expect(page.getByTestId('copilot-provider')).toHaveAttribute('data-simulated', 'true');
    await expect(page.getByTestId('copilot-provider')).toContainText('Simulated');

    await page.getByTestId('copilot-question').fill(`Why is order ${order.orderNumber} delayed?`);
    await page.getByTestId('copilot-ask').click();

    const turn = page.getByTestId('copilot-turn').first();
    const answer = turn.getByTestId('copilot-answer');
    await expect(answer).toContainText(order.orderNumber);
    await expect(answer).toContainText('severe weather');
    // The tracking number in the answer is the one Shipping holds: the answer comes from the tool, not from a prompt.
    await expect(answer).toContainText(shipment.trackingNumber!);
    await expect(answer).toContainText('Simulated answer');
    await expect(turn.locator('[data-testid="copilot-tool-call"][data-name="AnalyzeShipmentDelay"]')).toHaveAttribute('data-outcome', 'ok');
    await evidence(page, 'e2e-008-copilot-delay');

    // A follow-up in the same conversation reuses the order number.
    await page.getByTestId('copilot-question').fill('Suggest the next actions.');
    await page.getByTestId('copilot-ask').click();
    const followUp = page.getByTestId('copilot-turn').first();
    await expect(followUp.getByTestId('copilot-turn-question')).toHaveText('Suggest the next actions.');
    await expect(followUp.locator('[data-testid="copilot-tool-call"][data-name="SuggestRecoveryActions"]')).toHaveAttribute('data-outcome', 'ok');
    await expect(followUp.getByTestId('copilot-answer')).toContainText('nothing has been changed');
    await expect(followUp.getByTestId('copilot-answer')).toContainText(order.orderNumber);
    await evidence(page, 'e2e-008-copilot-next-actions');
  });
});
