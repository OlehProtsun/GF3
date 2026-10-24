import { expect, test } from "@playwright/test";

for (const status of [401, 500]) {
  test(`profile handles HTTP ${status} without an endless loading state`, async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem("gf3.auth.access-token", "review-token");
    });
    let profileRequests = 0;
    await page.route("**/api/**", async route => {
      const path = new URL(route.request().url()).pathname;
      if (!path.startsWith("/api/")) return route.continue();
      if (path === "/api/auth/session") return route.fulfill({ json: {
        role: "employee", employeeId: 1, managerId: null, userName: "worker", displayName: "Worker",
      } });
      if (path === "/api/account-language") return route.fulfill({ json: { language: "en" } });
      if (path === "/api/employee-profile/me") {
        profileRequests++;
        return route.fulfill({ status, json: { detail: "Injected API failure" } });
      }
      if (path.includes("negotiate")) return route.fulfill({ status: 503 });
      return route.fulfill({ json: [] });
    });
    await page.goto("/profile");
    if (status === 401) {
      await expect(page).toHaveURL(/\/login/);
      await expect(page.getByRole("button", { name: "Sign in", exact: true })).toBeVisible();
      expect(await page.evaluate(() => localStorage.getItem("gf3.auth.access-token"))).toBeNull();
    } else {
      await expect(page.getByText("Could not load your profile right now.")).toBeVisible();
      await expect(page.getByText("Loading your profile...")).toHaveCount(0);
      expect(await page.evaluate(() => localStorage.getItem("gf3.auth.access-token"))).toBe("review-token");
    }
    expect(profileRequests).toBeGreaterThan(0);
  });
}
