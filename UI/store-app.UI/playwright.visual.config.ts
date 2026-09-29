import { defineConfig, devices } from '@playwright/test';

/**
 * The visual tests: a screenshot of every story of the built Storybook (npm run storybook:build),
 * compared with the one committed in visual/__screenshots__. Fonts are drawn differently on each
 * system, so the screenshots are Linux ones, taken in the Playwright image (npm run test:visual:docker
 * locally, the same image in CI).
 */
const PORT = 6006;

export default defineConfig({
  testDir: './visual',
  // One file per story, the same on every system (the image is always Linux)
  snapshotPathTemplate: '{testDir}/__screenshots__/{arg}{ext}',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: 0,
  reporter: process.env.CI ? [['github'], ['html', { open: 'never', outputFolder: 'playwright-report-visual' }]] : [['list']],
  outputDir: 'test-results-visual',
  use: {
    ...devices['Desktop Chrome'],
    baseURL: `http://localhost:${PORT}`,
    viewport: { width: 1100, height: 1000 },
    colorScheme: 'light',
    reducedMotion: 'reduce',
    locale: 'en-US',
    timezoneId: 'Europe/Warsaw',
  },
  expect: {
    // The same image draws the same pixels every time, so no pixel may change (each only within
    // the default colour tolerance): a corner radius is a few dozen pixels of a whole card
    toHaveScreenshot: { animations: 'disabled', caret: 'hide' },
  },
  webServer: {
    command: `node scripts/serve-static.mjs storybook-static ${PORT}`,
    url: `http://localhost:${PORT}/index.json`,
    reuseExistingServer: !process.env.CI,
  },
});
