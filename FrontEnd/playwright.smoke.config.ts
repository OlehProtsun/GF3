import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./e2e",
  testMatch: ["**/pwa-launch.spec.ts", "**/employee-install-guide.spec.ts", "**/communications-media.spec.ts"],
  outputDir: "../GF3.Tests/TestResults/playwright",
  workers: 2,
  use: {
    baseURL: "http://localhost:5173",
    serviceWorkers: "block",
    trace: "on-first-retry",
    screenshot: "only-on-failure",
  },
  webServer: {
    command: "npm run dev -- --host localhost",
    url: "http://localhost:5173/login",
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
  },
  projects: [
    { name: "chromium", use: { ...devices["Desktop Chrome"] } },
    { name: "webkit-iphone-x", use: { ...devices["iPhone X"] } },
  ],
});
