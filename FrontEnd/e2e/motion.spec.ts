import { expect, test } from "@playwright/test";

for (const reducedMotion of ["no-preference", "reduce"] as const) {
  test(`page navigation and button feedback: ${reducedMotion}`, async ({ page }) => {
    await page.emulateMedia({ reducedMotion });
    const errors: string[] = [];
    page.on("pageerror", error => errors.push(error.message));
    await page.goto("/login");
    await expect(page.getByRole("heading", { name: "Sign in", exact: true })).toBeVisible();
    const recovery = page.getByRole("button", { name: "Forgot password?", exact: true });
    await recovery.focus();
    await page.keyboard.down("Space");
    const animation = await recovery.evaluate(node => getComputedStyle(node).animationName);
    expect(animation).toBe("gf3-press");
    await page.keyboard.up("Space");
    await expect(page.getByRole("heading", { name: "Reset password", exact: true })).toBeVisible();
    const root = page.locator('div[style="display: contents;"] > *').first();
    await expect.poll(() => root.evaluate(node => (node as HTMLElement).style.opacity)).toBe("");
    await page.goBack();
    await expect(page.getByRole("heading", { name: "Sign in", exact: true })).toBeVisible();
    await expect.poll(() => root.evaluate(node => (node as HTMLElement).style.opacity)).toBe("");
    expect(errors).toEqual([]);
  });

  test(`record list entrances stay bounded: ${reducedMotion}`, async ({ page }) => {
    await page.emulateMedia({ reducedMotion });
    const errors: string[] = [];
    page.on("pageerror", error => errors.push(error.message));
    await page.addInitScript(() => localStorage.setItem("gf3.auth.access-token", "motion-fixture"));
    await page.route("**/*", async route => {
      const path = new URL(route.request().url()).pathname;
      if (!path.startsWith("/api/")) return route.continue();
      let body: unknown = [];
      if (path === "/api/auth/session") body = { role: "manager", managerId: 1, userName: "motion", displayName: "Motion test" };
      else if (path === "/api/account-language") body = { language: "en" };
      else if (path === "/api/employees") body = Array.from({ length: 20 }, (_, i) => ({ id: i + 1, firstName: `Employee ${i}`, lastName: "Test", isOnline: false }));
      else if (path.includes("notepad")) body = { notes: [], state: { isExpanded: false, isPinned: false, height: 700 } };
      else if (path.includes("negotiate")) return route.fulfill({ status: 503 });
      await route.fulfill({ contentType: "application/json", body: JSON.stringify(body) });
    });
    await page.goto("/employee");
    const cards = page.locator("[data-motion-list] > *");
    await expect(cards).toHaveCount(20);
    const motion = await cards.evaluateAll(nodes => nodes.map(node => {
      const style = getComputedStyle(node);
      return { name: style.animationName, total: parseFloat(style.animationDuration) + parseFloat(style.animationDelay) };
    }));
    expect(motion[0].name).toBe("gf3-enter");
    expect(motion.every(item => item.total <= 0.24)).toBe(true);
    expect(motion[12].name).toBe("none");
    expect(errors).toEqual([]);
    await expect.poll(() => cards.first().evaluate(node => getComputedStyle(node).translate)).toBe("none");
  });
}
