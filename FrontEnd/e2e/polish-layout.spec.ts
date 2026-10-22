import { expect, test } from "@playwright/test";

test.use({ reducedMotion: "reduce" });

for (const role of ["manager", "manager-phone", "employee"]) {
  test(`Polish ${role} labels fit their controls`, async ({ page }) => {
    test.setTimeout(420_000);
    const manager = role !== "employee";
    const phone = role === "manager-phone";
    const graph = { id: 10, containerId: 1, shopId: 1, name: "F27", year: 2026, month: 10, publicationStatus: "public", allowSwap: true, peoplePerShift: 1,
      shift1Time: "09:00 - 16:30", shift2Time: "16:30 - 22:00", maxHoursPerEmpMonth: 160, maxConsecutiveDays: 5, maxConsecutiveFull: 3, maxFullPerMonth: 10 };
    const errors: string[] = [];
    page.on("pageerror", error => errors.push(error.message));
    await page.addInitScript(() => localStorage.setItem("gf3.auth.access-token", "polish-fixture"));
    await page.route("**/*", async route => {
      const path = new URL(route.request().url()).pathname;
      if (!path.startsWith("/api/")) return route.continue();
      let body: unknown = [];
      if (path === "/api/auth/session") body = { role: manager ? "manager" : "employee", isSystemManager: manager, workspaceMode: phone ? "phone" : "pc", managerId: manager ? 1 : null, employeeId: !manager ? 1 : null, userName: "polish", displayName: "Test account" };
      else if (path === "/api/account-language") body = { language: "pl" };
      else if (path === "/api/manager-profile/me") body = { id: 1, userName: "polish", displayName: "Test account", recoveryEmail: "test@example.com", isOnline: true, isSystem: false };
      else if (path === "/api/employee-profile/me") body = { employeeId: 1, username: "polish", displayName: "Test account", recoveryEmail: "test@example.com", phone: "+48123456789" };
      else if (path === "/api/containers") body = [{ id: 1, name: "Main" }];
      else if (path === "/api/containers/1") body = { id: 1, name: "Main" };
      else if (path === "/api/containers/1/graphs") body = [graph];
      else if (path === "/api/containers/1/graphs/10") body = graph;
      else if (path === "/api/containers/1/graphs/10/employees") body = [{ id: 1, scheduleId: 10, employeeId: 1, displayOrder: 1, minHoursMonth: 160 }];
      else if (path === "/api/containers/1/graphs/10/slots") body = [{ id: 100, scheduleId: 10, dayOfMonth: 1, slotNo: 1, employeeId: 1, fromTime: "09:00", toTime: "16:30", status: "Working" }];
      else if (path === "/api/employees") body = [{ id: 1, firstName: "Anna", lastName: "Kowalska" }];
      else if (path === "/api/shops") body = [{ id: 1, name: "Central", address: "Warszawa" }];
      else if (path === "/api/employee-ui-state") body = { scheduleColumnOrders: {}, readNotificationIds: [], pinnedSwapIds: [] };
      else if (path === "/api/employee-schedules") body = [{ id: 10, containerId: 1, containerName: "Main", shopId: 1, shopName: "Central", name: "F27", year: 2026, month: 10, publicationStatus: "public", allowSwap: true,
        publishedAtUtc: new Date().toISOString(),
        employees: [{ id: 1, employeeId: 1, firstName: "Owner", lastName: "Worker", displayName: "Owner Worker", displayOrder: 1 }],
        slots: [{ id: 100, dayOfMonth: 1, slotNo: 1, employeeId: 1, fromTime: "09:00", toTime: "16:30", status: "ASSIGNED" }] }];
      else if (path.includes("notepad")) body = { notes: [], state: { isExpanded: false, isPinned: false, height: 700 } };
      else if (path.includes("negotiate")) return route.fulfill({ status: 503 });
      await route.fulfill({ contentType: "application/json", body: JSON.stringify(body) });
    });
    const paths = phone ? ["/", "/employee", "/shop", "/availability", "/container", "/container/1/graphs/10", "/more", "/manager-profile"] : manager
      ? ["/", "/employee", "/employee/new", "/shop", "/shop/new", "/availability", "/availability/new", "/container", "/container/1/graphs/10", "/information", "/communications", "/database", "/manager-profile"]
      : ["/profile", "/notifications", "/swap", "/schedule", "/availability"];
    for (const width of [320, 768, 1024, 1440]) {
      await page.setViewportSize({ width, height: 900 });
      for (const path of paths) {
        await page.goto(path);
        await expect(page.locator("html")).toHaveAttribute("lang", "pl");
        const surface = path === "/database" ? page.getByRole("dialog") : page.locator("main").first();
        await expect(surface).toBeVisible({ timeout: 30_000 });
        await expect(page.getByRole("status", { name: "Wczytywanie strony", exact: true })).toHaveCount(0, { timeout: 30_000 });
        await expect.poll(() => surface.innerText()).not.toBe("");
        if (role === "manager" && path !== "/database") await page.getByRole("button", { name: "Zwiń pasek boczny", exact: true }).click();
        if (!manager && path === "/swap") {
          await page.getByRole("button", { name: "Utwórz ofertę", exact: true }).click();
          await page.locator("dialog:modal").getByRole("button", { name: /F27/ }).click();
          await page.getByRole("radio", { name: /09:00.*16:30/ }).click();
          await expect(page.locator("dialog:modal")).toContainText("7,5 godz.");
        }
        if (!manager && path === "/notifications") await expect(page.getByText("Opublikowano nowy grafik", { exact: true })).toBeVisible();
        await page.evaluate(async () => {
          await document.fonts.ready;
          for (const animation of document.getAnimations()) {
            if (animation.playbackRate !== 0 && Number.isFinite(Number(animation.effect?.getComputedTiming().endTime))) animation.finish();
          }
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
            if (text.width && (text.left < box.left - 2 || text.right > box.right + 2)) {
              let clipped = false;
              let ancestor: HTMLElement | null = node.parentElement;
              while (ancestor && element.contains(ancestor)) {
                const ancestorStyle = getComputedStyle(ancestor);
                const bounds = ancestor.getBoundingClientRect();
                if (ancestorStyle.overflowX === "hidden" && ancestorStyle.textOverflow === "ellipsis" &&
                    bounds.width > 0 && bounds.left >= box.left - 2 && bounds.right <= box.right + 2) {
                  clipped = true;
                  break;
                }
                ancestor = ancestor.parentElement;
              }
              if (!clipped) return [element.textContent?.trim()];
            }
          }
          return [];
        })), { message: `${path} at ${width}px` }).toEqual([]);
        await page.screenshot({ path: test.info().outputPath(`${width}-${path.replaceAll("/", "_")}.png`), animations: "disabled" });
      }
    }
    expect(errors).toEqual([]);
  });
}
