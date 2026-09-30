import { expect, test, type Page } from "@playwright/test";

// Windows WebKit otherwise disables the compositor that runs Safari's native animations.
test.use({ launchOptions: process.platform === "win32" ? { ignoreDefaultArgs: ["--disable-accelerated-compositing"] } : {} });

async function employeeFixture(page: Page, employeeCount = 1) {
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
    elements.every(element => !(element as HTMLElement).style.transform && !(element as HTMLElement).style.opacity && element.getAnimations().length === 0),
  )).toBe(true);
}

for (const reducedMotion of ["no-preference", "reduce"] as const) {
  test(`schedule view toggle pulses blue and preserves focus: ${reducedMotion}`, async ({ page }) => {
    const errors: string[] = [];
    page.on("pageerror", error => errors.push(error.message));
    await page.emulateMedia({ reducedMotion });
    await employeeFixture(page);
    await page.goto("/schedule");
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
    const pulse = await dailyToggle.evaluate(button => {
      const animation = button.getAnimations().find(animation =>
        (animation as CSSAnimation).animationName?.includes("schedule-toggle-pulse"),
      )!;
      const duration = Number(animation.effect!.getTiming().duration);
      animation.pause();
      animation.currentTime = 0;
      const compressed = new DOMMatrixReadOnly(getComputedStyle(button).transform);
      animation.currentTime = duration * 0.45;
      const highlighted = getComputedStyle(button);
      const expanded = new DOMMatrixReadOnly(highlighted.transform);
      const result = {
        duration,
        compressed: Math.hypot(compressed.m11, compressed.m12),
        expanded: Math.hypot(expanded.m11, expanded.m12),
        blue: highlighted.backgroundColor,
        viewAnimation: getComputedStyle(button.closest("section")!).animationName,
      };
      animation.play();
      return result;
    });
    expect(pulse.duration).toBeGreaterThanOrEqual(600);
    expect(pulse.compressed).toBeLessThan(0.9);
    expect(pulse.expanded).toBeGreaterThan(1.02);
    expect(pulse.blue).toMatch(/^rgb\((?:[0-9]|[1-9][0-9]), /);
    expect(pulse.viewAnimation).toContain("schedule-view-enter");
    await expect.poll(() => dailyToggle.evaluate(button => button.getAnimations().length)).toBe(0);
    // Switching again must restart the pulse and keep keyboard activation on the new icon.
    await page.keyboard.press("Enter");
    await expect(matrixToggle).toHaveAttribute("aria-pressed", "false");
    await expect(matrixToggle).toBeFocused();
    await expect(page.getByRole("tablist", { name: "Schedule days" })).toHaveCount(0);
    expect(await matrixToggle.evaluate(button => button.getAnimations().some(animation =>
      (animation as CSSAnimation).animationName?.includes("schedule-toggle-pulse"),
    ))).toBe(true);
    await page.keyboard.press("Enter");
    await expect(dailyToggle).toBeFocused();
    await expect.poll(() => dailyToggle.evaluate(button => button.getAnimations().length)).toBe(0);
    await expectMotionSettled(page);
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth)).toBeLessThanOrEqual(1);
    expect(errors).toEqual([]);
  });

  test(`schedule enters from above and cards react to repeated clicks: ${reducedMotion}`, async ({ page }) => {
    const errors: string[] = [];
    page.on("pageerror", error => errors.push(error.message));
    await page.emulateMedia({ reducedMotion });
    await employeeFixture(page, 40);
    await page.goto("/availability");
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
    expect(distinctPositions, "Hero must move continuously, rather than in a few JS-driven steps").toBeGreaterThan(12);
    expect(longestHold, "Hero position must not freeze during its entrance").toBeLessThan(100);
    expect(entrance.some(sample => sample.y < -4)).toBe(true);
    expect(entrance.every(sample => sample.scale === 1 && sample.opacity === "1")).toBe(true);
    for (let index = 1; index < entrance.length; index++) {
      expect(entrance[index].y).toBeGreaterThanOrEqual(entrance[index - 1].y);
    }
    await expectMotionSettled(page);
    const hero = page.locator('[data-employee-motion="from-top"]');
    expect(await hero.evaluate(element => getComputedStyle(element).animationName)).toContain("hero-enter");
    expect(await hero.evaluate(element => element.getAttribute("style"))).toBeNull();
    const card = page.getByRole("button", { name: /September backup/ });
    await expect(card).toBeVisible();
    expect(await card.evaluate(button => getComputedStyle(button).animationName)).toBe("none");
    await card.focus();
    await page.keyboard.press("Enter");
    await expect(card).toHaveAttribute("aria-pressed", "true");
    await expect(card).toBeFocused();
    const clickMotion = await card.evaluate(button => {
      (button as HTMLButtonElement).click();
      (button as HTMLButtonElement).click();
      return {
        card: button.getAnimations().filter(animation => animation.id === "schedule-card-press").map(animation => animation.effect!.getTiming().duration),
        badge: button.querySelector('[class*="scheduleCardDate_"]')!.getAnimations().filter(animation => animation.id === "schedule-card-press").length,
      };
    });
    expect(clickMotion.card).toEqual([520]);
    expect(clickMotion.badge).toBe(1);
    await expect.poll(() => card.evaluate(button => button.getAnimations().filter(animation => animation.id === "schedule-card-press").length)).toBe(0);
    await expect(card).toBeFocused();
    await expect(page.locator('[class*="openScheduleTitleBlock_"]')).toContainText("September backup");
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth)).toBeLessThanOrEqual(1);
    expect(errors).toEqual([]);
  });

  test(`employee motion remains visible after 200ms: ${reducedMotion}`, async ({ page }) => {
    await page.emulateMedia({ reducedMotion });
    await employeeFixture(page);
    await page.goto("/availability");
    const secondWindow = page.getByRole("button", { name: /Window 6/ });
    await expect(secondWindow).toBeVisible();
    await expectMotionSettled(page);
    const samples = await secondWindow.evaluate(async button => {
      (button as HTMLButtonElement).click();
      const frames: { elapsed: number; opacity: number; y: number }[] = [];
      const started = performance.now();
      await new Promise<void>(resolve => {
        const sample = () => {
          const elapsed = performance.now() - started;
          const editor = document.querySelector<HTMLElement>('[data-employee-motion][data-motion-key="6"]');
          if (editor?.style.transform) {
            const style = getComputedStyle(editor);
            frames.push({ elapsed, opacity: Number(style.opacity), y: new DOMMatrixReadOnly(style.transform).m42 });
          }
          if (elapsed >= 450) resolve();
          else setTimeout(sample, 20);
        };
        setTimeout(sample, 20);
      });
      return frames;
    });
    expect(samples.some(frame => frame.elapsed >= 200 && frame.elapsed <= 350 && frame.opacity < 0.95 && frame.y > 3), JSON.stringify(samples.filter((_, index) => index % 5 === 0))).toBe(true);
    await expectMotionSettled(page);
    const listMotion = await page.locator('[data-motion-list] > button').first().evaluate(element => {
      const style = getComputedStyle(element);
      return { name: style.animationName, duration: parseFloat(style.animationDuration) };
    });
    expect(listMotion.name).toBe("gf3-employee-enter");
    expect(listMotion.duration).toBeGreaterThanOrEqual(0.6);
    const nav = page.locator("[data-employee-nav]").filter({ visible: true });
    const movingIndicator = await nav.locator('a[href="/profile"]').evaluate(async link => {
      (link as HTMLAnchorElement).click();
      await new Promise(resolve => setTimeout(resolve, 240));
      const target = link.getBoundingClientRect();
      const pill = link.parentElement!.querySelector("[data-employee-nav-indicator]")!.getBoundingClientRect();
      return Math.abs(target.x - pill.x);
    });
    expect(movingIndicator).toBeGreaterThan(3);
    await expectIndicatorAligned(page);
    await expectMotionSettled(page);
  });

  test(`employee navigation follows routes and responsive layout: ${reducedMotion}`, async ({ page }) => {
    test.setTimeout(60_000);
    await page.emulateMedia({ reducedMotion });
    const errors: string[] = [];
    page.on("pageerror", error => errors.push(error.message));
    await employeeFixture(page);
    await page.goto("/");
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
        await expect(page.locator("[data-employee-motion]").first()).toBeVisible();
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
