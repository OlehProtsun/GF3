import { expect, test, type Page } from "@playwright/test";

async function fixture(page: Page) {
  const calls: { method: string; employeeId: number; endpoint: string }[] = [];
  const recipients = new Map<string, number>();
  const session = (employeeId: number) => ({ role: "employee", employeeId, userName: employeeId === 77 ? "push-a" : "push-b", displayName: `Push Worker ${employeeId}` });
  await page.addInitScript(() => {
    if (!sessionStorage.getItem("push-fixture-bootstrapped")) {
      localStorage.setItem("gf3.auth.access-token", "push-a");
      sessionStorage.setItem("push-fixture-bootstrapped", "yes");
    }
    Object.defineProperty(navigator, "standalone", { configurable: true, value: true });
    Object.defineProperty(window, "PushManager", { configurable: true, value: class {} });
    Object.defineProperty(window, "Notification", { configurable: true, value: {
      get permission() { return sessionStorage.getItem("push-fixture-permission") ?? "default"; },
      requestPermission() {
        const count = Number(sessionStorage.getItem("push-fixture-prompts"));
        sessionStorage.setItem("push-fixture-prompts", String(count + 1));
        sessionStorage.setItem("push-fixture-permission", "granted");
        return Promise.resolve("granted");
      },
    } });
    const subscription = (endpoint: string) => ({ endpoint, toJSON: () => ({ endpoint, keys: { p256dh: "test", auth: "test" } }),
      unsubscribe: () => { sessionStorage.removeItem("push-fixture-endpoint"); return Promise.resolve(true); } });
    const registration = { pushManager: {
      getSubscription: () => {
        const endpoint = sessionStorage.getItem("push-fixture-endpoint");
        return Promise.resolve(endpoint ? subscription(endpoint) : null);
      },
      subscribe: () => {
        const version = Number(sessionStorage.getItem("push-fixture-endpoint-version")) + 1;
        const endpoint = `https://fcm.googleapis.com/test-${version}`;
        sessionStorage.setItem("push-fixture-endpoint", endpoint);
        sessionStorage.setItem("push-fixture-endpoint-version", String(version));
        return Promise.resolve(subscription(endpoint));
      },
    } };
    Object.defineProperty(navigator, "serviceWorker", { configurable: true, value: {
      register: () => Promise.resolve(registration), ready: Promise.resolve(registration), getRegistration: () => Promise.resolve(registration),
    } });
  });
  await page.route("**/*", async route => {
    const request = route.request();
    const path = new URL(request.url()).pathname;
    if (!path.startsWith("/api/")) return route.continue();
    const employeeId = request.headers().authorization === "Bearer push-b" ? 88 : 77;
    let body: unknown = [];
    if (path === "/api/auth/session") body = session(employeeId);
    else if (path === "/api/auth/login") {
      const username = request.postDataJSON().username;
      body = { accessToken: username, expiresAtUtc: "", session: session(username === "push-b" ? 88 : 77) };
    } else if (path === "/api/auth/logout") return route.fulfill({ status: 204 });
    else if (path === "/api/account-language") body = { language: "pl" };
    else if (path === "/api/employee-ui-state") body = { scheduleColumnOrders: {}, pinnedSwapIds: [], readNotificationIds: [] };
    else if (path === "/api/employee-push/config") body = { enabled: true, publicKey: Buffer.from([4, ...Array<number>(64).fill(0)]).toString("base64url") };
    else if (path === "/api/employee-profile/me") body = { ...session(employeeId), username: session(employeeId).userName, recoveryEmail: "push@example.com", phone: null };
    else if (path === "/api/employee-push/subscriptions") {
      const method = request.method(), { endpoint } = request.postDataJSON();
      calls.push({ method, employeeId, endpoint });
      if (method === "PUT") recipients.set(endpoint, employeeId);
      else if (recipients.get(endpoint) === employeeId) recipients.delete(endpoint);
      return route.fulfill({ status: 204 });
    } else if (path.includes("negotiate")) return route.fulfill({ status: 503 });
    return route.fulfill({ contentType: "application/json", body: JSON.stringify(body) });
  });
  return { calls, recipients };
}

const promptCount = (page: Page) => page.evaluate(() => Number(sessionStorage.getItem("push-fixture-prompts")));

test("welcome stays centered, never prompts automatically, and respects Not now after reload", async ({ page }) => {
  await fixture(page); await page.goto("/swap");
  const dialog = page.getByRole("dialog", { name: "Włącz powiadomienia" }); await expect(dialog).toBeVisible();
  expect(await promptCount(page)).toBe(0);
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
  await page.setViewportSize({ width: 320, height: 812 });
  await page.goto("/profile");
  await expect(dialog).not.toBeVisible();
  const history = page.getByRole("region", { name: "Historia regulaminów" });
  const locked = history.getByText("Tymczasowo zamknięte");
  await expect(locked).toBeVisible();
  const headingBox = await history.getByRole("heading").boundingBox();
  const lockedBox = await locked.boundingBox();
  expect(lockedBox!.y).toBeGreaterThan(headingBox!.y + headingBox!.height);
  expect(await history.evaluate(element => element.scrollWidth <= element.clientWidth + 1)).toBe(true);
  await history.screenshot({ path: test.info().outputPath("regulation-history-locked-320.png") });
  await page.getByRole("button", { name: "Włącz powiadomienia", exact: true }).click();
  await expect(page.getByText("Włączone na tym urządzeniu", { exact: true })).toBeVisible();
});

test("granted permission follows A, B and A again across signouts without another prompt", async ({ page }) => {
  const { calls, recipients } = await fixture(page); await page.goto("/profile");
  const dialog = page.getByRole("dialog", { name: "Włącz powiadomienia" }); await expect(dialog).toBeVisible();
  expect(calls).toHaveLength(0);
  await dialog.getByRole("button", { name: "Włącz powiadomienia" }).click();
  await expect(dialog).not.toBeVisible();
  const enabled = page.getByText("Włączone na tym urządzeniu", { exact: true });
  await expect(enabled).toBeVisible();
  await expect(page.getByRole("button", { name: "Wyłącz powiadomienia" })).toHaveCount(0);
  expect(calls).toEqual([{ method: "PUT", employeeId: 77, endpoint: "https://fcm.googleapis.com/test-1" }]);
  expect(await promptCount(page)).toBe(1);

  const logout = async () => {
    await page.getByRole("button", { name: "Wyloguj się" }).click();
    await expect(page.locator("#login-username")).toBeVisible();
    expect(await page.evaluate(() => sessionStorage.getItem("push-fixture-endpoint"))).toBeNull();
    expect([...recipients], JSON.stringify(calls)).toEqual([]);
  };
  const login = async (username: string, employeeId: number) => {
    await page.locator("#login-username").fill(username);
    await page.locator("#login-password").fill("123456");
    await page.getByRole("button", { name: "Zaloguj się", exact: true }).click();
    await expect.poll(() => [...recipients.values()]).toEqual([employeeId]);
    await expect.poll(() => page.evaluate(() => Number(localStorage.getItem("gf3.push.employeeId")))).toBe(employeeId);
    const profile = page.getByRole("link", { name: "Profil", exact: true });
    if (!new URL(page.url()).pathname.endsWith("/profile")) await profile.click();
    await expect(enabled).toBeVisible();
    await expect(dialog).not.toBeVisible();
    expect(await promptCount(page)).toBe(1);
  };
  await logout();
  await login("push-b", 88);
  await logout();
  await login("push-a", 77);
  const expectedOperations = [
    ["PUT", 77, "test-1"], ["DELETE", 77, "test-1"], ["PUT", 88, "test-2"],
    ["DELETE", 88, "test-2"], ["PUT", 77, "test-3"],
  ];
  expect(calls.map(({ method, employeeId, endpoint }) => [method, employeeId, endpoint.split("/").at(-1)])).toEqual(expectedOperations);
  await page.reload();
  await expect(enabled).toBeVisible();
  expect(recipients.size).toBe(1);
  expect(await promptCount(page)).toBe(1);
});
