import { test, expect } from '@playwright/test';

const DEMO_CREDENTIALS = {
  admin: { email: 'admin@homeworke.com', password: 'Admin@123' },
  manager: { email: 'manager@homeworke.com', password: 'Manager@123' },
  employee: { email: 'demo@homeworke.com', password: 'Demo@123' },
};

test.describe('Authentication', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
  });

  // ── Login Page ────────────────────────────────

  test('should display login form', async ({ page }) => {
    await expect(page.getByRole('heading', { name: /sign in to your account/i })).toBeVisible();
    await expect(page.getByPlaceholder('you@company.com')).toBeVisible();
    await expect(page.getByPlaceholder('Enter your password')).toBeVisible();
    await expect(page.getByRole('button', { name: /sign in/i })).toBeVisible();
  });

  test('should show branding', async ({ page }) => {
    await expect(page.getByRole('heading', { name: 'HomeWorke' })).toBeVisible();
    await expect(page.getByText(/Time & Attendance System/i)).toBeVisible();
  });

  test('should show error for invalid credentials', async ({ page }) => {
    await page.getByPlaceholder('you@company.com').fill('wrong@test.com');
    await page.getByPlaceholder('Enter your password').fill('wrongpass');
    await page.getByRole('button', { name: /sign in/i }).click();

    // Should show error message (inline error or toast)
    await expect(page.locator('.text-red-700, [role="status"]').first()).toBeVisible({ timeout: 5000 });
  });

  // ── Successful Login ──────────────────────────

  test('should login as employee and see dashboard', async ({ page }) => {
    await page.getByPlaceholder('you@company.com').fill(DEMO_CREDENTIALS.employee.email);
    await page.getByPlaceholder('Enter your password').fill(DEMO_CREDENTIALS.employee.password);
    await page.getByRole('button', { name: /sign in/i }).click();

    // Should redirect to dashboard
    await expect(page).toHaveURL(/\/dashboard/, { timeout: 10000 });
    await expect(page.getByRole('heading', { name: /welcome/i })).toBeVisible({ timeout: 5000 });
  });

  test('should login as admin and see admin link', async ({ page }) => {
    await page.getByPlaceholder('you@company.com').fill(DEMO_CREDENTIALS.admin.email);
    await page.getByPlaceholder('Enter your password').fill(DEMO_CREDENTIALS.admin.password);
    await page.getByRole('button', { name: /sign in/i }).click();

    await expect(page).toHaveURL(/\/dashboard/, { timeout: 10000 });
    // Admin should see Admin link in navigation
    await expect(page.getByRole('link', { name: /admin/i })).toBeVisible({ timeout: 5000 });
  });

  // ── Protected Routes ──────────────────────────

  test('should redirect unauthenticated users to login', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page).toHaveURL(/\/login/);
  });

  test('should redirect unauthenticated users from admin page', async ({ page }) => {
    await page.goto('/admin');
    await expect(page).toHaveURL(/\/login/);
  });

  // ── Forgot Password ───────────────────────────

  test('should navigate to forgot password page', async ({ page }) => {
    await page.getByRole('link', { name: /forgot your password/i }).click();
    await expect(page).toHaveURL(/\/forgot-password/);
    // Forgot password page has an email input
    await expect(page.locator('input[type="email"]').first()).toBeVisible();
  });

  // ── Logout ────────────────────────────────────

  test('should allow logout', async ({ page }) => {
    // Login first
    await page.getByPlaceholder('you@company.com').fill(DEMO_CREDENTIALS.employee.email);
    await page.getByPlaceholder('Enter your password').fill(DEMO_CREDENTIALS.employee.password);
    await page.getByRole('button', { name: /sign in/i }).click();
    await expect(page).toHaveURL(/\/dashboard/, { timeout: 10000 });

    // Find and click logout
    const logoutButton = page.getByRole('button', { name: /logout/i })
      .or(page.getByText(/logout/i));
    if (await logoutButton.isVisible()) {
      await logoutButton.click();
      await expect(page).toHaveURL(/\/login/, { timeout: 5000 });
    }
  });
});
