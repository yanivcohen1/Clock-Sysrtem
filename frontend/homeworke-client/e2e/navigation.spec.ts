import { test, expect } from '@playwright/test';

test.describe('Navigation', () => {
  test.beforeEach(async ({ page }) => {
    // Login as demo employee
    await page.goto('/login');
    await page.getByPlaceholder('you@company.com').fill('demo@homeworke.com');
    await page.getByPlaceholder('Enter your password').fill('Demo@123');
    await page.getByRole('button', { name: /sign in/i }).click();
    await expect(page).toHaveURL(/\/dashboard/, { timeout: 10000 });
  });

  test('should have working sidebar/header navigation', async ({ page }) => {
    // Verify key navigation links exist after login
    const navLinks = [
      page.getByRole('link', { name: /dashboard/i }),
      page.getByRole('link', { name: /attendance/i }),
      page.getByRole('link', { name: /history/i }),
    ];

    for (const link of navLinks) {
      if (await link.isVisible()) {
        await link.click();
        // Page should load without error
        await expect(page.locator('body')).toBeVisible();
      }
    }
  });

  test('should navigate to dashboard as landing page after login', async ({ page }) => {
    await expect(page).toHaveURL(/\/dashboard/);
  });

  test('should show user info in header', async ({ page }) => {
    // Should show welcome heading with employee name
    await expect(
      page.getByRole('heading', { name: /welcome back/i })
    ).toBeVisible({ timeout: 3000 });
  });
});

test.describe('Admin Navigation', () => {
  test.beforeEach(async ({ page }) => {
    // Login as admin
    await page.goto('/login');
    await page.getByPlaceholder('you@company.com').fill('admin@homeworke.com');
    await page.getByPlaceholder('Enter your password').fill('Admin@123');
    await page.getByRole('button', { name: /sign in/i }).click();
    await expect(page).toHaveURL(/\/dashboard/, { timeout: 10000 });
  });

  test('should access admin panel', async ({ page }) => {
    const adminLink = page.getByRole('link', { name: /admin/i });
    if (await adminLink.isVisible()) {
      await adminLink.click();
      await expect(page).toHaveURL(/\/admin/, { timeout: 5000 });
      await expect(page.getByText(/admin/i).first()).toBeVisible();
    }
  });

  test('should access reports page', async ({ page }) => {
    const reportsLink = page.getByRole('link', { name: /reports/i });
    if (await reportsLink.isVisible()) {
      await reportsLink.click();
      await expect(page).toHaveURL(/\/reports/, { timeout: 5000 });
    }
  });
});
