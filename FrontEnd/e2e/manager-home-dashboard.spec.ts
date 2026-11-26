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
async function fixture(page: Page, items = events, language = "en", failNext = false, manyShops = false, unavailableReview = false) {
 const errors: string[] = [], writes: string[] = [];
 const offsets: number[] = [];
 page.on("pageerror", error => errors.push(error.message));
 await page.clock.setFixedTime(new Date("2026-11-04T10:00:00Z"));
 await page.addInitScript(() => localStorage.setItem("gf3.auth.access-token", "fixture-token"));
 const graph = (containerId: number, id: number) => ({ id, containerId, shopId: 2, name: `Target graph ${id}`, year: 2026, month: 11, publicationStatus: "private", peoplePerShift: 1, shift1Time: "08:00 - 16:00", shift2Time: "16:00 - 20:00", maxHoursPerEmpMonth: 160, maxConsecutiveDays: 5, maxConsecutiveFull: 3, maxFullPerMonth: 10, note: "" });
 const graphs = [graph(8, 20), { ...graph(9, 21), shopId: manyShops ? 3 : 2 }];
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
  const correctionDetail = /^\/api\/containers\/9\/graphs\/21\/shift-corrections\/2(?:\/(approve|reject))?$/.exec(path);
  const contractDetail = /^\/api\/containers\/8\/graphs\/20\/shift-swap-approvals\/3(?:\/(approve|reject))?$/.exec(path);
  if (correctionDetail || contractDetail) {
   if (unavailableReview) return json({ title: "Request unavailable" }, 404);
   const item = items.find(row => row.requestId === (correctionDetail ? 2 : 3))!;
   const action = (correctionDetail ?? contractDetail)?.[1];
   if (action) {
    if (correctionDetail && action === "approve") expect(request.postDataJSON()).toEqual({});
    item.status = action === "approve" ? "approved" : "rejected";
    item.requiresApproval = false;
   }
   const common = { id: item.requestId, employeeId: 99, employeeName: "Navigation Worker", scheduleId: item.graphId,
    scheduleName: "Assigned graph", shopName: "Target shop", year: 2026, month: 11, dayOfMonth: 4,
    status: item.status, createdAtUtc: item.occurredAtUtc };
   return json(correctionDetail ? { ...common, containerId: 9, scheduleSlotId: 1, isDayOffRequest: false,
    originalFromTime: "08:00", originalToTime: "16:00", requestedFromTime: "08:30", requestedToTime: "16:00",
    comment: "Please adjust the start time." } : { ...common, kind: "claim", fromEmployeeName: "Original Worker",
    toEmployeeName: "Navigation Worker", fromTime: "08:00", toTime: "16:00", isStale: false });
  }
  if (path === "/api/manager-home/activity") {
   const categories = {
    swaps: items.filter(item => item.kind.startsWith("swap_")),
    corrections: items.filter(item => item.kind.startsWith("correction_") || item.kind.startsWith("day_off_")),
    vacations: items.filter(item => item.kind.startsWith("urlop_")),
    availability: items.filter(item => item.kind === "availability_updated"),
   };
   const tab = url.searchParams.get("tab");
   const pending = items.filter(item => item.requiresApproval && item.status === "pending");
   const filtered = tab === "approvals" ? pending : categories[tab as keyof typeof categories] ?? items;
   const offset = Number(url.searchParams.get("offset")), limit = Number(url.searchParams.get("limit"));
   offsets.push(offset);
   if (failNext && offset > 0) {
    failNext = false;
    return json({ title: "Activity unavailable" }, 503);
   }
   return json({ items: filtered.slice(offset, offset + limit), allActivityCount: items.length, requiresApprovalCount: pending.length,
    categoryCounts: Object.fromEntries(Object.entries(categories).map(([key, value]) => [key, value.length])),
    hasMore: offset + limit < filtered.length, nextOffset: Math.min(offset + limit, filtered.length) });
  }
  if (path === "/api/employees") return json(manyShops ? [employee, { ...employee, id: 100, firstName: "Second" }] : [employee]);
  if (path === "/api/shops") return json([{ id: 2, name: "Target shop", address: "Street" }, ...(manyShops ? [{ id: 3, name: "North shop", address: "Street" },
   ...Array.from({ length: 12 }, (_, index) => ({ id: 4 + index, name: `Shop ${index + 1}`, address: "Street" }))] : [])]);
  if (path === "/api/shops/2") return json({ id: 2, name: "Target shop", address: "Street" });
  if (path === "/api/containers") return json([{ id: 8, name: "First container" }, { id: 9, name: "Second container" }]);
  if (/^\/api\/containers\/\d+$/.test(path)) return json({ id: Number(path.split("/").at(-1)), name: "Target container" });
  if (/^\/api\/containers\/\d+\/graphs$/.test(path)) return json(graphs.filter(g => g.containerId === Number(path.split("/")[3])));
  if (/\/graphs\/\d+$/.test(path)) return json(graphs.find(g => g.id === Number(path.split("/").at(-1))) ?? {}, graphs.some(g => g.id === Number(path.split("/").at(-1))) ? 200 : 404);
  if (path.endsWith("/slots") && path.includes("/graphs/")) {
   const id = Number(path.split("/")[5]);
   const slot = { id: 1, scheduleId: id, employeeId: id === 20 ? 99 : 100, dayOfMonth: 4, slotNo: 1, fromTime: "08:00", toTime: "16:00", status: "Working" };
   return json(manyShops ? [slot] : Array.from({ length: 20 }, (_, i) => ({ ...slot, id: i + 1, employeeId: 99, slotNo: i + 1 })));
  }
  if (path.endsWith("/employees") && path.includes("/graphs/")) return json([{ id: 1, scheduleId: Number(path.split("/")[5]), employeeId: 99, displayOrder: 0, minHoursMonth: 0 }]);
  if (path === "/api/availability-groups") return json([group]);
  if (path === "/api/availability-groups/15") return json(group);
  if (path === "/api/employment-contract-settings") return json({});
  if (path === "/api/manager-urlop") return json({ shopId: 2, shopName: "Target shop", year: Number(url.searchParams.get("year")), month: Number(url.searchParams.get("month")), isOpen: false, requests: [], approvedDays: [] });
  if (path.includes("notepad")) return json({ notes: [], state: { isExpanded: false, isPinned: false, height: 700 } });
  return json([]);
 });
 return { errors, writes, offsets };
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
 const row = page.getByRole("tabpanel").getByRole("link").nth(index);
 await expect(row).toHaveAttribute("href", destination); await row.click();
 await expect(page).toHaveURL(new RegExp(destination.replace(/[.*+?^${}()|[\]\\]/g, "\\$&") + "$"));
 await expect(page.getByRole("heading", { name: title, exact: true })).toBeVisible();
 if (index <= 2) {
  await page.getByRole("button", { name: "Schedule Information", exact: true }).click();
  await expect(page.getByRole("dialog", { name: "Schedule Information" })
   .getByRole("heading", { name: `Target graph ${index === 1 ? 21 : 20}`, exact: true })).toBeVisible();
 }
 if (index === 3) await expect(page.getByText("Target availability 15", { exact: true }).first()).toBeVisible();
 if (index === 4 || index === 5) {
  await expect(page.getByRole("button", { name: "Month", exact: true })).toContainText(index === 5 ? "February" : "November");
  await expect(page.getByRole("button", { name: "Year", exact: true })).toContainText(index === 5 ? "2027" : "2026");
 }
 expect(f.errors).toEqual([]); expect(f.writes.filter(path => !path.includes("negotiate"))).toEqual([]);
});

test("approval filtering keeps graph ownership and keyboard activation", async ({ page }) => {
 await page.emulateMedia({ reducedMotion: "no-preference" });
 const f = await fixture(page); await page.goto("/");
 const all = page.getByRole("tab", { name: /All Activity/ });
 await expect(all).toContainText("8");
 const correction = page.getByRole("tabpanel").getByRole("link", { name: /requested a schedule correction/ });
 await expect(correction).toHaveAttribute("href", "/container/9/graphs/21");
 await expect(page.getByRole("tab")).toHaveCount(5);
 await expect(page.getByRole("tab", { name: /Availability/ })).toHaveCount(0);
 const approvals = page.getByRole("tab", { name: /Requires Approval/ });
 const badge = approvals.getByText("3", { exact: true });
 const aura = () => approvals.evaluate(element => getComputedStyle(element, "::after").animationName);
 await expect(approvals).toHaveCSS("background-color", "rgb(253, 240, 242)");
 await expect.poll(aura).toContain("approvalAura");
 await page.emulateMedia({ reducedMotion: "reduce" });
 await expect.poll(aura).toBe("none");
 await expect(badge).toHaveCSS("color", "rgb(172, 25, 54)");
 await approvals.click();
 await expect(approvals).toHaveCSS("background-color", "rgb(248, 216, 224)");
 await expect(badge).toHaveCSS("background-color", "rgb(255, 237, 240)");
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

test("pending Home rows open focused review and the explicit action refreshes the feed", async ({ page, isMobile }) => {
 test.skip(isMobile, "Manager PC review");
 test.setTimeout(75_000);
 await page.emulateMedia({ reducedMotion: "reduce" });
 const f = await fixture(page, [{ ...events[1], requestId: 2 }, { ...events[2], requestId: 3 }]);
 await page.goto("/");
 const tab = page.getByRole("tab", { name: /Requires Approval/ });
 await tab.click();
 await expect(tab).toContainText("2");
 const panel = page.getByRole("tabpanel");
 const source = panel.getByRole("button", { name: /requested a schedule correction/ });
 await source.focus();
 await page.keyboard.press("Enter");
 const menu = page.getByRole("menu", { name: "Approval actions" });
 await expect(menu.getByRole("menuitem")).toHaveText(["Go to schedule", "Fast approve"]);
 await menu.getByRole("menuitem", { name: "Fast approve" }).click();
 const review = page.getByRole("dialog", { name: "Review request" });
 await expect(review.getByText("Please adjust the start time.")).toBeVisible();
 expect(f.writes.filter(path => path.includes("shift-corrections"))).toEqual([]);
 await review.getByRole("button", { name: "Reject", exact: true }).click();
 await expect(review.getByText("Rejected", { exact: true })).toBeVisible();
 await expect(tab).toContainText("1");
 await expect(source).toHaveCount(0);
 await review.getByRole("button", { name: "Close", exact: true }).click();
 await expect(panel).toBeFocused();
 await panel.getByRole("button", { name: /Shift approval requested/ }).click();
 await page.getByRole("menuitem", { name: "Fast approve" }).click();
 await review.getByRole("button", { name: "Approve", exact: true }).click();
 const transfer = page.getByRole("dialog", { name: "Approve shift transfer?" });
 await expect(transfer).toBeVisible();
 await expect.poll(() => transfer.evaluate(node => Number(getComputedStyle(node).zIndex))).toBeGreaterThan(1500);
 expect(f.writes.filter(path => path.includes("shift-swap-approvals"))).toEqual([]);
 await page.keyboard.press("Escape");
 await expect(transfer).toHaveCount(0);
 await expect(review).toBeVisible();
 await review.getByRole("button", { name: "Approve", exact: true }).click();
 await transfer.getByRole("button", { name: "Approve", exact: true }).click();
 await expect(review.getByText("Approved", { exact: true })).toBeVisible();
 await expect(panel.getByText("No outstanding approvals.")).toBeVisible();
 await expect(page).toHaveURL(/\/$/);
 expect(f.errors).toEqual([]);
});

test("a deleted request is unavailable and the schedule action still routes by the owning IDs", async ({ page, isMobile }) => {
 test.skip(isMobile, "Manager PC review");
 const f = await fixture(page, [{ ...events[1], requestId: 2 }], "en", false, false, true);
 await page.goto("/");
 const row = page.getByRole("tabpanel").getByRole("button", { name: /requested a schedule correction/ });
 await row.click();
 await page.getByRole("menuitem", { name: "Fast approve" }).click();
 const review = page.getByRole("dialog", { name: "Review request" });
 await expect(review.getByText("This request is no longer available.")).toBeVisible();
 await expect(review.getByRole("button", { name: "Approve", exact: true })).toHaveCount(0);
 await review.getByRole("button", { name: "Close" }).click();
 await row.click();
 await page.getByRole("menuitem", { name: "Go to schedule" }).click();
 await expect(page).toHaveURL(/\/container\/9\/graphs\/21$/);
 expect(f.writes.filter(path => path.includes("shift-corrections"))).toEqual([]);
});

test("long shop filters scroll below the date controls and preserve selection across dates", async ({ page }) => {
 await page.setViewportSize({ width: 1366, height: 768 });
 await fixture(page, events, "en", false, true);
 await page.goto("/");
 const shops = page.getByRole("group", { name: "Filter by shop" });
 const all = shops.getByRole("button", { name: "All", exact: true });
 const next = page.getByRole("button", { name: "Next day" });
 const table = page.getByRole("region", { name: "Scheduled employees" });
 await expect(shops.getByRole("button")).toHaveCount(15);
 const dateBox = (await next.boundingBox())!, shopBox = (await all.boundingBox())!;
 expect(shopBox.y).toBeGreaterThanOrEqual(dateBox.y + dateBox.height);
 expect(await shops.evaluate(el => el.scrollWidth > el.clientWidth)).toBe(true);
 await shops.evaluate(el => { el.scrollLeft = el.scrollWidth; });
 expect(await shops.evaluate(el => el.scrollLeft)).toBeGreaterThan(0);
 expect((await next.boundingBox())!.x).toBe(dateBox.x);
 await shops.getByRole("button", { name: "North shop" }).click();
 await expect(table.locator("tbody tr")).toHaveCount(1);
 await expect(table.getByText("Second Worker")).toBeVisible();
 await expect(page.getByText("1 assigned employees")).toBeVisible();
 await next.click();
 await expect(shops.getByRole("button", { name: "North shop" })).toHaveAttribute("aria-pressed", "true");
 await expect(table.getByText("No employees scheduled for this shop and date.")).toBeVisible();
 await page.setViewportSize({ width: 390, height: 844 });
 await expect(next).toBeInViewport({ ratio: 1 });
 expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
});

test("custom month selection, paging and unavailable destinations remain safe", async ({ page }) => {
 const invalid = { ...base, id: "invalid:9", graphId: null };
 const f = await fixture(page, [...events, invalid]); await page.goto("/");
 const panel = page.getByRole("tabpanel");
 await expect(panel.getByRole("link").first()).toBeVisible();
 await panel.evaluate(element => element.scrollTo(0, element.scrollHeight));
 await expect(panel.getByRole("link")).toHaveCount(8);
 await expect(page.getByText("This activity's resource is no longer available.")).toBeVisible();
 await expect(page.getByRole("button", { name: "Show more", exact: true })).toHaveCount(0);
 await page.getByRole("group", { name: "Period", exact: true }).getByRole("button", { name: "Period", exact: true }).click();
 await expect(page.getByRole("option")).toHaveCount(25);
 await page.getByRole("option", { name: "December 2026", exact: true }).click();
 await expect(page.getByRole("heading", { name: "Who Works Today?", exact: true })).toBeVisible();
 await expect(page.getByText("Events recorded in December 2026", { exact: true })).toBeVisible();
 await expect(panel.getByRole("link").first()).toBeVisible();
 expect(await page.locator("select").count()).toBe(0);
 expect(f.errors).toEqual([]);
});

const viewports = [[1694, 864], [1920, 1080], [1366, 768], [1024, 768], [768, 1024], [390, 844]] as const;
for (const [width, height] of viewports) test(`balanced dashboard and header geometry at ${width}×${height}`, async ({ page }) => {
 await page.setViewportSize({ width, height }); const f = await fixture(page); await page.goto("/");
 const heading = page.getByRole("heading", { name: "Home", exact: true }); await expect(heading).toBeVisible();
 expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
 await expect(page.getByText("Overview of today's schedules and recent application activity.")).toBeVisible();
 const cards = page.locator("main section"); await expect(cards).toHaveCount(2);
 if (width > 1100) {
  await expect.poll(async () => {
   const [left, right] = await Promise.all([cards.nth(0).boundingBox(), cards.nth(1).boundingBox()]);
   return Math.abs(left!.y - right!.y);
  }).toBeLessThanOrEqual(1);
 }
 const left = (await cards.nth(0).boundingBox())!, right = (await cards.nth(1).boundingBox())!;
 const minimumMargin = width <= 550 ? 12 : 24;
 expect(left.x).toBeGreaterThanOrEqual(minimumMargin); expect(width - right.x - right.width).toBeGreaterThanOrEqual(minimumMargin);
 expect(Math.abs(left.height - right.height)).toBeLessThanOrEqual(1); expect(left.height).toBeGreaterThanOrEqual(559);
 if (width > 1100) {
  expect(Math.abs(left.y - right.y)).toBeLessThanOrEqual(1);
  expect(Math.abs(left.width - right.width)).toBeLessThanOrEqual(1);
  if (height >= 864) expect(left.height).toBeGreaterThan(680);
  expect(right.x - left.x - left.width).toBeCloseTo(22, 0);
  expect(right.x + right.width - left.x).toBeLessThanOrEqual(1501);
  const header = (await page.locator("header").filter({ has: heading }).boundingBox())!;
  expect(header.y + header.height).toBeLessThanOrEqual(left.y);
  expect(height - left.y - left.height).toBeGreaterThanOrEqual(24);
  expect(header.width).toBeLessThanOrEqual(1100);
  expect(Math.abs(header.x + header.width / 2 - (left.x + right.x + right.width) / 2)).toBeLessThanOrEqual(2);
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

for (const count of [0, 3, 7, 8, 33]) test(`automatic activity paging with ${count} rows`, async ({ page }) => {
 await page.setViewportSize({ width: 1694, height: 864 });
 const items = Array.from({ length: count }, (_, index) => ({ ...base, id: `swap:${index}` }));
 const f = await fixture(page, items); await page.goto("/");
 const panel = page.getByRole("tabpanel"), links = panel.getByRole("link");
 if (!count) await expect(panel).toContainText("No activity recorded in this month.");
 else await expect(links.first()).toBeVisible();
 if (count === 33) {
  await expect.poll(() => f.offsets.find(offset => offset > 0)).toBe(7);
  await expect(panel).toHaveAttribute("data-overflow-bottom", "true");
  await panel.evaluate(element => { element.scrollTop = 80; });
  await expect(panel).toHaveAttribute("data-overflow-top", "true");
  for (const offset of [14, 21, 28]) {
   await panel.evaluate(element => element.scrollTo(0, element.scrollHeight));
   await expect.poll(() => f.offsets.includes(offset)).toBe(true);
   await expect(links).toHaveCount(Math.min(offset + 7, count));
  }
  await panel.evaluate(element => element.scrollTo(0, element.scrollHeight));
 }
 await expect(links).toHaveCount(count);
 await expect(panel).not.toHaveAttribute("data-overflow-bottom", "true");
 expect(f.offsets[0]).toBe(0);
 // Development StrictMode may restart the initial request when it remounts the page.
 expect(f.offsets.filter(offset => offset > 0)).toEqual(Array.from({ length: Math.max(0, Math.ceil(count / 7) - 1) }, (_, index) => (index + 1) * 7));
});

test("automatic paging stops on error until the manager retries", async ({ page }) => {
 await page.setViewportSize({ width: 1694, height: 864 });
 const f = await fixture(page, events, "en", true); await page.goto("/");
 await expect(page.getByText("Could not load more activity.")).toBeVisible();
 expect(f.offsets.filter(offset => offset > 0)).toEqual([7]);
 await page.getByRole("button", { name: "Retry", exact: true }).click();
 await expect(page.getByRole("tabpanel").getByRole("link")).toHaveCount(8);
 expect(f.offsets.filter(offset => offset > 0)).toEqual([7, 7]);
});

test("a resource deleted after activity was loaded shows the profile error without a fallback", async ({ page }) => {
 const f = await fixture(page, [{ ...base, graphId: 999 }]); await page.goto("/");
 await page.getByRole("tabpanel").getByRole("link").click();
 await expect(page).toHaveURL(/\/container\/8\/graphs\/999$/);
 await expect(page.getByText("Could not load this schedule.", { exact: true })).toBeVisible();
 await expect(page.getByText("Target graph 20", { exact: true })).toHaveCount(0);
 expect(f.errors).toEqual([]);
});
