import { evidence, expect, signIn, test, users } from '../support/fixtures';

test.describe('E2E-010 Responsive UI', () => {
  test('dashboard and navigation work on a phone viewport', async ({ page }) => {
    await signIn(page, users.opsManager);

    await expect(page.getByTestId('kpi-total-orders')).toBeVisible();
    const scrollWidth = await page.evaluate(() => document.documentElement.scrollWidth);
    const viewportWidth = page.viewportSize()!.width;
    expect(scrollWidth).toBeLessThanOrEqual(viewportWidth);
    await evidence(page, 'e2e-010-mobile-dashboard');

    await page.getByRole('button', { name: 'Toggle navigation' }).click();
    await page.getByTestId('nav-orders').click();
    await expect(page.getByRole('heading', { name: 'Orders' })).toBeVisible();
    await evidence(page, 'e2e-010-mobile-orders');
  });
});
