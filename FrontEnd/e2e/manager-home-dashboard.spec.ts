import { expect, test, type Page } from "@playwright/test";
import type { ManagerHomeActivityItem } from "../src/pages/home/api/managerHomeActivityApi";

test.use({ timezoneId: "Europe/Warsaw", launchOptions: process.platform === "win32" ? { ignoreDefaultArgs: ["--disable-accelerated-compositing"] } : {} });
const base: ManagerHomeActivityItem = { id: "swap:1", kind: "swap_created", occurredAtUtc: "2026-11-04T10:00:00Z", employeeName: "Navigation Worker", status: "open", requiresApproval: false, targetType: "graph", containerId: 8, graphId: 20, shiftDate: "2026-11-04", fromTime: "08:00", toTime: "16:00" };
const events: ManagerHomeActivityItem[] = [
 base,
 { ...base, id: "correction:2", kind: "correction_requested", status: "pending", requiresApproval: true, containerId: 9, graphId: 21 },
 { ...base, id: "approval:3", kind: "approval_requested", status: "pending", requiresApproval: true },
 { ...base, id: "availability:4", kind: "availability_updated", targetType: "availability", availabilityGroupId: 15, employeeId: 99, status: "updated" },
 { ...base, id: "urlop:5", kind: "urlop_requested", targetType: "urlop", shopId: 2, year: 2026, month: 11, vacationDays: ["2026-11-10"], status: "pending", requiresApproval: true },
 { ...base, id: "urlop:6", kind: "urlop_reviewed", targetType: "urlop", shopId: 2, year: 2027, month: 2, vacationDays: ["2027-02-10"], status: "approved" },
 { ...base, id: "swap:7", kind: "swap_accepted", counterpartyName: "Receiving Worker", status: "accepted" },
 { ...base, id: "urlop:8", kind: "urlop_manager_created", targetType: "urlop", shopId: 2, year: 2026, month: 12, vacationDays: ["2026-12-10"], status: "approved" },
];
async function fixture(page: Page, items = events, language = "en") {
 const errors: string[] = [], writes: string[] = [];
 page.on("pageerror", error => errors.push(error.message));
 await page.clock.setFixedTime(new Date("2026-11-04T10:00:00Z"));
 await page.addInitScript(() => localStorage.setItem("gf3.auth.access-token", "fixture-token"));
 const graph = (containerId: number, id: number) => ({ id, containerId, shopId: 2, name: `Target graph ${id}`, year: 2026, month: 11, publicationStatus: "private", peoplePerShift: 1, shift1Time: "08:00 - 16:00", shift2Time: "16:00 - 20:00", maxHoursPerEmpMonth: 160, maxConsecutiveDays: 5, maxConsecutiveFull: 3, maxFullPerMonth: 10, note: "" });
 const graphs = [graph(8, 20), graph(9, 21)];
 const group = { id: 15, name: "Target availability 15", year: 2026, month: 11, publicationStatus: "private" };
 const employee = { id: 99, firstName: "Navigation", lastName: "Worker", hasLoginAccount: true, isOnline: false };
 await page.route("**/api/**", async route => {
  const request = route.request(), url = new URL(request.url()), path = url.pathname;
  if (!path.startsWith("/api/")) return route.continue();
  if (!["GET", "HEAD", "OPTIONS"].includes(request.method())) writes.push(path);
  const json = (data: unknown, status = 200) => route.fulfill({ status, contentType: "application/json", body: JSON.stringify(data) });
  if (path.includes("negotiate")) return json({}, 503);
  if (path === "/api/auth/session") return json({ role: "manager", workspaceMode: "pc", managerId: 1, userName: "fixture", displayName: "Manager", isSystemManager: false });
  if (path === "/api/account-language") return json({ language });
  if (path === "/api/manager-home/activity") {
   const filtered = url.searchParams.get("tab") === "approvals" ? items.filter(item => item.requiresApproval) : items;
   const offset = Number(url.searchParams.get("offset")), limit = Number(url.searchParams.get("limit"));
   return json({ items: filtered.slice(offset, offset + limit), allActivityCount: items.length, requiresApprovalCount: items.filter(item => item.requiresApproval).length, hasMore: offset + limit < filtered.length, nextOffset: Math.min(offset + limit, filtered.length) });
  }
  if (path === "/api/employees") return json([employee]);
  if (path === "/api/shops") return json([{ id: 2, name: "Target shop", address: "Street" }]);
  if (path === "/api/shops/2") return json({ id: 2, name: "Target shop", address: "Street" });
  if (path === "/api/containers") return json([{ id: 8, name: "First container" }, { id: 9, name: "Second container" }]);
  if (/^\/api\/containers\/\d+$/.test(path)) return json({ id: Number(path.split("/").at(-1)), name: "Target container" });
  if (/^\/api\/containers\/\d+\/graphs$/.test(path)) return json(graphs.filter(g => g.containerId === Number(path.split("/")[3])));
  if (/\/graphs\/\d+$/.test(path)) return json(graphs.find(g => g.id === Number(path.split("/").at(-1))) ?? {}, graphs.some(g => g.id === Number(path.split("/").at(-1))) ? 200 : 404);
  if (path.endsWith("/slots") && path.includes("/graphs/")) return json(Array.from({ length: 20 }, (_, i) => ({ id: i + 1, scheduleId: Number(path.split("/")[5]), employeeId: 99, dayOfMonth: 4, slotNo: i + 1, fromTime: "08:00", toTime: "16:00", status: "Working" })));
  if (path.endsWith("/employees") && path.includes("/graphs/")) return json([{ id: 1, scheduleId: Number(path.split("/")[5]), employeeId: 99, displayOrder: 0, minHoursMonth: 0 }]);
  if (path === "/api/availability-groups") return json([group]);
  if (path === "/api/availability-groups/15") return json(group);
  if (path === "/api/employment-contract-settings") return json({});
  if (path === "/api/manager-urlop") return json({ shopId: 2, shopName: "Target shop", year: Number(url.searchParams.get("year")), month: Number(url.searchParams.get("month")), isOpen: false, requests: [], approvedDays: [] });
  if (path.includes("notepad")) return json({ notes: [], state: { isExpanded: false, isPinned: false, height: 700 } });
  return json([]);
 });
 return { errors, writes };
}
for (const [label, index, destination, title] of [
 ["swap", 0, "/container/8/graphs/20", "Schedule Profile"],
 ["correction", 1, "/container/9/graphs/21", "Schedule Profile"],
 ["pickup approval", 2, "/container/8/graphs/20", "Schedule Profile"],
 ["availability", 3, "/availability/15", "Availability Profile"],
 ["vacation", 4, "/urlop/2?period=2026-11", "Urlop"],
 ["vacation in another month", 5, "/urlop/2?period=2027-02", "Urlop"],
] as const) test(`${label} opens its actual owning profile`, async ({ page }) => {
 const f = await fixture(page); await page.goto("/");
 await page.getByRole("button", { name: "Collapse sidebar", exact: true }).click();
 const row = page.getByRole("tabpanel").getByRole("link").nth(index);
 await expect(row).toHaveAttribute("href", destination); await row.click();
 await expect(page).toHaveURL(new RegExp(destination.replace(/[.*+?^${}()|[\]\\]/g, "\\$&") + "$"));
 await expect(page.getByRole("heading", { name: title, exact: true })).toBeVisible();
 if (index <= 2) await expect(page.getByText(`Target graph ${index === 1 ? 21 : 20}`, { exact: true }).first()).toBeVisible();
 if (index === 3) await expect(page.getByText("Target availability 15", { exact: true }).first()).toBeVisible();
 if (index === 4 || index === 5) {
  await expect(page.getByRole("button", { name: "Month", exact: true })).toContainText(index === 5 ? "February" : "November");
  await expect(page.getByRole("button", { name: "Year", exact: true })).toContainText(index === 5 ? "2027" : "2026");
 }
 expect(f.errors).toEqual([]); expect(f.writes.filter(path => !path.includes("negotiate"))).toEqual([]);
});

test("approval filtering keeps graph ownership and keyboard activation", async ({ page }) => {
 const f = await fixture(page); await page.goto("/");
 await page.getByRole("button", { name: "Collapse sidebar", exact: true }).click();
 const all = page.getByRole("tab", { name: /All Activity/ });
 await expect(all).toContainText("8");
 const correction = page.getByRole("tabpanel").getByRole("link", { name: /requested a schedule correction/ });
 await expect(correction).toHaveAttribute("href", "/container/9/graphs/21");
 await page.getByRole("tab", { name: /Requires Approval/ }).click();
 await expect(page.getByRole("tabpanel").getByRole("link")).toHaveCount(3);
 await expect(correction).toHaveAttribute("href", "/container/9/graphs/21");
 await expect(correction).toContainText("Requires approval");
 const approval = page.getByRole("tabpanel").getByRole("link", { name: /Shift approval requested/ });
 await page.keyboard.press("Tab"); await approval.focus(); await expect(approval).toBeFocused();
 expect(await approval.evaluate(element => getComputedStyle(element).outlineStyle)).not.toBe("none");
 await page.keyboard.press("Enter");
 await expect(page).toHaveURL(/\/container\/8\/graphs\/20$/);
 await expect(page.getByRole("heading", { name: "Schedule Profile", exact: true })).toBeVisible();
 expect(f.errors).toEqual([]);
});

test("custom month selection, paging and unavailable destinations remain safe", async ({ page }) => {
 const invalid = { ...base, id: "invalid:9", graphId: null };
 const f = await fixture(page, [...events, invalid]); await page.goto("/");
 await page.getByRole("button", { name: "Collapse sidebar", exact: true }).click();
 await expect(page.getByRole("tabpanel").getByRole("link")).toHaveCount(7);
 await page.getByRole("button", { name: "Show more", exact: true }).click();
 await expect(page.getByRole("tabpanel").getByRole("link")).toHaveCount(8);
 await expect(page.getByText("This activity's resource is no longer available.")).toBeVisible();
 await expect(page.getByRole("button", { name: "Show more", exact: true })).toHaveCount(0);
 await page.getByRole("button", { name: "Activity month", exact: true }).click();
 await expect(page.getByRole("option")).toHaveCount(25);
 await page.getByRole("option", { name: "December 2026", exact: true }).click();
 await expect(page.getByRole("heading", { name: "Who Works on 4 December 2026?", exact: true })).toBeVisible();
 await expect(page.getByRole("tabpanel").getByRole("link")).toHaveCount(7);
 expect(await page.locator("select").count()).toBe(0);
 expect(f.errors).toEqual([]);
});

for (const width of [1280, 1800, 1024, 768, 390]) test(`balanced dashboard and header geometry at ${width}px`, async ({ page }) => {
 await page.setViewportSize({ width, height: 900 }); const f = await fixture(page); await page.goto("/");
 const heading = page.getByRole("heading", { name: "Home", exact: true }); await expect(heading).toBeVisible();
 expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
 await expect(page.getByText("Overview of today's schedules and recent application activity.")).toBeVisible();
 await page.getByRole("button", { name: "Collapse sidebar", exact: true }).click();
 const cards = page.locator("main section"); await expect(cards).toHaveCount(2);
 const left = (await cards.nth(0).boundingBox())!, right = (await cards.nth(1).boundingBox())!;
 expect(left.x).toBeGreaterThanOrEqual(24); expect(width - right.x - right.width).toBeGreaterThanOrEqual(24);
 expect(Math.abs(left.height - right.height)).toBeLessThanOrEqual(1); expect(left.height).toBeGreaterThanOrEqual(620);
 if (width > 1100) {
  expect(Math.abs(left.y - right.y)).toBeLessThanOrEqual(1);
  expect(right.x - left.x - left.width).toBeCloseTo(22, 0);
  expect(right.x + right.width - left.x).toBeLessThanOrEqual(1501);
  const header = (await page.locator("header").filter({ has: heading }).boundingBox())!;
  expect(Math.abs(header.x - left.x)).toBeLessThanOrEqual(2);
  expect(Math.abs(header.x + header.width - right.x - right.width)).toBeLessThanOrEqual(2);
 }
 expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
 const table = page.getByRole("region", { name: "Scheduled employees", exact: true });
 await expect(table.locator("tbody tr")).toHaveCount(40);
 expect(await table.evaluate(element => element.scrollHeight > element.clientHeight)).toBe(true);
 await table.evaluate(element => { element.scrollTop = 150; });
 expect(await table.evaluate(element => element.scrollTop)).toBeGreaterThan(0);
 const shop = table.locator("tbody tr").first().locator("td").nth(2).locator("svg");
 expect(await shop.evaluate(element => getComputedStyle(element).transform)).toBe("matrix(-1, 0, 0, -1, 0, 0)");
 await page.screenshot({ path: test.info().outputPath(`home-${width}.png`), fullPage: true });
 expect(f.errors).toEqual([]);
});

test("a resource deleted after activity was loaded shows the profile error without a fallback", async ({ page }) => {
 const f = await fixture(page, [{ ...base, graphId: 999 }]); await page.goto("/");
 await page.getByRole("button", { name: "Collapse sidebar", exact: true }).click();
 await page.getByRole("tabpanel").getByRole("link").click();
 await expect(page).toHaveURL(/\/container\/8\/graphs\/999$/);
 await expect(page.getByText("Could not load this schedule.", { exact: true })).toBeVisible();
 await expect(page.getByText("Target graph 20", { exact: true })).toHaveCount(0);
 expect(f.errors).toEqual([]);
});
