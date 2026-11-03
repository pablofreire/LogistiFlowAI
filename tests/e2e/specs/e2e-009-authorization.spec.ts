import { apiToken, expect, signIn, test, users } from '../support/fixtures';

test.describe('E2E-009 Authorization', () => {
  test('a viewer can read orders but cannot create them, in the UI or through the API', async ({ page, request }) => {
    await signIn(page, users.viewer);
    await page.getByTestId('nav-orders').click();

    await expect(page.getByRole('heading', { name: 'Orders' })).toBeVisible();
    await expect(page.getByTestId('new-order')).toHaveCount(0);

    // Hiding the button is not the control: the API enforces the policy.
    const token = await apiToken(request, users.viewer);
    const response = await request.post('/api/orders', {
      headers: { Authorization: `Bearer ${token}`, 'Idempotency-Key': `viewer-${Date.now()}` },
      data: { customerName: 'Viewer', destinationCity: 'Austin', destinationCountry: 'US', lines: [{ sku: 'SKU-1001', quantity: 1 }] },
    });
    expect(response.status()).toBe(403);
  });

  test('a viewer gets no copilot, in the UI or through the API', async ({ page, request }) => {
    await signIn(page, users.viewer);
    await expect(page.getByTestId('nav-dashboard')).toBeVisible();
    await expect(page.getByTestId('nav-copilot')).toHaveCount(0);

    const token = await apiToken(request, users.viewer);
    const response = await request.post('/api/ai/copilot/ask', {
      headers: { Authorization: `Bearer ${token}` },
      data: { question: 'Which warehouse has the highest number of fulfillment failures?' },
    });
    expect(response.status()).toBe(403);
  });

  test('the API rejects calls without a token', async ({ request }) => {
    expect((await request.get('/api/orders')).status()).toBe(401);
    expect((await request.get('/api/dashboard/summary')).status()).toBe(401);
    expect((await request.get('/api/ai/copilot/status')).status()).toBe(401);
  });
});
