import { expect, test, type Page } from "@playwright/test";

test.use({ launchOptions: process.platform === "win32" ? { ignoreDefaultArgs: ["--disable-accelerated-compositing"] } : {} });

async function fixture(page: Page, mode = "phone") {
  const attempts: { username: string; password: string }[] = [];
  const errors: string[] = [];
  let authenticated = false;
  page.on("pageerror", error => errors.push(error.message));
  await page.addInitScript(mode => {
    localStorage.setItem("gf3.auth.last-username", "manager");
    localStorage.setItem("gf3.auth.password-mode", mode);
  }, mode);
  const session = { role: "manager", workspaceMode: "choose", managerId: 1, employeeId: null, userName: "manager", displayName: "Manager", isSystemManager: true };
  await page.route("**/api/**", async route => {
    const path = new URL(route.request().url()).pathname;
    if (!path.startsWith("/api/")) return route.continue();
    const json = (body: unknown, status = 200) => route.fulfill({ status, contentType: "application/json", body: JSON.stringify(body) });
    if (path === "/api/auth/session") return json(authenticated ? session : {}, authenticated ? 200 : 401);
    if (path === "/api/auth/login") {
      const input = route.request().postDataJSON();
      attempts.push(input);
      if (input.password !== "654321") return json({ detail: "Invalid credentials" }, 401);
      authenticated = true;
      return json({ accessToken: "fixture-token", expiresAtUtc: "2099-01-01T00:00:00Z", session });
    }
    if (path === "/api/account-language") return json({ language: "en" });
    if (path.includes("negotiate")) return route.fulfill({ status: 503 });
    return json([]);
  });
  await page.goto("/login");
  return { attempts, errors };
}

async function open(page: Page) {
  await expect(page.getByRole("dialog")).toHaveCount(0);
  await page.getByRole("button", { name: "Enter password", exact: true }).click();
  const dialog = page.getByRole("dialog", { name: "Enter your 6-digit PIN" });
  await expect(dialog).toBeVisible();
  await expect(dialog).toHaveCount(1);
  return dialog;
}

async function enter(page: Page, pin: string) {
  for (const digit of pin) {
    const key = page.getByRole("dialog").getByRole("button", { name: digit, exact: true });
    if (test.info().project.use.hasTouch) await key.tap();
    else await key.click();
  }
}

async function geometry(page: Page) {
  return page.evaluate(() => {
    const rect = (element: Element) => { const r = element.getBoundingClientRect(); return { x: r.x, y: r.y, width: r.width, height: r.height }; };
    return {
      keys: Array.from(document.querySelectorAll('dialog button[aria-label="1"], dialog button[aria-label="5"], dialog button[aria-label="0"]')).map(rect),
      frame: rect(document.querySelector('dialog')!.previousElementSibling!),
      scroll: scrollY,
    };
  });
}

test("native modal, stable failed PIN and one successful retry", async ({ page }) => {
  const f = await fixture(page);
  await expect(page.getByLabel("Numeric keypad")).not.toBeVisible();
  const dialog = await open(page);
  await expect(dialog).toHaveJSProperty("open", true);
  await expect(dialog.getByRole("button", { name: "Close" })).toBeFocused();
  const before = await geometry(page);
  const box = await dialog.boundingBox();
  expect(box!.x).toBe(0); expect(box!.y).toBe(0);
  expect(box!.width).toBe(page.viewportSize()!.width);
  expect(box!.height).toBe(page.viewportSize()!.height);
  for (const key of before.keys) expect(key.width).toBeGreaterThanOrEqual(76);
  await page.getByRole("textbox", { name: "Username" }).evaluate(element => (element as HTMLElement).focus());
  await expect(dialog.getByRole("button", { name: "Close" })).toBeFocused();
  await enter(page, "123456");
  await expect(dialog.getByRole("alert")).toHaveText("Invalid credentials");
  await expect(dialog.getByRole("status", { name: "0 of 6 digits entered" })).toBeVisible();
  const after = await geometry(page);
  for (let i = 0; i < before.keys.length; i++) {
    expect(Math.abs(before.keys[i].x - after.keys[i].x)).toBeLessThanOrEqual(1);
    expect(Math.abs(before.keys[i].y - after.keys[i].y)).toBeLessThanOrEqual(1);
  }
  expect(after.frame).toEqual(before.frame); expect(after.scroll).toBe(before.scroll);
  expect(f.attempts).toEqual([{ username: "manager", password: "123456" }]);
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth - innerWidth)).toBeLessThanOrEqual(1);
  await enter(page, "654321");
  await expect(page.getByRole("heading", { name: "Choose workspace" })).toBeVisible();
  expect(f.attempts).toEqual([{ username: "manager", password: "123456" }, { username: "manager", password: "654321" }]);
  await expect(page.locator("dialog[open]")).toHaveCount(0);
  expect(f.errors).toEqual([]);
});

test("Close, Escape, keyboard delete and reopening clear partial PIN", async ({ page }) => {
  const f = await fixture(page);
  let dialog = await open(page);
  await dialog.press("1"); await dialog.press("2"); await dialog.press("Backspace");
  await expect(dialog.getByRole("status", { name: "1 of 6 digits entered" })).toBeVisible();
  await dialog.press("Escape");
  await expect(page.getByRole("button", { name: "Enter password" })).toBeFocused();
  dialog = await open(page);
  await expect(dialog.getByRole("status", { name: "0 of 6 digits entered" })).toBeVisible();
  await enter(page, "12");
  await dialog.getByRole("button", { name: "Close" }).click();
  await expect(page.getByRole("button", { name: "Enter password" })).toBeFocused();
  await expect(page.getByRole("textbox", { name: "Username" })).toHaveValue("manager");
  await open(page);
  await expect(page.getByRole("status", { name: "0 of 6 digits entered" })).toBeVisible();
  expect(f.attempts).toEqual([]); expect(f.errors).toEqual([]);
});

for (const [width, height] of [[320, 568], [375, 667], [390, 844], [430, 850]]) {
  test(`full viewport and reachable large targets at ${width}x${height}`, async ({ page }) => {
    await page.setViewportSize({ width, height });
    const f = await fixture(page);
    const dialog = await open(page);
    await expect(page.getByLabel("Numeric keypad")).toHaveCSS("grid-template-columns", /\S+ \S+ \S+/);
    for (const name of ["Close", "1", "9", "0", "Delete last digit"]) {
      const key = dialog.getByRole("button", { name, exact: true });
      await key.scrollIntoViewIfNeeded();
      const box = (await key.boundingBox())!;
      expect(box.width).toBeGreaterThanOrEqual(name === "Close" ? 44 : 76);
      expect(box.height).toBeGreaterThanOrEqual(name === "Close" ? 44 : 76);
      expect(box.x).toBeGreaterThanOrEqual(0); expect(box.x + box.width).toBeLessThanOrEqual(width + 1);
      expect(box.y).toBeGreaterThanOrEqual(0); expect(box.y + box.height).toBeLessThanOrEqual(height + 1);
    }
    expect(await page.evaluate(() => document.documentElement.scrollWidth - innerWidth)).toBeLessThanOrEqual(1);
    expect(await page.evaluate(() => scrollY)).toBe(0);
    expect(f.errors).toEqual([]);
  });
}

test("PC manual sign-in remains available", async ({ page }) => {
  const f = await fixture(page, "pc");
  const password = page.getByLabel("Password", { exact: true });
  await expect(password).toHaveAttribute("inputmode", "numeric");
  await expect(page.locator("dialog")).toHaveCount(0);
  await password.fill("654321");
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
  await expect(page.getByRole("heading", { name: "Choose workspace" })).toBeVisible();
  expect(f.attempts).toEqual([{ username: "manager", password: "654321" }]);
  expect(f.errors).toEqual([]);
});
