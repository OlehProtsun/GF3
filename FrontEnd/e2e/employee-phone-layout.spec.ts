import { expect, test, type Page } from "@playwright/test";

test.use({ timezoneId: "Europe/Warsaw" });
test.describe.configure({ mode: "parallel" });

// Match the authenticated API interception used by schedule-hero and swap-restoration.
async function fixture(page: Page, role = "employee") {
  await page.addInitScript(() => localStorage.setItem("gf3.auth.access-token", "phone-layout-fixture"));
  await page.route("**/*", async route => {
    const path = new URL(route.request().url()).pathname;
    if (!path.startsWith("/api/")) return route.continue();
    let body: unknown = [];
    if (path === "/api/auth/session") body = { role, employeeId: role === "employee" ? 12 : null, managerId: role === "manager" ? 1 : null, userName: "worker", displayName: "Worker with a long account name", isSystemManager: true };
    else if (path === "/api/account-language") body = { language: "en" };
    else if (path === "/api/employee-profile/me") body = { employeeId: 12, username: "worker", displayName: "Worker with a long account name", recoveryEmail: "worker@example.com" };
    else if (path === "/api/employee-ui-state") body = { scheduleColumnOrders: {}, readNotificationIds: [], pinnedSwapIds: [] };
    else if (path.includes("notepad")) body = { notes: [], state: { isExpanded: false, isPinned: false, height: 700 } };
    else if (path === "/api/employee-availability") body = [{ id: 5, name: "Availability", year: 2026, month: 9, canSubmit: true, isEditLocked: false, visibleFromUtc: "2026-09-01T08:00:00Z", visibleToUtc: "2026-10-01T18:00:00Z", slots: [] }];
    else if (path === "/api/employee-schedules") body = [{
      id: 10, containerId: 2, containerName: "Team", shopId: 4, shopName: "Central", name: "September", year: 2026, month: 9, publicationStatus: "public", allowSwap: true, publishedAtUtc: new Date().toISOString(),
      employees: [{ id: 1, employeeId: 12, firstName: "Test", lastName: "Worker", displayName: "Test Worker", minHoursMonth: 0, displayOrder: 1 }],
      slots: [{ id: 100, dayOfMonth: 1, slotNo: 1, employeeId: 12, fromTime: "08:00", toTime: "16:00", status: "ASSIGNED" }],
    }];
    else if (path === "/api/employee-shift-swaps") body = Array.from({ length: 12 }, (_, index) => ({
      id: index + 1, scheduleId: 10, scheduleSlotId: 100, scheduleName: `Offer ${index + 1}`, containerName: "Team", shopName: "Central", year: 2026, month: 9, dayOfMonth: index + 1, fromTime: "08:00", toTime: "16:00", fromEmployeeId: 2, fromEmployeeName: "Other Worker", visibility: "public", status: "open", createdAtUtc: new Date().toISOString(), shiftHours: 8, canAccept: true, canCancel: false, isCreatedByCurrentEmployee: false,
    }));
    else if (path.includes("negotiate")) return route.fulfill({ status: 503 });
    await route.fulfill({ contentType: "application/json", body: JSON.stringify(body) });
  });
}

const routes = ["/", "/notifications", "/availability", "/schedule", "/swap", "/profile"];
const shell = (page: Page) => page.locator("header:has(> [data-employee-nav])").locator("..");
const mobileNav = (page: Page) => page.locator('[data-employee-nav]').last();

for (const [width, height] of [[320, 640], [375, 812], [390, 844], [430, 932]]) {
  test(`mobile overscroll background ${width}`, async ({ page }) => {
    await page.setViewportSize({ width, height });
    await fixture(page);
    await page.goto("/schedule");
    const hero = page.locator('[class*="hero_"]').first();
    await expect(hero).toBeVisible();
    await expect.poll(() => page.evaluate(() => getComputedStyle(document.documentElement, "::before").position)).toBe("fixed");
    await expect.poll(() => hero.evaluate(element => getComputedStyle(element, "::before").content)).toBe('""');
    const continuation = await hero.evaluate(element => {
      const style = getComputedStyle(element);
      const extension = getComputedStyle(element, "::before");
      return {
        background: style.backgroundImage,
        extensionBackground: extension.backgroundImage,
        height: element.getBoundingClientRect().height,
        extensionHeight: extension.height,
        transform: extension.transform,
        overflowX: getComputedStyle(document.body).overflowX,
        overflowY: getComputedStyle(document.body).overflowY,
        scrollHeight: document.documentElement.scrollHeight,
      };
    });
    expect(continuation.extensionBackground).toBe(continuation.background);
    expect(Math.abs(parseFloat(continuation.extensionHeight) - continuation.height)).toBeLessThan(1);
    expect(continuation.transform).toBe("matrix(1, 0, 0, -1, 0, 0)");
    expect(continuation.overflowX).toBe("clip");
    expect(continuation.overflowY).toBe("visible");
    const hideExtension = await page.addStyleTag({ content: '[class*="hero_"]::before { content: none !important; }' });
    expect(await page.evaluate(() => document.documentElement.scrollHeight)).toBe(continuation.scrollHeight);
    await hideExtension.evaluate(element => element.remove());
    await page.screenshot({ path: test.info().outputPath(`schedule-top-${width}.png`) });
    const backgrounds = await page.evaluate(() => ({
      canvas: getComputedStyle(document.documentElement).backgroundImage,
      bottom: getComputedStyle(document.documentElement).backgroundColor,
      body: getComputedStyle(document.body).backgroundColor,
      overflow: document.documentElement.scrollWidth - innerWidth,
    }));
    expect(backgrounds.canvas).toContain("43, 145, 236");
    expect(backgrounds.bottom).toBe("rgb(243, 244, 246)");
    expect(backgrounds.body).toBe(backgrounds.bottom);
    expect(backgrounds.overflow).toBeLessThanOrEqual(1);
    await page.evaluate(() => window.scrollTo(0, document.documentElement.scrollHeight));
    await expect.poll(() => page.evaluate(() => scrollY)).toBeGreaterThan(0);
    await page.screenshot({ path: test.info().outputPath(`schedule-${width}.png`) });
    await page.goto("/profile");
    await expect.poll(() => page.evaluate(() => getComputedStyle(document.documentElement).backgroundImage)).toBe("none");
    expect(await page.locator('meta[name="theme-color"]').getAttribute("content")).toBe("#f3f4f6");
  });
}

for (const [width, height] of [[320, 640], [375, 812], [390, 844], [430, 932], [390, 480]]) {
  test(`adjust visible viewport ${width}x${height}`, async ({ page }) => {
    await page.emulateMedia({ reducedMotion: "reduce" });
    await page.setViewportSize({ width, height });
    await fixture(page);
    await page.goto("/schedule");
    const adjust = page.getByRole("button", { name: "Request a shift correction", exact: true });
    await expect(adjust).toBeVisible();
    for (const fraction of [0, .5, 1, .25, .25]) {
      await page.evaluate(value => window.scrollTo(0, value * (document.documentElement.scrollHeight - innerHeight)), fraction);
      const scroll = await page.evaluate(() => scrollY);
      await adjust.evaluate(element => (element as HTMLButtonElement).click());
      const modal = page.getByRole("dialog", { name: "Request a shift change" });
      await expect(modal).toBeVisible();
      expect(await modal.evaluate(element => element.parentElement === document.body && element.matches(":modal"))).toBe(true);
      const bounds = await modal.locator(":scope > div").boundingBox();
      const viewport = await page.evaluate(() => ({ top: visualViewport?.offsetTop ?? 0, height: visualViewport?.height ?? innerHeight }));
      expect(bounds!.y).toBeGreaterThanOrEqual(viewport.top);
      expect(bounds!.y + bounds!.height).toBeLessThanOrEqual(viewport.top + viewport.height + 1);
      expect(Math.abs(bounds!.y + bounds!.height / 2 - viewport.top - viewport.height / 2)).toBeLessThan(2);
      await expect(modal.getByRole("button", { name: "Close shift correction dialog" })).toBeInViewport();
      expect(await page.evaluate(() => document.body.style.position)).toBe("fixed");
      await modal.getByRole("button", { name: "Close shift correction dialog" }).click();
      await expect(modal).toHaveCount(0);
      expect(await page.evaluate(() => scrollY)).toBeCloseTo(scroll, 0);
    }
  });
}

async function geometry(page: Page, width: number) {
  await expect(shell(page)).toBeVisible();
  await expect.poll(() => shell(page).evaluate(element => getComputedStyle(element).containerName)).toBe("employee-workspace");
  await expect.poll(async () => {
    const box = (await shell(page).boundingBox())!;
    return Math.abs(box.width - Math.min(width, 430));
  }).toBeLessThanOrEqual(1);
  await expect.poll(async () => {
    const box = (await shell(page).boundingBox())!;
    return Math.abs(box.x + box.width / 2 - width / 2);
  }).toBeLessThanOrEqual(2);
  const main = shell(page).locator(":scope > main");
  await expect.poll(() => main.evaluate(element => {
    const box = element.getBoundingClientRect();
    const parent = element.parentElement!.getBoundingClientRect();
    return box.left >= parent.left - 1 && box.right <= parent.right + 1;
  })).toBe(true);
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth)).toBeLessThanOrEqual(1);
  // Measure visible top-level page content, preserving inner schedule-table scrolling.
  await expect.poll(() => main.locator(":scope > *").evaluateAll(elements => elements.flatMap(element => {
    const box = element.getBoundingClientRect();
    if (!box.width || !box.height || getComputedStyle(element).position === "fixed") return [];
    const parent = element.closest("main")!.parentElement!.getBoundingClientRect();
    return box.left >= parent.left - 1 && box.right <= parent.right + 1 ? [] : [{ className: element.className, left: box.left, right: box.right, shellLeft: parent.left, shellRight: parent.right }];
  }))).toEqual([]);
  await expect(page.locator('[data-employee-nav]').first()).toBeHidden();
  await expect(mobileNav(page)).toBeVisible();
  await expect(mobileNav(page)).not.toHaveAttribute("inert");
  await expect.poll(async () => {
    const box = (await mobileNav(page).boundingBox())!;
    return Math.abs(box.x + box.width / 2 - width / 2);
  }).toBeLessThanOrEqual(2);
}

async function oneColumn(page: Page, selector: string) {
  await expect.poll(() => page.locator(selector).first().evaluate(element => getComputedStyle(element).gridTemplateColumns.split(" ").length)).toBe(1);
}

async function responsiveContent(page: Page, route: string) {
  if (route === "/availability") {
    await expect(page.getByRole("button", { name: /Availability/ }).first()).toBeVisible();
    await oneColumn(page, 'main [class*="shell_"]');
    await expect.poll(() => page.locator('[class*="dayGrid_"]').evaluate(element => getComputedStyle(element).gridTemplateColumns.split(" ").length)).toBe(4);
  } else if (route === "/profile") {
    await expect(page.getByText("Recovery email", { exact: true })).toBeVisible();
    await oneColumn(page, '[class*="contentGrid_"]');
    await page.getByRole("button", { name: "Edit", exact: true }).click();
    await oneColumn(page, '[class*="passwordFormGrid_"]');
  } else if (route === "/" || route === "/notifications") {
    await expect(page.getByRole("tab", { name: /^Inbox/ })).toBeVisible();
    await expect.poll(() => page.locator('[class*="sectionHeader_"]').first().evaluate(element => getComputedStyle(element).display)).toBe("grid");
    await expect(page.locator('[class*="notificationItem_"]').first()).toBeVisible();
    await expect.poll(() => page.locator('[class*="notificationActions_"]').first().evaluate(element => getComputedStyle(element).display)).toBe("grid");
  } else if (route === "/schedule") {
    const hero = page.getByRole("region", { name: "Your upcoming shifts" });
    await expect(hero).toBeVisible();
    await expect(hero).toHaveCSS("border-top-left-radius", "0px");
    await expect(page.locator('[class*="hoursSummaryGridRow_"]').first()).toBeVisible();
    await expect.poll(() => page.locator('[class*="hoursSummaryGridHeader_"]').evaluate(element => getComputedStyle(element).gridTemplateColumns.split(" ").length)).toBe(3);
    if (page.viewportSize()!.width === 360) await expect(hero.locator('[class*="clock_"]')).toHaveCSS("min-height", "33px");
  } else if (route === "/swap") {
    await expect(page.getByRole("button", { name: "Create offer", exact: true })).toBeVisible();
    await expect(page.locator('[class*="swapBody_"]')).toHaveCSS("top", "106px");
  }
}

for (const width of [1280, 1440, 390, 360]) {
  test(`employee routes retain phone geometry at ${width}px`, async ({ page }) => {
    test.setTimeout(60_000);
    await page.setViewportSize({ width, height: 900 });
    await fixture(page);
    for (const route of routes) {
      await page.goto(route);
      await responsiveContent(page, route);
      await geometry(page, width);
    }
  });

  test(`mobile navigation mouse and keyboard flow at ${width}px`, async ({ page }) => {
    await page.setViewportSize({ width, height: 568 });
    await fixture(page);
    await page.goto("/");
    await geometry(page, width);
    const nav = mobileNav(page);
    await nav.locator('a[href="/availability"]').click();
    await expect(page).toHaveURL(/\/availability$/);
    await nav.locator('a[href="/schedule"]').click();
    await expect(page).toHaveURL(/\/schedule$/);
    await expect(nav.getByRole("button", { name: "Collapse navigation" })).toBeInViewport({ ratio: 1 });
    await nav.locator('a[href="/profile"]').focus();
    await page.keyboard.press("Tab");
    const collapse = nav.getByRole("button", { name: "Collapse navigation" });
    await expect(collapse).toBeFocused();
    await page.keyboard.press("Enter");
    await expect(nav).toHaveAttribute("inert", "");
    await expect(nav).toHaveCSS("pointer-events", "none");
    await expect(nav).toHaveCSS("opacity", "0");
    const reopen = page.getByRole("button", { name: "Open navigation" });
    await expect(reopen).toBeFocused();
    await expect(reopen).toBeInViewport({ ratio: 1 });
    await expect.poll(async () => {
      const button = (await reopen.boundingBox())!;
      const column = (await shell(page).boundingBox())!;
      return button.x >= column.x && button.x + button.width <= column.x + column.width;
    }).toBe(true);
    await page.keyboard.press("Enter");
    await expect(nav.locator('[aria-current="page"]')).toBeFocused();
    await expect(nav).not.toHaveAttribute("inert");
    await geometry(page, width);
    await expect.poll(() => nav.evaluate(element => {
      const box = element.getBoundingClientRect();
      return window.innerHeight - box.bottom;
    })).toBeGreaterThanOrEqual(9);
    if (width > 430) {
      expect(await page.evaluate(() => document.elementFromPoint(20, window.innerHeight - 30)?.closest('[data-employee-nav]'))).toBeNull();
    }
  });
}

test("Swap search, filters, independent scroll and dialogs", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 720 });
  await fixture(page);
  await page.goto("/swap");
  await responsiveContent(page, "/swap");
  const search = page.getByRole("searchbox", { name: "Search swaps by giver, receiver, date or schedule" });
  await search.fill("Offer 12");
  await expect(page.locator('[class*="offerCard_"]')).toHaveCount(1);
  await search.fill("");
  await expect(page.locator('[class*="offerCard_"]')).toHaveCount(12);
  const filters = page.getByRole("group", { name: "Filter swap offers" });
  await filters.getByRole("button", { name: "My offers", exact: true }).click();
  await expect(page.locator('[class*="offerCard_"]')).toHaveCount(0);
  await filters.getByRole("button", { name: "All", exact: true }).click();
  const area = page.locator('[data-swap-offers-scroll]');
  await area.evaluate(element => { element.scrollTop = element.scrollHeight; });
  await expect.poll(() => area.evaluate(element => element.scrollTop)).toBeGreaterThan(0);
  await expect(page.locator('[class*="offerCard_"]').last()).toBeInViewport();
  expect(await page.evaluate(() => window.scrollY)).toBe(0);
  await area.evaluate(element => { element.scrollTop = 0; });
  await page.getByRole("button", { name: /^View offer details for/ }).first().click();
  await page.getByRole("dialog", { name: "Offer details" }).getByRole("button", { name: "Accept shift", exact: true }).click();
  const confirmation = page.getByRole("dialog");
  await expect(confirmation).toBeVisible();
  await expect(confirmation.getByText("Accept this shift?", { exact: true })).toBeVisible();
  await page.keyboard.press("Escape");
  await expect(confirmation).toBeHidden();
  await page.getByRole("button", { name: "Create offer", exact: true }).click();
  const creation = page.getByRole("dialog");
  await expect(creation).toBeVisible();
  expect((await creation.boundingBox())!.width).toBeLessThanOrEqual(430);
  await creation.getByRole("button", { name: /September/ }).click();
  const picker = page.getByRole("region", { name: "Select your shift", exact: true });
  await expect(picker).toBeVisible();
  await expect(page.locator("dialog:modal")).toHaveCount(1);
  expect(await picker.locator(':scope > div').evaluate(element => element.getBoundingClientRect().width)).toBeLessThanOrEqual(430);
  await page.keyboard.press("Escape");
  await expect(picker).toBeHidden();
  await page.keyboard.press("Escape");
  await expect(creation).toBeHidden();
  await expect(page.getByRole("button", { name: "Create offer", exact: true })).toBeFocused();
});

for (const width of [320, 360, 390, 430, 431, 768, 1280, 1440, 1920]) {
  test(`responsive boundary and screenshots at ${width}px`, async ({ page }) => {
    test.setTimeout(60_000);
    await fixture(page);
    await page.setViewportSize({ width, height: 844 });
    for (const route of routes) {
      await page.goto(route);
      await responsiveContent(page, route);
      await geometry(page, width);
      await expect.poll(() => page.locator('[data-employee-motion]').evaluateAll(elements => elements.every(element => element.getAnimations().length === 0))).toBe(true);
      await page.screenshot({ path: test.info().outputPath(`employee-${route.slice(1) || "inbox"}-${width}.png`), animations: "disabled" });
    }
  });
}

test("430px phone and desktop shell reference", async ({ page }) => {
  await fixture(page);
  const references: { width: number; padding: string; radius: string; fontSize: string }[] = [];
  for (const width of [430, 1440]) {
    await page.setViewportSize({ width, height: 844 });
    await page.goto("/schedule");
    await responsiveContent(page, "/schedule");
    await geometry(page, width);
    await expect.poll(() => page.locator('[data-employee-motion]').evaluateAll(elements => elements.every(element => element.getAnimations().length === 0))).toBe(true);
    references.push(await page.getByRole("region", { name: "Your upcoming shifts" }).evaluate(element => {
      const style = getComputedStyle(element);
      return { width: element.getBoundingClientRect().width, padding: style.padding, radius: style.borderRadius, fontSize: getComputedStyle(element.querySelector("h1")!).fontSize };
    }));
    await shell(page).screenshot({ path: test.info().outputPath(`schedule-shell-${width}.png`), animations: "disabled" });
  }
  const [phone, desktop] = references;
  // The plan retains 3.6vw phone gutters and uses rounded 15px desktop gutters.
  expect(Math.abs(desktop.width - phone.width)).toBeLessThanOrEqual(1);
  expect({ ...desktop, width: 0 }).toEqual({ ...phone, width: 0 });
});

for (const width of [320, 1440]) {
  test(`loading, empty and error states fit at ${width}px`, async ({ page }) => {
    await fixture(page);
    await page.setViewportSize({ width, height: 844 });
    let releaseLoading!: () => void;
    const loading = new Promise<void>(resolve => { releaseLoading = resolve; });
    let state = "loading";
    await page.route("**/api/employee-schedules", async route => {
      if (state === "loading") await loading;
      await route.fulfill({ status: state === "error" ? 500 : 200, contentType: "application/json", body: JSON.stringify(state === "error" ? { detail: "Fixture schedule failure" } : []) });
    });
    await page.goto("/schedule");
    await expect(page.getByText("Checking public schedules for your account.")).toBeVisible();
    await geometry(page, width);
    state = "empty";
    releaseLoading();
    await expect(page.getByText("No published schedules", { exact: true })).toBeVisible();
    await geometry(page, width);
    state = "error";
    await page.reload();
    await expect(page.getByText("Fixture schedule failure", { exact: true }).first()).toBeVisible();
    await geometry(page, width);
  });
}

test("communication overlay remains viewport-wide and dismissible", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 720 });
  await fixture(page);
  await page.route("**/api/communications/pending", route => route.fulfill({
    contentType: "application/json",
    body: JSON.stringify([{ id: 1, title: "Team update", body: "A manager communication remains readable outside the shell container.", visibleFromUtc: "2026-09-01T00:00:00Z", deadlineAtUtc: "2026-12-01T00:00:00Z", createdAtUtc: "2026-09-01T00:00:00Z", createdByManagerName: "Manager", isActive: true }]),
  }));
  await page.goto("/");
  const dialog = page.getByRole("dialog", { name: "Team update" });
  await expect(dialog).toBeVisible();
  expect(await dialog.evaluate(element => element.parentElement!.getBoundingClientRect().width)).toBe(1440);
  expect(await dialog.evaluate(element => getComputedStyle(element).containerName)).toBe("none");
  await expect(dialog.getByRole("button", { name: "Close communication" })).toBeInViewport({ ratio: 1 });
  await dialog.getByRole("button", { name: "Close communication" }).click();
  await expect(dialog).toBeHidden();
  await geometry(page, 1440);
});

test("manager desktop navigation and public login remain isolated", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await fixture(page, "manager");
  await page.goto("/");
  await expect(page.locator('aside[class*="sidebar_"]')).toBeVisible();
  await expect(page.getByRole("link", { name: "Open manager profile" })).toBeVisible();
  await expect(page.locator('[data-employee-nav]')).toHaveCount(0);
  expect(await page.locator("main").first().evaluate(element => getComputedStyle(element).containerName)).toBe("none");
  await page.evaluate(() => localStorage.removeItem("gf3.auth.access-token"));
  await page.route("**/api/auth/session", route => route.fulfill({ status: 401, contentType: "application/json", body: JSON.stringify({ message: "Unauthenticated" }) }));
  await page.goto("/login");
  await expect(page.getByRole("button", { name: /Sign in|Log in/ }).first()).toBeVisible();
  await expect(page.locator('[data-employee-nav]')).toHaveCount(0);
  expect(await page.evaluate(() => [...document.querySelectorAll("*")].some(element => getComputedStyle(element).containerName === "employee-workspace"))).toBe(false);
});
