import { defineConfig, devices } from "@playwright/test";

if (process.env.PLAYWRIGHT_BASE_URL)
  throw new Error("External Playwright base URLs are disabled. Run npm run test:e2e:prepare and use the isolated Playwright configuration.");
const port = Number(process.env.GF3_E2E_PORT ?? "5179");
if (!Number.isInteger(port) || port < 1024 || port > 65535)
  throw new Error("GF3_E2E_PORT must be a loopback test port from 1024 to 65535.");
const baseURL = `http://127.0.0.1:${port}`;

export default defineConfig({
  testDir: "./e2e",
  outputDir: `../GF3.Tests/TestResults/playwright/${port}`,
  workers: 2,
  timeout: 30_000,
  expect: {
    timeout: 5_000,
  },
  use: {
    baseURL,
    trace: "on-first-retry",
    screenshot: "only-on-failure",
  },
  webServer: {
    command: `node scripts/playwright-preview.mjs ${port}`,
    url: `${baseURL}/version.json`,
    reuseExistingServer: false,
    timeout: 120_000,
  },
  projects: [
    {
      name: "webkit-iphone-x",
      use: { ...devices["iPhone X"] },
    },
    {
      name: "webkit-laptop",
      use: { ...devices["Desktop Safari"], viewport: { width: 1280, height: 720 } },
    },
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
    {
      name: "mobile-chrome",
      use: { ...devices["Pixel 7"] },
    },
  ],
});
