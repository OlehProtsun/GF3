import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./e2e",
  testMatch: ["**/pwa-launch.spec.ts", "**/employee-install-guide.spec.ts", "**/communications-media.spec.ts"],
  outputDir: "../GF3.Tests/TestResults/playwright",
  workers: 2,
  use: {
    baseURL: "http://127.0.0.1:5199",
    serviceWorkers: "block",
    trace: "on-first-retry",
    screenshot: "only-on-failure",
  },
  webServer: {
    command: "node scripts/playwright-preview.mjs 5199",
    url: "http://127.0.0.1:5199/version.json",
    reuseExistingServer: false,
    timeout: 120_000,
  },
  projects: [
    { name: "chromium", use: { ...devices["Desktop Chrome"] } },
    { name: "webkit-iphone-x", use: { ...devices["iPhone X"] } },
  ],
});
