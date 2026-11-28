import { writeFile } from "node:fs/promises";
import { expect, test, type Page, type Locator, type TestInfo } from "@playwright/test";
import pl from "../src/shared/i18n/pl.json" with { type: "json" };
import { test as isolatedTest } from "./fixtures/isolatedBackend";
import { createEmployeeAccount, loginAsManager } from "./helpers";

const pcChoice = `PC ${pl["Full access"]}`;
const phoneChoice = `${pl.Phone} ${pl["Read only"]}`;

isolatedTest("system manager sees employee and phone presence until the last manager tab closes", async ({ page, browser, isolatedBackend }) => {
  isolatedTest.setTimeout(100_000);
  await page.setViewportSize({ width: 1440, height: 900 });
  const employee = await createEmployeeAccount(page.request, "presence");
  const headers = { Authorization: "Bearer " + employee.managerAccessToken };
  const profileResponse = await page.request.get("/api/employees/" + employee.id, { headers });
  expect(profileResponse.ok()).toBe(true);
  const profile = await profileResponse.json();
  const employeeName = profile.firstName + " " + profile.lastName;
  const managerUser = { userName: "presence-manager", displayName: "Phone Presence Manager", password: "824681" };
  const create = await page.request.post("/api/manager-profile/managers", { headers, data: managerUser });
  expect(create.ok(), await create.text()).toBe(true);
  const tokenFor = async (username: string, password: string, mode?: "phone" | "pc") => {
    const login = await page.request.post("/api/auth/login", { data: { username, password } });
    expect(login.ok()).toBe(true);
    const initial = await login.json();
    if (!mode) return initial.accessToken as string;
    const exchange = await page.request.post("/api/auth/manager-mode", {
      headers: { Authorization: "Bearer " + initial.accessToken }, data: { mode },
    });
    expect(exchange.ok()).toBe(true);
    return (await exchange.json()).accessToken as string;
  };
  const open = async (token: string, viewport = { width: 1440, height: 900 }) => {
    const context = await browser.newContext({ baseURL: isolatedBackend.url, viewport });
    await context.addInitScript(value => {
      localStorage.setItem("gf3.auth.access-token", value);
      sessionStorage.setItem("gf3.pwa.install.dismissed", "1");
    }, token);
    const tab = await context.newPage();
    await tab.goto("/");
    return context;
  };

  await loginAsManager(page);
  await page.goto("/information");
  const online = page.getByRole("region", { name: "Online users" });
  await expect(online.getByRole("row", { name: "Manager Manager Online", exact: true })).toBeVisible();
  const phoneToken = await tokenFor(managerUser.userName, managerUser.password, "phone");
  const phone = await open(phoneToken, { width: 390, height: 844 });
  const worker = await open(await tokenFor(employee.username, employee.password));
  const remote = online.getByRole("row").filter({ hasText: managerUser.displayName });
  try {
    await expect(remote).toContainText("Online", { timeout: 15_000 });
    await expect(online.getByRole("row").filter({ hasText: employeeName })).toContainText("Online", { timeout: 15_000 });
    const write = await page.request.post("/api/containers", { headers: { Authorization: "Bearer " + phoneToken }, data: { name: "Forbidden phone write" } });
    expect(write.status()).toBe(403);
    const pc = await open(await tokenFor(managerUser.userName, managerUser.password, "pc"));
    try {
      await expect(pc.pages()[0].getByRole("heading", { name: /^(Home|Strona główna)$/ })).toBeVisible();
      await pc.close();
      await expect(remote).toContainText("Online");
      await phone.close();
      await expect(remote).toHaveCount(0, { timeout: 15_000 });
    } finally {
      if (pc.pages().length) await pc.close();
    }
  } finally {
    if (phone.pages().length) await phone.close();
    await worker.close();
  }
});

test.use({ timezoneId: "Europe/Warsaw", launchOptions: process.platform === "win32" ? { ignoreDefaultArgs: ["--disable-accelerated-compositing"] } : {} });

async function fixture(page: Page, initialMode: "choose" | "pc" | "phone" | "guest" = "guest", role = "manager", options: { workerCount?: number; manualColumn?: boolean; longNames?: boolean; language?: "en" | "pl" } = {}) {
  let mode = initialMode;
  let failExchange = false;
  let pending = false;
  const unsafe: string[] = [];
  const errors: string[] = [];
  page.on("pageerror", error => errors.push(error.message));
  if (mode !== "guest") await page.addInitScript(() => localStorage.setItem("gf3.auth.access-token", "fixture-token"));
  if (role === "employee") await page.addInitScript(() => sessionStorage.setItem("gf3.pwa.install.dismissed", "1"));
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
    if (path === "/api/realtime/presence/negotiate" && method === "POST" && (mode === "pc" || mode === "phone")) return json({ status: 503 }, 503);
    if (mode === "phone" && !["GET", "HEAD", "OPTIONS"].includes(method)) { unsafe.push(path); return json({ code: "manager_phone_read_only" }, 403); }
    if (mode === "choose" && !path.startsWith("/api/regulations/")) return json({ code: "manager_workspace_mode_required" }, 403);
    let data: unknown = [];
    if (path === "/api/account-language") data = { language: options.language ?? "en" };
    else if (path === "/api/public-holiday-settings") data = { showPublicHolidays: true };
    else if (path === "/api/manager-home/activity") data = { items: [], allActivityCount: 0, requiresApprovalCount: 0, categoryCounts: {}, hasMore: false, nextOffset: 0 };
    else if (path === "/api/manager-profile/me") data = { id: 1, displayName: "Manager Workspace", userName: "manager", recoveryEmail: "manager@example.com", isSystem: true };
    else if (path === "/api/manager-profile/managers") data = [];
    else if (path === "/api/employees") data = employees;
    else if (/^\/api\/employees\/\d+$/.test(path)) data = employees.find(item => String(item.id) === path.split("/")[3]);
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

async function collapseSidebarIfOpen(page: Page) {
  const collapse = page.getByRole("button", { name: "Collapse sidebar", exact: true });
  if (await collapse.isVisible()) await collapse.click();
}

async function login(page: Page) {
  await page.waitForLoadState("networkidle"); await page.goto("/login");
  await page.getByRole("textbox", { name: pl.Username }).fill("manager");
  await page.getByRole("textbox", { name: pl.Password }).fill("123456");
  await page.getByRole("button", { name: pl["Sign in"], exact: true }).click();
  await expect(page.getByRole("heading", { name: pl["Choose workspace"] })).toBeVisible();
}

test("PC container schedule tiles preserve open and MultiOpen selection", async ({ page }) => {
  test.setTimeout(60_000);
  await page.setViewportSize({ width: 1440, height: 900 });
  await fixture(page, "pc");
  const openContainer = async () => {
    await page.goto("/container?openContainerId=1");
    await page.waitForLoadState("networkidle");
    const collapseSidebar = page.getByRole("button", { name: "Collapse sidebar", exact: true });
    if (await collapseSidebar.isVisible()) await collapseSidebar.click();
    const profileButton = page.getByRole("button", { name: "Open All manager container profile", exact: true });
    if (await profileButton.isVisible()) await profileButton.click();
  };
  await openContainer();
  const first = page.locator('[data-schedule-graph-id="3"]');
  const second = page.locator('[data-schedule-graph-id="6"]');
  const indicators = page.locator("[data-schedule-selection-indicator]");
  const multiOpen = page.getByRole("button", { name: "MultiOpen", exact: true });
  for (const card of [first, second]) {
    await expect(card).toBeVisible();
    for (const label of ["Shop", "Employees", "Hours", "Month Year", "Status", "Last Update"]) {
      await expect(card.getByText(label, { exact: true })).toBeVisible();
    }
  }
  await expect(indicators).toHaveCount(0);
  await first.click();
  await expect(page).toHaveURL(/\/container\/1\/graphs\/3$/);
  await openContainer();
  await multiOpen.click();
  await expect(indicators).toHaveCount(2);
  await first.click();
  await second.click();
  await expect(page).toHaveURL(/\/container\?openContainerId=1$/);
  await expect(first).toHaveAttribute("aria-pressed", "true");
  await expect(second).toHaveAttribute("aria-pressed", "true");
  await expect(page.getByRole("button", { name: "Open (2)", exact: true })).toBeVisible();
  await first.click();
  await expect(first).toHaveAttribute("aria-pressed", "false");
  await expect(second).toHaveAttribute("aria-pressed", "true");
  await page.getByRole("button", { name: "Open (1)", exact: true }).click();
  await expect(page).toHaveURL(/\/container\/1\/graphs\/6\?openGraphIds=6$/);
  await openContainer();
  await multiOpen.click();
  await first.click();
  await multiOpen.click();
  await expect(indicators).toHaveCount(0);
  await expect(first).not.toHaveAttribute("aria-pressed");
  await multiOpen.click();
  await expect(first).toHaveAttribute("aria-pressed", "false");
  await expect(second).toHaveAttribute("aria-pressed", "false");
  await multiOpen.click();
  await page.setViewportSize({ width: 375, height: 812 });
  await expect(first).toBeVisible();
  await expect(second).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth + 1)).toBe(true);
});

test("PC Schedule Profile uses the full matrix width and opens compact schedule information", async ({ page }) => {
  await page.emulateMedia({ reducedMotion: "reduce" });
  await page.setViewportSize({ width: 1440, height: 900 });
  await fixture(page, "pc", "manager", { manualColumn: true, workerCount: 12 });
  await page.goto("/container/1/graphs/3");

  const title = page.getByText("Schedule Matrix", { exact: true });
  const matrix = title.locator("xpath=ancestor::section[1]");
  const workspace = matrix.locator("xpath=ancestor::div[following-sibling::section][1]/parent::div");
  const info = matrix.getByRole("button", { name: "Schedule Information", exact: true });
  const edit = matrix.getByRole("button", { name: "Edit Schedule", exact: true });
  const remove = matrix.getByRole("button", { name: "Delete Schedule", exact: true });
  await expect(info).toBeVisible();
  await expect(workspace.locator("aside")).toHaveCount(0);
  const matrixBox = (await matrix.boundingBox())!;
  const workspaceBox = (await workspace.boundingBox())!;
  expect(matrixBox.x).toBeCloseTo(workspaceBox.x, 0);
  expect(matrixBox.width).toBeCloseTo(workspaceBox.width, 0);
  const titleBox = (await title.boundingBox())!;
  for (const control of [matrix.getByText("Last Update", { exact: true }), info, edit, remove]) {
    const box = (await control.boundingBox())!;
    expect(box.x).toBeGreaterThan(titleBox.x + titleBox.width);
    expect(Math.abs(box.y + box.height / 2 - titleBox.y - titleBox.height / 2)).toBeLessThan(2);
  }
  await expect(matrix.getByText("Total Employees: 12")).toBeVisible();
  const cell = matrix.getByRole("button", { name: "Worker 1 day 1", exact: true });
  await expect(cell).toHaveText("Public schedule");
  await expect(cell.locator("xpath=ancestor::td[1]")).toContainText("08:00 - 16:00");
  await expect(matrix.getByText("Reserve coverage", { exact: true })).toBeVisible();
  const scroll = matrix.locator("table").locator("../..");
  const scrollLeft = await scroll.evaluate(element => {
    element.scrollLeft = element.scrollWidth;
    return element.scrollLeft;
  });
  expect(scrollLeft).toBeGreaterThan(0);

  await info.click();
  const dialog = page.getByRole("dialog", { name: "Schedule Information" });
  await expect(dialog).toBeVisible();
  const close = dialog.getByRole("button", { name: "Close", exact: true });
  await expect(close).toBeFocused();
  await expect(dialog.getByRole("heading", { name: "Private schedule" })).toBeVisible();
  await expect(dialog.getByRole("group", { name: "Schedule details" }).locator(":scope > div")).toHaveCount(11);
  expect((await dialog.boundingBox())!.width).toBeLessThan(480);
  expect(await page.evaluate(() => getComputedStyle(document.documentElement).overflowY)).toBe("hidden");
  await page.keyboard.press("Escape");
  await expect(dialog).toHaveCount(0);
  await expect(info).toBeFocused();
  expect(await scroll.evaluate(element => element.scrollLeft)).toBe(scrollLeft);

  await page.setViewportSize({ width: 390, height: 480 });
  for (const control of [info, edit, remove]) await expect(control).toBeInViewport();
  expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(391);
  await info.click();
  await expect(close).toBeInViewport();
  const details = dialog.getByRole("group", { name: "Schedule details" });
  await details.getByText("Availability", { exact: true }).scrollIntoViewIfNeeded();
  await expect(details.getByText("Availability", { exact: true })).toBeInViewport();
  await expect(close).toBeInViewport();
  await close.click();
  await expect(info).toBeFocused();
});

test("PC Schedule Profile preserves single and multi-schedule confirmations", async ({ page }) => {
  await fixture(page, "pc");
  await page.goto("/container/1/graphs/3");
  await page.getByRole("button", { name: "Delete Schedule", exact: true }).click();
  const deleteConfirmation = page.getByRole("dialog", { name: "Delete schedule" });
  await expect(deleteConfirmation).toBeVisible();
  await deleteConfirmation.getByRole("button", { name: "Cancel", exact: true }).click();
  await page.getByRole("button", { name: "Edit Schedule", exact: true }).click();
  const editConfirmation = page.getByRole("dialog", { name: "Edit schedule" });
  await expect(editConfirmation).toContainText("Private schedule");
  await editConfirmation.getByRole("button", { name: "Edit", exact: true }).click();
  await expect(page).toHaveURL(/\/container\/1\/graphs\/3\/edit$/);

  await page.goto("/container/1/graphs/3?openGraphIds=3,6");
  await expect(page.getByRole("button", { name: "Edit Schedule", exact: true })).toHaveCount(0);
  await expect(page.getByRole("button", { name: "Delete Schedule", exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Edit all", exact: true }).click();
  const multiConfirmation = page.getByRole("dialog", { name: "Edit selected schedules" });
  await expect(multiConfirmation).toContainText("Private schedule");
  await expect(multiConfirmation).toContainText("Public schedule");
  await multiConfirmation.getByRole("button", { name: "Edit", exact: true }).click();
  await expect(page).toHaveURL(url =>
    url.pathname === "/container/1/graphs/3/edit" && url.searchParams.get("openGraphIds") === "3,6",
  );
});

const legalDestinations = ["/legal/index.html", "/legal/regulamin.html", "/legal/polityka-prywatnosci.html"];
async function legalFooter(page: Page, destinations: string[], name = "Legal documents") {
  const nav = page.getByRole("navigation", { name, exact: true });
  await expect(nav).toHaveCount(1);
  await expect(nav.getByRole("link")).toHaveCount(destinations.length);
  for (const href of destinations) await expect(nav.locator(`a[href="${href}"]`)).toHaveCount(1);
}

test("legal footer remains on login in both password input modes", async ({ page }) => {
  await fixture(page);
  await page.goto("/login");
  for (const mode of ["PC", "PIN", "PC"]) {
    await page.getByRole("group", { name: pl["Password input mode"] }).getByRole("button", { name: mode, exact: true }).click();
    await legalFooter(page, [...legalDestinations, "/legal/pliki-cookies.html"], pl["Legal documents"]);
  }
});

test("legal footer exists only on manager PC account profile", async ({ page }) => {
  test.setTimeout(60_000);
  await fixture(page, "pc", "manager");
  await page.goto("/manager-profile");
  await legalFooter(page, legalDestinations);
  for (const path of ["/", "/employee", "/shop", "/availability", "/container", "/container/1/graphs/3", "/communications"]) {
    await page.waitForLoadState("networkidle");
    await page.goto(path);
    await expect(page.locator("aside").first()).toBeVisible();
    await expect(page.getByRole("navigation", { name: "Legal documents", exact: true })).toHaveCount(0);
    const expand = page.getByRole("button", { name: "Open sidebar", exact: true });
    if (await expand.isVisible()) await expand.click();
    await page.getByRole("link", { name: "Open manager profile", exact: true }).click();
    await expect(page).toHaveURL(/\/manager-profile$/);
    await legalFooter(page, legalDestinations);
  }
});

test("legal footer exists only on employee account profile", async ({ page }) => {
  test.setTimeout(60_000);
  await fixture(page, "pc", "employee");
  await page.goto("/profile");
  await legalFooter(page, legalDestinations);
  for (const path of ["/", "/availability", "/schedule", "/swap"]) {
    await page.waitForLoadState("networkidle");
    await page.goto(path);
    await expect(page.locator("[data-employee-nav]:visible")).toHaveCount(1);
    await expect(page.getByRole("navigation", { name: "Legal documents", exact: true })).toHaveCount(0);
  }
  await page.locator("[data-employee-nav]:visible").getByRole("link", { name: "Profile", exact: true }).click();
  await expect(page).toHaveURL(/\/profile$/);
  await legalFooter(page, legalDestinations);
});

test("legal footer is absent from manager phone shell including More", async ({ page }) => {
  test.setTimeout(60_000);
  await fixture(page, "phone", "manager");
  for (const path of ["/", "/employee", "/shop", "/availability", "/container", "/more"]) {
    await page.waitForLoadState("networkidle");
    await page.goto(path);
    await expect(page.locator("[data-manager-phone]")).toBeVisible();
    await expect(page.getByRole("navigation", { name: "Legal documents", exact: true })).toHaveCount(0);
  }
});
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
async function navigation(page: Page) {
  const nav = page.getByRole("navigation", { name: "Manager navigation" });
  await expect(nav.getByRole("link")).toHaveCount(5);
  const current = nav.locator('[aria-current="page"]');
  await expect(current).toHaveCount(1);
  await expect(current.locator('span[aria-hidden="true"]')).toHaveCSS("opacity", "1");
  const glass = await nav.evaluate(element => ({ background: getComputedStyle(element).backgroundColor, filter: getComputedStyle(element).backdropFilter }));
  expect(glass.background).toBe("rgba(255, 255, 255, 0.16)");
  expect(glass.filter).toContain("blur(20px)");
  for (const link of await nav.getByRole("link").all()) {
    const box = await link.boundingBox(); expect(box!.height).toBeGreaterThanOrEqual(44);
    expect(box!.width).toBeGreaterThan(0);
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

test("Manager Phone navigation matches Employee navigation geometry", async ({ page, context }) => {
  const employeePage = await context.newPage();
  await fixture(page, "phone");
  await fixture(employeePage, "phone", "employee");
  await Promise.all([page.goto("/"), employeePage.goto("/")]);
  const manager = page.getByRole("navigation", { name: "Manager navigation" });
  const employee = employeePage.getByRole("navigation", { name: "Employee sections" }).last();
  const measure = (nav: Locator) => nav.evaluate(element => {
    const box = element.getBoundingClientRect();
    const size = (child: Element) => {
      const { width, height } = child.getBoundingClientRect();
      return { width, height };
    };
    return {
      bar: size(element), x: box.x, bottom: innerHeight - box.bottom, padding: getComputedStyle(element).padding,
      tabs: [...element.querySelectorAll("a")].map(size),
      icons: [...element.querySelectorAll("a svg")].map(size),
      activeGap: getComputedStyle(element.querySelector('[aria-current="page"]')!).gap,
      collapse: size(element.querySelector("button")!),
    };
  });
  for (const width of [320, 390, 768]) {
    await Promise.all([page.setViewportSize({ width, height: 844 }), employeePage.setViewportSize({ width, height: 844 })]);
    await expect(manager).toBeVisible();
    await expect(employee).toBeVisible();
    const reference = await measure(employee);
    await expect.poll(() => measure(manager)).toEqual(reference);
    expect(reference.tabs[0].width).toBeGreaterThan(reference.tabs[1].width);
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  }
  for (const browserPage of [page, employeePage]) {
    await browserPage.getByRole("button", { name: "Collapse navigation", exact: true }).click();
  }
  const managerOpen = page.getByRole("button", { name: "Open navigation", exact: true });
  const employeeOpen = employeePage.getByRole("button", { name: "Open navigation", exact: true });
  await expect(managerOpen).toBeFocused();
  await expect(employeeOpen).toBeFocused();
  await expect(managerOpen).toHaveCSS("opacity", "1");
  await expect(employeeOpen).toHaveCSS("opacity", "1");
  const managerBox = (await managerOpen.boundingBox())!, employeeBox = (await employeeOpen.boundingBox())!;
  for (const key of ["x", "y", "width", "height"] as const) {
    expect(managerBox[key]).toBeCloseTo(employeeBox[key], 1);
  }
});

test("login chooser selects original PC shell and edit controls", async ({ page }) => {
  const f = await fixture(page); await login(page);
  for (const name of [pcChoice, phoneChoice]) {
    const choice = page.getByRole("button", { name, exact: true });
    await expect(choice).toHaveCSS("box-shadow", "none");
    await expect(choice).toHaveCSS("border-top-color", "rgb(226, 232, 240)");
  }
  await expect(page.getByRole("button", { name: pcChoice, exact: true }).locator("svg")).toHaveCSS("transform", "matrix(1, 0, 0, -1, 0, 0)");
  await page.getByRole("button", { name: pcChoice, exact: true }).click();
  await page.waitForLoadState("networkidle"); await page.goto("/employee"); await expect(page.getByRole("button", { name: "Add New" })).toBeVisible(); await expect(page.locator("aside").first()).toBeVisible(); expect(f.errors).toEqual([]);
});
test("Phone switches both ways, refresh persists and back never exposes an editor", async ({ page }) => {
  const f = await fixture(page); await login(page);
  const holidaySettings = page.waitForResponse(response =>
    new URL(response.url()).pathname === "/api/public-holiday-settings" && response.ok());
  await page.getByRole("button", { name: phoneChoice, exact: true }).click();
  await expect(page.locator("[data-manager-phone]")).toBeVisible();
  await (await holidaySettings).finished();
  await page.waitForLoadState("networkidle");
  await page.reload();
  await expect(page.locator("[data-manager-phone]")).toBeVisible();
  await page.waitForLoadState("networkidle"); await page.getByRole("link", { name: "More", exact: true }).click(); await page.waitForLoadState("networkidle"); await page.getByRole("button", { name: /Switch to Desktop/ }).click(); await expect(page.locator("[data-manager-phone]")).toHaveCount(0); await page.waitForLoadState("networkidle");
  await page.evaluate(() => { history.pushState({}, "", "/employee/2/edit"); dispatchEvent(new PopStateEvent("popstate")); });
  await expect(page.getByRole("heading", { name: "Edit Employee", exact: true })).toBeVisible();
  const profile = page.getByRole("link", { name: "Open manager profile", exact: true });
  if (!await profile.isVisible()) await page.getByRole("button", { name: "Open sidebar", exact: true }).click();
  await profile.click();
  await expect(page).toHaveURL(/\/manager-profile$/);
  await collapseSidebarIfOpen(page);
  await page.getByRole("button", { name: /Switch to Phone/ }).click();
  await expect(page.locator("[data-manager-phone]")).toBeVisible(); await page.waitForLoadState("networkidle"); await page.goBack(); await expect(page.locator("[data-manager-phone]")).toBeVisible(); await page.waitForLoadState("networkidle"); expect(new URL(page.url()).pathname).not.toContain("/edit"); await noManagement(page); expect(f.errors).toEqual([]);
});
for (const width of [320, 375, 390, 430, 768, 1440]) {
  test(`Phone all manager data, matrix scrolling and shell at ${width}px`, async ({ page }, testInfo) => {
    test.setTimeout(60_000);
    await page.setViewportSize({ width, height: 850 }); const f = await fixture(page, "phone");
    await page.waitForLoadState("networkidle"); await page.goto("/"); await expect(page.getByRole("navigation", { name: "Manager navigation" })).toBeVisible(); await expect(page.getByRole("heading", { name: "Home", exact: true })).toBeVisible();
    await expect(page.getByRole("link", { name: /Back/ })).toHaveCount(0); await noOverflow(page); await navigation(page);
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
    await summary.locator("xpath=ancestor::section[1]").getByRole("button").click();
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
    await page.waitForLoadState("networkidle"); await page.goto("/availability"); await expect(page.getByRole("link", { name: "Back", exact: true })).toHaveAttribute("href", "/"); await noOverflow(page);
    await page.waitForLoadState("networkidle"); await page.goto("/employee"); await expect(page.getByText("Worker 1", { exact: true })).toBeVisible(); await expect(page.getByText("Worker 6", { exact: true })).toBeVisible(); await noOverflow(page);
    await page.getByText("Worker 1", { exact: true }).click(); await expect(page.getByRole("heading", { name: "Employee Profile", exact: true })).toBeVisible(); await noOverflow(page); await noManagement(page);
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
  await expect(page.getByRole("dialog", { name: "Required rules" })).toBeVisible(); await page.getByRole("checkbox").check(); await page.getByRole("button", { name: pl["Accept and continue"] }).click();
  await expect(page.getByRole("dialog")).toHaveCount(0); f.failExchange(true); await page.getByRole("button", { name: phoneChoice }).click(); await expect(page.getByRole("alert").filter({ hasText: pl["Mode change failed. Please try again."] })).toBeVisible();
  await expect(page.getByRole("heading", { name: pl["Choose workspace"] })).toBeVisible(); f.failExchange(false); await page.getByRole("button", { name: phoneChoice }).click(); await expect(page.locator("[data-manager-phone]")).toBeVisible();
  await page.waitForLoadState("networkidle"); await page.goto("/more"); f.failExchange(true); await page.getByRole("button", { name: /Switch to Desktop/ }).click(); await expect(page.locator("[data-manager-phone]")).toBeVisible(); await expect(page.getByRole("alert").filter({ hasText: "Mode change failed" })).toBeVisible();
  f.requireLegal(); await page.reload(); await expect(page.getByRole("dialog", { name: "Required rules" })).toBeVisible();
});
test("unsafe replay returns 403", async ({ page }) => {
  await fixture(page, "phone"); await page.waitForLoadState("networkidle"); await page.goto("/employee"); await expect(page.locator("[data-manager-phone]")).toBeVisible();
  const result = await page.evaluate(async () => { const r = await fetch("/api/employees", { method: "POST", headers: { Authorization: `Bearer ${localStorage.getItem("gf3.auth.access-token")}`, "Content-Type": "application/json" }, body: "{}" }); return { status: r.status, code: (await r.json()).code }; });
  expect(result).toEqual({ status: 403, code: "manager_phone_read_only" });
});
test("employee login never shows workspace choice", async ({ page }) => {
  await fixture(page, "guest", "employee"); await page.waitForLoadState("networkidle"); await page.goto("/login"); await expect(page.getByRole("button", { name: "PIN", exact: true })).toBeVisible();
  await page.getByRole("textbox", { name: pl.Username }).fill("worker"); await page.getByRole("textbox", { name: pl.Password }).fill("123456"); await page.getByRole("button", { name: pl["Sign in"], exact: true }).click();
  await expect(page.locator("nav[aria-label='Employee sections']:visible").first()).toBeVisible(); await expect(page.getByRole("heading", { name: pl["Choose workspace"] })).toHaveCount(0);
});

test("chooser keyboard focus and reduced motion; phone nav touch targets", async ({ page }, testInfo) => {
  await page.emulateMedia({ reducedMotion: "reduce" }); await fixture(page, "choose"); await page.waitForLoadState("networkidle"); await page.goto("/");
  const phone = page.getByRole("button", { name: /^(Phone Read only|Telefon Tylko podgląd)$/ }); await expect(phone).toBeVisible(); await phone.focus(); await expect(phone).toBeFocused();
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
 const logout = page.getByRole("button", { name: pl["Log out"], exact: true }); await expect(logout).toBeVisible();
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
 await page.goto("/container"); await expect(page.getByRole("button", { name: "Open Container nine profile" })).toBeVisible({ timeout: 10_000 });
 const titles = page.getByRole("button", { name: /^Open .* profile$/ }).locator("span[title]");
 await expect(titles).toHaveText(["Container nine", "Container four", "All manager container"]);
 await page.getByRole("button", { name: /Pin All manager container/ }).click(); await expect(titles).toHaveText(["All manager container", "Container nine", "Container four"]);
 const searchToggle = page.getByRole("button", { name: "Search", exact: true });
 await searchToggle.click();
 const search = page.getByRole("searchbox", { name: "Search", exact: true }); await search.fill("nine"); await expect(titles).toHaveText(["Container nine"]);
 await page.getByRole("button", { name: "Clear Search", exact: true }).click(); await expect(titles).toHaveCount(3);
 await page.getByRole("button", { name: "Open All manager container profile" }).click(); await expect(page.getByRole("heading", { name: "Statistics" })).toBeVisible();
 await expect(page.locator("main").getByText("Full manager records", { exact: true })).toHaveCount(0);
 const toolbar = searchToggle.locator("xpath=ancestor::header[1]");
 const firstSchedule = page.getByRole("link", { name: /Private schedule/ });
 const toolbarBox = (await toolbar.boundingBox())!;
 const scheduleBox = (await firstSchedule.boundingBox())!;
 expect(scheduleBox.y - toolbarBox.y - toolbarBox.height).toBeCloseTo(16, 0);
 await expect(firstSchedule).toHaveCSS("border-top-left-radius", "30px");
 await expect(firstSchedule.getByText("October 2026")).toBeVisible();
 await expect(firstSchedule.getByText("6 employees")).toBeVisible();
 await expect(firstSchedule.getByText("48h 0m")).toBeVisible();
 const stats = page.locator("[data-phone-container-statistics]"); await expect(stats.locator("table")).toHaveCount(0);
 await stats.getByRole("button", { name: "Statistics", exact: true }).click();
 await expect(stats.locator("article")).toHaveCount(7); await stats.locator("article").first().getByRole("button", { name: "Shops", exact: true }).click();
 await expect(stats.locator("article").first().getByText("Central", { exact: true })).toBeVisible(); await noOverflow(page);
 await page.setViewportSize({ width: 320, height: 560 });
 await noOverflow(page);
 expect(await firstSchedule.evaluate(card => card.scrollWidth <= card.clientWidth)).toBe(true);
 await page.evaluate(() => window.scrollTo(0, 400));
 await expect.poll(() => page.evaluate(() => window.scrollY)).toBeGreaterThan(200);
 await expect.poll(async () => (await toolbar.boundingBox())!.y).toBeCloseTo(toolbarBox.y, 0);
 await searchToggle.click();
 await search.fill("public");
 await expect(firstSchedule).toHaveCount(0);
 await expect(page.getByRole("link", { name: /Public schedule/ })).toHaveCount(1);
 await page.getByRole("link", { name: "Back", exact: true }).click(); await expect(page).toHaveURL(/\/container$/);
 await page.setViewportSize({ width: 390, height: 850 });
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
    const summary = page.locator("[data-phone-schedule-summary]"); await summary.locator("xpath=ancestor::section[1]").getByRole("button").click(); await expect(summary.locator(":scope > details")).toHaveCount(workerCount);
    expect(await summary.evaluate(element => element.scrollWidth <= element.clientWidth)).toBe(true); await expect(summary.locator("table")).toHaveCount(0);
    const search = page.getByRole("searchbox", { name: "Search schedule summary by employee name or surname" });
    await search.fill("surname 1"); await expect(summary.locator(":scope > details")).toHaveCount(1); await expect(summary).toContainText("Worker A very long employee surname 1");
    await summary.locator("summary").click(); await expect(summary).toContainText("08:00"); await expect(summary).toContainText("16:00"); await expect(summary.locator("details > summary dl dd").nth(3)).toHaveText("8");
    await search.fill("missing employee"); await expect(page.getByRole("status")).toContainText('No employees found');
    await search.fill(""); await expect(summary.locator(":scope > details")).toHaveCount(workerCount);
    const lastEmployee = summary.locator(":scope > details").last();
    const navigation = page.getByRole("navigation", { name: "Manager navigation" });
    await expect.poll(async () => {
     await page.evaluate(() => window.scrollTo(0, document.documentElement.scrollHeight));
     const last = (await lastEmployee.boundingBox())!;
     const nav = (await navigation.boundingBox())!;
     return last.y + last.height - nav.y;
    }).toBeLessThanOrEqual(0);
    if (width === 390 && workerCount === 8) await page.screenshot({ path: testInfo.outputPath("manager-phone-summary.png"), fullPage: true });
   } else {
    await expect(page.locator("[data-phone-matrix-scroll]")).toHaveCount(0); await expect(page.getByText("No employees are assigned to this schedule yet.")).toBeVisible();
   }
   await noOverflow(page); await noManagement(page); expect(f.unsafe).toEqual([]); expect(f.errors).toEqual([]);
  });
 }
}


test("PC schedule retains its desktop matrix dimensions and full summary table", async ({ page }) => {
 await page.setViewportSize({ width: 1440, height: 900 });
 await fixture(page, "pc", "manager", { longNames: true }); await page.goto("/container/1/graphs/3");
 await expect(page.getByText("Schedule Summary", { exact: true })).toBeVisible(); await page.waitForLoadState("networkidle");
 await expect(page.locator("[data-manager-phone], [data-phone-matrix-scroll], [data-phone-schedule-summary]")).toHaveCount(0);
 const table = page.locator('[class*="summaryTableScroll"] table'); await expect(table).toBeVisible();
 await expect(table.locator("thead th")).toHaveCount(130); await expect(table.locator("tbody")).toContainText("08:00");
 const matrixHeader = page.locator('table').first().locator("thead th").nth(1);
 expect((await matrixHeader.boundingBox())!.width).toBeGreaterThan(156);
});


for (const language of ["en", "pl"] as const) for (const width of [320, 375, 390, 430]) {
 test(`Phone profile refinements ${language} at ${width}px`, async ({ page }, testInfo) => {
  await page.setViewportSize({ width, height: 850 });
  const f = await fixture(page, "phone", "manager", { workerCount: 6, longNames: true, manualColumn: true, language });
  await page.route("**/api/availability-groups/5", route => route.fulfill({ contentType: "application/json", body: JSON.stringify({ id: 5, name: "DyspozycyjnoÃ…â€ºÃ„â€¡ BardzoDÃ…â€šugiePolskieNazwisko ZespoÃ…â€šu", year: 2026, month: 10, publicationStatus: "public", visibleFromUtc: "2026-10-01T08:00:00Z", visibleToUtc: "2026-10-31T18:00:00Z" }) }));
  await page.goto("/container/1/graphs/3");
  const information = page.locator("[data-phone-schedule-information]"); await expect(information).toBeVisible();
  const summary = page.locator("[data-phone-schedule-summary]"); await summary.locator("xpath=ancestor::section[1]").getByRole("button").click(); await expect(summary.locator(":scope > details")).toHaveCount(6);
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
  await page.getByRole("button", { name: language === "en" ? "Close related schedule" : pl["Close related schedule"] }).click();
  await noOverflow(page); await noManagement(page);
  await page.goto("/container/1"); const statistics = page.locator("[data-phone-container-statistics]");
  await statistics.getByRole("button", { name: language === "en" ? "Statistics" : pl.Statistics, exact: true }).click();
  await expect(statistics.locator("article")).toHaveCount(7);
  const firstCard = statistics.locator("article").first(); await expect(firstCard.locator("dl").first().locator("dd")).toHaveText(["1", "30", "0", "16"]);
  await firstCard.getByRole("button", { name: language === "en" ? "Shops" : pl.Shops, exact: true }).click();
  await expect(firstCard.getByText("Central", { exact: true })).toBeVisible(); await expect(firstCard.locator("dl").last().locator("dd")).toHaveText("16");
  const columns = await statistics.locator('[class*="_metrics_"]').evaluate(element => getComputedStyle(element).gridTemplateColumns.split(" ").length); expect(columns).toBe(width === 320 ? 1 : 2);
  await noOverflow(page); await noManagement(page);
  const total = statistics.locator("article").last();
  await expect.poll(async () => {
    await page.evaluate(() => window.scrollTo(0, document.documentElement.scrollHeight));
    const totalBox = (await total.boundingBox())!;
    const statisticsNav = (await page.getByRole("navigation").last().boundingBox())!;
    return totalBox.y + totalBox.height - statisticsNav.y;
  }).toBeLessThanOrEqual(0);
  if (width === 390) await page.screenshot({ path: testInfo.outputPath(`statistics-${language}.png`), fullPage: true });
  await page.getByRole("link", { name: /Back|Wstecz/, exact: true }).click(); await expect(page).toHaveURL(/\/container$/);
  await page.goto("/availability/5"); const availability = page.locator("[data-phone-availability-information]"); await expect(availability).toBeVisible();
  await expect(page.getByText(language === "en" ? "Availability Profile" : pl["Availability Profile"], { exact: true })).toBeVisible();
  await expect(page.locator("main details, main nav")).toHaveCount(0); await expect(availability).toContainText("DyspozycyjnoÃ…â€ºÃ„â€¡ BardzoDÃ…â€šugiePolskieNazwisko ZespoÃ…â€šu");
  await expect(availability).toContainText(language === "en" ? "Public" : pl.Public); await expect(availability).toContainText("2026");
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


for (const width of [320, 375, 390, 430]) test(`Phone Home schedule and controls fit at ${width}px`, async ({ page }) => {
 await page.setViewportSize({ width, height: 850 }); const f = await fixture(page, "phone"); await page.goto("/");
 const title = page.getByRole("heading", { name: "Home", exact: true }); await expect(title).toBeVisible();
 await expect(page.getByRole("region", { name: "Scheduled employees" })).toBeVisible();
 await expect(page.getByRole("group", { name: "Date" }).getByRole("button", { name: "Today" })).toBeVisible();
 await expect(page.getByRole("group", { name: "Filter by shop" }).getByRole("button", { name: "All" })).toBeVisible();
 await expect(page.getByRole("button", { name: "Notepad", exact: true })).toBeVisible();
 await noOverflow(page); await page.getByRole("link", { name: "Containers", exact: true }).click(); await expect(page).toHaveURL(/\/container$/);
 await page.getByRole("link", { name: "Home", exact: true }).click(); await expect(title).toBeVisible(); expect(f.errors).toEqual([]); expect(f.unsafe).toEqual([]);
});
for (const height of [720, 1000]) test(`Desktop profile switch is accessible and reaches phone Home at ${height}px`, async ({ page }) => {
 await page.setViewportSize({ width: 1440, height }); const f = await fixture(page, "pc"); await page.goto("/");
 const title = page.getByRole("heading", { name: "Home", exact: true }); await expect(title).toBeVisible();
 await page.waitForLoadState("networkidle");
 await page.goto("/manager-profile");
 await expect(page.locator("aside").getByRole("button", { name: /Switch to Phone|Log out/ })).toHaveCount(0);
 await collapseSidebarIfOpen(page);
 const card = page.locator('section[aria-labelledby="manager-account-actions-heading"]');
 const mode = card.getByRole("button", { name: "Switch to Phone", exact: true });
 await expect(mode).toBeVisible();
 await expect(mode).toContainText("Read only");
 expect((await mode.boundingBox())!.height).toBeGreaterThanOrEqual(44);
 await expect(mode.locator("svg")).toBeVisible();
 await page.keyboard.press("Tab"); await mode.focus(); await expect(mode).toBeFocused(); expect(await mode.evaluate(element => element.matches(":focus-visible"))).toBe(true);
 await mode.click(); await expect(page.locator("[data-manager-phone]")).toBeVisible(); await expect(title).toBeVisible();
 await page.getByRole("link", { name: "More", exact: true }).click(); await page.getByRole("button", { name: "Switch to Desktop", exact: true }).click();
 await expect(page.locator("[data-manager-phone]")).toHaveCount(0); await expect(title).toBeVisible(); expect(f.errors).toEqual([]);
});


test("PC Home restores data and interactions through PC Phone PC without login", async ({ page }) => {
 await page.setViewportSize({ width: 1440, height: 1000 });
 await page.clock.setFixedTime(new Date("2026-10-01T10:00:00Z"));
 const f = await fixture(page, "pc");
 const requests: string[] = [];
 page.on("request", request => requests.push(new URL(request.url()).pathname));
 await page.goto("/");
 await expect(page.getByRole("heading", { name: "Home", exact: true })).toBeVisible();
 await expect(page.locator("[data-manager-phone]")).toHaveCount(0);
 await collapseSidebarIfOpen(page);
 const today = page.locator("section").filter({ has: page.getByText("Who Works Today?", { exact: true }) });
 await expect(today.getByRole("table")).toBeVisible();
 const workerSchedules = today.getByRole("link", { name: /^Open schedule for Worker 1:/ });
 await expect(workerSchedules).toHaveCount(2);
 await expect(workerSchedules.nth(0)).toHaveAttribute("href", "/container/1/graphs/3");
 await expect(workerSchedules.nth(1)).toHaveAttribute("href", "/container/1/graphs/6");
 const dashboardDate = page.getByRole("group", { name: "Date", exact: true });
 await dashboardDate.getByRole("button", { name: "Next day" }).click();
 await expect(page.getByRole("heading", { name: "Who Works on 2 October 2026?" })).toBeVisible();
 await expect(page.getByText("No employees scheduled for this date.")).toBeVisible();
 await dashboardDate.getByRole("button", { name: "Today", exact: true }).click();
 await expect(workerSchedules).toHaveCount(2);
 const period = page.getByRole("group", { name: "Period", exact: true });
 await period.getByRole("button", { name: "Previous month" }).click();
 await expect(period.getByRole("button", { name: "Period", exact: true })).toHaveText("September 2026");
 await expect(workerSchedules).toHaveCount(2);
 await workerSchedules.first().click();
 await expect(page).toHaveURL(/\/container\/1\/graphs\/3$/);
 await page.goto("/");
 await expect(page.getByText("Who Works Today?", { exact: true })).toBeVisible();
 await page.waitForLoadState("networkidle");
 await page.goto("/manager-profile");
 await collapseSidebarIfOpen(page);
 await page.getByRole("button", { name: "Switch to Phone", exact: true }).click();
 const phone = page.locator("[data-manager-phone]");
 await expect(phone.getByRole("heading", { name: "Home", exact: true })).toBeVisible();
 await expect(phone.getByRole("heading", { name: "Who Works Today?", exact: true })).toBeVisible();
 await expect(phone.getByRole("tab", { name: /Requires Approval/ })).toHaveCount(0);
 await page.waitForLoadState("networkidle");
 const phoneStart = requests.length;
 await page.reload();
 await expect(phone.getByRole("heading", { name: "Home", exact: true })).toBeVisible();
 await page.waitForLoadState("networkidle");
 expect(requests.slice(phoneStart)).toEqual(expect.arrayContaining(["/api/containers", "/api/employees", "/api/shops"]));
 await page.getByRole("link", { name: "More", exact: true }).click();
 await page.getByRole("button", { name: "Switch to Desktop", exact: true }).click();
 await expect(page.getByRole("heading", { name: "Home", exact: true })).toBeVisible();
 await expect(today.getByRole("table")).toBeVisible();
 await expect(workerSchedules).toHaveCount(2);
 await expect(phone).toHaveCount(0);
 expect(requests.filter(path => path === "/api/auth/login")).toEqual([]);
 expect(f.errors).toEqual([]);
 expect(f.unsafe).toEqual([]);
});

test("PC profile actions preserve exchange failure, retry and phone More", async ({ page }) => {
  const f = await fixture(page, "pc");
  await page.goto("/manager-profile");
  const card = page.locator('section[aria-labelledby="manager-account-actions-heading"]');
  await expect(page.locator("aside").getByRole("button", { name: /Switch to Phone|Log out/ })).toHaveCount(0);
  await collapseSidebarIfOpen(page);
  await expect(card.getByRole("button", { name: "Log out", exact: true })).toBeVisible();
  for (const width of [320, 390, 768, 1440]) {
    await page.setViewportSize({ width, height: 900 });
    await expect(card).toBeVisible();
    for (const button of await card.getByRole("button").all()) {
      expect((await button.boundingBox())!.height).toBeGreaterThanOrEqual(44);
      expect(await button.evaluate(element => element.scrollWidth <= element.clientWidth)).toBe(true);
    }
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
    await page.screenshot({ path: test.info().outputPath(`profile-actions-${width}.png`) });
  }
  f.failExchange(true);
  await card.getByRole("button", { name: "Switch to Phone", exact: true }).click();
  await expect(card).toContainText("Mode change failed. Please try again.");
  await expect(page).toHaveURL(/\/manager-profile$/);
  await expect(page.locator("[data-manager-phone]")).toHaveCount(0);
  f.failExchange(false);
  await card.getByRole("button", { name: "Switch to Phone", exact: true }).click();
  await expect(page.locator("[data-manager-phone]")).toBeVisible();
  await page.getByRole("link", { name: "More", exact: true }).click();
  await expect(page.getByRole("button", { name: "Switch to Desktop", exact: true })).toBeVisible();
  await expect(page.getByRole("button", { name: "Log out", exact: true })).toBeVisible();
  expect(f.errors).toEqual([]);
});

test("PC profile failure keeps actions and local logout available", async ({ page }) => {
  await fixture(page, "pc");
  await page.route("**/api/manager-profile/me", route => route.fulfill({ status: 503, json: {} }));
  await page.route("**/api/auth/logout", route => route.fulfill({ status: 503, json: {} }));
  await page.goto("/manager-profile");
  await expect(page.getByText("Could not load manager profile.", { exact: true })).toBeVisible();
  await collapseSidebarIfOpen(page);
  const card = page.locator('section[aria-labelledby="manager-account-actions-heading"]');
  await expect(card.getByRole("button", { name: "Switch to Phone", exact: true })).toBeVisible();
  await card.getByRole("button", { name: "Log out", exact: true }).click();
  await expect(page).toHaveURL(/\/login$/);
});
