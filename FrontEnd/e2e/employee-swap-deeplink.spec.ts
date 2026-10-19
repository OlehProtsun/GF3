import { expect, test, type Page } from "@playwright/test";

async function fixture(page: Page, { authenticated = true, history = false } = {}) {
  if (authenticated) await page.addInitScript(() => localStorage.setItem("gf3.auth.access-token", "deep-link-test"));
  const session = { role: "employee", employeeId: 2, userName: "target", displayName: "Target Worker" };
  await page.route("**/*", async route => {
    const path = new URL(route.request().url()).pathname;
    if (!path.startsWith("/api/")) return route.continue();
    let body: unknown = [];
    if (path === "/api/auth/session") body = session;
    else if (path === "/api/auth/login") body = { session, accessToken: "deep-link-test" };
    else if (path === "/api/account-language") body = { language: "en" };
    else if (path === "/api/employee-ui-state") body = { scheduleColumnOrders: {}, pinnedSwapIds: [], readNotificationIds: [] };
    else if (path === "/api/employee-shift-swaps") body = Array.from({ length: 25 }, (_, index) => ({
      id: index + 1, scheduleId: 10, scheduleSlotId: index + 100, scheduleName: `Offer ${index + 1}`, containerName: "Main", shopName: "Central",
      year: 2026, month: 10, dayOfMonth: 19, fromTime: "08:00", toTime: "16:00", fromEmployeeId: 1, fromEmployeeName: "Owner Worker",
      visibility: "public", status: history ? "accepted" : "open", createdAtUtc: new Date().toISOString(), acceptedAtUtc: history ? new Date().toISOString() : null,
      shiftHours: 8, isManagerCreated: index === 24, manualColumnId: index === 24 ? 1 : null, manualColumnName: "Open shift",
      isCreatedByCurrentEmployee: false, isScheduleLocked: false, canAccept: !history, canCancel: false,
    }));
    else if (path === "/api/employee-push/config") body = { enabled: false, publicKey: null };
    else if (path.includes("negotiate")) return route.fulfill({ status: 503 });
    return route.fulfill({ contentType: "application/json", body: JSON.stringify(body) });
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
