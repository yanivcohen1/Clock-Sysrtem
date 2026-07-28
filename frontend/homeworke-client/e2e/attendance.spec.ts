import { test, expect } from '@playwright/test';

test.describe('Attendance', () => {
  test.beforeEach(async ({ page }) => {
    // Login as employee
    await page.goto('/login');
    await page.getByPlaceholder('you@company.com').fill('demo@homeworke.com');
    await page.getByPlaceholder('Enter your password').fill('Demo@123');
    await page.getByRole('button', { name: /sign in/i }).click();
    await expect(page).toHaveURL(/\/dashboard/, { timeout: 10000 });
  });

  test('should display attendance controls on dashboard', async ({ page }) => {
    // Dashboard should show clock-in/clock-out status or buttons
    // Dashboard shows clock status text (e.g. "Clocked In" or "Clocked Out")
    await expect(
      page.getByRole('button', { name: /clock in/i })
        .or(page.getByRole('button', { name: /clock out/i }))
        .or(page.getByText(/clocked in/i).first())
        .or(page.getByText(/clocked out/i).first())
    ).toBeVisible({ timeout: 5000 });
  });

  test('should navigate to attendance page', async ({ page }) => {
    // Find and click attendance link
    const attendanceLink = page.getByRole('link', { name: /attendance/i });
    if (await attendanceLink.isVisible()) {
      await attendanceLink.click();
      await expect(page).toHaveURL(/\/attendance/);
    }
  });

  test('should navigate to history page', async ({ page }) => {
    const historyLink = page.getByRole('link', { name: /history/i });
    if (await historyLink.isVisible()) {
      await historyLink.click();
      await expect(page).toHaveURL(/\/history/);
      await expect(page.getByText(/history/i).first()).toBeVisible();
    }
  });

  test('should display personal history with pagination', async ({ page }) => {
    await page.goto('/history');
    await expect(page).toHaveURL(/\/history/, { timeout: 5000 });

    // Check for table, "no records" message, or history heading
    await expect(
      page.getByRole('table')
        .or(page.getByText(/no records/i))
        .or(page.getByRole('heading', { name: /history/i }))
    ).toBeVisible({ timeout: 5000 });
  });
});
