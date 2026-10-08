import { expect, test, type Page } from "@playwright/test";

test.use({ timezoneId: "Europe/Warsaw", launchOptions: process.platform === "win32" ? { ignoreDefaultArgs: ["--disable-accelerated-compositing"] } : {} });

async function fixture(page: Page, initialMode: "choose" | "pc" | "phone" | "guest" = "guest", role = "manager") {
  let mode = initialMode;
  let failExchange = false;
  let pending = false;
  const unsafe: string[] = [];
  const errors: string[] = [];
  page.on("pageerror", error => errors.push(error.message));
  if (mode !== "guest") await page.addInitScript(() => localStorage.setItem("gf3.auth.access-token", "fixture-token"));
  const session = () => ({ role, workspaceMode: role === "manager" ? mode : null, managerId: role === "manager" ? 1 : null, employeeId: role === "employee" ? 12 : null, userName: "manager", displayName: "Manager Workspace", isSystemManager: true });
  const employees = Array.from({ length: 6 }, (_, index) => ({ id: index + 2, firstName: "Worker", lastName: `${index + 1}`, hasLoginAccount: true, isOnline: index === 0, username: `worker${index}`, email: `worker${index}@example.com`, phone: "123456" }));
  const graph = { id: 3, containerId: 1, shopId: 4, name: "Private schedule", year: 2026, month: 10, publicationStatus: "private", peoplePerShift: 1, shift1Time: "08:00 - 16:00", shift2Time: "16:00 - 20:00", maxHoursPerEmpMonth: 160, maxConsecutiveDays: 5, maxConsecutiveFull: 3, maxFullPerMonth: 10 };
  const graphs = [graph, { ...graph, id: 6, name: "Public schedule", publicationStatus: "public" }];
  const group = { id: 5, name: "Private dispo", year: 2026, month: 10, publicationStatus: "private" };
  await page.route("**/api/**", async route => {
    const request = route.request(); const path = new URL(request.url()).pathname; const method = request.method();
    if (!path.startsWith("/api/")) return route.continue();
    const json = (body: unknown, status = 200) => route.fulfill({ status, contentType: "application/json", body: JSON.stringify(body) });
    if (path === "/api/auth/login") { mode = role === "manager" ? "choose" : "pc"; return json({ accessToken: `fixture-${mode}`, expiresAtUtc: "2099-01-01T00:00:00Z", session: session() }); }
    if (path === "/api/auth/manager-mode") {
      if (failExchange) return json({ detail: "offline" }, 503);
      mode = request.postDataJSON().mode;
      return json({ accessToken: `fixture-${mode}`, expiresAtUtc: "2099-01-01T00:00:00Z", session: session() });
    }
    if (path === "/api/auth/session") return mode === "guest" ? json({}, 401) : json(session());
    if (path === "/api/auth/logout") { mode = "guest"; return route.fulfill({ status: 204 }); }
    if (path === "/api/regulations/9/accept") { pending = false; return json({ id: 1 }); }
    if (path === "/api/regulations/pending") return json(pending ? [{ id: 9, title: "Required rules", version: "1", message: "Read these rules", pdfFileName: "rules.pdf" }] : []);
    if (path === "/api/regulations/9/pdf") return route.fulfill({ contentType: "application/pdf", body: "%PDF-1.7 test" });
    if (mode === "phone" && !["GET", "HEAD", "OPTIONS"].includes(method)) { unsafe.push(path); return json({ code: "manager_phone_read_only" }, 403); }
    if (mode === "choose" && !path.startsWith("/api/regulations/")) return json({ code: "manager_workspace_mode_required" }, 403);
    let data: unknown = [];
    if (path === "/api/account-language") data = { language: "en" };
    else if (path === "/api/employees") data = employees;
    else if (path.startsWith("/api/employees/") && !path.includes("regulation")) data = employees.find(item => String(item.id) === path.split("/")[3]);
    else if (path === "/api/containers") data = [{ id: 1, name: "All manager container", note: "Full manager records" }];
    else if (path === "/api/containers/1") data = { id: 1, name: "All manager container", note: "Full manager records" };
    else if (path === "/api/containers/1/graphs") data = graphs;
    else if (/\/graphs\/\d+$/.test(path)) data = graphs.find(item => String(item.id) === path.split("/").at(-1));
    else if (path.endsWith("/employees") && path.includes("graphs")) data = employees.map((item, index) => ({ id: index + 1, scheduleId: 3, employeeId: item.id, displayOrder: index }));
    else if (path.endsWith("/slots") && path.includes("graphs")) data = employees.map((item, index) => ({ id: index + 1, scheduleId: 3, employeeId: item.id, dayOfMonth: 1, slotNo: index + 1, fromTime: "08:00", toTime: "16:00", status: "Working" }));
    else if (path === "/api/shops") data = [{ id: 4, name: "Central", address: "Main Street" }];
    else if (path === "/api/shops/4") data = { id: 4, name: "Central", address: "Main Street" };
    else if (path === "/api/availability-groups") data = [group];
    else if (path === "/api/availability-groups/5") data = group;
    else if (path === "/api/availability-groups/5/items") data = employees.map((item, index) => ({ memberId: index + 1, employeeId: item.id, displayOrder: index, dayId: index + 1, dayOfMonth: 1, kind: "Available" }));
    else if (path.includes("notepad")) data = { notes: [], state: { isExpanded: false, isPinned: false, height: 700 } };
    else if (path === "/api/employee-ui-state") data = { scheduleColumnOrders: {}, readNotificationIds: [], pinnedSwapIds: [] };
    else if (path === "/api/employee-profile/me") data = { employeeId: 12, username: "worker", displayName: "Worker", recoveryEmail: "worker@example.com" };
    else if (path.includes("negotiate")) return route.fulfill({ status: 503 });
    return json(data);
  });
  return { unsafe, errors, failExchange: (value: boolean) => { failExchange = value; }, requireLegal: () => { pending = true; } };
}
async function login(page: Page) {
  await page.goto("/login");
  await page.getByRole("textbox", { name: "Username" }).fill("manager");
  await page.getByRole("textbox", { name: "Password" }).fill("123456");
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
  await expect(page.getByRole("heading", { name: "Choose workspace" })).toBeVisible();
}
async function noOverflow(page: Page) {
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth - innerWidth)).toBeLessThanOrEqual(1);
  await expect.poll(() => page.locator("[data-manager-phone] > div").evaluate(element => element.getBoundingClientRect().width)).toBeLessThanOrEqual(430);
}
async function noManagement(page: Page) {
  await expect(page.getByRole("button", { name: /^(Add New|Edit|Delete|Kick|Publish|Export|Edit all)/ })).toHaveCount(0);
}
test("login chooser selects original PC shell and edit controls", async ({ page }) => {
  const f = await fixture(page); await login(page); await page.getByRole("button", { name: "PC Full access", exact: true }).click();
  await page.goto("/employee"); await expect(page.getByRole("button", { name: "Add New" })).toBeVisible(); await expect(page.locator("aside").first()).toBeVisible(); expect(f.errors).toEqual([]);
});
test("Phone switches both ways, refresh persists and back never exposes an editor", async ({ page }) => {
  const f = await fixture(page); await login(page); await page.getByRole("button", { name: "Phone Read only", exact: true }).click();
  await expect(page.locator("[data-manager-phone]")).toBeVisible(); await page.waitForLoadState("networkidle"); await page.reload(); await expect(page.locator("[data-manager-phone]")).toBeVisible();
  await page.waitForLoadState("networkidle"); await page.getByRole("link", { name: "More", exact: true }).click(); await page.waitForLoadState("networkidle"); await page.getByRole("button", { name: /Switch to PC/ }).click(); await expect(page.locator("[data-manager-phone]")).toHaveCount(0); await page.waitForLoadState("networkidle");
  await page.evaluate(() => { history.pushState({}, "", "/employee/2/edit"); dispatchEvent(new PopStateEvent("popstate")); }); await expect(page.getByRole("heading", { name: "Edit Employee", exact: true })).toBeVisible(); await page.waitForLoadState("networkidle"); await expect(page.getByRole("button", { name: /Switch to Phone/ })).toBeVisible(); await page.getByRole("button", { name: /Switch to Phone/ }).click();
  await expect(page.locator("[data-manager-phone]")).toBeVisible(); await page.waitForLoadState("networkidle"); await page.goBack(); await expect(page.locator("[data-manager-phone]")).toBeVisible(); expect(new URL(page.url()).pathname).not.toContain("/edit"); await noManagement(page); expect(f.errors).toEqual([]);
});
for (const width of [320, 375, 390, 430, 768, 1440]) {
  test(`Phone all manager data, matrix scrolling and shell at ${width}px`, async ({ page }, testInfo) => {
    await page.setViewportSize({ width, height: 850 }); const f = await fixture(page, "phone");
    await page.goto("/container"); await expect(page.getByText("All manager container", { exact: true })).toBeVisible(); await noOverflow(page); await noManagement(page);
    await page.getByText("All manager container", { exact: true }).click(); await expect(page.getByText("Private schedule", { exact: true })).toBeVisible(); await expect(page.getByText("Public schedule", { exact: true })).toBeVisible(); await noOverflow(page);
    await page.getByText("Private schedule", { exact: true }).click(); await expect(page.getByText("Schedule Summary", { exact: true })).toBeVisible(); await noOverflow(page); await noManagement(page);
    await expect(page.locator('nav[aria-label="Manager navigation"] [aria-current="page"]')).toHaveAttribute("href", "/container");
    if (width === 390 || width === 1440) await page.screenshot({ path: testInfo.outputPath("phone-graph.png"), fullPage: true });
    const table = page.locator("main table").first();
    await expect(table).toBeVisible();
    await expect.poll(() => table.evaluate(element => { const parent = element.parentElement!; return parent.scrollWidth > parent.clientWidth; })).toBe(true);
    await page.goto("/availability/5"); await expect(page.getByText("Private dispo", { exact: true })).toBeVisible(); await noOverflow(page); await noManagement(page);
    await page.goto("/employee"); await expect(page.getByText("Worker 1", { exact: true })).toBeVisible(); await expect(page.getByText("Worker 6", { exact: true })).toBeVisible(); await noOverflow(page);
    await page.getByText("Worker 1", { exact: true }).click(); await expect(page.getByText("Employee Profile", { exact: true })).toBeVisible(); await noOverflow(page); await noManagement(page);
    await page.goto("/shop/4"); await expect(page.getByText("Central", { exact: true })).toBeVisible(); await noOverflow(page); await noManagement(page);
    expect(f.unsafe).toEqual([]); expect(f.errors).toEqual([]);
  });
}
test("failed switch retains current UI and retry works; legal gate remains in choose and phone", async ({ page }) => {
  const f = await fixture(page, "choose"); f.requireLegal(); await page.goto("/");
  await expect(page.getByRole("dialog", { name: "Required rules" })).toBeVisible(); await page.getByRole("checkbox").check(); await page.getByRole("button", { name: "Accept and continue" }).click();
  await expect(page.getByRole("dialog")).toHaveCount(0); f.failExchange(true); await page.getByRole("button", { name: "Phone Read only" }).click(); await expect(page.getByRole("alert").filter({ hasText: "Mode change failed" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Choose workspace" })).toBeVisible(); f.failExchange(false); await page.getByRole("button", { name: "Phone Read only" }).click(); await expect(page.locator("[data-manager-phone]")).toBeVisible();
  await page.goto("/more"); f.failExchange(true); await page.getByRole("button", { name: /Switch to PC/ }).click(); await expect(page.locator("[data-manager-phone]")).toBeVisible(); await expect(page.getByRole("alert").filter({ hasText: "Mode change failed" })).toBeVisible();
  f.requireLegal(); await page.reload(); await expect(page.getByRole("dialog", { name: "Required rules" })).toBeVisible();
});
test("unsafe replay returns 403", async ({ page }) => {
  await fixture(page, "phone"); await page.goto("/employee"); await expect(page.locator("[data-manager-phone]")).toBeVisible();
  const result = await page.evaluate(async () => { const r = await fetch("/api/employees", { method: "POST", headers: { Authorization: `Bearer ${localStorage.getItem("gf3.auth.access-token")}`, "Content-Type": "application/json" }, body: "{}" }); return { status: r.status, code: (await r.json()).code }; });
  expect(result).toEqual({ status: 403, code: "manager_phone_read_only" });
});
test("employee login never shows workspace choice", async ({ page }) => {
  await fixture(page, "guest", "employee"); await page.goto("/login"); await expect(page.getByRole("button", { name: /Phone/ })).toBeVisible();
  await page.getByRole("textbox", { name: "Username" }).fill("worker"); await page.getByRole("textbox", { name: "Password" }).fill("123456"); await page.getByRole("button", { name: "Sign in", exact: true }).click();
  await expect(page.locator("nav[aria-label='Employee sections']:visible").first()).toBeVisible(); await expect(page.getByRole("heading", { name: "Choose workspace" })).toHaveCount(0);
});

test("chooser keyboard focus and reduced motion; phone nav touch targets", async ({ page }, testInfo) => {
  await page.emulateMedia({ reducedMotion: "reduce" }); await fixture(page, "choose"); await page.goto("/");
  const phone = page.getByRole("button", { name: "Phone Read only", exact: true }); await expect(phone).toBeVisible(); await phone.focus(); await expect(phone).toBeFocused();
  await page.screenshot({ path: testInfo.outputPath("chooser.png") }); await page.keyboard.press("Enter"); await expect(page.locator("[data-manager-phone]")).toBeVisible();
  for (const link of await page.locator('nav[aria-label="Manager navigation"] a').all()) {
    const box = await link.boundingBox(); expect(box!.height).toBeGreaterThanOrEqual(44); expect(box!.width).toBeGreaterThanOrEqual(44);
  }
});
