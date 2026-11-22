import { expect, test, type Page } from "@playwright/test";

const apiUrl = /^https?:\/\/[^/]+\/api\//;

async function mockEmployee(page: Page, mode: "browser" | "media" | "ios" = "browser") {
  const launches: string[] = [];
  await page.addInitScript(displayMode => {
    localStorage.setItem("gf3.auth.access-token", "pwa-test-only");
    if (displayMode === "ios") Object.defineProperty(navigator, "standalone", { value: true });
    if (displayMode === "media") {
      const matchMedia = window.matchMedia.bind(window);
      window.matchMedia = query => query === "(display-mode: standalone)"
        ? Object.assign(new EventTarget(), { media: query, matches: true }) as MediaQueryList
        : matchMedia(query);
    }
  }, mode);
  await page.route(apiUrl, async route => {
    const path = new URL(route.request().url()).pathname;
    if (path === "/api/employee-launches") {
      launches.push(route.request().postDataJSON().mode);
      return route.fulfill({ status: 204 });
    }
    if (path.endsWith("/negotiate")) return route.fulfill({ status: 503 });
    const body = path === "/api/auth/session" ? { role: "employee", employeeId: 1, userName: "worker", displayName: "Test Worker" }
      : path === "/api/account-language" ? { language: "en" } : [];
    await route.fulfill({ json: body });
  });
  return launches;
}

test("pwa install banner links to a blank employee page and records only document launches", async ({ page }) => {
  const launches = await mockEmployee(page);
  await page.goto("/swap");
  const banner = page.getByRole("link", { name: "Download" });
  await expect(banner).toBeVisible();
  await expect.poll(() => launches).toEqual(["browser"]);
  await banner.click();
  await expect(page).toHaveURL(/\/install$/);
  await expect(page.locator("[data-employee-content] section")).toBeEmpty();
  await expect(banner).toHaveCount(0);
  expect(launches).toEqual(["browser"]);
  await page.reload();
  await expect(page.locator("[data-employee-content] section")).toBeEmpty();
  await expect.poll(() => launches).toEqual(["browser", "browser"]);
});

test("pwa cold launch draws before React and exits after the employee route is ready", async ({ page, browserName }) => {
  const launches = await mockEmployee(page, browserName === "webkit" ? "ios" : "media");
  await page.emulateMedia({ reducedMotion: browserName === "webkit" ? "reduce" : "no-preference" });
  let release!: () => void;
  const reactAllowed = new Promise<void>(resolve => { release = resolve; });
  await page.route(/\/(?:src\/main\.tsx|assets\/app-[^/]+\.js)(?:\?|$)/, async route => {
    await reactAllowed;
    await route.continue();
  });
  try {
    await page.goto("/install", { waitUntil: "commit" });
    const splash = page.locator("#gf3-startup-splash");
    await expect(splash).toBeVisible();
    await expect(splash).toHaveCSS("background-color", "rgb(243, 244, 246)");
    await expect(splash).toHaveCSS("pointer-events", "none");
    await expect(page.locator("#root")).toBeEmpty();
    release();
    await expect(page.locator("[data-employee-content] section")).toBeEmpty();
    await expect(splash).toHaveCount(0);
    await page.goto("/swap");
    await expect(page.locator("[data-employee-content]")).toBeVisible();
    await expect(page.getByRole("link", { name: "Download" })).toHaveCount(0);
    await expect(splash).toHaveCount(0);
    await expect.poll(() => launches).toEqual(["pwa", "pwa"]);
  } finally { release(); }
});

test("standalone pwa login redirect still finishes the startup splash", async ({ page }) => {
  await page.addInitScript(() => Object.defineProperty(navigator, "standalone", { value: true }));
  await page.route(apiUrl, route => route.fulfill({ status: 401 }));
  await page.goto("/install");
  await expect(page).toHaveURL(/\/login$/);
  await expect(page.locator("#gf3-startup-splash")).toHaveCount(0);
  await expect(page.getByRole("link", { name: /Download|Pobierz/ })).toHaveCount(0);
});
