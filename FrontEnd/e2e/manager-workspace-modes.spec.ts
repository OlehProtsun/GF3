import { writeFile } from "node:fs/promises";
import { expect, test, type Page, type Locator, type TestInfo } from "@playwright/test";

test.use({ timezoneId: "Europe/Warsaw", launchOptions: process.platform === "win32" ? { ignoreDefaultArgs: ["--disable-accelerated-compositing"] } : {} });

async function fixture(page: Page, initialMode: "choose" | "pc" | "phone" | "guest" = "guest", role = "manager", options: { workerCount?: number; manualColumn?: boolean; longNames?: boolean; language?: "en" | "pl" } = {}) {
  let mode = initialMode;
  let failExchange = false;
  let pending = false;
  const unsafe: string[] = [];
  const errors: string[] = [];
  page.on("pageerror", error => errors.push(error.message));
  if (mode !== "guest") await page.addInitScript(() => localStorage.setItem("gf3.auth.access-token", "fixture-token"));
  const session = () => ({ role, workspaceMode: role === "manager" ? mode : null, managerId: role === "manager" ? 1 : null, employeeId: role === "employee" ? 12 : null, userName: "manager", displayName: "Manager Workspace", isSystemManager: true });
  const employees = Array.from({ length: options.workerCount ?? 6 }, (_, index) => ({ id: index + 2, firstName: "Worker", lastName: options.longNames ? `A very long employee surname ${index + 1}` : `${index + 1}`, hasLoginAccount: true, isOnline: index === 0, username: `worker${index}`, email: `worker${index}@example.com`, phone: "123456" }));
  const graph = { id: 3, containerId: 1, shopId: 4, name: "Private schedule", note: options.manualColumn ? `[[GF3_GRAPH_META:${JSON.stringify({ manualColumns: [{ id: 1, label: "Manual coverage", cells: { "1": "Reserve coverage" } }] })}]]` : "", year: 2026, month: 10, publicationStatus: "private", peoplePerShift: 1, shift1Time: "08:00 - 16:00", shift2Time: "16:00 - 20:00", maxHoursPerEmpMonth: 160, maxConsecutiveDays: 5, maxConsecutiveFull: 3, maxFullPerMonth: 10 };
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
    if (path === "/api/account-language") data = { language: options.language ?? "en" };
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
  await page.waitForLoadState("networkidle"); await page.goto("/login");
  await page.getByRole("textbox", { name: "Username" }).fill("manager");
  await page.getByRole("textbox", { name: "Password" }).fill("123456");
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
  await expect(page.getByRole("heading", { name: "Choose workspace" })).toBeVisible();
}
async function noOverflow(page: Page) {
  await expect(page.locator("[data-manager-phone] header").filter({ hasText: "GF3" })).toHaveCount(0);
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth - innerWidth)).toBeLessThanOrEqual(1);
  await expect.poll(() => page.locator("[data-manager-phone] > div").evaluate(element => element.getBoundingClientRect().width)).toBeLessThanOrEqual(430);
}
async function noManagement(page: Page) {
  await expect(page.getByRole("button", { name: /^(Add New|Edit|Delete|Kick|Publish|Export|Edit all)/ })).toHaveCount(0);
}
async function recordGeometry(testInfo: TestInfo, name: string, geometry: unknown) {
  const path = testInfo.outputPath(`${name}.json`);
  await writeFile(path, JSON.stringify(geometry, null, 2));
  await testInfo.attach(name, { path, contentType: "application/json" });
}
async function reachesViewport(scroll: Locator, cell: Locator) {
  await expect.poll(async () => {
    const viewport = await scroll.boundingBox(); const box = await cell.boundingBox();
    return Boolean(viewport && box && box.x < viewport.x + viewport.width && box.x + box.width > viewport.x);
  }).toBe(true);
}
async function scrollMatrix(page: Page, headerCount = 7) {
  const scroll = page.locator("[data-phone-matrix-scroll]");
  await expect(scroll).toBeVisible();
  const headers = scroll.locator("thead th");
  await expect(headers).toHaveCount(headerCount);
  await expect(headers.first()).toHaveText("Day");
  const before = await scroll.evaluate(element => ({ clientWidth: element.clientWidth, scrollWidth: element.scrollWidth, scrollLeft: element.scrollLeft, overflowX: getComputedStyle(element).overflowX }));
  expect(before.clientWidth).toBeGreaterThan(150);
  if (headerCount > 2) expect(before.scrollWidth).toBeGreaterThan(before.clientWidth);
  expect(["auto", "scroll"]).toContain(before.overflowX);
  await scroll.evaluate(element => { element.scrollLeft = element.scrollWidth; });
  if (before.scrollWidth > before.clientWidth) await expect.poll(() => scroll.evaluate(element => element.scrollLeft)).toBeGreaterThan(0);
  else await expect.poll(() => scroll.evaluate(element => element.scrollLeft)).toBe(0);
  await reachesViewport(scroll, headers.last());
  await reachesViewport(scroll, scroll.locator("tbody tr").first().locator("td").last());
  const after = await scroll.evaluate(element => element.scrollLeft);
  await noOverflow(page);
  return { ...before, after, headerCount };
}
async function navigation(page: Page, width: number) {
  const nav = page.getByRole("navigation", { name: "Manager navigation" });
  await expect(nav.getByRole("link")).toHaveCount(5);
  const current = nav.locator('[aria-current="page"]');
  await expect(current).toHaveCount(1);
  await expect(current.locator("span")).toHaveCSS("opacity", width < 375 ? "0" : "1");
  const glass = await nav.evaluate(element => ({ background: getComputedStyle(element).backgroundColor, filter: getComputedStyle(element).backdropFilter }));
  expect(glass.background).toBe("rgba(255, 255, 255, 0.16)");
  expect(glass.filter).toContain("blur(20px)");
  for (const link of await nav.getByRole("link").all()) {
    const box = await link.boundingBox(); expect(box!.height).toBeGreaterThanOrEqual(44);
    expect(box!.width).toBeGreaterThanOrEqual(44);
    await expect(link.locator("svg")).toBeVisible();
  }
  await page.getByRole("button", { name: "Collapse navigation", exact: true }).click();
  await expect(nav).toHaveAttribute("inert", "");
  const open = page.getByRole("button", { name: "Open navigation", exact: true });
  await expect(open).toBeFocused();
  await current.focus(); await expect(open).toBeFocused();
  await open.click(); await expect(current).toBeFocused();
  await expect(nav).not.toHaveAttribute("inert");
}

test("login chooser selects original PC shell and edit controls", async ({ page }) => {
  const f = await fixture(page); await login(page);
  for (const name of ["PC Full access", "Phone Read only"]) {
    const choice = page.getByRole("button", { name, exact: true });
    await expect(choice).toHaveCSS("box-shadow", "none");
    await expect(choice).toHaveCSS("border-top-color", "rgb(226, 232, 240)");
  }
  await expect(page.getByRole("button", { name: "PC Full access", exact: true }).locator("svg")).toHaveCSS("transform", "matrix(1, 0, 0, -1, 0, 0)");
  await page.getByRole("button", { name: "PC Full access", exact: true }).click();
  await page.waitForLoadState("networkidle"); await page.goto("/employee"); await expect(page.getByRole("button", { name: "Add New" })).toBeVisible(); await expect(page.locator("aside").first()).toBeVisible(); expect(f.errors).toEqual([]);
});
test("Phone switches both ways, refresh persists and back never exposes an editor", async ({ page }) => {
  const f = await fixture(page); await login(page); await page.getByRole("button", { name: "Phone Read only", exact: true }).click();
  await expect(page.locator("[data-manager-phone]")).toBeVisible(); await page.waitForLoadState("networkidle"); await page.reload(); await expect(page.locator("[data-manager-phone]")).toBeVisible();
  await page.waitForLoadState("networkidle"); await page.getByRole("link", { name: "More", exact: true }).click(); await page.waitForLoadState("networkidle"); await page.getByRole("button", { name: /Switch to Desktop/ }).click(); await expect(page.locator("[data-manager-phone]")).toHaveCount(0); await page.waitForLoadState("networkidle");
  await page.evaluate(() => { history.pushState({}, "", "/employee/2/edit"); dispatchEvent(new PopStateEvent("popstate")); }); await expect(page.getByRole("heading", { name: "Edit Employee", exact: true })).toBeVisible(); await page.waitForLoadState("networkidle"); await expect(page.getByRole("button", { name: /Switch to Phone/ })).toBeVisible(); await page.getByRole("button", { name: /Switch to Phone/ }).click();
  await expect(page.locator("[data-manager-phone]")).toBeVisible(); await page.waitForLoadState("networkidle"); await page.goBack(); await expect(page.locator("[data-manager-phone]")).toBeVisible(); expect(new URL(page.url()).pathname).not.toContain("/edit"); await noManagement(page); expect(f.errors).toEqual([]);
});
for (const width of [320, 375, 390, 430, 768, 1440]) {
  test(`Phone all manager data, matrix scrolling and shell at ${width}px`, async ({ page }, testInfo) => {
    test.setTimeout(60_000);
    await page.setViewportSize({ width, height: 850 }); const f = await fixture(page, "phone");
    await page.waitForLoadState("networkidle"); await page.goto("/"); await expect(page.getByRole("navigation", { name: "Manager navigation" })).toBeVisible(); await expect(page.getByRole("heading", { name: "Coming Soon" })).toBeVisible();
    await expect(page.getByRole("link", { name: /Back/ })).toHaveCount(0); await noOverflow(page); await navigation(page, width);
    await page.waitForLoadState("networkidle"); await page.goto("/container"); await expect(page.getByText("All manager container", { exact: true })).toBeVisible(); await noOverflow(page); await noManagement(page);
    await page.getByText("All manager container", { exact: true }).click(); await expect(page.getByText("Private schedule", { exact: true })).toBeVisible(); await expect(page.getByText("Public schedule", { exact: true })).toBeVisible(); await noOverflow(page);
    await page.getByText("Private schedule", { exact: true }).click(); await expect(page.getByText("Schedule Summary", { exact: true })).toBeVisible(); await noOverflow(page); await noManagement(page);
    await expect(page.locator('nav[aria-label="Manager navigation"] [aria-current="page"]')).toHaveAttribute("href", "/container");
    if (width === 390 || width === 1440) await page.screenshot({ path: testInfo.outputPath("phone-graph.png"), fullPage: true });
    const geometry = await scrollMatrix(page);
    for (let index = 1; index <= 6; index++) await expect(page.locator("[data-phone-matrix-scroll] thead th").nth(index)).toContainText(`Worker ${index}`);
    await recordGeometry(testInfo, "graph-scroll-geometry", geometry);
    if (width === 390 || width === 1440) await page.screenshot({ path: testInfo.outputPath("phone-graph-last-column.png"), fullPage: true });
    const scroll = page.locator("[data-phone-matrix-scroll]");
    await scroll.evaluate(element => { element.scrollLeft = 0; });
    await expect.poll(() => scroll.evaluate(element => element.scrollLeft)).toBe(0);
    await reachesViewport(scroll, scroll.locator("thead th").first());
    await reachesViewport(scroll, scroll.locator("thead th").nth(1));
    const day = await scroll.locator("thead th").first().boundingBox(); const viewport = await scroll.boundingBox();
    expect(Math.abs(day!.x - viewport!.x)).toBeLessThanOrEqual(2);
    await expect(scroll.locator("tbody")).toContainText("08:00");
    await scroll.getByRole("button", { name: "Worker 1 day 1", exact: true }).click();
    const dialog = page.getByRole("dialog"); await expect(dialog).toBeVisible();
    const dialogBox = await dialog.boundingBox();
    expect(dialogBox!.x).toBeGreaterThanOrEqual(0); expect(dialogBox!.x + dialogBox!.width).toBeLessThanOrEqual(width + 1);
    expect(dialogBox!.y).toBeGreaterThanOrEqual(0); expect(dialogBox!.y + dialogBox!.height).toBeLessThanOrEqual(851);
    await dialog.getByRole("button", { name: "Close related schedule", exact: true }).click();
    await expect(dialog).toHaveCount(0);
    const innerWidth = await page.locator("main").evaluate(element => element.clientWidth);
    expect((await scroll.boundingBox())!.width).toBeGreaterThanOrEqual(innerWidth * .75);
    await expect(page.locator("[data-phone-schedule-information]")).toBeVisible();
    await expect(page.getByRole("button", { name: /Expand Schedule Information|Collapse Schedule Information/ })).toHaveCount(0);
    const summary = page.locator('[data-phone-schedule-summary]');
    await expect(summary.locator(":scope > details")).toHaveCount(6);
    await expect(summary.locator("table")).toHaveCount(0);
    await summary.locator("summary").first().click();
    await expect(summary.locator(":scope > details").first()).toContainText("08:00");
    await noOverflow(page);
    await page.waitForLoadState("networkidle"); await page.goto("/availability/5"); await expect(page.getByText("Availability Schedule", { exact: true })).toBeVisible();
    const info = page.locator("[data-phone-availability-information]"); await expect(info).toBeVisible();
    const matrix = page.locator("[data-phone-matrix-scroll]"); await expect(matrix).toBeVisible();
    await matrix.locator("thead").scrollIntoViewIfNeeded();
    expect((await matrix.locator("thead").boundingBox())!.y).toBeLessThan(700);
    await expect(page.getByText("Private dispo", { exact: true })).toBeVisible(); await noOverflow(page); await noManagement(page);
    await recordGeometry(testInfo, "availability-scroll-geometry", await scrollMatrix(page));
    await page.waitForLoadState("networkidle"); await page.goto("/availability"); await expect(page.getByRole("link", { name: /Back/ })).toHaveCount(0); await noOverflow(page);
    await page.waitForLoadState("networkidle"); await page.goto("/employee"); await expect(page.getByText("Worker 1", { exact: true })).toBeVisible(); await expect(page.getByText("Worker 6", { exact: true })).toBeVisible(); await noOverflow(page);
    await page.getByText("Worker 1", { exact: true }).click(); await expect(page.getByText("Employee Profile", { exact: true })).toBeVisible(); await noOverflow(page); await noManagement(page);
    await page.waitForLoadState("networkidle"); await page.goto("/shop/4"); await expect(page.getByText("Central", { exact: true })).toBeVisible(); await noOverflow(page); await noManagement(page);
    await expect(page.locator('nav[aria-label="Manager navigation"] [aria-current="page"]')).toHaveAttribute("href", "/more");
    await page.waitForLoadState("networkidle"); await page.goto("/shop"); await expect(page.getByText("Central", { exact: true })).toBeVisible(); await noOverflow(page);
    await page.waitForLoadState("networkidle"); await page.goto("/more"); await expect(page.getByRole("button", { name: /Switch to Desktop/ })).toBeVisible(); await expect(page.getByRole("link", { name: /Shops/ })).toHaveCount(0); await noOverflow(page);
    await page.evaluate(() => window.scrollTo(0, document.documentElement.scrollHeight));
    const logout = page.getByRole("button", { name: /Logout|Log out|Sign out/ }); await expect(logout).toBeVisible();
    const logoutBox = await logout.boundingBox(); const navBox = await page.getByRole("navigation", { name: "Manager navigation" }).boundingBox();
    expect(logoutBox!.y + logoutBox!.height).toBeLessThanOrEqual(navBox!.y);
    expect(f.unsafe).toEqual([]); expect(f.errors).toEqual([]);
  });
}
test("failed switch retains current UI and retry works; legal gate remains in choose and phone", async ({ page }) => {
  const f = await fixture(page, "choose"); f.requireLegal(); await page.waitForLoadState("networkidle"); await page.goto("/");
  await expect(page.getByRole("dialog", { name: "Required rules" })).toBeVisible(); await page.getByRole("checkbox").check(); await page.getByRole("button", { name: "Accept and continue" }).click();
  await expect(page.getByRole("dialog")).toHaveCount(0); f.failExchange(true); await page.getByRole("button", { name: "Phone Read only" }).click(); await expect(page.getByRole("alert").filter({ hasText: "Mode change failed" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Choose workspace" })).toBeVisible(); f.failExchange(false); await page.getByRole("button", { name: "Phone Read only" }).click(); await expect(page.locator("[data-manager-phone]")).toBeVisible();
  await page.waitForLoadState("networkidle"); await page.goto("/more"); f.failExchange(true); await page.getByRole("button", { name: /Switch to Desktop/ }).click(); await expect(page.locator("[data-manager-phone]")).toBeVisible(); await expect(page.getByRole("alert").filter({ hasText: "Mode change failed" })).toBeVisible();
  f.requireLegal(); await page.reload(); await expect(page.getByRole("dialog", { name: "Required rules" })).toBeVisible();
});
test("unsafe replay returns 403", async ({ page }) => {
  await fixture(page, "phone"); await page.waitForLoadState("networkidle"); await page.goto("/employee"); await expect(page.locator("[data-manager-phone]")).toBeVisible();
  const result = await page.evaluate(async () => { const r = await fetch("/api/employees", { method: "POST", headers: { Authorization: `Bearer ${localStorage.getItem("gf3.auth.access-token")}`, "Content-Type": "application/json" }, body: "{}" }); return { status: r.status, code: (await r.json()).code }; });
  expect(result).toEqual({ status: 403, code: "manager_phone_read_only" });
});
test("employee login never shows workspace choice", async ({ page }) => {
  await fixture(page, "guest", "employee"); await page.waitForLoadState("networkidle"); await page.goto("/login"); await expect(page.getByRole("button", { name: /Phone/ })).toBeVisible();
  await page.getByRole("textbox", { name: "Username" }).fill("worker"); await page.getByRole("textbox", { name: "Password" }).fill("123456"); await page.getByRole("button", { name: "Sign in", exact: true }).click();
  await expect(page.locator("nav[aria-label='Employee sections']:visible").first()).toBeVisible(); await expect(page.getByRole("heading", { name: "Choose workspace" })).toHaveCount(0);
});

test("chooser keyboard focus and reduced motion; phone nav touch targets", async ({ page }, testInfo) => {
  await page.emulateMedia({ reducedMotion: "reduce" }); await fixture(page, "choose"); await page.waitForLoadState("networkidle"); await page.goto("/");
  const phone = page.getByRole("button", { name: "Phone Read only", exact: true }); await expect(phone).toBeVisible(); await phone.focus(); await expect(phone).toBeFocused();
  await page.screenshot({ path: testInfo.outputPath("chooser.png") }); await page.keyboard.press("Enter"); await expect(page.locator("[data-manager-phone]")).toBeVisible();
  const nav = page.getByRole("navigation", { name: "Manager navigation" });
  await page.getByRole("button", { name: "Collapse navigation", exact: true }).focus();
  await page.keyboard.press("Space"); await expect(nav).toHaveAttribute("inert", "");
  await expect(page.getByRole("button", { name: "Open navigation", exact: true })).toBeFocused();
  await page.keyboard.press("Tab"); expect(await nav.locator("a").evaluateAll(elements => elements.some(element => element === document.activeElement))).toBe(false);
  await page.getByRole("button", { name: "Open navigation", exact: true }).focus();
  await page.keyboard.press("Enter"); await expect(nav.locator('[aria-current="page"]')).toBeFocused();
  await expect(nav).not.toHaveAttribute("inert");
  await page.keyboard.press("Tab");
  await expect(nav.getByRole("link", { name: "Containers", exact: true })).toBeFocused();
  await page.keyboard.press("Enter"); await expect(page).toHaveURL(/\/container$/);
  for (const link of await page.locator('nav[aria-label="Manager navigation"] a').all()) {
    const box = await link.boundingBox(); expect(box!.height).toBeGreaterThanOrEqual(44); expect(box!.width).toBeGreaterThanOrEqual(44);
  }
});


for (const workerCount of [0, 1, 6]) {
  test(`Phone matrix preserves ${workerCount} workers and optional manual column`, async ({ page }, testInfo) => {
    await page.setViewportSize({ width: 320, height: 650 });
    const f = await fixture(page, "phone", "manager", { workerCount, manualColumn: workerCount === 6, longNames: true });
    await page.waitForLoadState("networkidle"); await page.goto("/container/1/graphs/3"); await expect(page.getByText("Schedule Summary", { exact: true })).toBeVisible();
    if (workerCount === 0) {
      await expect(page.getByText("No employees are assigned to this schedule yet.")).toBeVisible();
      await expect(page.locator("[data-phone-matrix-scroll]")).toHaveCount(0);
    } else {
      await recordGeometry(testInfo, "edge-scroll-geometry", await scrollMatrix(page, workerCount === 6 ? 8 : 2));
      if (workerCount === 6) {
        await expect(page.locator("[data-phone-matrix-scroll] thead th").last()).toHaveText("Manual coverage");
        await expect(page.locator("[data-phone-matrix-scroll] tbody tr").first().locator("td").last()).toContainText("Reserve coverage");
      }
    }
    await noOverflow(page); await noManagement(page); expect(f.unsafe).toEqual([]); expect(f.errors).toEqual([]);
  });
}


test("rounded chooser logout ends the session", async ({ page }) => {
 await fixture(page, "choose"); await page.goto("/");
 const logout = page.getByRole("button", { name: "Log out", exact: true }); await expect(logout).toBeVisible();
 expect((await logout.boundingBox())!.height).toBeGreaterThanOrEqual(44);
 await expect(logout).toHaveCSS("border-radius", "999px"); await logout.click(); await expect(page).toHaveURL(/\/login$/);
});

for (const width of [320, 375, 390, 430]) {
 for (const workerCount of [0, 1, 6]) {
  test(`Availability ${workerCount} workers visible at ${width}px with visible information`, async ({ page }, testInfo) => {
   await page.setViewportSize({ width, height: 850 }); const f = await fixture(page, "phone", "manager", { workerCount, longNames: true });
   await page.goto("/availability/5"); await expect(page.getByText("Availability Schedule", { exact: true })).toBeVisible();
   await expect(page.locator("[data-phone-availability-information]")).toBeVisible();
   if (workerCount === 0) {
    await expect(page.getByText("No employees are assigned to this availability group yet.")).toBeVisible();
    await expect(page.locator("[data-phone-matrix-scroll]")).toHaveCount(0);
   } else {
    const scroll = page.locator("[data-phone-matrix-scroll]");
    await scroll.locator("thead").scrollIntoViewIfNeeded();
    expect((await scroll.locator("thead").boundingBox())!.y).toBeLessThan(700);
    await recordGeometry(testInfo, "availability-first-screen", await scrollMatrix(page, workerCount + 1));
    await scroll.locator("tbody tr").last().scrollIntoViewIfNeeded(); await expect(scroll.locator("tbody tr").last()).toContainText("31");
    await noOverflow(page);
   }
   if (width === 390) await page.screenshot({ path: testInfo.outputPath("phone-availability.png"), fullPage: true });
   await noOverflow(page); await noManagement(page); expect(f.errors).toEqual([]); expect(f.unsafe).toEqual([]);
  });
 }
}

test("phone search, descending containers, pin priority, metrics and long employee fields", async ({ page }, testInfo) => {
 await page.setViewportSize({ width: 390, height: 850 }); const f = await fixture(page, "phone", "manager", { longNames: true });
 await page.route("**/api/containers", route => route.fulfill({ contentType: "application/json", body: JSON.stringify([{ id: 1, name: "All manager container" }, { id: 9, name: "Container nine" }, { id: 4, name: "Container four" }]) }));
 await page.goto("/container"); await expect(page.getByText("Container nine", { exact: true })).toBeVisible();
 const titles = page.locator('main article[role="button"] [class*="_title_"]'); await expect(titles).toHaveText(["Container nine", "Container four", "All manager container"]);
 await page.getByRole("button", { name: /Pin All manager container/ }).click(); await expect(titles).toHaveText(["All manager container", "Container nine", "Container four"]);
 const search = page.getByRole("searchbox", { name: "Search", exact: true }); await search.fill("nine"); await expect(titles).toHaveText(["Container nine"]);
 await page.getByRole("button", { name: "Clear Search", exact: true }).click(); await expect(titles).toHaveCount(3);
 await page.getByText("All manager container", { exact: true }).click(); await expect(page.getByRole("heading", { name: "Statistics" })).toBeVisible();
 const stats = page.getByRole("heading", { name: "Statistics" }).locator(".."); await expect(stats.locator("table")).toHaveCount(0);
 await expect(stats.locator("article")).toHaveCount(7); await stats.locator("article details summary").first().click(); await expect(stats.locator("article").first()).toContainText("Central"); await noOverflow(page);
 await page.getByRole("link", { name: "Back", exact: true }).click(); await expect(page).toHaveURL(/\/container$/);
 await page.goto("/employee/2"); await expect(page.getByText("Worker A very long employee surname 1", { exact: true })).toBeVisible(); await noOverflow(page); await noManagement(page);
 await page.goto("/more"); await expect(page.getByRole("button", { name: /Switch to Desktop/ }).locator("svg")).toHaveCSS("transform", "matrix(1, 0, 0, -1, 0, 0)");
 await page.screenshot({ path: testInfo.outputPath("phone-more.png"), fullPage: true }); await page.getByRole("button", { name: "Log out", exact: true }).click(); await expect(page).toHaveURL(/\/login$/); expect(f.unsafe).toEqual([]);
});


for (const width of [320, 375, 390, 430]) {
 for (const workerCount of [0, 1, 8]) {
  test(`Manager schedule sticky axes and mobile summary at ${width}px with ${workerCount} long names`, async ({ page }, testInfo) => {
   await page.setViewportSize({ width, height: 740 });
   const f = await fixture(page, "phone", "manager", { workerCount, longNames: true, manualColumn: workerCount === 8 });
   await page.goto("/container/1/graphs/3"); await expect(page.getByText("Schedule Summary", { exact: true })).toBeVisible(); await page.waitForLoadState("networkidle");
   if (workerCount > 0) {
    const scroll = page.locator("[data-phone-matrix-scroll]");
    await scroll.scrollIntoViewIfNeeded();
    const before = await scroll.evaluate(element => ({ height: element.clientHeight, scrollHeight: element.scrollHeight, width: element.clientWidth, scrollWidth: element.scrollWidth }));
    expect(before.height).toBeLessThanOrEqual(560); expect(before.scrollHeight).toBeGreaterThan(before.height);
    const nameHeader = scroll.locator("thead th").nth(1); expect((await nameHeader.boundingBox())!.width).toBeLessThanOrEqual(157);
    expect(await nameHeader.evaluate(element => element.scrollWidth <= element.clientWidth)).toBe(true);
    await scroll.evaluate(element => { element.scrollLeft = element.scrollWidth; element.scrollTop = element.scrollHeight; });
    const day = scroll.locator("tbody th").last(); const corner = scroll.locator("thead th").first(); const header = scroll.locator("thead th").last();
    const geometry = await scroll.evaluate(element => ({ left: element.scrollLeft, top: element.scrollTop, box: element.getBoundingClientRect().toJSON() }));
    if (before.scrollWidth > before.width) expect(geometry.left).toBeGreaterThan(0);
    else expect(geometry.left).toBe(0);
    expect(geometry.top).toBeGreaterThan(0);
    const cornerBox = (await corner.boundingBox())!; const headerBox = (await header.boundingBox())!; const dayBox = (await day.boundingBox())!;
    expect(Math.abs(cornerBox.x - geometry.box.x)).toBeLessThanOrEqual(2); expect(Math.abs(dayBox.x - cornerBox.x)).toBeLessThanOrEqual(2);
    expect(Math.abs(headerBox.y - geometry.box.y)).toBeLessThanOrEqual(2); expect(Math.abs(cornerBox.y - headerBox.y)).toBeLessThanOrEqual(2);
    expect(await corner.evaluate(element => Number(getComputedStyle(element).zIndex))).toBeGreaterThan(await header.evaluate(element => Number(getComputedStyle(element).zIndex)));
    await reachesViewport(scroll, scroll.locator("tbody tr").last().locator("td").last());
    await recordGeometry(testInfo, "sticky-matrix-axes", { before, geometry, cornerBox, headerBox, dayBox });
    const summary = page.locator("[data-phone-schedule-summary]"); await expect(summary.locator(":scope > details")).toHaveCount(workerCount);
    expect(await summary.evaluate(element => element.scrollWidth <= element.clientWidth)).toBe(true); await expect(summary.locator("table")).toHaveCount(0);
    const search = page.getByRole("searchbox", { name: "Search schedule summary by employee name or surname" });
    await search.fill("surname 1"); await expect(summary.locator(":scope > details")).toHaveCount(1); await expect(summary).toContainText("Worker A very long employee surname 1");
    await summary.locator("summary").click(); await expect(summary).toContainText("08:00"); await expect(summary).toContainText("16:00"); await expect(summary.locator("details > summary dl dd").nth(2)).toHaveText("8");
    await search.fill("missing employee"); await expect(page.getByRole("status")).toContainText('No employees found');
    await search.fill(""); await expect(summary.locator(":scope > details")).toHaveCount(workerCount);
    await page.evaluate(() => window.scrollTo(0, document.documentElement.scrollHeight));
    const last = (await summary.locator(":scope > details").last().boundingBox())!; const nav = (await page.getByRole("navigation", { name: "Manager navigation" }).boundingBox())!;
    expect(last.y + last.height).toBeLessThanOrEqual(nav.y);
    if (width === 390 && workerCount === 8) await page.screenshot({ path: testInfo.outputPath("manager-phone-summary.png"), fullPage: true });
   } else {
    await expect(page.locator("[data-phone-matrix-scroll]")).toHaveCount(0); await expect(page.getByText("No employees are assigned to this schedule yet.")).toBeVisible();
   }
   await noOverflow(page); await noManagement(page); expect(f.unsafe).toEqual([]); expect(f.errors).toEqual([]);
  });
 }
}


test("PC schedule retains its desktop matrix dimensions and full summary table", async ({ page }) => {
 await fixture(page, "pc", "manager", { longNames: true }); await page.goto("/container/1/graphs/3");
 await expect(page.getByText("Schedule Summary", { exact: true })).toBeVisible(); await page.waitForLoadState("networkidle");
 await expect(page.locator("[data-manager-phone], [data-phone-matrix-scroll], [data-phone-schedule-summary]")).toHaveCount(0);
 const table = page.locator('[class*="summaryTableScroll"] table'); await expect(table).toBeVisible();
 await expect(table.locator("thead th")).toHaveCount(128); await expect(table.locator("tbody")).toContainText("08:00");
 const matrixHeader = page.locator('table').first().locator("thead th").nth(1);
 expect((await matrixHeader.boundingBox())!.width).toBeGreaterThan(156);
});


for (const language of ["en", "pl"] as const) for (const width of [320, 375, 390, 430]) {
 test(`Phone profile refinements ${language} at ${width}px`, async ({ page }, testInfo) => {
  await page.setViewportSize({ width, height: 850 });
  const f = await fixture(page, "phone", "manager", { workerCount: 6, longNames: true, manualColumn: true, language });
  await page.route("**/api/availability-groups/5", route => route.fulfill({ contentType: "application/json", body: JSON.stringify({ id: 5, name: "Dyspozycyjność BardzoDługiePolskieNazwisko Zespołu", year: 2026, month: 10, publicationStatus: "public", visibleFromUtc: "2026-10-01T08:00:00Z", visibleToUtc: "2026-10-31T18:00:00Z" }) }));
  await page.goto("/container/1/graphs/3");
  const information = page.locator("[data-phone-schedule-information]"); await expect(information).toBeVisible();
  const summary = page.locator("[data-phone-schedule-summary]"); await expect(summary.locator(":scope > details")).toHaveCount(6);
  const matrix = page.locator("[data-phone-matrix-scroll]");
  expect(await information.evaluate(element => Boolean(element.compareDocumentPosition(document.querySelector('[data-phone-matrix-scroll]')!) & Node.DOCUMENT_POSITION_FOLLOWING))).toBe(true);
  expect(await summary.evaluate(element => Boolean(document.querySelector('[data-phone-matrix-scroll]')!.compareDocumentPosition(element) & Node.DOCUMENT_POSITION_FOLLOWING))).toBe(true);
  const input = page.locator('main input[type="search"]'); await input.fill("SURNAME 1"); await expect(summary.locator(":scope > details")).toHaveCount(1); await input.fill("");
  const employee = summary.locator(":scope > details").first(); await employee.locator("summary").focus(); await page.keyboard.press("Enter"); await expect(employee).toHaveAttribute("open", ""); await expect(employee).toContainText("08:00"); await page.keyboard.press("Space"); await expect(employee).not.toHaveAttribute("open");
  await matrix.scrollIntoViewIfNeeded(); await matrix.evaluate(element => { element.scrollLeft = element.scrollWidth; element.scrollTop = element.scrollHeight; });
  await expect.poll(() => matrix.evaluate(element => element.scrollLeft)).toBeGreaterThan(0);
  const region = (await matrix.boundingBox())!; const corner = (await matrix.locator("thead th").first().boundingBox())!;
  expect(Math.abs(corner.x - region.x)).toBeLessThanOrEqual(2); expect(Math.abs(corner.y - region.y)).toBeLessThanOrEqual(2);
  await matrix.evaluate(element => { element.scrollLeft = 0; element.scrollTop = 0; }); await matrix.locator("tbody button").first().click(); await expect(page.getByRole("dialog")).toBeVisible();
  await page.getByRole("button", { name: /Close related schedule|Zamknij powiązany grafik/ }).click();
  await noOverflow(page); await noManagement(page);
  await page.goto("/container/1"); const statistics = page.locator("[data-phone-container-statistics]"); await expect(statistics.locator("article")).toHaveCount(7);
  const firstCard = statistics.locator("article").first(); expect(await firstCard.locator("dl").first().locator("dd").allTextContents()).toEqual(["1", "30", "16"]);
  await firstCard.locator("summary").click(); await expect(firstCard).toContainText("Central"); await expect(firstCard.locator("details dd")).toHaveText("16");
  const columns = await statistics.locator('[class*="_metrics_"]').evaluate(element => getComputedStyle(element).gridTemplateColumns.split(" ").length); expect(columns).toBe(width === 320 ? 1 : 2);
  await noOverflow(page); await noManagement(page);
  await page.evaluate(() => window.scrollTo(0, document.documentElement.scrollHeight));
  const totalBox = (await statistics.locator("article").last().boundingBox())!;
  const statisticsNav = (await page.getByRole("navigation").last().boundingBox())!;
  expect(totalBox.y + totalBox.height).toBeLessThanOrEqual(statisticsNav.y);
  if (width === 390) await page.screenshot({ path: testInfo.outputPath(`statistics-${language}.png`), fullPage: true });
  await page.getByRole("link", { name: /Back|Wstecz/, exact: true }).click(); await expect(page).toHaveURL(/\/container$/);
  await page.goto("/availability/5"); const availability = page.locator("[data-phone-availability-information]"); await expect(availability).toBeVisible();
  await expect(page.getByText(language === "en" ? "Availability Profile" : "Profil dyspozycyjności", { exact: true })).toBeVisible();
  await expect(page.locator("main details, main nav")).toHaveCount(0); await expect(availability).toContainText("Dyspozycyjność BardzoDługiePolskieNazwisko Zespołu");
  await expect(availability).toContainText(language === "en" ? "Public" : "Udostępnione"); await expect(availability).toContainText("2026");
  await expect(availability).toContainText("6"); await expect(availability.locator('[class*="_visibilityEndpoint_"]')).toHaveCount(2);
  const availabilityMatrix = page.locator("[data-phone-matrix-scroll]"); await availabilityMatrix.scrollIntoViewIfNeeded(); await availabilityMatrix.evaluate(element => { element.scrollLeft = element.scrollWidth; });
  await expect.poll(() => availabilityMatrix.evaluate(element => element.scrollLeft)).toBeGreaterThan(0); await reachesViewport(availabilityMatrix, availabilityMatrix.locator("thead th").last());
  expect(await availabilityMatrix.evaluate(element => element.clientHeight)).toBeLessThanOrEqual(560);
  await availabilityMatrix.evaluate(element => { element.scrollTop = element.scrollHeight; });
  await expect.poll(() => availabilityMatrix.evaluate(element => element.scrollTop)).toBeGreaterThan(0);
  expect((await availabilityMatrix.locator("thead th").last().boundingBox())!.width).toBeLessThanOrEqual(157);
  const availabilityRegion = (await availabilityMatrix.boundingBox())!;
  const availabilityCorner = (await availabilityMatrix.locator("thead th").first().boundingBox())!;
  expect(Math.abs(availabilityCorner.x - availabilityRegion.x)).toBeLessThanOrEqual(2);
  expect(Math.abs(availabilityCorner.y - availabilityRegion.y)).toBeLessThanOrEqual(2);
  await noOverflow(page); await noManagement(page);
  await page.evaluate(() => window.scrollTo(0, document.documentElement.scrollHeight)); const last = (await availabilityMatrix.boundingBox())!; const nav = (await page.getByRole("navigation").last().boundingBox())!; expect(last.y + last.height).toBeLessThanOrEqual(nav.y);
  if (width === 390) await page.screenshot({ path: testInfo.outputPath(`availability-${language}.png`), fullPage: true });
  await page.getByRole("link", { name: /Back|Wstecz/, exact: true }).click(); await expect(page).toHaveURL(/\/availability$/);
  expect(f.unsafe).toEqual([]); expect(f.errors).toEqual([]);
 });
}


for (const width of [320, 375, 390, 430]) test(`Home Coming Soon centered in Phone at ${width}px`, async ({ page }) => {
 await page.setViewportSize({ width, height: 850 }); const f = await fixture(page, "phone"); await page.goto("/");
 const title = page.getByRole("heading", { name: "Coming Soon", exact: true }); await expect(title).toBeVisible();
 const heading = (await title.boundingBox())!; const content = (await title.locator("..").boundingBox())!;
 expect(Math.abs(heading.x + heading.width / 2 - content.x - content.width / 2)).toBeLessThanOrEqual(2);
 expect(Math.abs(heading.y + heading.height / 2 - content.y - content.height / 2)).toBeLessThanOrEqual(2);
 await noOverflow(page); await page.getByRole("link", { name: "Containers", exact: true }).click(); await expect(page).toHaveURL(/\/container$/);
 await page.getByRole("link", { name: "Home", exact: true }).click(); await expect(title).toBeVisible(); expect(f.errors).toEqual([]); expect(f.unsafe).toEqual([]);
});
for (const height of [720, 1000]) test(`Desktop sidebar switch matches navigation and reaches phone Home at ${height}px`, async ({ page }) => {
 await page.setViewportSize({ width: 1440, height }); const f = await fixture(page, "pc"); await page.goto("/");
 const title = page.getByRole("heading", { name: "Coming Soon", exact: true }); await expect(title).toBeVisible();
 const titleBox = (await title.boundingBox())!; const content = (await title.locator("..").boundingBox())!;
 expect(Math.abs(titleBox.x + titleBox.width / 2 - content.x - content.width / 2)).toBeLessThanOrEqual(2);
 expect(Math.abs(titleBox.y + titleBox.height / 2 - content.y - content.height / 2)).toBeLessThanOrEqual(2);
 const mode = page.getByRole("button", { name: "Switch to Phone", exact: true }); const employee = page.getByRole("link", { name: "Employee", exact: true });
 const design = (element: Element) => { const style = getComputedStyle(element); return [style.width, style.height, style.borderRadius, style.backgroundColor, style.border, style.padding, style.display]; };
 const baseline = await employee.evaluate(design); expect(await mode.evaluate(design)).toEqual(baseline);
 expect(await mode.textContent()).toBe("");
 const label = page.getByText("Switch to Phone", { exact: true }); const buttonBox = (await mode.boundingBox())!; const labelBox = (await label.boundingBox())!;
 expect(labelBox.y).toBeGreaterThanOrEqual(buttonBox.y + buttonBox.height); expect(Math.abs(labelBox.x + labelBox.width / 2 - buttonBox.x - buttonBox.width / 2)).toBeLessThanOrEqual(2);
 const iconBox = (await mode.locator("svg").boundingBox())!; expect(Math.abs(iconBox.x + iconBox.width / 2 - buttonBox.x - buttonBox.width / 2)).toBeLessThanOrEqual(2); expect(Math.abs(iconBox.y + iconBox.height / 2 - buttonBox.y - buttonBox.height / 2)).toBeLessThanOrEqual(2);
 await employee.hover(); await expect.poll(() => employee.evaluate(element => element.getAnimations().length)).toBe(0); const hoverBackground = await employee.evaluate(element => getComputedStyle(element).backgroundColor);
 await mode.hover(); await expect.poll(() => mode.evaluate(element => getComputedStyle(element).backgroundColor)).toBe(hoverBackground);
 await page.keyboard.press("Tab"); await mode.focus(); await expect(mode).toBeFocused(); expect(await mode.evaluate(element => element.matches(":focus-visible"))).toBe(true);
 await mode.click(); await expect(page.locator("[data-manager-phone]")).toBeVisible(); await expect(title).toBeVisible();
 await page.getByRole("link", { name: "More", exact: true }).click(); await page.getByRole("button", { name: "Switch to Desktop", exact: true }).click();
 await expect(page.locator("[data-manager-phone]")).toHaveCount(0); await expect(title).toBeVisible(); expect(f.errors).toEqual([]);
});
