import { expect, test, type Page } from "@playwright/test";
import type { EmployeeAvailabilityGroup } from "../src/entities/employee-availability/model/types";
import type { UrlopRequest } from "../src/entities/urlop/model/types";

async function fixture(page: Page, language = "en") {
  const state = { role: "manager", mode: "pc", isOpen: false, requests: [] as UrlopRequest[], failApprove: false };
  const puts: unknown[] = [];
  const errors: string[] = [];
  page.on("pageerror", error => errors.push(error.message));
  await page.clock.setFixedTime(new Date("2026-11-01T10:00:00Z"));
  await page.addInitScript(() => localStorage.setItem("gf3.auth.access-token", "urlop-fixture"));
  const employees = [{ id: 7, employeeId: 7, firstName: "Ada", lastName: "Worker", displayName: "Ada Worker", hasLoginAccount: true, isOnline: false, displayOrder: 0 }];
  const graph = { id: 10, containerId: 1, shopId: 1, shopName: "A", name: "November", year: 2026, month: 11, publicationStatus: "public", allowSwap: true,
    peoplePerShift: 1, shift1Time: "09:00 - 17:00", shift2Time: "", maxHoursPerEmpMonth: 160, maxConsecutiveDays: 5, maxConsecutiveFull: 3, maxFullPerMonth: 10, availabilityGroupId: null };
  const group: EmployeeAvailabilityGroup = { id: 5, name: "November availability", year: 2026, month: 11, canSubmit: true, slots: [],
    members: [{ employeeId: 7, firstName: "Ada", lastName: "Worker", displayOrder: 0, slots: [] }] };
  const approved = () => state.requests.filter(x => x.status === "approved").flatMap(x => x.days.map(day => ({ shopId: x.shopId, employeeId: x.employeeId, employeeName: x.employeeName, date: `${x.year}-${String(x.month).padStart(2, "0")}-${String(day).padStart(2, "0")}` })));
  const request = (body: { shopId: number; year: number; month: number; days: number[] }, status: UrlopRequest["status"]) => ({
    ...body, requestId: `00000000-0000-0000-0000-${String(state.requests.length + 1).padStart(12, "0")}`, status, employeeId: 7, employeeName: "Ada Worker",
    createdAtUtc: "2026-11-01T10:00:00Z", reviewedAtUtc: null, createdByManagerId: status === "approved" ? 1 : null, reviewedByManagerId: status === "approved" ? 1 : null,
  });
  await page.route("**/api/**", async route => {
    const url = new URL(route.request().url()); const path = url.pathname; const method = route.request().method();
    if (!path.startsWith("/api/")) return route.continue();
    let body: unknown = []; let status = 200;
    if (path.includes("negotiate")) return route.fulfill({ status: 503 });
    if (path.includes("notepad")) return route.fulfill({ json: { notes: [], state: { isExpanded: false, isPinned: false, height: 700 } } });
    if (path === "/api/auth/session") body = { role: state.role, workspaceMode: state.mode, employeeId: state.role === "employee" ? 7 : null, managerId: 1, userName: "ada", displayName: "Ada Worker" };
    else if (path === "/api/account-language") body = { language };
    else if (path === "/api/shops") body = [{ id: 1, name: "A", address: "Address" }, { id: 2, name: "B", address: "Address B" }];
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
    else if (path === "/api/manager-urlop") body = { shopId: 1, shopName: "A", year: 2026, month: 11, isOpen: state.isOpen, requests: state.requests, approvedDays: approved() };
    else if (path === "/api/manager-urlop/month") { state.isOpen = route.request().postDataJSON().isOpen; body = { ...route.request().postDataJSON() }; }
    else if (path === "/api/manager-urlop/requests") { const value = request(route.request().postDataJSON(), "approved"); state.requests.push(value); body = value; status = 201; }
    else if (path.startsWith("/api/manager-urlop/requests/")) {
      const id = path.split("/")[4]; const value = state.requests.find(x => x.requestId === id)!;
      if (path.endsWith("/approve")) {
        if (state.failApprove) return route.fulfill({ status: 409, json: { detail: "Conflict with scheduled shift.", code: "urlop_shift_conflict", conflictingDates: ["2026-11-03"], scheduleIds: [10] } });
        value.status = "approved";
      } else if (path.endsWith("/reject")) value.status = "rejected";
      else if (method === "DELETE") { state.requests = state.requests.filter(x => x !== value); return route.fulfill({ status: 204 }); }
      else value.days = route.request().postDataJSON().days;
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
    await route.fulfill({ status, contentType: "application/json", body: JSON.stringify(body) });
  });
  return { state, puts, errors, open: async (role: string, path: string) => {
    await page.waitForLoadState("networkidle");
    state.role = role; await page.goto(path);
    if (role === "manager" && state.mode === "pc") await page.getByRole("button", { name: "Collapse sidebar", exact: true }).click();
  } };
}

test("open, submit Pending, approve, UR overlays and availability save remain independent", async ({ page }) => {
  test.setTimeout(60_000);
  const f = await fixture(page);
  await f.open("manager", "/urlop");
  await page.getByRole("button", { name: "Open requests", exact: true }).click();
  await expect(page.getByText("Open for requests", { exact: true })).toBeVisible();
  await f.open("employee", "/availability");
  await page.getByRole("tab", { name: "Urlop", exact: true }).click();
  const panel = page.getByRole("tabpanel", { name: "Urlop", exact: true });
  await expect(panel.getByLabel("Month").locator("option")).toHaveCount(12);
  for (const day of [3, 4, 5]) await panel.getByRole("button", { name: `2026-11-0${day}`, exact: true }).click();
  const refreshedAfterSubmit = page.waitForResponse(response => new URL(response.url()).pathname === "/api/employee-availability" && response.request().method() === "GET");
  await panel.getByRole("button", { name: "Submit request" }).click();
  await expect(panel.locator('[data-urlop-status="pending"]')).toBeVisible();
  await refreshedAfterSubmit;
  await expect(panel.getByText("UR", { exact: true })).toHaveCount(0);
  await f.open("manager", "/urlop");
  await page.getByRole("button", { name: "Approve", exact: true }).click();
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
  await expect(page.getByRole("columnheader", { name: "Urlop Days", exact: true })).toBeVisible();
  await expect(page.getByText("UR", { exact: true }).first()).toBeVisible();
  expect(f.errors).toEqual([]);
});

test("PC CRUD, rejection and explicit retry after 409", async ({ page }) => {
  const f = await fixture(page); f.state.isOpen = true;
  f.state.requests = [{ requestId: "00000000-0000-0000-0000-000000000001", employeeId: 7, employeeName: "Ada Worker", shopId: 1, year: 2026, month: 11, days: [3], status: "pending", createdAtUtc: "2026-11-01T10:00:00Z", reviewedAtUtc: null, createdByManagerId: null, reviewedByManagerId: null }];
  f.state.failApprove = true; await f.open("manager", "/urlop");
  await page.getByRole("button", { name: "Approve", exact: true }).click();
  await expect(page.getByRole("alert").filter({ hasText: "2026-11-03" })).toBeVisible();
  expect(f.state.requests[0].status).toBe("pending");
  f.state.failApprove = false;
  await page.getByRole("button", { name: "Reject", exact: true }).click();
  await expect(page.getByText("Rejected", { exact: true })).toBeVisible();
  await expect(page.getByRole("button", { name: "Edit dates", exact: true })).toHaveCount(0);
  await page.getByRole("button", { name: "2026-11-04", exact: true }).click();
  await page.getByRole("button", { name: "Create approved vacation", exact: true }).click();
  await expect(page.getByText("Approved", { exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Edit dates", exact: true }).click();
  await page.getByRole("button", { name: /2026-11-04/ }).click();
  await page.getByRole("button", { name: "2026-11-05", exact: true }).click();
  await page.getByRole("button", { name: "Save", exact: true }).click();
  await expect.poll(() => f.state.requests[1].days).toEqual([5]);
  await page.getByRole("button", { name: "Delete", exact: true }).last().click();
  await page.getByRole("dialog", { name: "Delete vacation request?" }).getByRole("button", { name: "Confirm", exact: true }).click();
  await expect(page.getByText("Approved", { exact: true })).toHaveCount(0);
  await page.getByRole("button", { name: "Close requests", exact: true }).click();
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
  await expect(page.getByRole("button", { name: "Create approved vacation", exact: true })).toHaveCount(0);
  expect(f.errors).toEqual([]);
});
