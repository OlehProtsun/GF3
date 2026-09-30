import { expect, test, type Page } from "@playwright/test";

async function employeeFixture(page: Page) {
  await page.addInitScript(() => localStorage.setItem("gf3.auth.access-token", "employee-motion-fixture"));
  await page.route("**/*", async route => {
    const path = new URL(route.request().url()).pathname;
    if (!path.startsWith("/api/")) return route.continue();
    let body: unknown = [];
    if (path === "/api/auth/session") body = { role: "employee", employeeId: 12, userName: "worker", displayName: "Motion Worker" };
    else if (path === "/api/account-language") body = { language: "en" };
    else if (path === "/api/employee-profile/me") body = { employeeId: 12, username: "worker", displayName: "Motion Worker", recoveryEmail: "worker@example.com" };
    else if (path === "/api/employee-ui-state") body = { scheduleColumnOrders: {}, readNotificationIds: [], pinnedSwapIds: [] };
    else if (path === "/api/employee-availability") body = [5, 6].map(id => ({
      id, name: `Window ${id}`, year: 2026, month: 9, canSubmit: true, isEditLocked: false,
      visibleFromUtc: "2026-09-01T00:00:00Z", visibleToUtc: "2026-10-01T23:59:00Z", slots: [],
    }));
    else if (path === "/api/employee-schedules") body = [{
      id: 10, containerId: 2, containerName: "Team", shopId: 4, shopName: "Central",
      name: "September", year: 2026, month: 9, publicationStatus: "public", allowSwap: true,
      employees: [{ id: 1, employeeId: 12, firstName: "Motion", lastName: "Worker", displayName: "Motion Worker", displayOrder: 1 }],
      slots: [{ id: 100, dayOfMonth: 1, slotNo: 1, employeeId: 12, fromTime: "08:00", toTime: "16:00", status: "ASSIGNED" }],
    }];
    else if (path === "/api/employee-shift-swaps") body = [{
      id: 3, scheduleId: 10, scheduleSlotId: 100, scheduleName: "September", containerName: "Team", shopName: "Central",
      year: 2026, month: 9, dayOfMonth: 1, fromTime: "08:00", toTime: "16:00",
      fromEmployeeId: 7, fromEmployeeName: "Other Worker", visibility: "public", status: "open",
      createdAtUtc: "2026-09-30T08:00:00Z", shiftHours: 8, isCreatedByCurrentEmployee: false,
      isScheduleLocked: false, canAccept: true, canCancel: false, isManagerCreated: false,
      currentEmployeeHoursBefore: 8, currentEmployeeHoursAfter: 16,
      currentEmployeeWorkDaysBefore: 1, currentEmployeeWorkDaysAfter: 2,
      currentEmployeeFreeDaysBefore: 29, currentEmployeeFreeDaysAfter: 28,
      fromEmployeeHoursBefore: 8, fromEmployeeHoursAfter: 0,
    }];
    else if (path.includes("negotiate")) return route.fulfill({ status: 503 });
    await route.fulfill({ contentType: "application/json", body: JSON.stringify(body) });
  });
}

async function expectIndicatorAligned(page: Page) {
  const nav = page.locator("[data-employee-nav]").filter({ visible: true });
  await expect(nav.locator('[aria-current="page"]')).toBeVisible();
  await expect.poll(() => nav.evaluate(element => {
    const tab = element.querySelector('[aria-current="page"]')!.getBoundingClientRect();
    const pill = element.querySelector("[data-employee-nav-indicator]")!.getBoundingClientRect();
    return Math.max(Math.abs(tab.x - pill.x), Math.abs(tab.y - pill.y), Math.abs(tab.width - pill.width), Math.abs(tab.height - pill.height));
  })).toBeLessThanOrEqual(1);
}

async function expectMotionSettled(page: Page) {
  await expect.poll(() => page.locator("[data-employee-motion]").evaluateAll(elements =>
    elements.every(element => !(element as HTMLElement).style.transform && !(element as HTMLElement).style.opacity),
  )).toBe(true);
}

for (const reducedMotion of ["no-preference", "reduce"] as const) {
  test(`employee navigation follows routes and responsive layout: ${reducedMotion}`, async ({ page }) => {
    test.setTimeout(60_000);
    await page.emulateMedia({ reducedMotion });
    const errors: string[] = [];
    page.on("pageerror", error => errors.push(error.message));
    await employeeFixture(page);
    await page.goto("/");
    for (const width of [320, 768, 1024, 1440]) {
      await page.setViewportSize({ width, height: 900 });
      for (const path of ["/availability", "/schedule", "/swap", "/profile", "/"]) {
        const nav = page.locator("[data-employee-nav]").filter({ visible: true });
        await nav.locator(`a[href="${path}"]`).click();
        await expect(page).toHaveURL(new RegExp(`${path === "/" ? "/" : path}$`));
        await expectIndicatorAligned(page);
        await expect(page.locator("[data-employee-motion]").first()).toBeVisible();
        await expectMotionSettled(page);
        await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(width);
      }
    }
    await page.setViewportSize({ width: 390, height: 844 });
    await page.getByRole("button", { name: "Collapse navigation", exact: true }).click();
    await expect(page.getByRole("button", { name: "Open navigation", exact: true })).toBeFocused();
    await page.keyboard.press("Tab");
    await expect(page.locator('[data-employee-nav][inert] a:focus')).toHaveCount(0);
    await page.getByRole("button", { name: "Open navigation", exact: true }).click();
    await expect(page.locator("[data-employee-nav]").filter({ visible: true }).locator('[aria-current="page"]')).toBeFocused();
    await expectIndicatorAligned(page);
    // Consecutive client-side navigation must end with the pill on the latest tab.
    await page.locator("[data-employee-nav]").filter({ visible: true }).evaluate(nav => {
      for (const path of ["/schedule", "/profile", "/availability"]) {
        (nav.querySelector(`a[href="${path}"]`) as HTMLAnchorElement).click();
      }
    });
    await expect(page).toHaveURL(/\/availability$/);
    await expectIndicatorAligned(page);
    await expectMotionSettled(page);
    await page.screenshot({ path: test.info().outputPath("employee-availability.png") });
    expect(errors).toEqual([]);
  });

  test(`employee selections and dialogs keep focus and settle: ${reducedMotion}`, async ({ page }) => {
    await page.emulateMedia({ reducedMotion });
    const errors: string[] = [];
    page.on("pageerror", error => errors.push(error.message));
    await employeeFixture(page);
    await page.goto("/availability");
    const secondWindow = page.getByRole("button", { name: /Window 6/ });
    await secondWindow.focus();
    await page.keyboard.press("Enter");
    await expect(secondWindow).toBeFocused();
    await expect(page.locator('[data-motion-key="6"]')).toContainText("Window 6");
    await expectMotionSettled(page);
    const day = page.locator('[class*="dayButton_"]').first();
    await day.click();
    await expect(page.getByRole("dialog")).toBeVisible();
    await page.getByRole("dialog").getByRole("button", { name: "Close", exact: true }).click();
    await expect(page.getByRole("dialog")).toHaveCount(0);
    await expect(day).toBeEnabled();

    await page.locator("[data-employee-nav]").filter({ visible: true }).locator('a[href="/schedule"]').click();
    await page.getByRole("button", { name: "Show daily schedule view" }).click();
    const scheduleDay = page.getByRole("tablist", { name: "Schedule days" }).getByRole("tab").first();
    await scheduleDay.focus();
    await page.keyboard.press("Enter");
    await expect(scheduleDay).toBeFocused();
    await expectMotionSettled(page);

    await page.locator("[data-employee-nav]").filter({ visible: true }).locator('a[href="/swap"]').click();
    const offer = page.locator('article[data-expanded="false"]').first();
    await offer.focus();
    await page.keyboard.press("Enter");
    await expect(page.locator('article[data-expanded="true"]')).toBeFocused();
    await expectMotionSettled(page);
    await page.getByRole("button", { name: "Create offer", exact: true }).click();
    const creation = page.getByRole("dialog", { name: "Create a swap offer" });
    await expect(creation).toBeVisible();
    await expect.poll(() => creation.evaluate(element => getComputedStyle(element).translate)).toBe("none");
    await expect(creation.getByRole("button", { name: "Close", exact: true })).toBeInViewport();
    await page.keyboard.press("Escape");
    await expect(creation).not.toBeVisible();
    await expect(page.getByRole("button", { name: "Create offer", exact: true })).toBeFocused();
    await page.getByRole("button", { name: "Swap history", exact: true }).click();
    await expect(page.getByRole("dialog", { name: "Recent swap activity" })).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(page.getByRole("dialog", { name: "Recent swap activity" })).not.toBeVisible();
    expect(errors).toEqual([]);
  });
}
