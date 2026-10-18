import { expect, test, type Page } from "@playwright/test";

test.setTimeout(60_000);

const source = { scheduleId: 10, scheduleName: "F27", shopName: "Central", year: 2026, month: 10, dayOfMonth: 1,
  slotId: 100, fromTime: "09:00", toTime: "16:30", durationMinutes: 450, canSelect: true };
const returned = { ...source, scheduleId: 20, scheduleName: "F35", slotId: 200, dayOfMonth: 3, fromTime: "08:00", toTime: "18:00", durationMinutes: 600,
  suggestedFromTime: "09:00", suggestedToTime: "16:30", eligibleWindows: [{ fromTime: "09:00", toTime: "16:30" }, { fromTime: "09:15", toTime: "16:45" }] };
const employee = { id: 2, firstName: "Target", lastName: "Worker", displayName: "Target Worker" };
function schedule(actor: number) {
  const shift = actor === 1 ? source : returned;
  return { id: shift.scheduleId, containerId: 1, containerName: "Main", shopId: 1, shopName: shift.shopName, name: shift.scheduleName,
    year: shift.year, month: shift.month, publicationStatus: "public", allowSwap: true,
    employees: [{ id: actor, employeeId: actor, firstName: actor === 1 ? "Owner" : "Target", lastName: "Worker", displayName: actor === 1 ? "Owner Worker" : "Target Worker", displayOrder: 1 }],
    slots: [{ id: shift.slotId, dayOfMonth: shift.dayOfMonth, slotNo: 1, employeeId: actor, fromTime: shift.fromTime, toTime: shift.toTime, status: "ASSIGNED" }] };
}
type Offer = ReturnType<typeof offer>;
function offer(payload: Record<string, unknown>) {
  return { id: 1, scheduleId: 10, scheduleSlotId: 100, scheduleName: "F27", containerName: "Main", shopName: "Central", year: 2026, month: 10, dayOfMonth: 1,
    fromTime: "09:00", toTime: "16:30", fromEmployeeId: 1, fromEmployeeName: "Owner Worker", targetEmployeeName: "Target Worker", visibility: payload.targetEmployeeId ? "private" : "public",
    status: "open", createdAtUtc: "2026-10-01T00:00:00Z", shiftHours: 7.5, currentEmployeeHoursBefore: 7.5, currentEmployeeHoursAfter: 7.5,
    currentEmployeeWorkDaysBefore: 1, currentEmployeeWorkDaysAfter: 1, currentEmployeeFreeDaysBefore: 30, currentEmployeeFreeDaysAfter: 30,
    fromEmployeeHoursBefore: 7.5, fromEmployeeHoursAfter: 7.5, canAccept: false, canCancel: true, isCreatedByCurrentEmployee: true, isManagerCreated: false,
    ...(payload.requestKind === "exchange" ? { returnScheduleName: "F35", returnShopName: "Central", returnYear: 2026, returnMonth: 10, returnDayOfMonth: 3, returnShiftHours: 7.5 } : {}), ...payload };
}
async function mockApi(page: Page, actor: number, state: { offers: Offer[]; stale?: boolean; scheduleCount?: number; shiftCount?: number }) {
  await page.addInitScript(() => localStorage.setItem("gf3.auth.access-token", "swap-test"));
  await page.route("**/*", async route => {
    const path = new URL(route.request().url()).pathname;
    if (!path.startsWith("/api/")) return route.continue();
    let body: unknown = [];
    if (path === "/api/auth/session") body = { role: "employee", employeeId: actor, userName: actor === 1 ? "owner" : "target", displayName: actor === 1 ? "Owner Worker" : "Target Worker" };
    else if (path === "/api/account-language") body = { language: "en" };
    else if (path === "/api/employee-schedules") body = Array.from({ length: state.scheduleCount ?? 1 }, (_, i) => ({ ...schedule(actor), id: schedule(actor).id + i, name: i === 0 ? schedule(actor).name : `F${27+i}`, slots: Array.from({ length: state.shiftCount ?? 1 }, (_, j) => ({ ...schedule(actor).slots[0], id: schedule(actor).slots[0].id + i + j, dayOfMonth: j + 1 })) }));
    else if (path === "/api/employee-shift-swaps/employees") body = [employee];
    else if (path === "/api/employee-ui-state") body = { scheduleColumnOrders: {}, readNotificationIds: [], pinnedSwapIds: [] };
    else if (path === "/api/employee-shift-swaps/return-candidates") {
      const pageNo = Number(new URL(route.request().url()).searchParams.get("page") ?? 1);
      body = { items: pageNo === 1 ? [returned, { ...returned, slotId: 205, dayOfMonth: 4, fromTime: "09:00", toTime: "16:30", durationMinutes: 450 }, ...Array.from({ length: 29 }, (_, i) => ({ ...returned, slotId: i + 300, canSelect: false, unavailableReason: "Not enough hours." }))]
        : [{ ...returned, slotId: 999, dayOfMonth: 31 }], page: pageNo, pageSize: 30, hasMore: pageNo === 1 };
    } else if (path === "/api/employee-shift-swaps/preview") {
      const payload = route.request().postDataJSON();
      body = { requestKind: payload.requestKind, targetEmployeeId: payload.targetEmployeeId, targetEmployeeName: payload.targetEmployeeId ? "Target Worker" : "Everyone",
        source: { ...source, fromTime: payload.fromTime, toTime: payload.toTime, durationMinutes: (Number(payload.toTime.slice(0, 2)) * 60 + Number(payload.toTime.slice(3))) - (Number(payload.fromTime.slice(0, 2)) * 60 + Number(payload.fromTime.slice(3))) },
        return: payload.requestKind === "exchange" ? { ...returned, durationMinutes: 450, slotId: payload.returnScheduleSlotId, fromTime: payload.returnFromTime, toTime: payload.returnToTime } : null,
        projectedMonths: [{ year: 2026, month: 10, hoursBefore: 7.5, hoursAfter: payload.requestKind === "exchange" ? 7.5 : 0, workDaysBefore: 1, workDaysAfter: 1, freeDaysBefore: 30, freeDaysAfter: 30 }], isValid: true };
    } else if (path === "/api/employee-shift-swaps/1/accept") {
      if (state.stale) return route.fulfill({ status: 409, contentType: "application/problem+json", body: JSON.stringify({ title: "Return shift unavailable.", detail: "Return shift unavailable." }) });
      state.offers[0] = { ...state.offers[0], status: "accepted", canAccept: false, canCancel: false };
      body = state.offers[0];
    } else if (path === "/api/employee-shift-swaps") {
      if (route.request().method() === "POST") state.offers = [offer(route.request().postDataJSON())];
      body = route.request().method() === "POST" ? state.offers[0] : state.offers.map(item => ({ ...item,
        canAccept: actor === 2 && item.status === "open", canCancel: actor === 1 && item.status === "open", isCreatedByCurrentEmployee: actor === 1 }));
    } else if (path.includes("negotiate")) return route.fulfill({ status: 503 });
    await route.fulfill({ contentType: "application/json", body: JSON.stringify(body) });
  });
}

async function sourceSteps(page: Page) {
  await page.getByRole("button", { name: "Create offer", exact: true }).click();
  await page.locator("dialog:modal").getByRole("button", { name: /F27/ }).click();
  await expect(page.locator("dialog:modal")).toHaveCount(1);
  await page.getByRole("radio", { name: /09:00.*16:30/ }).click();
  await page.getByRole("button", { name: "Confirm shift" }).click();
  await expect(page.getByText("Thu 01 Oct · 09:00–16:30")).toBeVisible();
  await page.getByRole("button", { name: "Continue", exact: true }).click();
}

for (const viewport of [{ width: 375, height: 812 }, { width: 390, height: 844 }, { width: 430, height: 932 }, { width: 320, height: 640 }, { width: 1280, height: 800 }]) {
  for (const mode of ["public", "private", "exchange"]) {
    test(`${mode} wizard at ${viewport.width}x${viewport.height}`, async ({ page }) => {
      await page.setViewportSize(viewport);
      const state = { offers: [] as Offer[] };
      await mockApi(page, 1, state); await page.goto("/swap");
      await expect.poll(() => page.locator('[class*="swapBackdrop"]').evaluate(element => {
        const svg = element as SVGSVGElement;
        return Math.abs(svg.viewBox.baseVal.width - svg.getBoundingClientRect().width) < 1 &&
          Math.abs(svg.viewBox.baseVal.height - svg.getBoundingClientRect().height) < 1;
      })).toBeTruthy();
      const silhouette = await page.locator('[class*="swapBackdrop"] path').getAttribute("d");
      await sourceSteps(page);
      if (mode !== "public") {
        await page.getByRole("button", { name: "Specific employee", exact: true }).click();
        await expect(page.getByRole("button", { name: "Continue", exact: true })).toBeDisabled();
        await page.getByRole("searchbox", { name: "Search employee..." }).fill("Target");
        await page.getByRole("button", { name: /Target Worker/ }).click();
        if (mode === "exchange") {
          await page.getByRole("radio", { name: "Swap shifts" }).click();
          await page.getByRole("button", { name: "Choose their shift", exact: true }).click();
          await expect(page.locator("dialog:modal")).toHaveCount(1);
          await expect(page.getByRole("radio", { name: /Not enough hours/ }).first()).toBeDisabled();
          await page.getByRole("button", { name: "Load more" }).click();
          await page.getByRole("radio", { name: /31.10.2026/ }).scrollIntoViewIfNeeded();
          await expect(page.getByRole("radio", { name: /31.10.2026/ })).toBeInViewport({ ratio: 1 });
          await page.getByRole("radio", { name: /03.10.2026.*08:00.*18:00$/ }).click();
          await expect(page.getByRole("button", { name: "Full shift" })).toBeDisabled();
          await page.getByRole("button", { name: "Confirm selection" }).click();
        }
        await expect(page.getByRole("button", { name: "Save selection" })).toHaveCSS("background-color", "rgb(47, 109, 245)");
        await page.getByRole("button", { name: "Save selection" }).click();
      }
      await page.getByRole("button", { name: "Continue", exact: true }).click();
      const send = page.getByRole("button", { name: "Send request", exact: true });
      await expect(page.getByRole("heading", { name: "Review & confirm", exact: true })).toBeVisible();
      await expect(page.getByRole("region", { name: "Your shift (you give)", exact: true })).toHaveCount(1);
      await expect(page.getByRole("region", { name: "Employee shift (they give)", exact: true })).toHaveCount(mode === "exchange" ? 1 : 0);
      await expect(page.getByLabel("Reciprocal exchange", { exact: true })).toHaveCount(mode === "exchange" ? 1 : 0);
      expect(state.offers).toHaveLength(0);
      if (viewport.width === 390) {
        await page.screenshot({ path: test.info().outputPath(`review-${mode}-top.png`) });
        await page.getByText("Estimated, pending acceptance", { exact: true }).scrollIntoViewIfNeeded();
        await page.screenshot({ path: test.info().outputPath(`review-${mode}-statistics.png`) });
      }

      await expect(send).toBeEnabled(); await expect(send).toBeInViewport({ ratio: 1 });
      expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBeTruthy();
      const dialog = page.locator("dialog:modal");
      expect(await dialog.evaluate(element => element.scrollWidth <= element.clientWidth)).toBeTruthy();
      if (mode === "exchange" && viewport.width === 390) await page.screenshot({ path: test.info().outputPath("exchange-review.png"), fullPage: true });
      await send.click(); await expect(page.locator("dialog:modal")).toHaveCount(0);
      expect(state.offers[0].status).toBe("open");
      await expect(page.locator('[class*="swapBackdrop"] path')).toHaveAttribute("d", silhouette!);
      await page.reload();
      if (mode === "exchange") await expect(page.getByText(/In return.*F35/)).toBeVisible();
      await expect(page.getByRole("button", { name: "Swap history", exact: true })).toBeVisible();
    });
  }
}

for (const stale of [false, true]) {
  test(`two sessions read both transfers and ${stale ? "reject stale return" : "accept once"}`, async ({ page, browser }) => {
    const state = { offers: [offer({ requestKind: "exchange", targetEmployeeId: 2, returnScheduleId: 20, returnScheduleSlotId: 200, returnFromTime: "09:00", returnToTime: "16:30" })], stale };
    await mockApi(page, 1, state); await page.goto("/swap");
    const recipientContext = await browser.newContext(); const recipient = await recipientContext.newPage();
    await mockApi(recipient, 2, state); await recipient.goto(new URL("/swap", page.url()).href);
    await recipient.getByRole("button", { name: "Accept", exact: true }).click();
    const confirmation = recipient.getByRole("dialog", { name: "Accept this shift?" });
    await expect(confirmation).toContainText("F27"); await expect(confirmation).toContainText("F35");
    await confirmation.getByRole("button", { name: "Accept shift", exact: true }).click();
    if (stale) await expect(recipient.getByText("Return shift unavailable.", { exact: true }).first()).toBeVisible();
    else { await page.reload(); await page.getByRole("button", { name: "Swap history", exact: true }).click(); await expect(page.getByText(/In return.*F35/)).toBeVisible(); }
    expect(state.offers[0].status).toBe(stale ? "open" : "accepted");
    await recipientContext.close();
  });
}

for (const width of [320, 375, 390, 430]) {
  for (const scheduleCount of [1, 2, 6, 20]) {
    test(`schedule design ${width}px with ${scheduleCount} schedules`, async ({ page }) => {
      await page.setViewportSize({ width, height: width === 320 ? 640 : 844 });
      await mockApi(page, 1, { offers: [], scheduleCount });
      await page.goto("/swap");
      await page.getByRole("button", { name: "Create offer", exact: true }).click();
      const dialog = page.locator("dialog:modal");
      const next = dialog.getByRole("button", { name: "Continue", exact: true });
      await expect(next).toBeDisabled();
      await expect(dialog.getByRole("button", { name: "Back", exact: true })).toHaveCount(0);
      const list = dialog.getByRole("region", { name: "Schedules", exact: true });
      await expect(list.getByRole("button")).toHaveCount(scheduleCount);
      await list.getByRole("button").last().scrollIntoViewIfNeeded();
      await expect(next).toBeInViewport({ ratio: 1 });
      expect(await dialog.evaluate(el => el.scrollWidth <= el.clientWidth && el.scrollHeight <= el.clientHeight + 1)).toBeTruthy();
      await list.getByRole("button").first().click();
      await page.getByRole("radio", { name: /09:00.*16:30/ }).click();
      await page.getByRole("button", { name: "Confirm shift" }).click();
      await expect(dialog.getByText("Full shift", { exact: true })).toBeVisible();
      await expect(next).toBeEnabled();
      await dialog.getByRole("button", { name: "Change shift" }).click();
      await expect(page.getByRole("radio", { name: /09:00.*16:30/ })).toBeChecked();
      await page.getByRole("button", { name: "Confirm shift" }).click();
      await expect(dialog.getByRole("button", { name: "Change shift" })).toBeInViewport({ ratio: 1 });
      await dialog.getByRole("button", { name: "Change shift" }).click();
      await page.getByRole("button", { name: "Custom period", exact: true }).click();
      await page.getByRole("button", { name: "Increase From", exact: true }).click();
      await page.getByRole("button", { name: "Confirm shift" }).click();
      await expect(dialog.getByText("Custom period", { exact: true })).toBeVisible();
      await expect(dialog.getByText(/09:15/)).toBeVisible();
      await page.screenshot({ path: test.info().outputPath("schedule-selected.png") });
      await next.click();
      await expect(page.getByRole("heading", { name: "Give to whom?" })).toBeVisible();
      await page.getByRole("button", { name: "Continue", exact: true }).click();
      await expect(page.getByRole("button", { name: "Send request" })).toBeEnabled();
    });
  }
}


for (const width of [320, 375, 390, 430]) {
  test(`shift picker design ${width}px`, async ({ page }) => {
    await page.setViewportSize({ width, height: width === 320 ? 640 : 844 });
    await mockApi(page, 1, { offers: [{ ...offer({}), scheduleSlotId: 101 }], shiftCount: 20 });
    await page.goto("/swap");
    await page.getByRole("button", { name: "Create offer", exact: true }).click();
    await page.locator("dialog:modal").getByRole("button", { name: /F27/ }).click();
    const dialog = page.locator("dialog:modal");
    await expect(dialog.getByRole("button", { name: "Back", exact: true })).toHaveCount(0);
    const rows = dialog.getByRole("radio");
    await expect(rows).toHaveCount(20);
    await expect(rows.nth(1)).toBeDisabled();
    await rows.last().scrollIntoViewIfNeeded();
    await expect(dialog.getByRole("button", { name: "Confirm shift" })).toBeInViewport({ ratio: 1 });
    await rows.first().click();
    await expect(dialog.getByRole("button", { name: "Full shift" })).toHaveAttribute("aria-pressed", "true");
    await page.screenshot({ path: test.info().outputPath("picker-full.png") });
    await dialog.getByRole("button", { name: "Custom period" }).click();
    await dialog.getByRole("button", { name: "Increase From" }).click();
    await expect(dialog.locator('input[type="time"]').first()).toHaveValue("09:15");
    await expect(dialog.getByRole("button", { name: "Confirm shift" })).toBeInViewport({ ratio: 1 });
    expect(await dialog.evaluate(el => el.scrollWidth <= el.clientWidth && el.scrollHeight <= el.clientHeight + 1)).toBeTruthy();
    await page.screenshot({ path: test.info().outputPath("picker-custom.png") });
    await dialog.getByRole("button", { name: "Confirm shift" }).click();
    await expect(dialog.getByText(/Thu 01 Oct.*09:15/)).toBeVisible();
    await dialog.getByRole("button", { name: "Change shift" }).click();
    await expect(dialog.locator('input[type="time"]').first()).toHaveValue("09:15");
    await dialog.getByRole("button", { name: "Close", exact: true }).click();
    await expect(dialog.getByRole("heading", { name: "Choose a schedule" })).toBeVisible();
  });
}

for (const width of [320, 375, 390, 430]) {
  test(`recipient design ${width}px`, async ({ page }) => {
    await page.setViewportSize({ width, height: width === 320 ? 640 : 844 });
    await mockApi(page, 1, { offers: [] }); await page.goto("/swap");
    await sourceSteps(page);
    const dialog = page.locator("dialog:modal");
    const next = dialog.getByRole("button", { name: "Continue", exact: true });
    await expect(next).toBeEnabled();
    await page.screenshot({ path: test.info().outputPath("recipient.png") });
    await dialog.getByRole("button", { name: "Back", exact: true }).click();
    await expect(dialog.getByRole("button", { name: "Change shift" })).toBeVisible();
    await next.click();
    await dialog.getByRole("button", { name: "Specific employee", exact: true }).click();
    await expect(next).toBeDisabled();
    await dialog.getByRole("searchbox").fill("Target");
    await dialog.getByRole("button", { name: /Target Worker/ }).click();
    await expect(dialog.getByRole("button", { name: "Save selection" })).toBeEnabled();
    await dialog.getByRole("radio", { name: "Swap shifts", exact: true }).click();
    await expect(dialog.getByRole("button", { name: "Save selection" })).toBeDisabled();
    await page.screenshot({ path: test.info().outputPath("request-type.png") });
    await dialog.getByRole("button", { name: "Choose their shift", exact: true }).click();
    await expect(dialog.getByRole("button", { name: "Confirm selection" })).toBeDisabled();
    await expect(dialog.getByRole("radio", { name: /Not enough hours/ }).first()).toBeDisabled();
    await dialog.getByRole("radio", { name: /04.10.2026.*09:00.*16:30$/ }).click();
    await expect(dialog.getByRole("button", { name: "Full shift" })).toBeEnabled();
    await expect(dialog.getByText("Time to receive", { exact: true })).toBeVisible();
    const confirm = dialog.getByRole("button", { name: "Confirm selection" });
    await expect(confirm).toBeInViewport({ ratio: 1 });
    expect(await dialog.evaluate(el => el.scrollWidth <= el.clientWidth && el.scrollHeight <= el.clientHeight + 1)).toBeTruthy();
    await page.screenshot({ path: test.info().outputPath("their-shift-full.png") });
    await confirm.click();
    await dialog.getByRole("button", { name: "Change", exact: true }).click();
    await dialog.getByRole("radio", { name: /03.10.2026.*08:00.*18:00$/ }).click();
    await dialog.getByRole("button", { name: "Increase From" }).click();
    await expect(dialog.locator('input[type="time"]').first()).toHaveValue("09:15");
    await expect(dialog.locator('input[type="time"]').last()).toHaveValue("16:45");
    await expect(confirm).toBeInViewport({ ratio: 1 });
    await page.screenshot({ path: test.info().outputPath("their-shift-custom.png") });
    await confirm.click();
    await dialog.getByRole("radio", { name: "Give shift", exact: true }).click();
    await dialog.getByRole("radio", { name: "Swap shifts", exact: true }).click();
    await expect(dialog.getByText(/09:15.*16:45/)).toBeVisible();
    await dialog.getByRole("button", { name: "Save selection" }).click();
    await dialog.getByRole("button", { name: /Target Worker/ }).click();
    await dialog.getByRole("button", { name: "Change", exact: true }).click();
    await expect(dialog.locator('input[type="time"]').first()).toHaveValue("09:15");
    await dialog.getByRole("button", { name: "Close", exact: true }).click();
    await expect(dialog.getByRole("heading", { name: "Request type", exact: true })).toBeFocused();
    await dialog.getByRole("button", { name: "Close", exact: true }).click();
    await expect(next).toBeEnabled(); await expect(next).toBeInViewport({ ratio: 1 });
    await next.click(); await expect(dialog.getByRole("button", { name: "Send request" })).toBeEnabled();
  });
}

for (const width of [320, 375, 390, 430]) {
  test(`custom Review design ${width}px`, async ({ page }) => {
    await page.setViewportSize({ width, height: width === 320 ? 640 : 844 });
    const state = { offers: [] as Offer[] }; await mockApi(page, 1, state); await page.goto("/swap");
    await page.getByRole("button", { name: "Create offer", exact: true }).click();
    const dialog = page.locator("dialog:modal");
    await dialog.getByRole("button", { name: /F27/ }).click();
    await dialog.getByRole("radio", { name: /09:00.*16:30/ }).click();
    await dialog.getByRole("button", { name: "Custom period" }).click();
    await dialog.getByRole("button", { name: "Increase From" }).click();
    await dialog.getByRole("button", { name: "Confirm shift" }).click();
    await dialog.getByRole("button", { name: "Continue", exact: true }).click();
    await dialog.getByRole("button", { name: "Continue", exact: true }).click();
    const card = dialog.getByRole("region", { name: "Your shift (you give)", exact: true });
    await expect(card).toContainText("09:15"); await expect(card).toContainText("7.25h");
    await expect(card.getByText("Custom period", { exact: true })).toBeVisible();
    await dialog.getByRole("button", { name: "Edit schedule", exact: true }).click();
    await expect(dialog.getByText("Custom period", { exact: true })).toBeVisible();
    await dialog.getByRole("button", { name: "Continue", exact: true }).click();
    await dialog.getByRole("button", { name: "Continue", exact: true }).click();
    await dialog.getByRole("button", { name: "Edit recipient", exact: true }).click();
    await expect(dialog.getByRole("button", { name: "Everyone", exact: true })).toHaveAttribute("aria-pressed", "true");
    await dialog.getByRole("button", { name: "Continue", exact: true }).click();
    const scroller = dialog.getByRole("region", { name: "Review details" });
    await dialog.getByText("Estimated, pending acceptance", { exact: true }).scrollIntoViewIfNeeded();
    await expect(dialog.getByText("7.5 → 0", { exact: true })).toBeVisible();
    await expect(dialog.getByRole("button", { name: "Send request", exact: true })).toBeInViewport({ ratio: 1 });
    expect(await dialog.evaluate(el => el.scrollWidth <= el.clientWidth && el.scrollHeight <= el.clientHeight + 1)).toBeTruthy();
    expect(await scroller.evaluate(el => el.scrollWidth <= el.clientWidth)).toBeTruthy();
    expect(state.offers).toHaveLength(0);
    await page.screenshot({ path: test.info().outputPath("review-custom.png") });
    await dialog.getByRole("button", { name: "Send request", exact: true }).click();
    expect(state.offers[0]).toMatchObject({ fromTime: "09:15", toTime: "16:30", requestKind: "give" });
  });
}
