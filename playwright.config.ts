import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: 'frontend/homeworke-client/e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: [['html', { open: 'never' }], ['list']],

  use: {
    baseURL: 'http://localhost:5173',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  // Start both backend and frontend dev servers before running tests
  webServer: [
    {
      command: 'dotnet run --project ./backend/HomeWorke.Api/HomeWorke.Api.csproj',
      url: 'http://localhost:5001/swagger',
      timeout: 30_000,
      reuseExistingServer: !process.env.CI,
    },
    {
      command: 'npm run dev --prefix ./frontend/homeworke-client',
      url: 'http://localhost:5173',
      timeout: 30_000,
      reuseExistingServer: !process.env.CI,
    },
  ],
});
