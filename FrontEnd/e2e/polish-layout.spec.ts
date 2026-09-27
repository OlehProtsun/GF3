import { expect, test } from "@playwright/test";

for (const role of ["manager", "employee"]) {
  test(`Polish ${role} labels fit their controls`, async ({ page }) => {
    test.setTimeout(180_000);
    const errors: string[] = [];
    page.on("pageerror", error => errors.push(error.message));
    await page.addInitScript(() => localStorage.setItem("gf3.auth.access-token", "polish-fixture"));
    await page.route("**/*", async route => {
      const path = new URL(route.request().url()).pathname;
      if (!path.startsWith("/api/")) return route.continue();
      let body: unknown = [];
      if (path === "/api/auth/session") body = { role, managerId: role === "manager" ? 1 : null, employeeId: role === "employee" ? 1 : null, userName: "polish", displayName: "Test account" };
      else if (path === "/api/account-language") body = { language: "pl" };
      else if (path === "/api/manager-profile/me") body = { id: 1, userName: "polish", displayName: "Test account", recoveryEmail: "test@example.com", isOnline: true, isSystem: false };
      else if (path === "/api/employee-profile/me") body = { employeeId: 1, username: "polish", displayName: "Test account", recoveryEmail: "test@example.com", phone: "+48123456789" };
      else if (path.includes("notepad")) body = { notes: [], state: { isExpanded: false, isPinned: false, height: 700 } };
      else if (path.includes("negotiate")) return route.fulfill({ status: 503 });
      await route.fulfill({ contentType: "application/json", body: JSON.stringify(body) });
    });
    const paths = role === "manager"
      ? ["/", "/employee", "/employee/new", "/shop", "/shop/new", "/availability", "/availability/new", "/container", "/information", "/communications", "/database", "/manager-profile"]
      : ["/profile", "/notifications", "/swap", "/schedule", "/availability"];
    for (const width of [320, 768, 1024, 1440]) {
      await page.setViewportSize({ width, height: 900 });
      for (const path of paths) {
        await page.goto(path);
        await expect(page.locator("html")).toHaveAttribute("lang", "pl");
        await expect(page.locator("main").first()).toBeVisible();
        await expect(page.getByRole("status", { name: "Wczytywanie strony", exact: true })).toHaveCount(0);
        await expect.poll(() => page.locator("main").first().innerText()).not.toBe("");
        if (role === "manager" && path !== "/database") await page.getByRole("button", { name: "Zwiń pasek boczny", exact: true }).click();
        await page.evaluate(async () => {
          await document.fonts.ready;
          await Promise.all(document.getAnimations().filter(animation => Number.isFinite(Number(animation.effect?.getComputedTiming().endTime))).map(animation => animation.finished.catch(() => {})));
        });
        await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth), `${path} at ${width}px`).toBeLessThanOrEqual(width);
        await expect.poll(() => page.locator('button, a, label, h1, h2, h3').evaluateAll(elements => elements.flatMap(element => {
          const box = element.getBoundingClientRect();
          if (!element.checkVisibility({ opacityProperty: true, visibilityProperty: true }) || element.closest('[aria-hidden="true"], [inert]') || !box.width || !box.height || box.right < 0 || box.left > innerWidth || getComputedStyle(element).visibility === "hidden") return [];
          const walker = document.createTreeWalker(element, NodeFilter.SHOW_TEXT);
          let node: Node | null;
          while ((node = walker.nextNode())) {
            if (!node.textContent?.trim() || !(node.parentElement instanceof HTMLElement)) continue;
            const style = getComputedStyle(node.parentElement);
            if (style.position === "absolute" || style.clipPath !== "none" || style.visibility === "hidden" || !node.parentElement.getBoundingClientRect().width) continue;
            const range = document.createRange();
            range.selectNodeContents(node);
            const text = range.getBoundingClientRect();
            if (text.width && (text.left < box.left - 2 || text.right > box.right + 2)) return [element.textContent?.trim()];
          }
          return [];
        })), { message: `${path} at ${width}px` }).toEqual([]);
        await page.screenshot({ path: test.info().outputPath(`${width}-${path.replaceAll("/", "_")}.png`) });
      }
    }
    expect(errors).toEqual([]);
  });
}
