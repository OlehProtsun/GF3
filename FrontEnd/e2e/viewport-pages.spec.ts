import { expect, test } from "@playwright/test";

for (const role of ["manager", "employee"]) {
  test(`${role} pages and floating panels fit laptop viewports`, async ({ page }) => {
    test.setTimeout(90_000);
    const errors: string[] = [];
    page.on("pageerror", error => errors.push(error.message));
    await page.addInitScript(() => localStorage.setItem("gf3.auth.access-token", "viewport-fixture"));
    await page.route("**/*", async route => {
      const path = new URL(route.request().url()).pathname;
      if (!path.startsWith("/api/")) return route.continue();
      let body: unknown = [];
      if (path === "/api/auth/session") body = { role, managerId: role === "manager" ? 1 : null, employeeId: role === "employee" ? 1 : null, userName: "viewport", displayName: "Test account" };
      else if (path === "/api/account-language") body = { language: "en" };
      else if (path === "/api/manager-profile/me") body = { id: 1, userName: "viewport", displayName: "Test account", recoveryEmail: "test@example.com", isOnline: true, isSystem: false };
      else if (path === "/api/employee-profile/me") body = { employeeId: 1, username: "viewport", displayName: "Test account", recoveryEmail: "test@example.com", phone: "+48123456789" };
      else if (path.includes("notepad")) body = { notes: [], state: { isExpanded: false, isPinned: false, height: 700 } };
      else if (path.includes("negotiate")) return route.fulfill({ status: 503 });
      await route.fulfill({ contentType: "application/json", body: JSON.stringify(body) });
    });
    const paths = role === "manager"
      ? ["/", "/employee", "/employee/new", "/shop", "/shop/new", "/availability", "/availability/new", "/container", "/information", "/communications", "/database", "/manager-profile"]
      : ["/profile", "/notifications", "/swap", "/schedule", "/availability"];
    for (const size of [{ width: 1280, height: 720 }, { width: 1440, height: 800 }]) {
      await page.setViewportSize(size);
      for (const path of paths) {
        await page.goto(path);
        await expect(page.locator("main").first()).toBeVisible();
        await expect(page.getByRole("status", { name: "Loading page", exact: true })).toHaveCount(0);
        await expect(page.getByRole("heading", { name: "Sign in" })).toHaveCount(0);
        await expect.poll(() => page.locator("main").first().innerText()).not.toBe("");
        expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(size.width);
        await page.screenshot({ path: test.info().outputPath(`${size.width}-${path.replaceAll("/", "_")}.png`) });
      }
      if (role === "manager") {
        await page.getByRole("button", { name: "Open system news", exact: true }).click();
        const panel = page.getByRole("complementary", { name: "System news", exact: true });
        await expect(panel).toBeInViewport({ ratio: 1 });
        await page.getByRole("button", { name: "News settings", exact: true }).click();
        await expect(page.getByRole("dialog", { name: "Developer access" }).locator('[class*="surface_"]')).toBeInViewport({ ratio: 1 });
        await page.goto("/manager-profile");
        await page.getByRole("button", { name: "Open notepad", exact: true }).click();
        await expect(page.getByRole("complementary", { name: "Manager notepad", exact: true })).toBeInViewport({ ratio: 1 });
      }
    }
    expect(errors).toEqual([]);
  });
}
