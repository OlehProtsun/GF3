import { readFileSync } from "node:fs";
import { expect, test, type Page } from "@playwright/test";
import type { EmployeeAvailabilityGroup } from "../src/entities/employee-availability/model/types";
import type { UrlopRequest } from "../src/entities/urlop/model/types";

async function choose(page: Page, name: string, option: string) {
  await page.getByRole("button", { name, exact: true }).click();
  await page.getByRole("option", { name: option, exact: true }).click();
}

async function fixture(page: Page, language = "en") {
  const state = { role: "manager", mode: "pc", isOpen: false, requests: [] as UrlopRequest[], failApprove: false, failSummaries: false, monthDelay: 0, failSave: false };
  const puts: unknown[] = [];
  const errors: string[] = [];
  page.on("pageerror", error => errors.push(error.message));
  await page.clock.setFixedTime(new Date("2026-11-01T10:00:00Z"));
  await page.addInitScript(() => localStorage.setItem("gf3.auth.access-token", "urlop-fixture"));
  const employees = [{ id: 7, employeeId: 7, firstName: "Ada", lastName: "Worker", displayName: "Ada Worker", hasLoginAccount: true, isOnline: false, displayOrder: 0 }, { id: 8, employeeId: 8, firstName: "Other", lastName: "Worker", displayName: "Other Worker", hasLoginAccount: true, isOnline: false, displayOrder: 1 }];
  const graph = { id: 10, containerId: 1, shopId: 1, shopName: "A", name: "November", year: 2026, month: 11, publicationStatus: "public", allowSwap: true,
    peoplePerShift: 1, shift1Time: "09:00 - 17:00", shift2Time: "", maxHoursPerEmpMonth: 160, maxConsecutiveDays: 5, maxConsecutiveFull: 3, maxFullPerMonth: 10, availabilityGroupId: null };
  const group: EmployeeAvailabilityGroup = { id: 5, name: "November availability", year: 2026, month: 11, canSubmit: true, slots: [],
    members: [{ employeeId: 7, firstName: "Ada", lastName: "Worker", displayOrder: 0, slots: [] }] };
  const approved = () => state.requests.filter(x => x.status === "approved").flatMap(x => x.days.map(day => ({ shopId: x.shopId, employeeId: x.employeeId, employeeName: x.employeeName, date: `${x.year}-${String(x.month).padStart(2, "0")}-${String(day).padStart(2, "0")}` })));
  const request = (body: { shopId: number; year: number; month: number; days: number[] }, status: UrlopRequest["status"]) => ({
    ...body, requestId: `00000000-0000-0000-0000-${String(state.requests.length + 1).padStart(12, "0")}`, status, employeeId: 7, employeeName: "Ada Worker",
    createdAtUtc: "2026-11-01T10:00:00Z", reviewedAtUtc: null, createdByManagerId: status === "approved" ? 1 : null, reviewedByManagerId: status === "approved" ? 1 : null,
  });
  const shops = [{ id: 1, name: "A", address: "Address" }, { id: 2, name: "B", address: "Address B" }];
  await page.route("**/api/**", async route => {
    const url = new URL(route.request().url()); const path = url.pathname; const method = route.request().method();
    if (!path.startsWith("/api/")) return route.continue();
    let body: unknown = []; let status = 200;
    if (path.includes("negotiate")) return route.fulfill({ status: 503 });
    if (path.includes("notepad")) return route.fulfill({ json: { notes: [], state: { isExpanded: false, isPinned: false, height: 700 } } });
    if (path === "/api/auth/session") body = { role: state.role, workspaceMode: state.mode, employeeId: state.role === "employee" ? 7 : null, managerId: 1, userName: "ada", displayName: "Ada Worker" };
    else if (path === "/api/account-language") body = { language };
    else if (path === "/api/shops") body = shops;
    else if (/^\/api\/shops\/\d+$/.test(path)) body = shops.find(shop => shop.id === Number(path.split("/").at(-1)));
    else if (path === "/api/employees") body = employees;
    else if (path === "/api/public-holiday-settings") body = { showPublicHolidays: true };
    else if (path === "/api/employee-urlop") body = { shops: [{ id: 1, name: "A" }, { id: 2, name: "B" }], months: [1, 2].flatMap(shopId => Array.from({ length: 12 }, (_, i) => {
      const month = (10 + i) % 12 + 1; const year = 2026 + Math.floor((10 + i) / 12);
      return { shopId, year, month, isOpen: shopId === 1 && state.isOpen, requests: state.requests.filter(x => x.shopId === shopId && x.year === year && x.month === month), approved: approved().filter(x => x.shopId === shopId && x.date.startsWith(`${year}-${String(month).padStart(2, "0")}`)) };
    })) };
    else if (path === "/api/employee-urlop/requests") {
      if (!state.isOpen) return route.fulfill({ status: 409, json: { detail: "Month closed by manager.", code: "urlop_month_closed" } });
      const result = request(route.request().postDataJSON(), "pending"); state.requests.push(result); body = result; status = 201;
    }
    else if (path === "/api/employee-urlop/approved") body = approved().filter(x => x.shopId === Number(url.searchParams.get("shopId")));
    else if (path === "/api/manager-urlop/approved") body = approved();
    else if (path === "/api/manager-urlop/shop-summaries") {
      if (state.failSummaries) return route.fulfill({ status: 503, json: { detail: "Counters unavailable" } });
      body = [...new Set(state.requests.filter(x => x.status !== "rejected").map(x => x.shopId))].map(shopId => ({
        shopId,
        approvedRequests: new Set(state.requests.filter(x => x.shopId === shopId && x.status === "approved").map(x => x.requestId)).size,
        pendingRequests: new Set(state.requests.filter(x => x.shopId === shopId && x.status === "pending").map(x => x.requestId)).size,
      }));
    }
    else if (path === "/api/manager-urlop") {
      const shopId = Number(url.searchParams.get("shopId"));
      const shop = shops.find(x => x.id === shopId);
      if (!shop) return route.fulfill({ status: 404, json: { detail: "Shop not found." } });
      const year = Number(url.searchParams.get("year")); const month = Number(url.searchParams.get("month"));
      body = { shopId, shopName: shop.name, year, month, isOpen: state.isOpen,
        requests: state.requests.filter(x => x.shopId === shopId && x.year === year && x.month === month),
        approvedDays: approved().filter(x => x.shopId === shopId && x.date.startsWith(`${year}-${String(month).padStart(2, "0")}`)) };
    }
    else if (path === "/api/manager-urlop/month") { state.isOpen = route.request().postDataJSON().isOpen; body = { ...route.request().postDataJSON() }; }
    else if (path === "/api/manager-urlop/requests") { if (state.failSave) return route.fulfill({ status: 409, json: { detail: "Grafik: 42", code: "urlop_shift_conflict", conflictingDates: ["2026-11-04", "2026-11-05"], scheduleIds: [42] } }); const value = request(route.request().postDataJSON(), "approved"); state.requests.push(value); body = value; status = 201; }
    else if (path.startsWith("/api/manager-urlop/requests/")) {
      const id = path.split("/")[4]; const value = state.requests.find(x => x.requestId === id)!;
      if (path.endsWith("/approve")) {
        if (state.failApprove) return route.fulfill({ status: 409, json: { detail: "Conflict with scheduled shift.", code: "urlop_shift_conflict", conflictingDates: ["2026-11-03"], scheduleIds: [10] } });
        value.status = "approved";
      } else if (path.endsWith("/reject")) value.status = "rejected";
      else if (method === "DELETE") { state.requests = state.requests.filter(x => x !== value); return route.fulfill({ status: 204 }); }
      else { if (state.failSave) return route.fulfill({ status: 409, json: { detail: "Grafik: 42", code: "urlop_shift_conflict", conflictingDates: ["2026-11-05"], scheduleIds: [42] } }); value.days = route.request().postDataJSON().days; }
      body = value;
    }
    else if (path === "/api/employee-availability") body = [group];
    else if (path === "/api/employee-availability/5/slots") { const payload = route.request().postDataJSON(); puts.push(payload); group.slots = payload.slots; body = group; }
    else if (path === "/api/employee-ui-state/availability-time-presets") body = { presets: null };
    else if (path === "/api/employee-ui-state") body = { scheduleColumnOrders: {} };
    else if (path === "/api/employee-schedules") body = [{ ...graph, containerName: "Container", employees, slots: [] }];
    else if (path === "/api/containers") body = [{ id: 1, name: "Container", note: "" }];
    else if (path === "/api/containers/1") body = { id: 1, name: "Container", note: "" };
    else if (path === "/api/containers/1/graphs") body = [graph];
    else if (path === "/api/containers/1/graphs/10") body = graph;
    else if (path === "/api/containers/1/graphs/10/employees") body = [{ id: 1, scheduleId: 10, employeeId: 7, minHoursMonth: 0, displayOrder: 0 }];
    else if (path === "/api/containers/1/graphs/10/versions") body = { versions: [], currentVersionId: null };
    if (path === "/api/manager-urlop" && state.monthDelay) await new Promise(resolve => setTimeout(resolve, state.monthDelay));
    await route.fulfill({ status, contentType: "application/json", body: JSON.stringify(body) });
  });
  return { state, shops, puts, errors, open: async (role: string, path: string) => {
    await page.waitForLoadState("networkidle");
    state.role = role; await page.goto(path);
    if (role === "manager" && state.mode === "pc") await page.getByRole("button", { name: language === "pl" ? "Zwiń pasek boczny" : language === "uk" ? "Згорнути бічну панель" : "Collapse sidebar", exact: true }).click();
  } };
}

test("open, submit Pending, approve, UR overlays and availability save remain independent", async ({ page }) => {
  test.setTimeout(60_000);
  const f = await fixture(page);
  await f.open("manager", "/urlop");
  await expect(page.getByRole("button", { name: "Open requests", exact: true })).toHaveCount(0);
  await page.getByRole("button", { name: "Open vacation management for A", exact: true }).click();
  await expect(page).toHaveURL(/\/urlop\/1$/);
  await page.getByRole("button", { name: "Open requests", exact: true }).click();
  await expect(page.getByText("Open for requests", { exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Back", exact: true }).click();
  await expect(page).toHaveURL(/\/urlop$/);
  await expect(page.getByRole("button", { name: "Open vacation management for A", exact: true })).toBeVisible();
  await f.open("employee", "/availability");
  await page.getByRole("tab", { name: "Urlop", exact: true }).click();
  const panel = page.getByRole("tabpanel", { name: "Urlop", exact: true });
  await panel.getByRole("button", { name: "Month", exact: true }).click();
  await expect(page.getByRole("listbox", { name: "Month", exact: true }).getByRole("option")).toHaveCount(12);
  await page.keyboard.press("Escape");
  for (const day of [3, 4, 5]) await panel.getByRole("button", { name: `2026-11-0${day}`, exact: true }).click();
  const refreshedAfterSubmit = page.waitForResponse(response => new URL(response.url()).pathname === "/api/employee-availability" && response.request().method() === "GET");
  await panel.getByRole("button", { name: "Submit request" }).click();
  await expect(panel.locator('[data-urlop-status="pending"]')).toBeVisible();
  await refreshedAfterSubmit;
  await expect(panel.getByText("UR", { exact: true })).toHaveCount(0);
  await f.open("manager", "/urlop/1");
  await page.getByRole("button", { name: "Approve", exact: true }).click();
  await expect(page.getByRole("button", { name: "2026-11-03 UR: Ada Worker", exact: true })).toBeVisible();
  await expect(page.getByLabel("Pending: 0", { exact: true })).toBeVisible();
  await page.getByRole("button", { name: "All", exact: true }).click();
  await expect(page.getByText("Approved", { exact: true })).toBeVisible();
  await f.open("employee", "/availability");
  await page.getByRole("button", { name: /^November availability / }).click();
  await expect(page.locator('[data-own-availability-cell="3"]')).toHaveText("UR");
  await expect(page.locator('[data-own-availability-cell="3"]')).toHaveAttribute("title", "UR (A)");
  await page.locator('[data-own-availability-cell="6"]').click();
  await page.getByRole("dialog").getByRole("radio", { name: /^Available/ }).click();
  await page.getByRole("dialog").getByRole("button", { name: "Save", exact: true }).click();
  await page.getByRole("tab", { name: "Urlop", exact: true }).click();
  await page.getByRole("tab", { name: "Availability", exact: true }).click();
  await expect(page.locator('[data-own-availability-cell="6"]')).toHaveText("+");
  const refreshedAfterSave = page.waitForResponse(response => new URL(response.url()).pathname === "/api/employee-availability" && response.request().method() === "GET");
  await page.getByRole("button", { name: "Save availability", exact: true }).click();
  await expect.poll(() => f.puts.length).toBe(1);
  await refreshedAfterSave;
  await expect(page.getByRole("button", { name: "Save availability", exact: true })).toBeEnabled();
  expect(JSON.stringify(f.puts)).not.toContain("UR");
  await expect(page.locator('[data-own-availability-cell="3"]')).toHaveText("UR");
  await f.open("employee", "/schedule");
  await expect(page.getByRole("table").getByText("UR", { exact: true })).toHaveCount(3);
  await f.open("manager", "/container/1/graphs/10");
  await page.getByText("Schedule Summary", { exact: true }).scrollIntoViewIfNeeded();
  await expect(page.getByRole("columnheader", { name: "Urlop Days", exact: true })).toBeVisible();
  await expect(page.getByText("UR", { exact: true }).first()).toBeVisible();
  expect(f.errors).toEqual([]);
});

test("PC CRUD, rejection and explicit retry after 409", async ({ page }) => {
  const f = await fixture(page); f.state.isOpen = true;
  f.state.requests = [{ requestId: "00000000-0000-0000-0000-000000000001", employeeId: 7, employeeName: "Ada Worker", shopId: 1, year: 2026, month: 11, days: [3], status: "pending", createdAtUtc: "2026-11-01T10:00:00Z", reviewedAtUtc: null, createdByManagerId: null, reviewedByManagerId: null }];
  f.state.failApprove = true; await f.open("manager", "/urlop/1");
  await page.getByRole("button", { name: "Approve", exact: true }).click();
  await expect(page.getByRole("alert").filter({ hasText: "3 November 2026" })).toBeVisible();
  expect(f.state.requests[0].status).toBe("pending");
  f.state.failApprove = false;
  await page.getByRole("button", { name: "Reject", exact: true }).click();
  await page.getByRole("button", { name: "All", exact: true }).click();
  await expect(page.getByText("Rejected", { exact: true })).toBeVisible();
  await expect(page.getByRole("button", { name: "Edit dates", exact: true })).toHaveCount(0);
  await expect(page.getByRole("button", { name: "2026-11-03", exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Close requests", exact: true }).click();
  await expect(page.getByText("Month closed by manager.", { exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Add vacation", exact: true }).click();
  await page.getByRole("dialog", { name: "Add vacation", exact: true }).getByRole("button", { name: "2026-11-04", exact: true }).click();
  await page.getByRole("button", { name: "Create approved vacation", exact: true }).click();
  await expect(page.getByText("Approved", { exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Edit dates", exact: true }).click();
  await page.getByRole("dialog", { name: "Edit dates", exact: true }).getByRole("button", { name: /2026-11-04/ }).click();
  await page.getByRole("dialog", { name: "Edit dates", exact: true }).getByRole("button", { name: "2026-11-05", exact: true }).click();
  await page.getByRole("button", { name: "Save", exact: true }).click();
  await expect.poll(() => f.state.requests[1].days).toEqual([5]);
  await page.getByRole("button", { name: "Delete", exact: true }).last().click();
  await page.getByRole("dialog", { name: "Delete vacation request?" }).getByRole("button", { name: "Confirm", exact: true }).click();
  await expect(page.getByText("Approved", { exact: true })).toHaveCount(0);
  await expect(page.getByText("Month closed by manager.", { exact: true })).toBeVisible();
  await f.open("employee", "/availability"); await page.getByRole("tab", { name: "Urlop", exact: true }).click();
  await expect(page.getByRole("button", { name: "Submit request" })).toBeDisabled();
  expect(f.errors).toEqual([]);
});

for (const language of ["pl", "uk"]) test(`employee narrow calendar, keyboard and ${language} labels`, async ({ page }) => {
  await page.setViewportSize({ width: 320, height: 740 });
  const f = await fixture(page, language); f.state.isOpen = true;
  await f.open("employee", "/availability");
  await page.getByRole("tab", { name: language === "pl" ? "Urlop" : "Відпустки", exact: true }).click();
  const day = page.getByRole("button", { name: "2026-11-03", exact: true });
  await day.focus(); await day.press("Enter"); await expect(day).toHaveAttribute("aria-pressed", "true");
  await expect(page.getByText(language === "pl" ? "Otwarte na wnioski" : "Відкрито для заявок", { exact: true })).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
  await page.screenshot({ path: test.info().outputPath(`urlop-${language}-320.png`), animations: "disabled" });
  expect(f.errors).toEqual([]);
});

test("manager phone cannot reach the Urlop CRUD route", async ({ page }) => {
  const f = await fixture(page); f.state.mode = "phone";
  await f.open("manager", "/urlop");
  await expect(page).toHaveURL(/\/$/);
  await expect(page.getByRole("status", { name: "Loading page" })).toHaveCount(0);
  await expect(page.getByRole("button", { name: "Create approved vacation", exact: true })).toHaveCount(0);
  await page.goto("/urlop/1");
  await expect(page).toHaveURL(/\/$/);
  await expect(page.getByRole("status", { name: "Loading page" })).toHaveCount(0);
  await expect(page.getByRole("button", { name: "Create approved vacation", exact: true })).toHaveCount(0);
  expect(f.errors).toEqual([]);
});

test("shop list counters, search, keyboard, deep links and dirty browser Back", async ({ page }) => {
  const f = await fixture(page);
  f.state.requests = [{ requestId: "00000000-0000-0000-0000-000000000001", employeeId: 7, employeeName: "Ada Worker", shopId: 1,
    year: 2025, month: 11, days: [3, 4, 5], status: "approved", createdAtUtc: "2025-11-01T10:00:00Z",
    reviewedAtUtc: null, createdByManagerId: 1, reviewedByManagerId: 1 }];
  await f.open("manager", "/urlop");
  const a = page.getByRole("button", { name: "Open vacation management for A", exact: true });
  const b = page.getByRole("button", { name: "Open vacation management for B", exact: true });
  await expect(a).toContainText("1Approved vacations");
  await expect(b).toContainText("0Approved vacations");
  await expect(page.getByLabel("Month", { exact: true })).toHaveCount(0);
  await page.getByRole("textbox", { name: "Search vacation shop" }).fill("address b");
  await expect(a).toHaveCount(0); await expect(b).toBeVisible();
  await page.getByRole("textbox", { name: "Search vacation shop" }).fill("missing");
  await expect(page.getByText("Nothing found", { exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Clear Search" }).click();
  await b.focus(); await b.press("Enter");
  await expect(page).toHaveURL(/\/urlop\/2$/);
  await expect(page.getByRole("heading", { name: "B", exact: true })).toBeVisible();
  await page.reload();
  await expect(page.getByRole("heading", { name: "B", exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Collapse sidebar", exact: true }).click();
  await page.getByRole("button", { name: "Add vacation", exact: true }).click();
  await page.getByRole("dialog", { name: "Add vacation", exact: true }).getByRole("button", { name: "2026-11-04", exact: true }).click();
  await page.goBack();
  const dialog = page.getByRole("dialog", { name: "Leave without saving?" });
  await expect(dialog).toBeVisible();
  await dialog.getByRole("button", { name: "Stay here" }).click();
  await expect(page).toHaveURL(/\/urlop\/2$/);
  await expect(page.getByRole("dialog", { name: "Add vacation", exact: true }).getByRole("button", { name: "2026-11-04", exact: true })).toHaveAttribute("aria-pressed", "true");
  await page.getByRole("dialog", { name: "Add vacation", exact: true }).getByRole("button", { name: "Cancel", exact: true }).click();
  await dialog.getByRole("button", { name: "Stay here" }).click();
  await expect(page).toHaveURL(/\/urlop\/2$/);
  await page.getByRole("button", { name: "Create approved vacation", exact: true }).click();
  await expect.poll(() => f.state.requests.length).toBe(2);
  expect(f.state.requests[1].shopId).toBe(2);
  await page.getByRole("button", { name: "Back", exact: true }).click();
  await expect(page).toHaveURL(/\/urlop$/);
  await expect(b).toContainText("1Approved vacations");
  expect(f.errors).toEqual([]);
});

test("summary failure retains selectable cards and retry restores counts", async ({ page }) => {
  const f = await fixture(page); f.state.failSummaries = true;
  await f.open("manager", "/urlop");
  const card = page.getByRole("button", { name: "Open vacation management for A", exact: true });
  await expect(page.getByRole("alert").filter({ hasText: "Vacation counters unavailable." })).toBeVisible();
  await expect(card).toContainText("—Approved vacations");
  f.state.failSummaries = false;
  await page.getByRole("button", { name: "Retry", exact: true }).click();
  await expect(card).toContainText("0Approved vacations");
  await card.focus(); await card.press("Space");
  await expect(page).toHaveURL(/\/urlop\/1$/);
  expect(f.errors).toEqual([]);
});

test("pastel shop list stays within desktop and narrow viewports with long real fields", async ({ page }) => {
  test.setTimeout(60_000);
  const f = await fixture(page);
  f.shops.push(...Array.from({ length: 4 }, (_, i) => ({ id: i + 3, name: `Shop ${i + 3}`, address: `Address ${i + 3}` })));
  f.shops[1] = { id: 2, name: "Long shop name ".repeat(10), address: "VeryLongAddressWithoutSpaces".repeat(10) };
  await f.open("manager", "/urlop");
  await expect(page.getByRole("heading", { name: "Choose a shop", exact: true })).toHaveCount(0);
  await expect(page.getByText("View approved vacations and manage requests for each shop.", { exact: true })).toHaveCount(0);
  for (const width of [320, 375, 768, 1024, 1440, 1700]) {
    await page.setViewportSize({ width, height: 980 });
    await expect.poll(async () => {
      const firstCard = await page.getByRole("button", { name: "Open vacation management for A", exact: true }).boundingBox();
      const header = await page.locator("header").filter({ has: page.getByRole("heading", { name: "Urlop", exact: true }) }).boundingBox();
      return !!firstCard && !!header && firstCard.y >= header.y + header.height;
    }).toBe(true);
    const cards = page.getByRole("button", { name: /^Open vacation management for / });
    await expect(cards).toHaveCount(6);
    await expect(cards.first()).toBeVisible();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
    const boxes = await cards.evaluateAll(elements => elements.map(element => {
      const rect = element.getBoundingClientRect();
      return { left: rect.left, right: rect.right, top: rect.top, width: rect.width, height: rect.height };
    }));
    expect(boxes.every(box => box.left >= 0 && box.right <= width && box.width > 0)).toBe(true);
    expect(boxes.filter(box => Math.abs(box.top - boxes[0].top) <= 3)).toHaveLength(width > 1100 ? 3 : width > 680 ? 2 : 1);
    if (width === 1440) {
      expect(boxes[0].height).toBeGreaterThanOrEqual(201);
      expect(boxes[0].height).toBeLessThanOrEqual(215);
    }
    const orientations = await cards.evaluateAll(elements => elements.map(card => {
      const icons = card.querySelectorAll("svg");
      return {
        storefront: getComputedStyle(icons[0]).transform,
        badge: getComputedStyle(icons[0].parentElement!).transform,
        arrow: getComputedStyle(icons[icons.length - 1]).transform,
      };
    }));
    expect(orientations).toEqual(Array.from({ length: 6 }, () => ({
      storefront: "matrix(1, 0, 0, -1, 0, 0)", badge: "none", arrow: "none",
    })));
    await page.screenshot({ path: test.info().outputPath(`urlop-shops-${width}.png`), animations: "disabled", fullPage: true });
  }
  await page.emulateMedia({ reducedMotion: "reduce" });
  const card = page.getByRole("button", { name: "Open vacation management for A", exact: true });
  await card.focus();
  await page.keyboard.press("Tab"); await page.keyboard.press("Shift+Tab");
  await expect(card).toBeFocused();
  expect(await card.evaluate(element => getComputedStyle(element).outlineStyle)).toBe("solid");
  expect(await card.evaluate(element => getComputedStyle(element).transitionDuration)).toBe("0s");
  expect(await card.locator('[aria-hidden="true"]').count()).toBeGreaterThan(0);
  expect(f.errors).toEqual([]);
});

test("monthly dashboard overflow, exact dates, navigation and accessible modal across viewports", async ({ page }) => {
  test.setTimeout(180_000);
  const f = await fixture(page);
  const base: UrlopRequest = { requestId: "overflow-0", employeeId: 7, employeeName: "Ada Worker", shopId: 1, year: 2026, month: 11, days: [3], status: "approved", createdAtUtc: "2026-11-01T10:00:00Z", reviewedAtUtc: null, createdByManagerId: 1, reviewedByManagerId: 1 };
  f.state.requests = [0,1,2,3].map(i => ({ ...base, requestId: `approved-${i}`, employeeId: 7 + i, employeeName: `Real employee ${i}` }));
  f.state.requests.push(...Array.from({ length: 16 }, (_, i) => ({ ...base, requestId: `pending-${i}`, status: "pending" as const, days: [3,5,7], employeeName: `Pending employee ${i}`, createdByManagerId: null })));
  await f.open("manager", "/urlop/1");
  await expect(page.getByRole("heading", { name: "Monthly vacation schedule" })).toBeVisible();
  await expect(page.getByRole("combobox", { name: "Employee", exact: true })).toHaveCount(0);
  await expect(page.getByText("4 employees on vacation", { exact: true })).toBeVisible();
  await expect(page.getByText("Address", { exact: true })).toBeVisible();
  await expect(page.getByLabel("Pending: 16", { exact: true })).toBeVisible();
  const day = page.getByRole("button", { name: "2026-11-03 UR: Real employee 0, Real employee 1, Real employee 2, Real employee 3", exact: true });
  await expect(day.getByLabel("+2 more", { exact: true })).toBeVisible();
  await day.focus(); await day.press("Enter");
  const tray = page.getByRole("region", { name: "Approved vacations on 2026-11-03", exact: true });
  for (const i of [0,1,2,3]) await expect(tray.getByText(`Real employee ${i}`, { exact: true })).toBeVisible();
  await tray.getByRole("button", { name: "Close", exact: true }).click();
  await day.press("Space"); await expect(tray).toBeVisible(); await day.press("Escape"); await expect(tray).toHaveCount(0);
  const panel = page.getByRole("region", { name: "Vacation requests", exact: true });
  await expect(panel.getByRole("article").first()).toContainText("3, 5, 7 November 2026");
  await expect(panel.getByRole("article").first()).toContainText("3 days");
  for (const width of [1440,1024,768,390,375,320]) {
    await page.setViewportSize({ width, height: 980 });
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
    await expect(page.getByRole("button", { name: "Add vacation", exact: true })).toBeVisible();
    expect(await page.getByRole("button", { name: "Year", exact: true }).evaluate(element => { const label = element.querySelector("span")!; return label.scrollWidth <= label.clientWidth; })).toBe(true);
    expect(await day.evaluate(element => { const grid = element.parentElement!; return grid.scrollWidth <= grid.clientWidth; })).toBe(true);
    const calendarBox = await page.getByRole("region", { name: "Vacation calendar", exact: true }).boundingBox();
    const panelBox = await panel.boundingBox();
    if (width <= 1050) expect(panelBox!.y).toBeGreaterThan(calendarBox!.y);
    else expect(panelBox!.x).toBeGreaterThan(calendarBox!.x);
    await page.evaluate(() => window.scrollTo(0, 0));
    await expect.poll(async () => {
      const shop = await page.getByRole("heading", { name: "A", exact: true }).boundingBox();
      const header = await page.locator("header").filter({ has: page.getByRole("heading", { name: "Urlop", exact: true }) }).boundingBox();
      return !!shop && !!header && shop.y >= header.y + header.height;
    }).toBe(true);
    await page.screenshot({ path: test.info().outputPath(`urlop-overview-${width}.png`), fullPage: true, scale: "css", animations: "disabled" });
    const add = page.getByRole("button", { name: "Add vacation", exact: true });
    await add.click();
    const editor = page.getByRole("dialog", { name: "Add vacation", exact: true });
    const employee = editor.getByRole("button", { name: "Employee", exact: true });
    await expect(employee).toBeFocused();
    await employee.press("Shift+Tab");
    await expect(editor.getByRole("button", { name: "Cancel", exact: true })).toBeFocused();
    await page.keyboard.press("Tab"); await expect(employee).toBeFocused();
    await expect(editor.getByRole("button", { name: "Create approved vacation", exact: true })).toBeVisible();
    await page.screenshot({ path: test.info().outputPath(`urlop-editor-${width}.png`), scale: "css", animations: "disabled" });
    await employee.press("Escape"); await expect(editor).toHaveCount(0); await expect(add).toBeFocused();
  }
  await page.setViewportSize({ width: 1440, height: 980 });
  await page.getByRole("button", { name: "Open sidebar", exact: true }).click();
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
  await page.evaluate(() => window.scrollTo(0, 0));
  await page.screenshot({ path: test.info().outputPath("urlop-overview-sidebar-expanded.png"), fullPage: true, scale: "css", animations: "disabled" });
  await page.getByRole("button", { name: "Collapse sidebar", exact: true }).click();
  await choose(page, "Month", "December");
  await page.getByRole("button", { name: "Next month", exact: true }).click();
  await expect(page.getByRole("button", { name: "Year", exact: true })).toHaveText("2027");
  await expect(page.getByRole("button", { name: "Month", exact: true })).toHaveText("January");
  await page.getByRole("button", { name: "Previous month", exact: true }).click();
  await expect(page.getByRole("button", { name: "Year", exact: true })).toHaveText("2026");
  await expect(page.getByRole("button", { name: "Month", exact: true })).toHaveText("December");
  await choose(page, "Year", "2030");
  await expect(page.getByRole("heading", { name: "December 2030", exact: true })).toBeVisible();
  expect(f.errors).toEqual([]);
});


test("delayed and rapid month navigation keeps dashboard, focus, scroll and current data", async ({ page, isMobile, browserName }) => {
  test.setTimeout(60_000);
  const f = await fixture(page);
  f.state.requests = [{ requestId: "approved-nov", employeeId: 7, employeeName: "November only", shopId: 1, year: 2026, month: 11, days: [3], status: "approved", createdAtUtc: "2026-11-01T10:00:00Z", reviewedAtUtc: null, createdByManagerId: 1, reviewedByManagerId: 1 }];
  await page.setViewportSize({ width: 1440, height: 720 });
  await f.open("manager", "/urlop/1");
  const calendar = page.getByRole("region", { name: "Vacation calendar", exact: true });
  const requests = page.getByRole("region", { name: "Vacation requests", exact: true });
  await expect(calendar.getByRole("button", { name: /November only/ })).toBeVisible();
  await page.getByRole("button", { name: "All", exact: true }).click();
  const initialHeight = (await calendar.boundingBox())!.height;
  await calendar.evaluate(element => element.setAttribute("data-persistent", "true"));
  await page.evaluate(() => window.scrollTo({ top: 100, behavior: "instant" }));
  const initialY = await page.evaluate(() => window.scrollY);
  expect(initialY).toBeGreaterThan(0);
  f.state.monthDelay = 600;
  const url = page.url();
  let navigations = 0;
  page.on("framenavigated", frame => { if (frame === page.mainFrame()) navigations++; });
  const next = page.getByRole("button", { name: "Next month", exact: true });
  await next.click();
  await expect(calendar).toHaveAttribute("aria-busy", "true");
  await expect(page.getByText("November only", { exact: true })).toHaveCount(0);
  await expect(calendar).toHaveAttribute("data-persistent", "true");
  expect((await calendar.boundingBox())!.height).toBe(initialHeight);
  await next.click();
  await expect(page.getByRole("heading", { name: "January 2027", exact: true })).toBeVisible();
  await expect(calendar).toHaveAttribute("aria-busy", "false");
  await expect(requests.getByRole("button", { name: "All", exact: true })).toHaveAttribute("aria-pressed", "true");
  await expect(next).toBeFocused();
  await expect(page).toHaveURL(url); expect(navigations).toBe(0);
  expect(Math.abs((await page.evaluate(() => window.scrollY)) - initialY)).toBeLessThanOrEqual(4);
  await expect(page.locator("html")).toHaveClass(/page-scrollbar-hidden/);
  expect(await page.evaluate(() => getComputedStyle(document.documentElement).scrollbarWidth)).toBe("none");
  expect(await page.evaluate(() => getComputedStyle(document.body).overflowY)).not.toBe("hidden");
  if (isMobile && browserName === "webkit") await page.keyboard.press("PageDown");
  else { await page.mouse.move(300, 400); await page.mouse.wheel(0, 160); }
  await expect.poll(() => page.evaluate(() => window.scrollY)).toBeGreaterThan(initialY);
  await choose(page, "Month", "February");
  await expect(calendar).toHaveAttribute("aria-busy", "false");
  await expect(page.getByRole("button", { name: "Month", exact: true })).toBeFocused();
  await choose(page, "Year", "2028");
  await expect(page.getByRole("button", { name: "2028-02-29", exact: true })).toBeVisible();
  await expect(calendar).toHaveAttribute("data-persistent", "true");
  expect(f.errors).toEqual([]);
});

test("portal employee keyboard focus, dirty confirmation and failed writes preserve draft", async ({ page }) => {
  const f = await fixture(page); await f.open("manager", "/urlop/1");
  const add = page.getByRole("button", { name: "Add vacation", exact: true }); await add.click();
  const editor = page.getByRole("dialog", { name: "Add vacation", exact: true });
  const employee = editor.getByRole("button", { name: "Employee", exact: true });
  await employee.press("Enter");
  const search = page.getByRole("textbox", { name: "Search...", exact: true });
  await expect(search).toBeFocused();
  await expect(page.getByRole("option", { name: "Other Worker", exact: true })).toBeVisible();
  const popup = await page.getByRole("listbox", { name: "Employee" }).boundingBox();
  expect(popup!.x).toBeGreaterThanOrEqual(0); expect(popup!.x + popup!.width).toBeLessThanOrEqual(1280);
  await search.press("Shift+Tab"); await expect(page.getByRole("option", { name: "Other Worker", exact: true })).toBeFocused();
  await page.keyboard.press("Tab"); await expect(search).toBeFocused();
  await search.press("Escape"); await expect(employee).toBeFocused(); await expect(editor).toBeVisible();
  const day = editor.getByRole("button", { name: "2026-11-04", exact: true }); await day.click();
  await choose(page, "Employee", "Other Worker");
  await page.getByRole("button", { name: "Stay here", exact: true }).click();
  await expect(employee).toContainText("Ada Worker"); await expect(day).toHaveAttribute("aria-pressed", "true");
  f.state.failSave = true;
  await editor.getByRole("button", { name: "Create approved vacation", exact: true }).click();
  await expect(editor.getByRole("alert")).toContainText("4–5 November 2026");
  await expect(editor.getByRole("alert")).not.toContainText("42");
  await expect(day).toHaveAttribute("aria-pressed", "true"); expect(f.state.requests).toEqual([]);
  f.state.failSave = false;
  await editor.getByRole("button", { name: "Create approved vacation", exact: true }).click();
  await expect(editor).toHaveCount(0); await expect(add).toBeFocused();
  await page.getByRole("button", { name: "All", exact: true }).click();
  await page.getByRole("button", { name: "Edit dates", exact: true }).click();
  const edit = page.getByRole("dialog", { name: "Edit dates", exact: true });
  await expect(edit.getByRole("button", { name: "Employee", exact: true })).toBeDisabled();
  await edit.getByRole("button", { name: "2026-11-05", exact: true }).click();
  f.state.failSave = true; await edit.getByRole("button", { name: "Save", exact: true }).click();
  await expect(edit.getByRole("alert")).toContainText("5 November 2026");
  expect(f.state.requests[0].days).toEqual([4]); expect(f.errors).toEqual([]);
});


for (const width of [320, 390, 768]) test(`month changes keep real page scrolling at ${width}px`, async ({ page }) => {
  await page.setViewportSize({ width, height: 740 });
  const f = await fixture(page); await f.open("manager", "/urlop/1");
  await expect(page.getByRole("button", { name: "Add vacation", exact: true })).toBeEnabled();
  await page.evaluate(() => window.scrollTo({ top: 180, behavior: "instant" }));
  const before = await page.evaluate(() => window.scrollY); expect(before).toBeGreaterThan(0);
  f.state.monthDelay = 300;
  await page.getByRole("button", { name: "Next month", exact: true }).click();
  await expect(page.getByRole("heading", { name: "December 2026", exact: true })).toBeVisible();
  await expect(page.getByRole("region", { name: "Vacation calendar", exact: true })).toHaveAttribute("aria-busy", "false");
  expect(Math.abs((await page.evaluate(() => window.scrollY)) - before)).toBeLessThanOrEqual(4);
  await page.keyboard.press("PageDown");
  await expect.poll(() => page.evaluate(() => window.scrollY)).toBeGreaterThan(before);
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
  expect(f.errors).toEqual([]);
});

for (const language of ["pl", "uk"] as const) test(`manager localized dropdowns and conflict advice in ${language}`, async ({ page }) => {
  const f = await fixture(page, language); await f.open("manager", "/urlop/1");
  const labels = language === "pl" ? { month: "Miesiąc", december: "grudzień", november: "listopad", add: "Dodaj urlop", employee: "Pracownik", create: "Dodaj zatwierdzony urlop", date: "4–5 listopada 2026", advice: "Zmień lub usuń" } : { month: "Місяць", december: "грудень", november: "листопад", add: "Додати відпустку", employee: "Працівник", create: "Створити затверджену відпустку", date: "4–5 листопада 2026", advice: "Змініть або видаліть" };
  const month = page.getByRole("button", { name: labels.month, exact: true });
  await month.press("Enter");
  const december = page.getByRole("option", { name: labels.december, exact: true });
  await december.focus(); await december.press("Enter");
  await expect(month).toBeFocused(); await expect(month).toContainText(labels.december);
  await choose(page, labels.month, labels.november);
  await page.getByRole("button", { name: labels.add, exact: true }).click();
  const employee = page.getByRole("button", { name: labels.employee, exact: true });
  await employee.click(); await page.getByRole("option", { name: "Ada Worker", exact: true }).click();
  await expect(employee).toBeFocused();
  const editor = page.getByRole("dialog");
  await editor.getByRole("button", { name: "2026-11-04", exact: true }).click();
  f.state.failSave = true;
  await editor.getByRole("button", { name: labels.create, exact: true }).click();
  await expect(editor.getByRole("alert")).toContainText(labels.date);
  await expect(editor.getByRole("alert")).toContainText(labels.advice);
  await expect(editor.getByRole("alert")).not.toContainText("42");
  expect(f.state.requests).toEqual([]); expect(f.errors).toEqual([]);
});


test("manager request cards keep actions next to content for 1, 2, 5 and 8 entries", async ({ page }) => {
  test.setTimeout(120_000);
  const f = await fixture(page);
  const request = (index: number, status: UrlopRequest["status"] = "pending"): UrlopRequest => ({ requestId: `layout-${index}`, shopId: 1, employeeId: 7 + index,
    employeeName: index === 4 ? "A very long employee name that must wrap without stretching the request footer" : `Person ${index}`,
    year: 2026, month: 11, days: index % 2 ? Array.from({ length: 12 }, (_, day) => day + 1) : [15], status,
    createdAtUtc: "2026-11-01T10:00:00Z", reviewedAtUtc: null, createdByManagerId: status === "approved" ? 1 : null, reviewedByManagerId: null });
  for (const count of [1, 2, 5, 8]) {
    f.state.requests = Array.from({ length: count }, (_, index) => request(index));
    await page.setViewportSize({ width: 1440, height: 900 }); await f.open("manager", "/urlop/1");
    const panel = page.getByRole("region", { name: "Vacation requests", exact: true });
    await expect(panel.getByRole("article")).toHaveCount(count);
    for (const width of [1440, 320, 430]) {
      await page.setViewportSize({ width, height: 900 });
      const metrics = await panel.getByRole("article").evaluateAll(cards => cards.map(card => {
        const metadata = card.querySelector("small")!.getBoundingClientRect();
        const footer = card.querySelector('[class*="footer"]')!.getBoundingClientRect();
        const bounds = card.getBoundingClientRect();
        return { gap: footer.top - metadata.bottom, bottom: bounds.bottom - footer.bottom, overflow: card.scrollWidth > card.clientWidth };
      }));
      for (const item of metrics) {
        expect(item.gap).toBeGreaterThanOrEqual(12); expect(item.gap).toBeLessThanOrEqual(16);
        expect(item.bottom).toBeLessThanOrEqual(16); expect(item.overflow).toBe(false);
      }
      expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
      if (width === 1440 && count >= 5) expect(await panel.getByRole("article").first().evaluate(card => card.parentElement!.scrollHeight > card.parentElement!.clientHeight)).toBe(true);
    }
  }
  f.state.requests = [request(0), request(1, "approved"), request(2, "rejected")];
  await page.setViewportSize({ width: 1440, height: 900 }); await f.open("manager", "/urlop/1");
  await page.getByRole("button", { name: "All", exact: true }).click();
  const panel = page.getByRole("region", { name: "Vacation requests", exact: true });
  for (const status of ["Pending", "Approved", "Rejected"]) await expect(panel.getByText(status, { exact: true }).last()).toBeVisible();
  const gaps = await panel.getByRole("article").evaluateAll(cards => cards.map(card => card.querySelector('[class*="footer"]')!.getBoundingClientRect().top - card.querySelector("small")!.getBoundingClientRect().bottom));
  expect(gaps.every(gap => gap >= 12 && gap <= 16)).toBe(true);
  await page.screenshot({ path: test.info().outputPath("request-cards-content-height.png"), scale: "css", animations: "disabled" });
  expect(f.errors).toEqual([]);
});

for (const language of ["en", "pl", "uk"] as const) test(`employee custom Urlop dropdowns and compact ranges: ${language}`, async ({ page }) => {
  test.setTimeout(120_000);
  const dictionary: Record<string, string> = language === "en" ? {} : JSON.parse(readFileSync(new URL(`../src/shared/i18n/${language}.json`, import.meta.url), "utf8"));
  const tr = (key: string) => dictionary[key] ?? key;
  const f = await fixture(page, language); f.state.isOpen = true;
  const base: UrlopRequest = { requestId: "range-0", shopId: 1, employeeId: 7, employeeName: "Ada Worker", year: 2026, month: 11, days: [13, 2, 1, 12, 3, 8, 2], status: "pending",
    createdAtUtc: "2026-11-01T10:00:00Z", reviewedAtUtc: null, createdByManagerId: null, reviewedByManagerId: null };
  f.state.requests = [base, { ...base, requestId: "range-1", status: "approved", days: Array.from({ length: 12 }, (_, index) => index + 16) }, { ...base, requestId: "range-2", status: "rejected", days: [15] }];
  await page.setViewportSize({ width: 390, height: 740 }); await f.open("employee", "/availability");
  await page.getByRole("tab", { name: tr("Urlop"), exact: true }).click();
  const panel = page.getByRole("tabpanel", { name: tr("Urlop"), exact: true });
  const monthName = language === "en" ? "November" : language === "pl" ? "listopada" : "листопада";
  await expect(panel.getByRole("article").nth(0)).toContainText(`1–3, 8, 12–13 ${monthName} 2026`);
  await expect(panel.getByRole("article").nth(1)).toContainText(`16–27 ${monthName} 2026`);
  await expect(panel.getByRole("article").nth(2)).toContainText(`15 ${monthName} 2026`);
  const shop = panel.getByRole("button", { name: tr("Shop"), exact: true });
  const month = panel.getByRole("button", { name: tr("Month"), exact: true });
  await expect(panel.locator("select")).toHaveCount(0);
  for (const width of [320, 375, 390, 430]) {
    await page.setViewportSize({ width, height: 360 });
    await page.evaluate(() => window.scrollTo({ top: 0, behavior: "instant" }));
    await shop.click();
    const shopOptions = page.getByRole("listbox", { name: tr("Shop"), exact: true });
    await expect(shopOptions.getByRole("option", { name: "A", exact: true })).toHaveAttribute("aria-selected", "true");
    expect(await shopOptions.evaluate(element => element.parentElement!.parentElement!.parentElement === document.body)).toBe(true);
    await page.getByRole("textbox", { name: tr("Search..."), exact: true }).fill("B");
    await page.getByRole("option", { name: "B", exact: true }).click();
    await expect(shop).toContainText("B"); await expect(shop).toBeFocused();
    await expect(panel.getByRole("article")).toHaveCount(0);
    await shop.click(); await page.keyboard.press("Escape"); await expect(shopOptions).toHaveCount(0);
    await choose(page, tr("Shop"), "A");
    await month.click();
    const list = page.getByRole("listbox", { name: tr("Month"), exact: true });
    await expect(list.getByRole("option")).toHaveCount(12);
    const menuBox = await list.locator("..").locator("..").boundingBox();
    const triggerBox = await month.boundingBox();
    expect(menuBox!.y).toBeGreaterThanOrEqual(0); expect(menuBox!.y + menuBox!.height).toBeLessThanOrEqual(360);
    expect(menuBox!.x).toBeGreaterThanOrEqual(0); expect(menuBox!.x + menuBox!.width).toBeLessThanOrEqual(width);
    expect(menuBox!.y).toBeLessThan(triggerBox!.y);
    await page.screenshot({ path: test.info().outputPath(`employee-month-popup-${language}-${width}.png`), scale: "css", animations: "disabled" });
    await month.press("ArrowUp"); await page.keyboard.press("Enter");
    await expect(panel.getByRole("button", { name: "2027-10-03", exact: true })).toBeAttached();
    await expect(month).toBeFocused();
    await month.click(); await month.press("ArrowDown");
    await expect(list.getByRole("option").first()).toBeFocused();
    await page.keyboard.press("Enter");
    await expect(panel.getByRole("article")).toHaveCount(3);
    await month.click(); await panel.getByRole("heading", { name: tr("Your requests"), exact: true }).click();
    await expect(list).toHaveCount(0);
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
  }
  expect(f.state.requests[0].days).toEqual([13, 2, 1, 12, 3, 8, 2]); expect(f.puts).toEqual([]); expect(f.errors).toEqual([]);
});
