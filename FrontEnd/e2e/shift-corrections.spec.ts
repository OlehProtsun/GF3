import { expect, test, type Page } from "@playwright/test";
import { createEmployeeAccount, dismissEmployeeInstallPrompt, loginAsManager, loginWithCredentials } from "./helpers";
import { test as isolatedTest } from "./fixtures/isolatedBackend";
import pl from "../src/shared/i18n/pl.json" with { type: "json" };
import uk from "../src/shared/i18n/uk.json" with { type: "json" };

const correctionComment = "Zażółć 🌙\nДругий рядок <strong>literal</strong> " + "W".repeat(70);
const commentLabels = {
  en: { detail: "Comment", input: "Comment (optional)" },
  pl: { detail: "Komentarz", input: "Komentarz (opcjonalnie)" },
  uk: { detail: "Коментар", input: "Коментар (необов’язково)" },
};

async function correctionFixture(page: Page, language: string, includeScheduleActivity = false) {
  await page.addInitScript(() => {
    localStorage.setItem("gf3.auth.access-token", "correction-ui-test");
    sessionStorage.setItem("gf3.pwa.install.dismissed", "1");
  });
  const graph = { id: 3, containerId: 1, shopId: 4, name: "Schedule", note: "", year: 2026, month: 10,
    publicationStatus: "public", allowSwap: true, peoplePerShift: 1, shift1Time: "15:00 - 22:00", shift2Time: "08:00 - 14:00",
    maxHoursPerEmpMonth: 160, maxConsecutiveDays: 5, maxConsecutiveFull: 3, maxFullPerMonth: 10 };
  const requests = Array.from({ length: 25 }, (_, index) => ({
    id: index + 1, containerId: 1, scheduleId: 3, scheduleSlotId: 1, isDayOffRequest: false,
    employeeId: 2, employeeName: index === 0 ? "Oleh Protsun" : index === 1 ? "Oleh Protsun With A Very Long Surname" : `Employee ${index}`,
    scheduleName: index === 1 ? "Long schedule name with multiple words" : "F35", shopName: "Central",
    year: 2026, month: 10, dayOfMonth: 21, originalFromTime: "15:00", originalToTime: "22:00",
    requestedFromTime: "14:00", requestedToTime: "22:00", createdAtUtc: "2026-10-21T12:26:00Z",
    status: index < 2 ? "pending" : index % 2 ? "approved" : "rejected", reviewedByManagerName: "Ada Manager",
    comment: index === 0 || index === 2 ? correctionComment : null,
  }));
  const pickup = { id: 4, kind: "claim", status: "pending", employeeId: 2, employeeName: "Pickup Worker",
    fromEmployeeName: "Swap Giver", toEmployeeName: "Pickup Worker", scheduleId: 3, scheduleSlotId: 1,
    scheduleName: "Schedule", shopName: "Central", year: 2026, month: 10, dayOfMonth: 21,
    fromTime: "15:00", toTime: "22:00", createdAtUtc: "2026-10-21T12:26:00Z", isStale: false };
  const accepted = { ...pickup, id: 6, status: "accepted", acceptedAtUtc: "2026-10-21T14:00:00Z",
    fromEmployeeId: 2, acceptedByEmployeeId: 3, acceptedByEmployeeName: "Swap Receiver",
    visibility: "public", isManagerCreated: false, shiftHours: 7,
    beforeSnapshot: { rows: [{ employeeId: 2, employeeName: "Swap Giver", kind: "employee", dayValues: { 21: "15:00 - 22:00" } }] },
    afterSnapshot: { rows: [{ employeeId: 3, employeeName: "Swap Receiver", kind: "employee", dayValues: { 21: "15:00 - 22:00" } }] },
  };
  const open = { ...accepted, id: 7, status: "open", isManagerCreated: true, manualColumnId: 1 };
  const nonManual = { ...open, id: 8, manualColumnId: null };
  const activityRequests: string[] = [];
  await page.route("**/api/**", async route => {
    const path = new URL(route.request().url()).pathname;
    if (!path.startsWith("/api/")) return route.continue();
    let body: unknown = [];
    if (path === "/api/auth/session") body = { role: "manager", workspaceMode: "pc", managerId: 1, displayName: "Manager", isSystemManager: true };
    else if (path === "/api/account-language") body = { language };
    else if (path === "/api/containers/1") body = { id: 1, name: "Container" };
    else if (path === "/api/containers/1/graphs") body = [graph];
    else if (path === "/api/containers/1/graphs/3") body = graph;
    else if (path === "/api/containers/1/graphs/3/employees") body = [{ id: 1, scheduleId: 3, employeeId: 2, displayOrder: 0 }];
    else if (path === "/api/containers/1/graphs/3/slots") body = [{ id: 1, scheduleId: 3, employeeId: 2, dayOfMonth: 21, slotNo: 1, fromTime: "15:00", toTime: "22:00", status: "ASSIGNED" }];
    else if (path === "/api/containers/1/graphs/3/shift-corrections") body = requests;
    else if (path === "/api/containers/1/graphs/3/shift-swap-approvals") body = includeScheduleActivity ? [pickup] : [];
    else if (path === "/api/containers/1/graphs/3/shift-swap-approvals/4/reject") {
      pickup.status = "rejected";
      body = pickup;
    }
    else if (path === "/api/containers/1/graphs/3/shift-swaps") body = includeScheduleActivity ? [accepted, open, nonManual] : [];
    else if (path.endsWith("/highlight-setting") || path.endsWith("/manual-highlight-setting")) body = { highlightColor: "#BBF7D0" };
    else if (path === "/api/manager-shift-correction-settings") body = { highlightColor: "#FDE68A" };
    else if (path === "/api/employees") body = [{ id: 2, firstName: "Oleh", lastName: "Protsun" }];
    else if (path === "/api/shops") body = [{ id: 4, name: "Central" }];
    else if (path.includes("notepad")) body = { notes: [], state: { isExpanded: false, isPinned: false, height: 700 } };
    else if (path.includes("negotiate")) return route.fulfill({ status: 503 });
    if (/\/graphs\/\d+\/(shift-corrections|shift-swap-approvals|shift-swaps)/.test(path)) activityRequests.push(path);
    await route.fulfill({ json: body });
  });
  return activityRequests;
}

async function showScheduleActivity(page: Page) {
  const collapse = page.getByRole("button", { name: "Collapse sidebar", exact: true });
  if (await collapse.isVisible()) {
    await collapse.focus();
    await page.keyboard.press("Enter");
  }
  const publication = page.getByRole("button", { name: "Expand Publication", exact: true });
  await publication.focus();
  await page.keyboard.press("Enter");
  const trigger = page.getByRole("button", { name: "Open details", exact: true });
  await trigger.focus();
  await page.keyboard.press("Enter");
  return { trigger, activity: page.getByRole("dialog", { name: "Schedule Activity", exact: true }) };
}

test("employee requests removal of a workday and manager approves the same request", async ({ page, isMobile }) => {
  test.setTimeout(90_000);
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.emulateMedia({ reducedMotion: "reduce" });
  await correctionFixture(page, "en");
  await page.addInitScript(() => sessionStorage.setItem("gf3.pwa.install.dismissed", "1"));
  let manager = false;
  let posted = false;
  const originalShifts = [{ slotId: 51, fromTime: "08:00", toTime: "12:00" }, { slotId: 52, fromTime: "15:00", toTime: "19:00" }];
  const request = { id: 85, containerId: 1, scheduleId: 3, scheduleSlotId: null, requestType: "removeShift", isDayOffRequest: false,
    originalShifts, employeeId: 2, employeeName: "Fixture Worker", scheduleName: "Schedule", shopName: "Central",
    year: 2026, month: 10, dayOfMonth: 21, originalFromTime: null, originalToTime: null, requestedFromTime: null, requestedToTime: null,
    createdAtUtc: "2026-10-20T14:00:00Z", status: "pending", comment: "Fixture reason", reviewedAtUtc: null as string | null };
  await page.route("**/api/auth/session", route => manager ? route.fallback() : route.fulfill({ json: {
    role: "employee", employeeId: 2, userName: "fixture-worker", displayName: "Fixture Worker",
  } }));
  const candidate = () => ({
    id: 3, containerId: 1, shopId: 4, name: "Schedule", shopName: "Central", year: 2026, month: 10, publicationStatus: "public",
    employees: [{ id: 1, employeeId: 2, displayName: "Fixture Worker", displayOrder: 0 }],
    approvedCorrectionDays: request.status === "approved" ? [21] : [],
    slots: request.status === "approved" ? [] : originalShifts.map((shift, index) => ({ id: shift.slotId, employeeId: 2, slotNo: index + 1, dayOfMonth: 21,
      fromTime: shift.fromTime, toTime: shift.toTime, status: "ASSIGNED" })),
  });
  await page.route("**/api/employee-schedules", route => route.fulfill({ json: [candidate()] }));
  await page.route("**/api/employee-shift-corrections/schedules", route => route.fulfill({ json: [candidate()] }));
  await page.route("**/api/employee-shift-corrections", route => {
    if (route.request().method() !== "POST") return route.fulfill({ json: posted ? [request] : [] });
    expect(route.request().postDataJSON()).toEqual({ scheduleId: 3, requestType: "removeShift", dayOfMonth: 21, comment: "Fixture reason" });
    posted = true;
    return route.fulfill({ status: 201, json: request });
  });
  await page.route("**/api/containers/1/graphs/3/shift-corrections", route => route.fulfill({ json: posted ? [request] : [] }));
  await page.route("**/api/containers/1/graphs/3/shift-corrections/85/approve", route => {
    expect(route.request().method()).toBe("POST");
    request.status = "approved";
    request.reviewedAtUtc = new Date().toISOString();
    return route.fulfill({ json: request });
  });

  await page.goto("/schedule");
  await dismissEmployeeInstallPrompt(page);
  await page.getByRole("button", { name: "Request a shift correction" }).click();
  const wizard = page.getByRole("dialog", { name: "Request a shift change" });
  const next = wizard.getByRole("button", { name: "Continue", exact: true });
  await wizard.getByRole("button", { name: /^Schedule/ }).click();
  await next.click();
  await wizard.getByRole("radio", { name: /08:00 – 12:00/ }).click();
  await next.click();
  await wizard.getByRole("button", { name: /Request day off/ }).click();
  await next.click();
  await wizard.getByRole("textbox", { name: "Comment (optional)" }).fill("Fixture reason");
  await next.click();
  await expect(wizard.getByText("Duration change · −8h")).toBeVisible();
  const sendRequest = wizard.getByRole("button", { name: "Send request" });
  if (isMobile) await sendRequest.tap();
  else await sendRequest.click();
  await expect(wizard.getByText("Pending approval")).toBeVisible();

  manager = true;
  await page.goto("/container/1/graphs/3/edit");
  const { activity } = await showScheduleActivity(page);
  await activity.getByRole("tab", { name: /Shift Corrections/ }).press("Enter");
  const requests = activity.getByRole("list", { name: "Shift correction requests" });
  await requests.getByRole("listitem").filter({ hasText: "Fixture Worker" }).click();
  const detail = requests.getByRole("listitem").filter({ hasText: "Fixture reason" });
  await expect(detail.getByText("Day off requested", { exact: true })).toBeVisible();
  await detail.getByRole("button", { name: "Approve", exact: true }).click();
  await expect.poll(() => request.status).toBe("approved");
  await expect(activity.getByText("Approved", { exact: true }).first()).toBeVisible();

  const employeePage = await page.context().newPage();
  await employeePage.emulateMedia({ reducedMotion: "reduce" });
  await correctionFixture(employeePage, "en");
  await employeePage.route("**/api/auth/session", route => route.fulfill({ json: {
    role: "employee", employeeId: 2, userName: "fixture-worker", displayName: "Fixture Worker",
  } }));
  await employeePage.route("**/api/employee-schedules", route => route.fulfill({ json: [candidate()] }));
  await employeePage.goto("/schedule");
  const marker = employeePage.getByRole("button", { name: "Correction approved" });
  await marker.press("Enter");
  const popup = employeePage.getByRole("status").filter({ hasText: "Your manager approved a correction for this day" });
  await expect(popup).toBeVisible();
  await employeePage.keyboard.press("Escape");
  await employeePage.getByRole("button", { name: "Show daily schedule view" }).press("Enter");
  await employeePage.getByRole("tab", { name: /\b21, day off/ }).press("Enter");
  await expect(marker.locator("xpath=ancestor::article")).toContainText("Day off");
  await marker.press("Enter");
  await expect(popup).toBeVisible();
});

isolatedTest("an unenrolled employee joins a public schedule only when Home approves the addition", async ({ page, browser, isolatedBackend, isMobile }) => {
  isolatedTest.skip(isMobile, "Manager PC review");
  isolatedTest.setTimeout(90_000);
  await page.setViewportSize({ width: 1440, height: 900 });
  const worker = await createEmployeeAccount(page.request, "correction-catalog");
  const headers = { Authorization: "Bearer " + worker.managerAccessToken };
  const create = async (url: string, data: object) => {
    const response = await page.request.post(url, { headers, data });
    expect(response.ok(), url + ": " + await response.text()).toBe(true);
    return response.json();
  };
  const { id: containerId } = await create("/api/containers", { name: "Catalog container" });
  const { id: shopId } = await create("/api/shops", { name: "Catalog shop", address: "Test address" });
  const otherManager = { userName: "correction-lock-manager", displayName: "Correction lock manager", password: "824681" };
  await create("/api/manager-profile/managers", otherManager);
  const root = "/api/containers/" + containerId + "/graphs";
  const { id: graphId } = await create(root, {
    year: 2026, month: 12, name: "Catalog schedule", shopId, publicationStatus: "public", allowSwap: true,
    peoplePerShift: 1, shift1Time: "08:00 - 12:00", shift2Time: "12:00 - 16:00",
    maxHoursPerEmpMonth: 160, maxConsecutiveDays: 5, maxConsecutiveFull: 3, maxFullPerMonth: 10,
  });
  const roster = async () => (await page.request.get(root + "/" + graphId + "/employees", { headers })).json();
  const workerContext = await browser.newContext({ baseURL: isolatedBackend.url });
  try {
    const employee = await workerContext.newPage();
    await loginWithCredentials(employee, worker.username, worker.password);
    await employee.goto("/schedule");
    await expect(employee.getByText("Nothing is public for your account yet.")).toBeVisible();
    await employee.getByRole("button", { name: "Request a shift correction" }).click();
    const wizard = employee.getByRole("dialog", { name: "Request a shift change" });
    const next = wizard.getByRole("button", { name: "Continue", exact: true });
    await wizard.getByRole("button", { name: /^Catalog schedule/ }).click();
    await next.click();
    await wizard.getByRole("radio", { name: /^Saturday, 12 December 2026.*Day off/ }).click();
    await next.click();
    await wizard.getByLabel("Start time", { exact: true }).fill("08:00");
    await wizard.getByLabel("End time", { exact: true }).fill("12:00");
    await next.click();
    await wizard.getByRole("textbox", { name: "Comment (optional)" }).fill("Catalog approval fixture");
    await next.click();
    await wizard.getByRole("button", { name: "Send request" }).click();
    await expect(wizard.getByText("Pending approval")).toBeVisible();
    expect(await roster()).toEqual([]);
    await employee.reload();
    await expect(employee.getByText("Nothing is public for your account yet.")).toBeVisible();

    await loginAsManager(page);
    await page.goto("/");
    await page.getByRole("tab", { name: /Requires Approval/ }).click();
    const review = page.getByRole("dialog", { name: "Review request" });
    const lockingContext = await browser.newContext({ baseURL: isolatedBackend.url, viewport: { width: 1440, height: 900 } });
    try {
      const lockingPage = await lockingContext.newPage();
      await loginWithCredentials(lockingPage, otherManager.userName, otherManager.password);
      await lockingPage.goto("/container/" + containerId + "/graphs/" + graphId + "/edit");
      await expect(lockingPage.getByRole("button", { name: "Add manual column on the left" })).toBeVisible({ timeout: 20_000 });
      await page.getByRole("tabpanel").getByRole("button", { name: /requested a day-off correction/ }).click();
      await page.getByRole("menuitem", { name: "Fast approve" }).click();
      await expect(review.getByText("Catalog approval fixture")).toBeVisible();
      await expect(review.getByText(/currently being edited by Correction lock manager/i)).toBeVisible();
      await expect(review.getByRole("button", { name: "Approve", exact: true })).toBeDisabled();
      expect(await roster()).toEqual([]);
    } finally {
      await lockingContext.close();
    }
    await expect(review.getByRole("button", { name: "Approve", exact: true })).toBeEnabled({ timeout: 15_000 });
    await review.getByRole("button", { name: "Approve", exact: true }).click();
    await expect(review.getByText("Approved", { exact: true })).toBeVisible();
    await expect(page.getByRole("tabpanel").getByText("No outstanding approvals.")).toBeVisible();
    expect(await roster()).toEqual([expect.objectContaining({ employeeId: worker.id })]);
    await employee.reload();
    await expect(employee.getByRole("button", { name: /Current schedule: Catalog schedule/ })).toBeVisible();
    await expect(employee.getByRole("button", { name: "Correction approved" })).toBeVisible();
  } finally {
    await workerContext.close();
  }
});

test("Schedule Activity preserves graph panels, live counts and overlay layering", async ({ page }) => {
  test.setTimeout(60_000);
  await page.setViewportSize({ width: 1694, height: 864 });
  await correctionFixture(page, "en", true);
  await page.goto("/container/1/graphs/3/edit");
  await expect(page.getByRole("button", { name: "Expand Publication" })).toBeVisible({ timeout: 20_000 });
  expect(await page.getByRole("button", { name: /^Expand (Manual shifts|Shift Corrections)$/ }).count()).toBe(0);
  for (const label of ["Schedule Details", "Employees", "Bind Information"]) {
    await expect(page.getByRole("button", { name: `Expand ${label}`, exact: true })).toBeVisible();
  }
  const { trigger, activity } = await showScheduleActivity(page);
  const publication = trigger.locator("xpath=ancestor::section[1]");
  await expect(publication).toContainText("Publication (3)");
  await expect(publication.getByRole("radio")).toHaveCount(4);
  await expect(publication.getByText("Swap log")).toHaveCount(0);
  const tabs = activity.getByRole("tab");
  await expect(tabs).toHaveText(["All28", "Shift Corrections26", "Manual shifts1", "Swap log1"]);
  await expect(activity.getByText("Offered", { exact: true }).locator("..")).toHaveText("Offered1");
  for (const [width, height] of [[1694, 864], [1920, 1080], [1366, 768], [1024, 768], [768, 1024], [390, 844]]) {
    await page.setViewportSize({ width, height });
    const box = (await activity.boundingBox())!;
    expect(box.width).toBeLessThanOrEqual(width - 31);
    expect(box.height).toBeLessThanOrEqual(height - 31);
    expect(await activity.evaluate(element => element.scrollWidth <= element.clientWidth + 1)).toBe(true);
    const grid = activity.getByRole("tabpanel");
    const columns = await grid.evaluate(element => getComputedStyle(element).gridTemplateColumns.split(" ").length);
    expect(columns).toBe(width > 1050 ? 3 : width > 760 ? 2 : 1);
    if (width <= 760) expect(await grid.evaluate(element => element.scrollHeight > element.clientHeight)).toBe(true);
  }
  const search = activity.getByRole("searchbox");
  await search.fill("Swap Receiver");
  await tabs.nth(3).click();
  await expect(search).toHaveValue("Swap Receiver");
  for (const [tab, control, childName] of [
    [3, "View swap comparison for Swap Giver", "Swap Giver to Swap Receiver"],
    [3, "Accepted swap highlight color", "Choose accepted swap color"],
    [2, "Accepted manual shift highlight color", "Choose accepted shift color"],
    [1, "Approved correction highlight color", "Choose approval color"],
  ] as const) {
    await tabs.nth(tab).click();
    await activity.getByRole("button", { name: control, exact: true }).click();
    const child = page.getByRole("dialog", { name: childName, exact: true });
    await expect(child).toBeVisible();
    await expect.poll(() => child.evaluate(element => Number(getComputedStyle(element).zIndex))).toBeGreaterThan(1500);
    await expect.poll(() => child.evaluate(element => element.contains(document.elementFromPoint(innerWidth / 2, innerHeight / 2)))).toBe(true);
    if (childName === "Swap Giver to Swap Receiver") {
      await expect(child.getByRole("table")).toHaveCount(2);
    }
    await page.keyboard.press("Escape");
    await expect(child).toHaveCount(0);
    await expect(activity).toBeVisible();
  }
  const contract = activity.getByRole("region", { name: "Contract approvals" });
  await contract.getByRole("button", { name: /Pickup Worker/ }).click();
  await contract.getByRole("button", { name: "Approve", exact: true }).click();
  const confirmation = page.getByRole("dialog", { name: "Approve shift transfer?" });
  await expect(confirmation).toBeVisible();
  expect(await confirmation.evaluate(element => element.parentElement === document.body)).toBe(true);
  await page.keyboard.press("Escape");
  await expect(confirmation).toHaveCount(0);
  await contract.getByRole("button", { name: "Reject", exact: true }).click();
  await expect(activity.getByText("Pending", { exact: true }).first().locator("..")).toHaveText("Pending2");
  await expect(publication).toContainText("Publication (2)");
  await page.keyboard.press("Escape");
  await expect(activity).toHaveCount(0);
  await expect(trigger).toBeFocused();
});

test("Schedule Activity compares cross-schedule exchanges one schedule at a time", async ({ page, browserName, isMobile }, testInfo) => {
  test.setTimeout(50_000);
  await page.setViewportSize({ width: 1694, height: 864 });
  const mobileWebKit = browserName === "webkit" && isMobile;
  await correctionFixture(page, "en");
  const snapshot = (employeeId: number, employeeName: string, day: number, period: string) =>
    ({ rows: [
      { employeeId, employeeName, kind: "employee", dayValues: { [day]: period } },
      { employeeId: 4, employeeName: "Backup", kind: "employee", dayValues: {} },
    ] });
  await page.route("**/api/containers/1/graphs/3/shift-swaps", route => route.fulfill({ json: [{
    id: 9, status: "accepted", requestKind: "exchange", scheduleId: 3, scheduleName: "F27", shopName: "Central",
    year: 2026, month: 10, dayOfMonth: 21, shiftHours: 7, fromTime: "15:00", toTime: "22:00",
    fromEmployeeId: 2, fromEmployeeName: "Alice", acceptedByEmployeeId: 3, acceptedByEmployeeName: "Bob",
    acceptedAtUtc: "2026-10-21T14:00:00Z", visibility: "private",
    beforeSnapshot: snapshot(2, "Alice (F27)", 21, "15:00 - 22:00"),
    afterSnapshot: snapshot(3, "Bob (F27)", 21, "15:00 - 22:00"),
    returnScheduleId: 35, returnScheduleName: "F35", returnShopName: "West",
    returnYear: 2026, returnMonth: 11, returnDayOfMonth: 3, returnFromTime: "09:30", returnToTime: "14:00", returnShiftHours: 4.5,
    returnBeforeSnapshot: snapshot(3, "Bob (F35)", 3, "09:30 - 14:00"),
    returnAfterSnapshot: snapshot(2, "Alice (F35)", 3, "09:30 - 14:00"),
  }] }));
  await page.goto("/container/1/graphs/3/edit");
  const { activity } = await showScheduleActivity(page);
  await activity.getByRole("tab", { name: /Swap log/ }).click();
  await expect(activity.getByText("F27 · Central", { exact: true })).toBeVisible();
  await expect(activity.getByText("F35 · West", { exact: true })).toBeVisible();
  await expect(activity.getByRole("group", { name: "Alice", exact: true })).toContainText("Gives to Bob");
  await expect(activity.getByRole("group", { name: "Bob", exact: true })).toContainText("Gives to Alice");
  await activity.getByRole("button", { name: "View swap comparison for Alice" }).click();
  const dialog = page.getByRole("dialog", { name: "Alice to Bob" });
  const tables = dialog.getByRole("table");
  const source = dialog.getByRole("button", { name: /Source schedule.*F27/ });
  const returned = dialog.getByRole("button", { name: /Return schedule.*F35/ });
  await expect(dialog.getByText("Exchange between different schedules")).toBeVisible();
  await expect(tables).toHaveCount(2);
  await expect(tables.nth(0).getByRole("columnheader", { name: "Alice (F27)" })).toBeVisible();
  await returned.click();
  await expect(tables).toHaveCount(2);
  await expect(tables.nth(0).getByRole("columnheader", { name: "Bob (F35)" })).toBeVisible();
  await expect(tables.nth(1).getByRole("columnheader", { name: "Alice (F35)" })).toBeVisible();
  await expect(tables.getByRole("columnheader", { name: /F27/ })).toHaveCount(0);
  await expect(returned.getByText("03 Nov 2026", { exact: true })).toBeVisible();
  await expect(returned.getByText("09:30–14:00", { exact: true })).toBeVisible();
  const holiday = tables.nth(0).getByRole("rowheader", { name: /Holiday/ }).first();
  expect(await holiday.evaluate(cell => {
    const label = cell.firstElementChild!;
    return label.scrollWidth <= label.clientWidth + 1;
  })).toBe(true);
  await dialog.screenshot({ path: testInfo.outputPath("cross-schedule-desktop.png") });
  const body = dialog.getByRole("region", { name: "Swap comparison" });
  expect(await body.evaluate(element => element.scrollHeight <= element.clientHeight + 1)).toBe(true);
  expect(await tables.first().evaluate(element => {
    const tableViewport = element.parentElement!;
    return tableViewport.scrollHeight > tableViewport.clientHeight;
  })).toBe(true);
  const desktopShadowSpace = await body.evaluate(element => {
    const lastCard = element.firstElementChild!.lastElementChild!;
    return element.getBoundingClientRect().bottom - lastCard.getBoundingClientRect().bottom;
  });
  expect(desktopShadowSpace).toBeGreaterThanOrEqual(30);
  for (const width of [1024, 390]) {
    await page.setViewportSize({ width, height: 844 });
    expect(await dialog.evaluate(element => element.scrollWidth <= element.clientWidth + 1)).toBe(true);
    await expect(returned).toBeVisible();
    await expect(tables).toHaveCount(2);
  }
  expect(await tables.first().evaluate(element => {
    const tableViewport = element.parentElement!;
    return tableViewport.scrollWidth > tableViewport.clientWidth &&
      tableViewport.scrollHeight <= tableViewport.clientHeight + 1;
  })).toBe(true);
  if (!mobileWebKit) {
    await body.evaluate(element => { element.scrollTop = 0; });
    await tables.first().getByRole("columnheader", { name: "Bob (F35)" }).hover();
    await page.mouse.wheel(0, 280);
    await expect.poll(() => body.evaluate(element => element.scrollTop)).toBeGreaterThan(0);
  }
  await body.focus();
  await body.evaluate(element => { element.scrollTop = 0; });
  await page.keyboard.press("End");
  await expect.poll(() => body.evaluate(element => element.scrollTop)).toBeGreaterThan(0);
  await body.evaluate(element => { element.scrollTop = 0; });
  if (mobileWebKit) await source.press("Enter");
  else await source.click();
  await expect(tables.nth(1).getByRole("columnheader", { name: "Bob (F27)" })).toBeVisible();
  await page.keyboard.press("Escape");
  await expect(dialog).toHaveCount(0);
  await expect(activity).toBeVisible();
});

test("new graph manual columns are matrix drafts and Schedule Activity keeps offer controls", async ({ page, isMobile }) => {
  test.skip(isMobile, "Manager PC editor");
  const requests = await correctionFixture(page, "en");
  const writes: string[] = [];
  page.on("request", request => {
    const path = new URL(request.url()).pathname;
    if (path.includes("/graphs") && request.method() !== "GET") writes.push(path);
  });
  await page.goto("/container/1/graphs/new");
  const { activity } = await showScheduleActivity(page);
  await expect(activity.getByText("No accepted swaps yet.")).toBeVisible();
  await expect(activity.getByText("No correction requests yet.")).toBeVisible();
  await activity.getByRole("tab", { name: /Manual shifts/ }).click();
  await expect(activity.getByRole("region", { name: "Columns" })).toHaveCount(0);
  await expect(activity.getByRole("button", { name: "Publish shift" })).toBeDisabled();
  await activity.getByRole("button", { name: "Close", exact: true }).click();
  await page.getByRole("button", { name: "Add manual column on the left" }).click();
  const actions = page.getByRole("button", { name: /^Actions for manual column/ });
  await actions.click();
  await page.getByRole("menuitem", { name: "Delete manual column" }).click();
  await expect(actions).toHaveCount(0);
  expect(requests).toEqual([]);
  expect(writes).toEqual([]);
});

for (const language of ["en", "pl", "uk"] as const) {
  test(`compact correction history and details in ${language}`, async ({ page, isMobile }, info) => {
    test.setTimeout(60_000);
    await page.setViewportSize({ width: 1440, height: 1000 });
    await correctionFixture(page, language);
    await page.goto("/container/1/graphs/3/edit");
    const list = page.getByRole("list", { name: language === "en" ? "Shift correction requests" : language === "pl" ? "Wnioski o korektę zmiany" : "Запити на коригування змін" });
    const expand = page.getByRole("button", { name: /^(Expand Publication|Rozwiń Publikacja|Розгорнути Публікація)$/ });
    await expect(expand).toBeVisible({ timeout: 20_000 });
    const collapseNavigation = page.getByRole("button", { name: language === "en" ? "Collapse sidebar" : language === "pl" ? "Zwiń pasek boczny" : "Згорнути бічну панель", exact: true });
    if (await collapseNavigation.isVisible()) {
      await collapseNavigation.focus();
      await page.keyboard.press("Enter");
    }
    await page.mouse.move(1300, 100);
    // Keyboard activation avoids the app navigation's hover-to-open overlay.
    await expand.focus();
    await page.keyboard.press("Enter");
    await page.getByRole("button", { name: /^(Open details|Otwórz szczegóły|Відкрити подробиці)$/ }).click();
    await page.getByRole("tab", { name: /^(Shift Corrections|Korekty zmian|Коригування змін)/ }).click();
    await expect(list).toBeVisible();
    const shell = list.locator("..");
    for (const width of [320, 240]) {
      await shell.evaluate((element, value) => { (element as HTMLElement).style.width = `${value}px`; (element as HTMLElement).style.boxSizing = "border-box"; }, width);
      await list.getByRole("listitem").first().focus();
      await page.keyboard.press("Enter");
      const article = list.locator("article");
      await expect(article).toBeVisible();
      const comment = article.getByText(commentLabels[language].detail, { exact: true }).locator("..").locator("p");
      expect(await comment.textContent()).toBe(correctionComment);
      await expect(comment.locator("strong")).toHaveCount(0);
      await expect(comment).toHaveCSS("white-space", "pre-wrap");
      expect(await comment.evaluate(element => element.scrollWidth <= element.clientWidth + 1)).toBe(true);
      await expect(article.locator("svg")).toHaveCount(10);
      for (const icon of await article.locator("svg").all()) {
        await expect(icon).toBeVisible();
        const size = await icon.boundingBox();
        expect(size!.width).toBeGreaterThanOrEqual(12);
        expect(size!.height).toBeGreaterThanOrEqual(12);
      }
      await expect(article.locator("dt").first()).toContainText(language === "en" ? "Date" : language === "pl" ? "Data" : "Дата");
      const buttons = article.locator('button').filter({ has: page.locator('span[title]') });
      await expect(buttons).toHaveCount(2);
      for (const button of await buttons.all()) {
        expect(await button.evaluate(element => element.getBoundingClientRect().height)).toBe(42);
        expect(await button.evaluate(element => element.scrollHeight <= element.clientHeight + 1)).toBe(true);
      }
      const sizes = await buttons.evaluateAll(elements => elements.map(element => element.getBoundingClientRect().width));
      expect(Math.abs(sizes[0] - sizes[1])).toBeLessThan(1);
      expect(await article.evaluate(element => element.scrollWidth <= element.clientWidth + 1)).toBe(true);
      await article.screenshot({ path: info.outputPath(`pending-${width}.png`) });
      await article.getByRole("button").first().focus();
      await page.keyboard.press("Enter");
    }
    await list.getByRole("listitem").nth(1).focus();
    await page.keyboard.press("Enter");
    expect(await list.locator("article").evaluate(element => element.scrollWidth <= element.clientWidth + 1)).toBe(true);
    await list.locator("article").screenshot({ path: info.outputPath("long-names-240.png") });
    await list.locator("article button").first().focus();
    await page.keyboard.press("Enter");
    await shell.evaluate(element => { (element as HTMLElement).style.width = ""; });
    await page.setViewportSize({ width: 375, height: 812 });
    for (const index of [2, 3]) {
      await list.getByRole("listitem").nth(index).focus();
      await page.keyboard.press("Enter");
      await expect(list.locator("article em")).toBeVisible();
      await expect(list.locator("article").getByText(commentLabels[language].detail, { exact: true })).toHaveCount(index === 2 ? 1 : 0);
      await expect(list.locator("article svg")).toHaveCount(9);
      await expect(list.locator("article p svg")).toBeVisible();
      await expect(list.locator("article p:has(svg)")).toContainText("Ada Manager");
      expect(await list.locator("article button").count()).toBe(1);
      await list.locator("article").screenshot({ path: info.outputPath(`resolved-${index}.png`) });
      await list.locator("article button").focus();
      await page.keyboard.press("Enter");
    }
    expect(await list.evaluate(element => getComputedStyle(element).scrollbarWidth)).toBe("none");
    expect(await list.evaluate(element => getComputedStyle(element).touchAction)).toBe("auto");
    expect(await list.evaluate(element => element.scrollWidth <= element.clientWidth)).toBe(true);
    const color = page.getByRole("button", { name: language === "en" ? "Approved correction highlight color" : language === "pl" ? "Kolor wyróżnienia zatwierdzonej korekty" : "Колір виділення схвалених коригувань", exact: true });
    await color.focus();
    await page.keyboard.press("Tab");
    await expect(list).toBeFocused();
    expect(await list.evaluate(element => getComputedStyle(element).outlineStyle)).toBe("solid");
    await page.keyboard.press("End");
    await expect.poll(() => list.evaluate(element => element.scrollTop + element.clientHeight >= element.scrollHeight - 1)).toBe(true);
    const bottomSpace = await list.evaluate(element => element.getBoundingClientRect().bottom - element.lastElementChild!.getBoundingClientRect().bottom);
    expect(bottomSpace).toBeGreaterThanOrEqual(18);
    await shell.screenshot({ path: info.outputPath("history-bottom.png") });
    await page.keyboard.press("Home");
    await expect.poll(() => list.evaluate(element => element.scrollTop)).toBe(0);
    if (!isMobile) {
      await list.hover();
      await page.mouse.wheel(0, 180);
      await expect.poll(() => list.evaluate(element => element.scrollTop)).toBeGreaterThan(0);
    }
  });
}

for (const [width, language] of [[320, "en"], [375, "pl"], [390, "uk"]] as const) {
  test(`employee requests a day-off shift at ${width}px in ${language}`, async ({ page, isMobile }, info) => {
    test.setTimeout(60_000);
    await page.clock.setFixedTime(new Date("2026-08-12T10:00:00Z"));
    await page.setViewportSize({ width, height: 812 });
    await page.emulateMedia({ reducedMotion: "reduce" });
    await page.addInitScript(() => localStorage.setItem("gf3.auth.access-token", "day-off-fixture"));
    const requests: Record<string, unknown>[] = [];
    let submitted: unknown;
    let attempts = 0;
    await page.route("**/api/**", async route => {
      const path = new URL(route.request().url()).pathname;
      if (!path.startsWith("/api/")) return route.continue();
      let body: unknown = [];
      if (path === "/api/auth/session") body = { role: "employee", employeeId: 12, userName: "worker", displayName: "Worker" };
      else if (path === "/api/account-language") body = { language };
      else if (path === "/api/public-holiday-settings") body = { showPublicHolidays: true };
      else if (path === "/api/employee-ui-state") body = { scheduleColumnOrders: {}, readNotificationIds: [] };
      else if (path === "/api/employee-schedules" || path === "/api/employee-shift-corrections/schedules") {
        const august = {
        id: 10, containerId: 2, containerName: "Team", shopId: 4, shopName: "Central", name: "August", year: 2026, month: 8,
        publicationStatus: "public", employees: [{ id: 1, employeeId: 12, firstName: "Test", lastName: "Worker", displayOrder: 0 }],
        slots: [
          { id: 100, dayOfMonth: 12, slotNo: 1, employeeId: 12, fromTime: "08:00", toTime: "12:00", status: "ASSIGNED" },
          { id: 101, dayOfMonth: 12, slotNo: 2, employeeId: 12, fromTime: "15:00", toTime: "19:00", status: "ASSIGNED" },
        ], relatedScheduleAssignments: [{ employeeId: 12, dayOfMonth: 14, scheduleId: 11, scheduleName: "Other shop" }],
        };
        body = path === "/api/employee-schedules" ? [august] : [august, {
          ...august, id: 11, name: "Other shop", employees: [], relatedScheduleAssignments: [],
          slots: [{ ...august.slots[0], id: 102, dayOfMonth: 14 }],
        }];
      }
      else if (path === "/api/employee-shift-corrections") {
        if (route.request().method() === "POST") {
          const request = route.request().postDataJSON();
          submitted = request;
          if (++attempts === 1 && language === "en") {
            return route.fulfill({ status: 503, json: { detail: "Could not send this test request." } });
          }
          requests.push({ ...request, id: 1, containerId: 2, employeeId: 12, year: 2026, month: 8,
            scheduleSlotId: null, originalFromTime: null, originalToTime: null, isDayOffRequest: true, status: "pending",
            comment: request.comment ?? null });
          return route.fulfill({ status: 201, json: requests[0] });
        }
        body = requests;
      }
      else if (path.includes("negotiate")) return route.fulfill({ status: 503 });
      await route.fulfill({ json: body });
    });
    await page.goto("/schedule");
    await dismissEmployeeInstallPrompt(page);
    const adjust = page.getByRole("button", { name: language === "en" ? "Request a shift correction" : language === "pl" ? "Poproś o korektę zmiany" : "Запросити коригування зміни" });
    await adjust.click();
    const dialog = page.getByRole("dialog");
    const copy = language === "pl" ? pl : language === "uk" ? uk : null;
    const next = dialog.getByRole("button", { name: copy?.Continue ?? "Continue", exact: true });
    const chooseSchedule = async () => {
      await dialog.getByRole("button", { name: /^August / }).click();
      await next.click();
    };
    await chooseSchedule();
    const dates = dialog.getByRole("radio");
    await expect(dates).toHaveCount(32);
    await expect(dialog.getByText("08:00 – 12:00", { exact: true })).toBeVisible();
    await expect(dialog.getByText("15:00 – 19:00", { exact: true })).toBeVisible();
    const selectDay = async (index: number) => {
      await dates.nth(index).scrollIntoViewIfNeeded();
      if (isMobile) await dates.nth(index).tap();
      else { await dates.nth(index).focus(); await page.keyboard.press("Enter"); }
    };
    await expect(dates.nth(14)).toBeDisabled();
    await expect(dates.nth(14)).toContainText("Other shop");
    await selectDay(13);
    await expect(dates.nth(13)).toBeChecked();
    await next.click();
    const inputs = dialog.locator('input[type="time"]');
    await expect(inputs).toHaveCount(2);
    await expect(inputs.first()).toHaveValue(""); await expect(inputs.last()).toHaveValue("");
    await expect(next).toBeDisabled();
    await inputs.first().fill("08:15"); await expect(next).toBeDisabled();
    await inputs.last().fill("16:45"); await expect(next).toBeEnabled();
    await next.click();
    const comment = dialog.getByRole("textbox", { name: commentLabels[language].input });
    const draft = "  " + correctionComment + "  ";
    await comment.fill(draft);
    await expect(comment).toHaveValue(draft);
    await next.click();
    const send = dialog.getByRole("button", { name: copy?.["Send request"] ?? "Send request", exact: true });
    await expect(dialog.getByText("08:15 – 16:45", { exact: true })).toBeVisible();
    await expect(send).toBeInViewport();
    expect(await dialog.evaluate(element => element.scrollWidth <= element.clientWidth)).toBe(true);
    const surface = dialog.locator('div').first();
    expect(await surface.evaluate(element => { const rect = element.getBoundingClientRect(); return element.scrollWidth <= element.clientWidth && rect.left >= 0 && rect.right <= innerWidth; })).toBe(true);
    await dialog.screenshot({ path: info.outputPath(`day-off-${width}-${language}.png`) });
    await send.click();
    if (language === "en") {
      await expect(dialog.getByText("Could not send this test request.")).toBeVisible();
      await dialog.getByRole("button", { name: "Back", exact: true }).last().click();
      await expect(comment).toHaveValue(draft);
      await next.click();
      await send.click();
    }
    await expect(dialog.getByText(copy?.["Pending approval"] ?? "Pending approval", { exact: true })).toBeVisible();
    await dialog.getByRole("button", { name: copy?.Done ?? "Done", exact: true }).click();
    await expect(dialog).toHaveCount(0);
    expect(submitted).toEqual({ scheduleId: 10, dayOfMonth: 13, requestedFromTime: "08:15", requestedToTime: "16:45", comment: correctionComment });
    await adjust.click();
    await chooseSchedule();
    await expect(dates.nth(13)).toBeDisabled();
    await expect(dates.nth(13)).toContainText(copy?.["Pending approval"] ?? "Pending approval");
    await expect(next).toBeDisabled();
  });
}
