import { expect, test, type Page } from "@playwright/test";

// Windows WebKit otherwise disables the compositor that runs Safari's native animations.
test.use({ launchOptions: process.platform === "win32" ? { ignoreDefaultArgs: ["--disable-accelerated-compositing"] } : {} });

async function employeeFixture(page: Page, employeeCount = 1) {
  await page.addInitScript(() => {
    localStorage.setItem("gf3.auth.access-token", "employee-motion-fixture");
    sessionStorage.setItem("gf3.pwa.install.dismissed", "1");
  });
  await page.route("**/*", async route => {
    const path = new URL(route.request().url()).pathname;
    if (!path.startsWith("/api/")) return route.continue();
    let body: unknown = [];
    if (path === "/api/auth/session") body = { role: "employee", employeeId: 12, userName: "worker", displayName: "Motion Worker" };
    else if (path === "/api/account-language") body = { language: "en" };
    else if (path === "/api/employee-profile/me") body = { employeeId: 12, username: "worker", displayName: "Motion Worker", recoveryEmail: "worker@example.com" };
    else if (path === "/api/employee-ui-state") body = { scheduleColumnOrders: {}, readNotificationIds: [], pinnedSwapIds: [] };
    else if (path === "/api/employee-ui-state/availability-time-presets") body = { presets: null };
    else if (path === "/api/employee-urlop") body = { shops: [], months: [] };
    else if (path === "/api/employee-availability") body = [5, 6].map(id => ({
      id, name: `Window ${id}`, year: 2026, month: 9, canSubmit: true, isEditLocked: false,
      visibleFromUtc: "2026-09-01T00:00:00Z", visibleToUtc: "2026-10-01T23:59:00Z", slots: [],
      members: [{ employeeId: 12, firstName: "Motion", lastName: "Worker", displayOrder: 1, slots: [] }],
    }));
    else if (path === "/api/employee-schedules") body = [10, 11].map(id => ({
      id, containerId: 2, containerName: "Team", shopId: 4, shopName: "Central",
      name: id === 10 ? "September" : "September backup", year: 2026, month: 9, publicationStatus: "public", allowSwap: true,
      employees: Array.from({ length: employeeCount }, (_, index) => ({ id: index + 1, employeeId: 12 + index, firstName: "Motion", lastName: `Worker ${index + 1}`, displayName: `Motion Worker ${index + 1}`, displayOrder: index + 1 })),
      slots: Array.from({ length: employeeCount }, (_, index) => ({ id: 100 + index, dayOfMonth: 1, slotNo: index + 1, employeeId: 12 + index, fromTime: "08:00", toTime: "16:00", status: "ASSIGNED" })),
    }));
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

async function openEmployeePage(page: Page, path: string) {
  await page.goto(path);
  await expect(page.locator("[data-employee-nav]").first()).toBeAttached();
}

async function expectIndicatorAligned(page: Page) {
  const nav = page.locator("[data-employee-nav]").filter({ visible: true });
  const active = nav.locator('[aria-current="page"]');
  await expect(active).toBeVisible();
  if (await nav.locator("[data-employee-nav-indicator]").count() === 0) {
    await expect(active.locator("span").first()).toBeVisible();
    return;
  }
  await expect.poll(() => nav.evaluate(element => {
    const tab = element.querySelector('[aria-current="page"]')!.getBoundingClientRect();
    const pill = element.querySelector("[data-employee-nav-indicator]")!.getBoundingClientRect();
    return Math.max(Math.abs(tab.x - pill.x), Math.abs(tab.y - pill.y), Math.abs(tab.width - pill.width), Math.abs(tab.height - pill.height));
  })).toBeLessThanOrEqual(1);
}

async function expectMotionSettled(page: Page) {
  await expect.poll(() => page.locator("[data-employee-motion]").evaluateAll(elements =>
    (elements as HTMLElement[]).map(element => ({
      key: element.dataset.motionKey ?? element.dataset.employeeMotion,
      transform: element.style.transform,
      opacity: element.style.opacity,
      running: element.getAnimations().filter(animation => animation.playState === "running").length,
    })).filter(state => state.transform || state.opacity || state.running),
  )).toEqual([]);
}

for (const reducedMotion of ["no-preference", "reduce"] as const) {
  test(`schedule view toggle marks the active view and preserves focus: ${reducedMotion}`, async ({ page }) => {
    const errors: string[] = [];
    page.on("pageerror", error => errors.push(error.message));
    await page.emulateMedia({ reducedMotion });
    await employeeFixture(page);
    await openEmployeePage(page, "/schedule");
    const matrixToggle = page.getByRole("button", { name: "Show daily schedule view" });
    await expect(matrixToggle).toBeVisible();
    await expectMotionSettled(page);
    expect(await matrixToggle.evaluate(button => getComputedStyle(button).animationName)).toBe("none");
    await matrixToggle.focus();
    await page.keyboard.press("Enter");
    const dailyToggle = page.getByRole("button", { name: "Show schedule matrix view" });
    await expect(dailyToggle).toHaveAttribute("aria-pressed", "true");
    await expect(dailyToggle).toBeFocused();
    await expect(page.getByRole("tablist", { name: "Schedule days" })).toBeVisible();
    const stage = page.locator('[data-motion-key$=":daily"]');
    await expect(stage).toBeVisible();
    await expect(dailyToggle).toHaveCSS("background-color", "rgba(191, 219, 254, 0.92)");
    await expect(dailyToggle).toHaveCSS("transition-duration", reducedMotion === "reduce" ? "0s" : "0.16s, 0.16s, 0.16s, 0.16s");
    await page.keyboard.press("Enter");
    await expect(matrixToggle).toHaveAttribute("aria-pressed", "false");
    await expect(matrixToggle).toBeFocused();
    await expect(page.getByRole("tablist", { name: "Schedule days" })).toHaveCount(0);
    await page.keyboard.press("Enter");
    await expect(dailyToggle).toBeFocused();
    await expect(stage).toBeVisible();
    await expectMotionSettled(page);
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth)).toBeLessThanOrEqual(1);
    expect(errors).toEqual([]);
  });

  test(`schedule entrance and keyboard schedule selection: ${reducedMotion}`, async ({ page }) => {
    const errors: string[] = [];
    page.on("pageerror", error => errors.push(error.message));
    await page.emulateMedia({ reducedMotion });
    await employeeFixture(page, 40);
    await openEmployeePage(page, "/availability");
    await expect(page.getByRole("button", { name: /Window 6/ })).toBeVisible();
    await expectMotionSettled(page);
    const nav = page.locator("[data-employee-nav]").filter({ visible: true });
    const entrance = await nav.locator('a[href="/schedule"]').evaluate(async link => {
      (link as HTMLAnchorElement).click();
      const deadline = performance.now() + 2000;
      const samples: { elapsed: number; time: number; y: number; scale: number; opacity: string }[] = [];
      let started: number | undefined;
      await new Promise<void>(resolve => {
        const sample = () => {
          const hero = document.querySelector<HTMLElement>('[data-employee-motion="from-top"]');
          if (hero && getComputedStyle(hero).transform !== "none") {
            started ??= performance.now();
            const matrix = new DOMMatrixReadOnly(getComputedStyle(hero).transform);
            samples.push({ elapsed: performance.now() - started, time: Number(hero.getAnimations()[0]?.currentTime ?? 0), y: matrix.m42, scale: matrix.m11, opacity: getComputedStyle(hero.parentElement!).opacity });
          }
          if (performance.now() > deadline || (started !== undefined && performance.now() - started >= 650)) resolve();
          else setTimeout(sample, 20);
        };
        setTimeout(sample, 20);
      });
      return samples;
    });
    await test.info().attach("hero-frame-samples", { body: JSON.stringify(entrance), contentType: "application/json" });
    const distinctPositions = new Set(entrance.map(sample => sample.y.toFixed(2))).size;
    let lastChange = entrance[0];
    let longestHold = 0;
    for (const sample of entrance) {
      // A pending animation has not reached its first paint yet.
      if (sample.time === 0) { lastChange = sample; continue; }
      if (Math.abs(sample.y - lastChange.y) > 0.01) lastChange = sample;
      else if (sample.y < -1) longestHold = Math.max(longestHold, sample.elapsed - lastChange.elapsed);
    }
    if (reducedMotion === "reduce") {
      expect(entrance).toEqual([]);
    } else {
      expect(distinctPositions, "Hero must move continuously during entry").toBeGreaterThan(12);
      expect(longestHold, "Hero position must not freeze during its entrance").toBeLessThan(100);
      expect(entrance.some(sample => sample.y < -4)).toBe(true);
      expect(entrance.every(sample => sample.scale === 1 && sample.opacity === "1")).toBe(true);
      for (let index = 1; index < entrance.length; index++) {
        expect(entrance[index].y).toBeGreaterThanOrEqual(entrance[index - 1].y);
      }
    }
    await expectMotionSettled(page);
    const hero = page.locator('[data-employee-motion="from-top"]');
    await expect(hero).toHaveCSS("animation-name", reducedMotion === "reduce" ? "none" : /hero-enter/);
    expect(await hero.evaluate(element => element.getAttribute("style"))).toBeNull();
    const scheduleSelector = page.getByRole("button", { name: /^Select schedule\. Current schedule:/ });
    const selectorDialog = page.getByRole("dialog", { name: "Select schedule" });
    for (const name of ["September backup", "September", "September backup"]) {
      await scheduleSelector.press("Enter");
      const choice = selectorDialog.getByRole("button").filter({ has: page.getByText(name, { exact: true }) });
      await choice.press("Enter");
      await expect(selectorDialog).toHaveCount(0);
      await expect(scheduleSelector).toHaveAccessibleName(`Select schedule. Current schedule: ${name}`);
      await expect(scheduleSelector).toBeFocused();
      await expectMotionSettled(page);
    }
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth)).toBeLessThanOrEqual(1);
    expect(errors).toEqual([]);
  });

  test(`availability disclosure and navigation settle after motion: ${reducedMotion}`, async ({ page }) => {
    await page.emulateMedia({ reducedMotion });
    await employeeFixture(page);
    await openEmployeePage(page, "/availability");
    const secondWindow = page.getByRole("button", { name: /Window 6/ });
    const editor = page.locator("#employee-availability-content-6");
    await expect(secondWindow).toHaveAttribute("aria-expanded", "false");
    await expect(editor).toHaveCSS("opacity", "0");
    await expect(editor).toHaveCSS("transition-duration", reducedMotion === "reduce" ? "0s" : "0.18s, 0.18s");
    await secondWindow.press("Enter");
    await expect(secondWindow).toHaveAttribute("aria-expanded", "true");
    await expect(editor).not.toHaveAttribute("inert");
    await expect(editor).toHaveCSS("opacity", "1");
    await expect(editor.getByText("Team availability", { exact: true })).toBeVisible();
    const nav = page.locator("[data-employee-nav]").filter({ visible: true });
    const profile = nav.locator('a[href="/profile"]');
    const beforeWidth = (await profile.boundingBox())!.width;
    await profile.click();
    await expect(page).toHaveURL(/\/profile$/);
    await expectIndicatorAligned(page);
    await expect.poll(async () => (await profile.boundingBox())!.width).toBeGreaterThan(beforeWidth);
    await expectMotionSettled(page);
  });
  test(`employee navigation follows routes and responsive layout: ${reducedMotion}`, async ({ page }) => {
    test.setTimeout(60_000);
    await page.emulateMedia({ reducedMotion });
    const errors: string[] = [];
    page.on("pageerror", error => errors.push(error.message));
    await employeeFixture(page);
    await openEmployeePage(page, "/");
    const sizes = test.info().project.use.isMobile
      ? [{ width: 320, height: 812 }, { width: 375, height: 812 }, { width: 812, height: 375 }]
      : [320, 768, 1024, 1440].map(width => ({ width, height: 900 }));
    for (const size of sizes) {
      await page.setViewportSize(size);
      for (const path of ["/availability", "/schedule", "/swap", "/profile", "/"]) {
        const nav = page.locator("[data-employee-nav]").filter({ visible: true });
        await nav.locator(`a[href="${path}"]`).click();
        await expect(page).toHaveURL(new RegExp(`${path === "/" ? "/" : path}$`));
        await expectIndicatorAligned(page);
        await expect(page.locator("[data-employee-content]")).toContainText(/\S/);
        await expectMotionSettled(page);
        await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(size.width);
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
    await openEmployeePage(page, "/availability");
    const secondWindow = page.getByRole("button", { name: /Window 6/ });
    await secondWindow.focus();
    await page.keyboard.press("Enter");
    await expect(secondWindow).toBeFocused();
    await expect(secondWindow).toHaveAttribute("aria-expanded", "true");
    await expect(page.locator("#employee-availability-content-6").getByText("Team availability", { exact: true })).toBeVisible();
    await expectMotionSettled(page);
    const day = page.locator('#employee-availability-content-6 [data-own-availability-cell="1"]');
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
    const offer = page.getByRole("button", { name: "View offer details for September", exact: true });
    await offer.press("Enter");
    const details = page.getByRole("dialog", { name: "Offer details", exact: true });
    await expect(details).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(details).toHaveCount(0);
    await expect(offer).toBeFocused();
    await expectMotionSettled(page);
    await page.getByRole("button", { name: "Create offer", exact: true }).click();
    const creation = page.getByRole("dialog", { name: "Create a swap offer" });
    await expect(creation).toBeVisible();
    await expect.poll(() => creation.evaluate(element => getComputedStyle(element).transform)).toBe("none");
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
