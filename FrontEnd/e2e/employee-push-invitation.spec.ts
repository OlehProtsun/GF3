import { expect, test, type Page } from "@playwright/test";

async function fixture(page: Page) {
  const calls = { subscriptions: 0, deletions: 0 };
  await page.addInitScript(() => {
    localStorage.setItem("gf3.auth.access-token", "push-ux-test");
    Object.defineProperty(navigator, "standalone", { configurable: true, value: true });
    Object.defineProperty(window, "PushManager", { configurable: true, value: class {} });
    let permission = "default";
    const state = window as unknown as { pushPermissionRequests: number };
    state.pushPermissionRequests = 0;
    Object.defineProperty(window, "Notification", { configurable: true, value: {
      get permission() { return permission; },
      requestPermission() { state.pushPermissionRequests++; permission = "granted"; return Promise.resolve("granted"); },
    } });
    let subscription: object | null = null;
    const registration = { pushManager: {
      getSubscription: () => Promise.resolve(subscription),
      subscribe: () => {
        subscription = { endpoint: "https://fcm.googleapis.com/test", toJSON: () => ({ endpoint: "https://fcm.googleapis.com/test", keys: { p256dh: "test", auth: "test" } }),
          unsubscribe: () => { subscription = null; return Promise.resolve(true); } };
        return Promise.resolve(subscription);
      },
    } };
    Object.defineProperty(navigator, "serviceWorker", { configurable: true, value: {
      register: () => Promise.resolve(registration), ready: Promise.resolve(registration), getRegistration: () => Promise.resolve(registration),
    } });
  });
  await page.route("**/*", async route => {
    const path = new URL(route.request().url()).pathname;
    if (!path.startsWith("/api/")) return route.continue();
    let body: unknown = [];
    if (path === "/api/auth/session") body = { role: "employee", employeeId: 77, userName: "push", displayName: "Push Worker" };
    else if (path === "/api/account-language") body = { language: "pl" };
    else if (path === "/api/employee-ui-state") body = { scheduleColumnOrders: {}, pinnedSwapIds: [], readNotificationIds: [] };
    else if (path === "/api/employee-push/config") body = { enabled: true, publicKey: "AQID" };
    else if (path === "/api/employee-profile/me") body = { employeeId: 77, username: "push", displayName: "Push Worker", recoveryEmail: "push@example.com", phone: null };
    else if (path === "/api/employee-push/subscriptions") {
      if (route.request().method() === "PUT") calls.subscriptions++; else calls.deletions++;
      return route.fulfill({ status: 204 });
    } else if (path.includes("negotiate")) return route.fulfill({ status: 503 });
    return route.fulfill({ contentType: "application/json", body: JSON.stringify(body) });
  });
  return calls;
}

test("welcome stays centered, never prompts automatically, and respects Not now after reload", async ({ page }) => {
  await fixture(page); await page.goto("/swap");
  const dialog = page.getByRole("dialog", { name: "Włącz powiadomienia" }); await expect(dialog).toBeVisible();
  expect(await page.evaluate(() => (window as unknown as { pushPermissionRequests: number }).pushPermissionRequests)).toBe(0);
  const centered = () => dialog.evaluate(element => {
    const rect = element.getBoundingClientRect(), viewport = window.visualViewport;
    return { deltaX: Math.abs(rect.left + rect.width / 2 - (viewport?.width ?? innerWidth) / 2),
      deltaY: Math.abs(rect.top + rect.height / 2 - (viewport?.offsetTop ?? 0) - (viewport?.height ?? innerHeight) / 2),
      fits: rect.left >= 0 && rect.right <= innerWidth && rect.top >= 0 && rect.bottom <= innerHeight };
  });
  let position = await centered(); expect(position.deltaX).toBeLessThan(2); expect(position.deltaY).toBeLessThan(2); expect(position.fits).toBe(true);
  await page.evaluate(() => { document.body.style.minHeight = "2000px"; window.scrollTo(0, 200); });
  position = await centered(); expect(position.deltaY).toBeLessThan(2);
  await page.screenshot({ path: test.info().outputPath("push-welcome.png") });
  await dialog.getByRole("button", { name: "Nie teraz" }).click(); await expect(dialog).not.toBeVisible();
  await page.reload(); await expect(page.locator("[data-employee-content] h1")).toBeVisible();
  await expect(dialog).not.toBeVisible();
  expect(await page.evaluate(() => Number(localStorage.getItem("gf3.push.invitation.77")) > Date.now())).toBe(true);
  await page.goto("/profile");
  await expect(dialog).not.toBeVisible();
  await page.getByRole("button", { name: "Włącz powiadomienia", exact: true }).click();
  await expect(page.getByText("Włączone na tym urządzeniu", { exact: true })).toBeVisible();
});

test("explicit welcome opt-in updates Profile and per-device Disable still works", async ({ page }) => {
  const calls = await fixture(page); await page.goto("/profile");
  const dialog = page.getByRole("dialog", { name: "Włącz powiadomienia" }); await expect(dialog).toBeVisible();
  expect(calls.subscriptions).toBe(0);
  await dialog.getByRole("button", { name: "Włącz powiadomienia" }).click();
  await expect(dialog).not.toBeVisible();
  await expect(page.getByText("Włączone na tym urządzeniu", { exact: true })).toBeVisible();
  expect(calls.subscriptions).toBe(1);
  expect(await page.evaluate(() => (window as unknown as { pushPermissionRequests: number }).pushPermissionRequests)).toBe(1);
  await page.getByRole("button", { name: "Wyłącz powiadomienia" }).click();
  await expect(page.getByRole("button", { name: "Włącz powiadomienia", exact: true })).toBeVisible();
  expect(calls.deletions).toBe(1);
});
