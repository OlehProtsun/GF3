import { expect, test, type Page } from "@playwright/test";

async function fixture(page: Page, { authenticated = true, history = false, count = 25, longName = false } = {}) {
  if (authenticated) await page.addInitScript(() => localStorage.setItem("gf3.auth.access-token", "deep-link-test"));
  await page.addInitScript(() => {
    Object.defineProperty(navigator, "clipboard", { configurable: true, value: { writeText: (text: string) => {
      (window as unknown as { copiedSwapLink: string }).copiedSwapLink = text; return Promise.resolve();
    } } });
  });
  const session = { role: "employee", employeeId: 2, userName: "target", displayName: "Target Worker" };
  await page.route("**/*", async route => {
    const path = new URL(route.request().url()).pathname;
    if (!path.startsWith("/api/")) return route.continue();
    let body: unknown = [];
    if (path === "/api/auth/session") body = session;
    else if (path === "/api/auth/login") body = { session, accessToken: "deep-link-test" };
    else if (path === "/api/account-language") body = { language: "en" };
    else if (path === "/api/employee-ui-state") body = { scheduleColumnOrders: {}, pinnedSwapIds: [], readNotificationIds: [] };
    else if (path === "/api/employee-shift-swaps") body = Array.from({ length: count }, (_, index) => ({
      id: index + 1, scheduleId: 10, scheduleSlotId: index + 100, scheduleName: longName ? `Offer ${index + 1} — bardzo długa nazwa grafiku i zmiany pracownika` : `Offer ${index + 1}`, containerName: "Main", shopName: "Central",
      year: 2026, month: 10, dayOfMonth: 19, fromTime: "08:00", toTime: "16:00", fromEmployeeId: 1, fromEmployeeName: "Owner Worker",
      visibility: "public", status: history ? "accepted" : "open", createdAtUtc: new Date().toISOString(), acceptedAtUtc: history ? new Date().toISOString() : null,
      shiftHours: 8, isManagerCreated: index === count - 1, manualColumnId: index === count - 1 ? 1 : null, manualColumnName: "Open shift",
      isCreatedByCurrentEmployee: false, isScheduleLocked: false, canAccept: !history, canCancel: false,
    }));
    else if (path === "/api/employee-push/config") body = { enabled: false, publicKey: null };
    else if (path.includes("negotiate")) return route.fulfill({ status: 503 });
    return route.fulfill({ contentType: "application/json", body: JSON.stringify(body) });
  });
}

for (const count of [1, 25]) {
  test(`thin rounded highlight and matching copy/pin actions with ${count} long-name cards`, async ({ page }) => {
    await fixture(page, { count, longName: true }); await page.goto(`/swap?swapId=${count}`);
    const card = page.locator(`[data-swap-id="${count}"]`);
    await expect(card.locator("button").first()).toBeFocused();
    await expect(card).toHaveCSS("border-top-color", "rgb(156, 187, 255)");
    await expect(page.locator('[data-employee-content] [class*="swapPage"]').first()).toHaveCSS("opacity", "1");
    const appearance = await card.evaluate(element => {
      const style = getComputedStyle(element), focusStyle = getComputedStyle(element.querySelector("button")!);
      const actions = [...element.querySelectorAll(':scope > button[aria-pressed], :scope > button[aria-label^="Copy swap link"]')].map(button => {
        const rect = button.getBoundingClientRect(), css = getComputedStyle(button);
        return { x: rect.x, width: rect.width, height: rect.height, radius: css.borderRadius, border: css.border, background: css.backgroundColor };
      });
      const indicator = element.querySelector('[role="img"]')!, indicatorRect = indicator.getBoundingClientRect();
      const title = indicator.previousElementSibling!.getBoundingClientRect();
      const list = element.parentElement!, a = element.getBoundingClientRect(), b = list.getBoundingClientRect();
      return { border: style.borderTopWidth, color: style.borderTopColor, outline: style.outlineStyle, radius: style.borderRadius,
        shadow: style.boxShadow, focusOutline: focusStyle.outlineStyle, actions, badgeClear: title.right <= indicatorRect.left,
        inside: a.left >= b.left && a.right <= b.right && a.top >= b.top && a.bottom <= b.bottom, height: element.clientHeight };
    });
    expect(parseFloat(appearance.border)).toBeLessThanOrEqual(1.5);
    expect(appearance.color).toBe("rgb(156, 187, 255)"); expect(appearance.outline).toBe("none"); expect(appearance.focusOutline).toBe("none");
    expect(appearance.shadow).toContain("inset"); expect(parseFloat(appearance.radius)).toBeGreaterThan(0);
    const [pin, copy] = appearance.actions;
    expect(pin.width).toBeCloseTo(copy.width, 2); expect(pin.height).toBeCloseTo(copy.height, 2); expect(pin.x).toBeCloseTo(copy.x, 2);
    expect(pin.border).toBe(copy.border); expect(pin.background).toBe(copy.background); expect(pin.radius).toBe("50%"); expect(copy.radius).toBe("50%");
    expect(appearance.inside).toBe(true); expect(appearance.badgeClear).toBe(true);
    await expect(card.getByRole("img", { name: "Manager-posted shift" })).toHaveText("🔥");
    await page.screenshot({ path: test.info().outputPath(`swap-${count}-cards.png`) });
    await page.waitForTimeout(3600);
    expect(await card.evaluate(element => element.clientHeight)).toBe(appearance.height);
    await expect(card).not.toHaveCSS("border-top-color", "rgb(156, 187, 255)");
    await card.getByRole("button", { name: /^Copy swap link/ }).click();
    await expect(card.getByRole("status")).toHaveText("Link copied");
    expect(await page.evaluate(() => (window as unknown as { copiedSwapLink: string }).copiedSwapLink)).toBe(new URL(`/swap?swapId=${count}`, page.url()).href);
  });
}

for (const history of [false, true]) {
  test(`direct link scrolls and focuses the ${history ? "History" : "open"} card`, async ({ page }) => {
    await fixture(page, { history }); await page.goto("/swap?swapId=25");
    const card = page.locator('[data-swap-id="25"]');
    await expect(card).toBeVisible();
    await expect(card.getByRole("button", { name: "View offer details for Offer 25", exact: true })).toBeFocused();
    await expect(card.getByRole("img", { name: "Manager-posted shift" })).toHaveCount(1);
    const metrics = await card.evaluate(element => {
      const scroller = element.closest('[data-swap-offers-scroll], [data-swap-history-scroll]')!;
      const a = element.getBoundingClientRect(), b = scroller.getBoundingClientRect();
      return { inside: a.top >= b.top && a.bottom <= b.bottom, scroll: scroller.scrollTop, bodyScroll: window.scrollY };
    });
    expect(metrics.inside).toBe(true); expect(metrics.scroll).toBeGreaterThan(0); expect(metrics.bodyScroll).toBe(0);
    await page.getByRole("button", { name: "View offer details for Offer 25", exact: true }).click();
    await expect(page.locator("dialog:modal")).toHaveCount(1);
    await expect(page.getByRole("dialog", { name: "Offer details" })).toBeVisible();
  });
}

test("logged-out direct link returns to the card after login", async ({ page }) => {
  await fixture(page, { authenticated: false }); await page.goto("/swap?swapId=25");
  await expect(page).toHaveURL(/\/login\?returnTo=%2Fswap%3FswapId%3D25$/);
  await page.getByLabel("Username", { exact: true }).fill("target");
  await page.getByLabel("Password", { exact: true }).fill("123456");
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
  await expect(page).toHaveURL(/\/swap\?swapId=25$/);
  await expect(page.getByRole("button", { name: "View offer details for Offer 25", exact: true })).toBeFocused();
});

test("missing ID gives a generic unavailable message", async ({ page }) => {
  await fixture(page); await page.goto("/swap?swapId=999");
  await expect(page.getByText("This swap is unavailable or you do not have access.")).toBeVisible();
});

test("root push worker activates without creating an offline cache", async ({ page, browserName }) => {
  test.skip(browserName !== "chromium", "Service worker lifecycle is verified in Chromium; real iOS delivery requires a device.");
  await fixture(page); await page.goto("/swap");
  const result = await page.evaluate(async () => {
    await navigator.serviceWorker.register("/sw.js", { scope: "/" });
    const registration = await navigator.serviceWorker.ready;
    return { scope: new URL(registration.scope).pathname, script: new URL(registration.active!.scriptURL).pathname, caches: await caches.keys() };
  });
  expect(result).toEqual({ scope: "/", script: "/sw.js", caches: [] });
});
