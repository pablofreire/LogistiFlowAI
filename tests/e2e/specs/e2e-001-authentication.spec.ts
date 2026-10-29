import { evidence, expect, signIn, test, users } from '../support/fixtures';

test.describe('E2E-001 Login and authentication', () => {
  test('signs in, shows the operator, and signs out', async ({ page }) => {
    await signIn(page, users.opsManager);

    await expect(page.getByTestId('current-role')).toHaveText('OpsManager');
    await expect(page.getByRole('heading', { name: 'Operations dashboard' })).toBeVisible();
    await evidence(page, 'e2e-001-dashboard-after-login');

    await page.getByTestId('sign-out').click();
    await expect(page).toHaveURL(/\/login$/);
    await expect(page.getByTestId('login-submit')).toBeVisible();
  });

  test('rejects a wrong password without revealing which field was wrong', async ({ page }) => {
    await page.goto('/login');
    await page.getByTestId('login-email').fill(users.opsManager.email);
    await page.getByTestId('login-password').fill('definitely-not-the-password');
    await page.getByTestId('login-submit').click();

    await expect(page.getByTestId('login-error')).toHaveText('The e-mail or password is incorrect.');
    await evidence(page, 'e2e-001-login-rejected');
  });

  test('redirects anonymous visitors to the login page', async ({ page }) => {
    await page.goto('/orders');

    await expect(page).toHaveURL(/\/login$/);
  });

  test('denies the order form to a viewer', async ({ page }) => {
    await signIn(page, users.viewer);
    await page.goto('/orders/new');

    await expect(page.getByTestId('access-denied')).toBeVisible();
    await evidence(page, 'e2e-001-access-denied');
  });
});
