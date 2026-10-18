import { writeFile } from "node:fs/promises";
import { expect, test, type Page } from "@playwright/test";

test.setTimeout(60_000);

const source = { scheduleId: 10, scheduleName: "F27", shopName: "Central", year: 2026, month: 10, dayOfMonth: 1,
  slotId: 100, fromTime: "09:00", toTime: "16:30", durationMinutes: 450, canSelect: true };
const returned = { ...source, scheduleId: 20, scheduleName: "F35", slotId: 200, dayOfMonth: 3, fromTime: "08:00", toTime: "18:00", durationMinutes: 600,
  suggestedFromTime: "08:00", suggestedToTime: "18:00", eligibleWindows: [{ fromTime: "08:00", toTime: "18:00" }] };
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
    fromEmployeeHoursBefore: 7.5, fromEmployeeHoursAfter: 7.5, canAccept: false, canCancel: true, isCreatedByCurrentEmployee: true, isManagerCreated: false, isScheduleLocked: false, acceptanceUnavailableReason: "",
    ...(payload.requestKind === "exchange" ? { returnScheduleName: "F35", returnShopName: "Central", returnYear: 2026, returnMonth: 10, returnDayOfMonth: 3, returnShiftHours: 7.5 } : {}), ...payload };
}
async function mockApi(page: Page, actor: number, state: { offers: Offer[]; stale?: boolean; scheduleCount?: number; shiftCount?: number; employeeCount?: number; multipleReturnSchedules?: boolean; previewState?: "loading" | "error" | "invalid" | "stale"; ownerName?: string; targetName?: string; language?: "en" | "pl" }) {
  await page.addInitScript(() => localStorage.setItem("gf3.auth.access-token", "swap-test"));
  await page.route("**/*", async route => {
    const path = new URL(route.request().url()).pathname;
    if (!path.startsWith("/api/")) return route.continue();
    let body: unknown = [];
    if (path === "/api/auth/session") body = { role: "employee", employeeId: actor, userName: actor === 1 ? "owner" : "target", displayName: actor === 1 ? state.ownerName ?? "Owner Worker" : state.targetName ?? "Target Worker" };
    else if (path === "/api/account-language") body = { language: state.language ?? "en" };
    else if (path === "/api/employee-schedules") body = Array.from({ length: state.scheduleCount ?? 1 }, (_, i) => ({ ...schedule(actor), id: schedule(actor).id + i, name: i === 0 ? schedule(actor).name : `F${27+i}`, slots: Array.from({ length: state.shiftCount ?? 1 }, (_, j) => ({ ...schedule(actor).slots[0], id: schedule(actor).slots[0].id + i + j, dayOfMonth: j + 1 })) }));
    else if (path === "/api/employee-shift-swaps/employees") body = [{ ...employee, displayName: state.targetName ?? employee.displayName }, ...(state.multipleReturnSchedules ? [{ ...employee, id: 3, displayName: "Aaron Worker" }] : []), ...Array.from({ length: (state.employeeCount ?? 1) - 1 }, (_, i) => ({ ...employee, id: i + 3, displayName: `Zuzanna Aleksandra Kowalska-Wiśniewska ${i}` }))];
    else if (path === "/api/employee-ui-state") body = { scheduleColumnOrders: {}, readNotificationIds: [], pinnedSwapIds: [] };
    else if (path === "/api/employee-shift-swaps/return-candidates") {
      const pageNo = Number(new URL(route.request().url()).searchParams.get("page") ?? 1);
      body = { items: pageNo === 1 ? [returned, { ...returned, slotId: 205, dayOfMonth: 4, fromTime: "09:00", toTime: "16:30", durationMinutes: 450 }, ...Array.from({ length: 29 }, (_, i) => ({ ...returned, slotId: i + 300, canSelect: false, unavailableReason: "Not enough hours." }))]
        : [{ ...returned, slotId: 999, dayOfMonth: state.multipleReturnSchedules ? 5 : 31, ...(state.multipleReturnSchedules ? { scheduleId: 30, scheduleName: "F99 — Aleksandra Maria Kowalska-Wiśniewska", month: 11, fromTime: "10:00", toTime: "16:00", durationMinutes: 360, suggestedFromTime: "10:00", suggestedToTime: "16:00", eligibleWindows: [{ fromTime: "10:00", toTime: "16:00" }] } : {}) }], page: pageNo, pageSize: 30, hasMore: pageNo === 1 };
    } else if (path === "/api/employee-shift-swaps/preview") {
      if (state.previewState === "loading") return;
      if (state.previewState === "error") return route.fulfill({ status: 503, contentType: "application/json", body: JSON.stringify({ title: "Preview unavailable." }) });
      const payload = route.request().postDataJSON();
      body = { requestKind: payload.requestKind, targetEmployeeId: payload.targetEmployeeId, targetEmployeeName: payload.targetEmployeeId ? "Target Worker" : "Everyone",
        source: { ...source, slotId: state.previewState === "stale" ? 9999 : source.slotId, fromTime: payload.fromTime, toTime: payload.toTime, durationMinutes: (Number(payload.toTime.slice(0, 2)) * 60 + Number(payload.toTime.slice(3))) - (Number(payload.fromTime.slice(0, 2)) * 60 + Number(payload.fromTime.slice(3))) },
        return: payload.requestKind === "exchange" ? { ...returned, scheduleId: payload.returnScheduleId, durationMinutes: (Number(payload.returnToTime.slice(0, 2)) * 60 + Number(payload.returnToTime.slice(3))) - (Number(payload.returnFromTime.slice(0, 2)) * 60 + Number(payload.returnFromTime.slice(3))), slotId: payload.returnScheduleSlotId, fromTime: payload.returnFromTime, toTime: payload.returnToTime } : null,
        projectedMonths: [{ year: 2026, month: 10, hoursBefore: 7.5, hoursAfter: payload.requestKind === "exchange" ? 7.5 : 0, workDaysBefore: 1, workDaysAfter: 1, freeDaysBefore: 30, freeDaysAfter: 30 }], isValid: state.previewState !== "invalid" };
    } else if (path === "/api/employee-shift-swaps/1/accept") {
      if (state.stale) return route.fulfill({ status: 409, contentType: "application/problem+json", body: JSON.stringify({ title: "Return shift unavailable.", detail: "Return shift unavailable." }) });
      state.offers[0] = { ...state.offers[0], status: "accepted", canAccept: false, canCancel: false };
      body = state.offers[0];
    } else if (path === "/api/employee-shift-swaps/1/cancel") {
      state.offers[0] = { ...state.offers[0], status: "cancelled", canAccept: false, canCancel: false };
      body = state.offers[0];
    } else if (path === "/api/employee-shift-swaps") {
      if (route.request().method() === "POST") state.offers = [offer(route.request().postDataJSON())];
      body = route.request().method() === "POST" ? state.offers[0] : state.offers.map(item => ({ ...item,
        canAccept: actor === 2 && item.status === "open" && !item.isScheduleLocked && !item.acceptanceUnavailableReason, canCancel: actor === 1 && item.status === "open", isCreatedByCurrentEmployee: actor === 1 }));
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

test("custom private Give sends exact payload and cancellation appears in History", async ({ page }) => {
  const state: { offers: Offer[] } = { offers: [] };
  await mockApi(page, 1, state); await page.goto("/swap");
  await page.getByRole("button", { name: "Create offer", exact: true }).click();
  await page.getByRole("button", { name: /F27/ }).click();
  await page.getByRole("radio").click();
  await page.getByRole("button", { name: "Custom period" }).click();
  await page.getByRole("button", { name: "Increase From" }).click();
  await page.getByRole("button", { name: "Decrease To" }).click();
  await page.getByRole("button", { name: "Confirm shift" }).click();
  await page.getByRole("button", { name: "Continue", exact: true }).click();
  await page.getByRole("button", { name: "Specific employee" }).click();
  await page.getByRole("searchbox", { name: "Search employee...", exact: true }).fill("Target");
  await page.getByRole("button", { name: "Target Worker", exact: true }).click();
  await page.getByRole("button", { name: "Save selection" }).click();
  await page.getByRole("button", { name: "Continue", exact: true }).click();
  const sent = page.waitForRequest(r => new URL(r.url()).pathname === "/api/employee-shift-swaps" && r.method() === "POST");
  await page.getByRole("button", { name: "Send request" }).click();
  expect((await sent).postDataJSON()).toEqual({ requestKind: "give", scheduleId: 10, scheduleSlotId: 100, fromTime: "09:15", toTime: "16:15", targetEmployeeId: 2 });
  await page.getByRole("button", { name: "View offer details for F27" }).click();
  await page.getByRole("button", { name: "Cancel offer", exact: true }).click();
  await expect(page.getByRole("dialog", { name: "Offer details" })).toHaveCount(0);
  expect(state.offers[0].status).toBe("cancelled");
  await page.getByRole("button", { name: "Swap history", exact: true }).click();
  await expect(page.getByRole("dialog", { name: "Recent swap activity" })).toContainText("Cancelled");
});

test("eligible recipient accepts a manager manual opening", async ({ page }) => {
  const state = { offers: [offer({ isManagerCreated: true, fromEmployeeId: null, fromEmployeeName: "Manual counter", manualColumnName: "Manual counter", requestKind: "give" })] };
  await mockApi(page, 2, state); await page.goto("/swap");
  await page.getByRole("button", { name: "View offer details for F27" }).click();
  await page.getByRole("button", { name: "Accept shift", exact: true }).click();
  const confirmation = page.getByRole("dialog", { name: "Accept this shift?" });
  await expect(confirmation).toContainText("Manual counter");
  await confirmation.getByRole("button", { name: "Accept shift", exact: true }).click();
  await expect(confirmation).toHaveCount(0); expect(state.offers[0].status).toBe("accepted");
});

test("release notice preserves an active wizard until explicit update", async ({ page, request }) => {
  const metadata = await request.get("/version.json");
  test.skip(!metadata.headers()["content-type"]?.includes("application/json"), "Release polling requires a production preview.");
  const original = await metadata.json();
  let buildId = original.buildId;
  await mockApi(page, 1, { offers: [] });
  await page.route("**/version.json", route => route.fulfill({ contentType: "application/json", body: JSON.stringify({ buildId }) }));
  await page.goto("/swap");
  await sourceSteps(page);
  buildId = "wizard-new-release";
  await page.evaluate(() => window.dispatchEvent(new Event("focus")));
  await expect(page.locator("aside button").filter({ hasText: "Update now" })).toBeVisible();
  await expect(page.locator("dialog:modal")).toHaveCount(1);
  expect(await page.evaluate(() => localStorage.getItem("gf3.auth.access-token"))).toBe("swap-test");
  await page.locator("dialog:modal header > button").last().click();
  await Promise.all([page.waitForEvent("domcontentloaded"), page.getByRole("button", { name: "Update now" }).click()]);
  expect(await page.evaluate(() => localStorage.getItem("gf3.auth.access-token"))).toBe("swap-test");
});

async function geometry(page: Page, width: number, action: string | RegExp) {
  const dialog = page.locator("dialog:modal");
  await expect(dialog).toHaveCount(1);
  await expect.poll(() => dialog.evaluate(element => {
    const box = element.getBoundingClientRect(); const vv = window.visualViewport;
    const top = vv?.offsetTop ?? 0; const height = vv?.height ?? innerHeight;
    return box.top >= top - 1 && box.bottom <= top + height + 1 && box.left >= 0 && box.right <= innerWidth;
  })).toBeTruthy();
  expect((await dialog.boundingBox())!.width).toBeCloseTo(width, 0);
  expect(width).toBeLessThanOrEqual(430);
  await expect(dialog.locator("header > button").last()).toBeInViewport({ ratio: 1 });
  const primary = dialog.getByRole("button", { name: action, exact: true });
  await primary.scrollIntoViewIfNeeded();
  await expect(primary).toBeInViewport({ ratio: 1 });
  if (await primary.isEnabled()) await primary.click({ trial: true });
  await expect(dialog.locator("header > button").last()).toBeInViewport({ ratio: 1 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBeTruthy();
  expect(await dialog.evaluate(element => [element, ...element.querySelectorAll<HTMLElement>("[data-scroll-top] > div, input, fieldset, button")].every(node => {
    const box = node.getBoundingClientRect(); const modal = element.getBoundingClientRect();
    return node.scrollWidth <= node.clientWidth + 1 && box.left >= modal.left - 1 && box.right <= modal.right + 1;
  }))).toBeTruthy();
}

async function diagnostic(page: Page, label: string) {
  const path = test.info().outputPath(`${label}.png`);
  await page.evaluate(() => new Promise<void>(resolve => requestAnimationFrame(() => requestAnimationFrame(() => resolve()))));
  const geometryPath = test.info().outputPath(`${label}-viewport.json`);
  const geometry = await page.locator("dialog:modal").evaluate(dialog => {
    const box = dialog.getBoundingClientRect(); const vv = window.visualViewport;
    const close = dialog.querySelector<HTMLElement>('header > button:last-child')!;
    const closeBox = close.getBoundingClientRect();
    return { windowY: window.scrollY, visibleTop: vv?.offsetTop, visibleHeight: vv?.height,
      dialog: { x: box.x, y: box.y, width: box.width, height: box.height, scrollTop: dialog.scrollTop },
      close: { x: closeBox.x, y: closeBox.y, width: closeBox.width, height: closeBox.height,
        hit: close.contains(document.elementFromPoint(closeBox.x + closeBox.width / 2, closeBox.y + closeBox.height / 2)) },
      innerScroll: [...dialog.querySelectorAll<HTMLElement>('div')].filter(el => el.scrollTop > 0)
        .map(el => ({ className: el.className, scrollTop: el.scrollTop })) };
  });
  await writeFile(geometryPath, JSON.stringify(geometry, null, 2));
  await test.info().attach(`${label}-viewport`, { path: geometryPath, contentType: "application/json" });
  await page.screenshot({ path });
  await test.info().attach(label, { path, contentType: "image/png" });
}

for (const viewport of [{ width: 320, height: 640 }, { width: 375, height: 667 }, { width: 390, height: 844 },
  { width: 430, height: 932 }, { width: 667, height: 375 }, { width: 1280, height: 800 }]) {
  test(`mobile polish stability ${viewport.width}x${viewport.height}`, async ({ page, browserName }) => {
    test.setTimeout(120_000);
    await page.setViewportSize(viewport);
    await mockApi(page, 1, { offers: Array.from({ length: 15 }, (_, i) => ({ ...offer({}), id: i + 1, scheduleSlotId: 900 + i })), shiftCount: 20 });
    await page.goto("/swap");
    const background = page.locator('[data-swap-offers-scroll]');
    await expect(background.locator("article")).toHaveCount(15);
    await background.evaluate(el => { el.scrollTop = 80; });
    const saved = await background.evaluate(el => ({ top: el.scrollTop, overflow: el.style.overflow, y: window.scrollY }));
    const backgroundBox = (await background.boundingBox())!;
    await page.getByRole("button", { name: "Create offer", exact: true }).click();
    const modal = page.locator("dialog:modal");
    const progress = modal.getByRole("list", { name: "Request progress" });
    const headerProgress = await progress.evaluate(el => el.closest("header") !== null);
    const backgroundOverflow = await background.evaluate(el => getComputedStyle(el).overflowY);
    expect((await background.boundingBox())!.x).toBeCloseTo(backgroundBox.x, 0);
    expect((await background.boundingBox())!.y).toBeCloseTo(backgroundBox.y, 0);
    const closeBox = (await modal.getByRole("button", { name: "Close", exact: true }).boundingBox())!;
    const progressBox = (await progress.boundingBox())!;
    expect(progressBox.x + progressBox.width).toBeLessThanOrEqual(closeBox.x);
    expect(Math.abs(progressBox.y + progressBox.height / 2 - closeBox.y - closeBox.height / 2)).toBeLessThan(2);
    await modal.getByRole("button", { name: /F27/ }).click();
    await diagnostic(page, "polish-01-initial");
    const list = modal.getByRole("radiogroup", { name: "Shifts", exact: true });
    const measure = () => modal.evaluate(el => {
      const box = (selector: string) => {
        const node = el.querySelector<HTMLElement>(selector)!; const rect = node.getBoundingClientRect();
        return { x: rect.x, y: rect.y, width: rect.width, height: rect.height, display: getComputedStyle(node).display };
      };
      return { period: box('[class*="period_"]'), title: box('legend, [data-period-heading]'),
        modes: box('[class*="modes_"]'), controls: el.querySelector('[class*="controls_"]') ? box('[class*="controls_"]') : null,
        buttons: [...el.querySelectorAll<HTMLElement>('[class*="modes_"] button')].map(node => ({ width: node.offsetWidth, height: node.offsetHeight })) };
    });
    await list.getByRole("radio").first().click();
    const full = await measure(); await diagnostic(page, "polish-02-full");
    if (viewport.height >= 600) {
      await list.evaluate(el => { el.scrollTop = 100; });
      await expect(list.locator("..")).toHaveAttribute("data-scroll-top", "true");
      expect(await measure()).toEqual(full);
      await list.evaluate(el => { el.scrollTop = 0; });
    }
    await modal.getByRole("button", { name: "Custom period", exact: true }).click();
    const custom = await measure(); await diagnostic(page, "polish-03-custom");
    if (viewport.height >= 600) {
      await list.evaluate(el => { el.scrollTop = el.scrollHeight / 2; });
      await expect(list.locator("..")).toHaveAttribute("data-scroll-top", "true");
      await diagnostic(page, "polish-04-middle");
      const after = await measure();
      const report = test.info().outputPath("picker-geometry.json");
      await writeFile(report, JSON.stringify({ full, custom, after, headerProgress, backgroundOverflow }, null, 2));
      await test.info().attach("picker-geometry", { path: report, contentType: "application/json" });
      expect(after).toEqual(custom);
      await expect(list.locator("..")).toHaveAttribute("data-scroll-bottom", "true");
      expect(await list.locator("..").evaluate(el => getComputedStyle(el, "::before").pointerEvents)).toBe("none");
      await list.evaluate(el => { el.scrollTop = el.scrollHeight; });
      await expect(list.locator("..")).toHaveAttribute("data-scroll-bottom", "false");
      await diagnostic(page, "polish-05-bottom");
      if (!(test.info().project.use.isMobile && browserName === "webkit")) {
        await list.hover(); await page.mouse.wheel(0, 800);
        await list.evaluate(el => { el.scrollTop = 0; });
        await page.mouse.wheel(0, -800);
        await page.mouse.move(1, 1); await page.mouse.wheel(0, 800);
      }
    } else {
      await test.info().attach("picker-geometry", { body: JSON.stringify({ full, custom, headerProgress, backgroundOverflow }, null, 2), contentType: "application/json" });
    }
    expect.soft(headerProgress).toBeTruthy();
    expect.soft(backgroundOverflow).toBe("hidden");
    expect.soft(full.title.width).toBeGreaterThan(full.period.width * .8);
    expect.soft(full.title.height).toBeLessThanOrEqual(24);
    expect.soft(Math.abs(full.buttons[0].width - full.buttons[1].width)).toBeLessThanOrEqual(1);
    expect.soft(full.buttons.every(button => button.height >= 44 && button.height <= 52)).toBeTruthy();
    expect(await background.evaluate(el => ({ top: el.scrollTop, overflow: el.style.overflow, y: window.scrollY })) )
      .toEqual({ ...saved, overflow: "hidden" });
    for (let repeat = 0; repeat < 2; repeat++) {
      await modal.getByRole("button", { name: "Full shift", exact: true }).click();
      await modal.getByRole("button", { name: "Custom period", exact: true }).click();
    }
    const toggled = await measure();
    expect(toggled.period.width).toBe(custom.period.width);
    expect(toggled.period.height).toBe(custom.period.height);
    expect(toggled.buttons).toEqual(custom.buttons);
    for (const input of await modal.locator('input[type="time"]').all()) {
      await input.scrollIntoViewIfNeeded(); await expect(input).toBeInViewport({ ratio: 1 });
      expect((await input.boundingBox())!.height).toBeGreaterThanOrEqual(44);
    }
    expect(await modal.evaluate(el => el.scrollWidth <= el.clientWidth)).toBeTruthy();
    await modal.getByRole("button", { name: "Confirm shift", exact: true }).click();
    await modal.getByRole("button", { name: "Continue", exact: true }).click();
    await diagnostic(page, "polish-06-recipient");
    await modal.getByRole("button", { name: "Continue", exact: true }).click();
    await expect(modal.getByRole("button", { name: "Send request", exact: true })).toBeEnabled();
    await diagnostic(page, "polish-07-review");
    expect.soft(await modal.getByText("Your statistics", { exact: true }).count()).toBe(0);
    await modal.getByRole("button", { name: "Edit recipient", exact: true }).click();
    await modal.getByRole("button", { name: "Specific employee", exact: true }).click();
    await modal.getByRole("button", { name: /Target Worker/ }).click();
    await modal.getByRole("radio", { name: "Swap shifts", exact: true }).click();
    await modal.getByRole("button", { name: "Choose their shift", exact: true }).click();
    await modal.getByRole("button", { name: /F35/ }).click();
    await list.getByRole("radio", { name: /04.10.2026.*09:00.*16:30$/ }).click();
    const receiveFull = await measure();
    await expect(modal.getByRole("group", { name: "Time to receive", exact: true })).toBeVisible();
    if (viewport.height >= 600) {
      await list.evaluate(el => { el.scrollTop = el.scrollHeight; });
      await expect(list.locator("..")).toHaveAttribute("data-scroll-bottom", "false");
      expect(await measure()).toEqual(receiveFull);
    }
    await list.getByRole("radio", { name: /03.10.2026.*08:00.*18:00$/ }).click();
    await modal.getByRole("button", { name: "Custom period", exact: true }).click();
    const receiveCustom = await measure();
    if (viewport.height >= 600) {
      await list.evaluate(el => { el.scrollTop = el.scrollHeight; });
      await expect(list.locator("..")).toHaveAttribute("data-scroll-bottom", "false");
      expect(await measure()).toEqual(receiveCustom);
    }
    expect(receiveCustom.title.width).toBe(receiveCustom.period.width);
    expect(receiveCustom.title.height).toBeLessThanOrEqual(24);
    expect(receiveCustom.buttons.every(button => button.height >= 44 && button.height <= 52)).toBeTruthy();
    await diagnostic(page, "polish-08-receive");
    await modal.getByRole("button", { name: "Confirm selection", exact: true }).click();
    await modal.getByRole("button", { name: "Save selection", exact: true }).click();
    await modal.getByRole("button", { name: "Continue", exact: true }).click();
    await expect(modal.getByRole("region", { name: "Employee shift (they give)", exact: true })).toHaveCount(1);
    await expect(modal.getByLabel("Reciprocal exchange", { exact: true })).toHaveCount(1);
    await expect(modal.getByText("Your statistics", { exact: true })).toHaveCount(0);
    await diagnostic(page, "polish-09-exchange-review");
    await modal.getByRole("button", { name: "Close", exact: true }).click();
    expect(await background.evaluate(el => ({ top: el.scrollTop, overflow: el.style.overflow, y: window.scrollY }))).toEqual(saved);
    if (!(test.info().project.use.isMobile && browserName === "webkit")) {
      const scrollBox = (await background.boundingBox())!;
      await background.hover({ position: { x: scrollBox.width - 12, y: Math.min(10, scrollBox.height / 2) } });
      await page.mouse.wheel(0, 100);
      await expect.poll(() => background.evaluate(el => el.scrollTop)).toBeGreaterThan(saved.top);
    } else {
      await background.evaluate(el => { el.scrollTop += 100; });
      expect(await background.evaluate(el => el.scrollTop)).toBeGreaterThan(saved.top);
    }
  });
}

for (const viewport of [{ width: 320, height: 568 }, { width: 320, height: 640 }, { width: 375, height: 667 },
  { width: 375, height: 812 }, { width: 390, height: 844 }, { width: 430, height: 932 }, { width: 1280, height: 800 }, { width: 390, height: 500 }]) {
  test(`stable geometry, scrolling and focus ${viewport.width}x${viewport.height}`, async ({ page }) => {
    test.setTimeout(120_000);
    await page.setViewportSize(viewport);
    await mockApi(page, 1, { offers: [], scheduleCount: 20, shiftCount: 20, employeeCount: 35 });
    await page.goto("/swap");
    await page.getByRole("button", { name: "Create offer", exact: true }).click();
    const modal = page.locator("dialog:modal");
    const width = (await modal.boundingBox())!.width;
    const check = (action: string) => geometry(page, width, action);
    const capture = (name: string) => (viewport.width === 390 && viewport.height === 844) || viewport.width === 320 && viewport.height === 568 ? diagnostic(page, name) : Promise.resolve();
    const scroll = async (region: import("@playwright/test").Locator, action: string) => {
      if (viewport.height < 600) return; // Compact mode intentionally has one outer scroller.
      const frame = region.locator("..");
      await expect(frame).toHaveAttribute("data-scroll-top", "false");
      await expect(frame).toHaveAttribute("data-scroll-bottom", "true");
      const before = (await modal.getByRole("button", { name: action, exact: true }).boundingBox())!;
      await region.evaluate(el => { el.scrollTop = el.scrollHeight; });
      await expect(frame).toHaveAttribute("data-scroll-top", "true");
      await expect(frame).toHaveAttribute("data-scroll-bottom", "false");
      const after = (await modal.getByRole("button", { name: action, exact: true }).boundingBox())!;
      expect(after.y).toBeCloseTo(before.y, 0);
      expect(await frame.evaluate(el => getComputedStyle(el, "::after").pointerEvents)).toBe("none");
      await region.evaluate(el => { el.scrollTop = 0; });
      await expect(frame).toHaveAttribute("data-scroll-top", "false");
    };
    await check("Continue"); await capture("04-schedule");
    await scroll(modal.getByRole("region", { name: "Schedules", exact: true }), "Continue");
    await modal.getByRole("button", { name: /F27/ }).click();
    await check("Confirm shift");
    await scroll(modal.getByRole("radiogroup", { name: "Shifts", exact: true }), "Confirm shift");
    await modal.getByRole("radio").first().click();
    await check("Confirm shift"); await capture("03-source-full");
    await modal.getByRole("button", { name: "Custom period", exact: true }).click();
    const time = modal.locator('input[type="time"]').first();
    await time.focus(); await expect(time).toBeFocused();
    await expect(time).toHaveCSS("font-size", "16px");
    if (viewport.width === 390 && viewport.height === 844) {
      await page.setViewportSize({ width: 390, height: 500 });
      await expect(modal).toHaveAttribute("data-compact-viewport", "true");
      await check("Confirm shift");
      await page.setViewportSize(viewport);
      await expect(modal).toHaveAttribute("data-compact-viewport", "false");
    }
    await check("Confirm shift"); await capture("02-source-custom");
    await modal.getByRole("button", { name: "Confirm shift" }).click();
    await modal.getByRole("button", { name: "Continue", exact: true }).click();
    await check("Continue"); await capture("01-recipient-everyone");
    await modal.getByRole("button", { name: "Specific employee", exact: true }).click();
    const search = modal.getByRole("searchbox");
    await search.focus(); await expect(search).toBeFocused();
    if (viewport.width === 390 && viewport.height === 844) {
      await page.setViewportSize({ width: 390, height: 500 });
      await check("Continue");
      await page.setViewportSize(viewport);
    }
    await check("Continue"); await capture("06-recipient-specific");
    await scroll(modal.locator('[class*="recipientContent"]'), "Continue");
    await search.fill("Target");
    await modal.getByRole("button", { name: /Target Worker/ }).click();
    await check("Save selection");
    await modal.getByRole("radio", { name: "Swap shifts", exact: true }).click();
    await check("Save selection"); await capture("08-request-type");
    await modal.getByRole("button", { name: "Choose their shift", exact: true }).click();
    await modal.getByRole("button", { name: /F35/ }).click();
    await expect(modal.getByText("Only compatible shifts are selectable. Shifts that overlap with your schedule are unavailable.")).toBeVisible();
    await expect(modal.getByRole("radio", { name: /Not enough hours/ }).first()).toBeDisabled();
    await check("Confirm selection");
    await scroll(modal.getByRole("radiogroup", { name: "Shifts", exact: true }), "Confirm selection");
    await modal.getByRole("radio", { name: /03.10.2026.*08:00.*18:00$/ }).click();
    await check("Confirm selection"); await capture("07-return-custom");
    await modal.getByRole("button", { name: "Confirm selection" }).click();
    await check("Save selection"); await capture("08-request-type-selected");
    await modal.getByRole("button", { name: "Save selection" }).click();
    await modal.getByRole("button", { name: "Continue", exact: true }).click();
    await expect(modal.getByRole("button", { name: "Send request" })).toBeEnabled();
    await check("Send request"); await capture("05-review-exchange");
    await modal.getByRole("button", { name: "Edit recipient", exact: true }).click();
    await expect(modal.getByRole("button", { name: /Target Worker/ })).toHaveAttribute("aria-pressed", "true");
    await modal.getByRole("button", { name: "Continue", exact: true }).click();
    await check("Send request");
  });
}

for (const previewState of ["loading", "error", "invalid", "stale"] as const) {
  test(`preview ${previewState} keeps stable geometry and disables Send`, async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await mockApi(page, 1, { offers: [], previewState }); await page.goto("/swap");
    await sourceSteps(page); const width = (await page.locator("dialog:modal").boundingBox())!.width;
    await page.getByRole("button", { name: "Continue", exact: true }).click();
    await expect(page.getByRole("button", { name: "Send request" })).toBeDisabled();
    await geometry(page, width, "Send request");
  });
}

test("empty schedules have no scroll cues and cannot continue", async ({ page }) => {
  await mockApi(page, 1, { offers: [], scheduleCount: 0 }); await page.goto("/swap");
  await page.getByRole("button", { name: "Create offer", exact: true }).click();
  const list = page.getByRole("region", { name: "Schedules", exact: true });
  await expect(list).toContainText("No published schedules are available for swap.");
  await expect(list.locator("..")).toHaveAttribute("data-scroll-top", "false");
  await expect(list.locator("..")).toHaveAttribute("data-scroll-bottom", "false");
  await expect(page.getByRole("button", { name: "Continue", exact: true })).toBeDisabled();
});

test("empty shifts preserve Polish controls on the narrow short viewport", async ({ page }) => {
  await page.setViewportSize({ width: 320, height: 568 });
  await mockApi(page, 1, { offers: [], shiftCount: 0, language: "pl" }); await page.goto("/swap");
  await page.getByRole("button", { name: "Utwórz ofertę", exact: true }).click();
  const modal = page.locator("dialog:modal");
  await modal.getByRole("button", { name: /F27/ }).click();
  await expect(modal.getByRole("radio")).toHaveCount(0);
  await expect(modal.getByRole("button", { name: "Potwierdź zmianę" })).toBeDisabled();
  expect(await modal.evaluate(el => el.scrollWidth <= el.clientWidth)).toBeTruthy();
});

for (const viewport of [{ width: 320, height: 568 }, { width: 375, height: 667 }, { width: 390, height: 500 }, { width: 375, height: 812 }, { width: 390, height: 844 }, { width: 430, height: 932 }, { width: 320, height: 640 }, { width: 1280, height: 800 }]) {
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
    await page.getByRole("button", { name: /F35/ }).click();
          await expect(page.locator("dialog:modal")).toHaveCount(1);
          await expect(page.getByRole("radio", { name: /Not enough hours/ }).first()).toBeDisabled();
          await page.getByRole("radio", { name: /31.10.2026/ }).scrollIntoViewIfNeeded();
          await expect(page.getByRole("radio", { name: /31.10.2026/ })).toBeInViewport({ ratio: 1 });
          await page.getByRole("radio", { name: /03.10.2026.*08:00.*18:00$/ }).click();
          await expect(page.getByRole("button", { name: "Full shift" })).toBeEnabled();
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
        await page.locator("dialog:modal").getByText("Type of request", { exact: true }).scrollIntoViewIfNeeded();
        await page.screenshot({ path: test.info().outputPath(`review-${mode}-request.png`) });
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
      if (mode === "exchange") {
        await page.getByRole("button", { name: "View offer details for F27" }).click();
        await expect(page.getByRole("dialog", { name: "Offer details" }).getByRole("region", { name: "Shift you receive" })).toContainText("F35");
        await page.getByRole("dialog", { name: "Offer details" }).getByRole("button", { name: "Close", exact: true }).click();
      }
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
    await recipient.getByRole("button", { name: /^View offer details for/ }).click();
    await recipient.getByRole("dialog", { name: "Offer details" }).getByRole("button", { name: "Accept shift", exact: true }).click();
    const confirmation = recipient.getByRole("dialog", { name: "Accept this shift?" });
    await expect(confirmation).toContainText("F27"); await expect(confirmation).toContainText("F35");
    await confirmation.getByRole("button", { name: "Accept shift", exact: true }).click();
    if (stale) await expect(recipient.getByText("Return shift unavailable.", { exact: true }).first()).toBeVisible();
    else { await page.reload(); await page.getByRole("button", { name: "Swap history", exact: true }).click(); await page.getByRole("dialog", { name: "Recent swap activity" }).getByRole("button", { name: "View offer details for F27" }).click(); await expect(page.getByRole("dialog", { name: "Offer details" }).getByRole("region", { name: "Shift you receive" })).toContainText("F35"); }
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
    await expect(dialog.getByRole("button", { name: "Back", exact: true })).toHaveCount(1);
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
    await dialog.getByRole("button", { name: "Back", exact: true }).click();
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
    await dialog.getByRole("button", { name: /F35/ }).click();
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
    await dialog.getByRole("button", { name: /F35/ }).click();
    await dialog.getByRole("radio", { name: /03.10.2026.*08:00.*18:00$/ }).click();
    await dialog.getByRole("button", { name: "Custom period" }).click();
    await dialog.getByRole("button", { name: "Increase From" }).click();
    await expect(dialog.locator('input[type="time"]').first()).toHaveValue("08:15");
    await expect(dialog.locator('input[type="time"]').last()).toHaveValue("18:00");
    await expect(confirm).toBeInViewport({ ratio: 1 });
    await page.screenshot({ path: test.info().outputPath("their-shift-custom.png") });
    await confirm.click();
    await dialog.getByRole("radio", { name: "Give shift", exact: true }).click();
    await dialog.getByRole("radio", { name: "Swap shifts", exact: true }).click();
    await expect(dialog.getByText(/08:15.*18:00/)).toBeVisible();
    await dialog.getByRole("button", { name: "Save selection" }).click();
    await dialog.getByRole("button", { name: /Target Worker/ }).click();
    await dialog.getByRole("button", { name: "Change", exact: true }).click();
    await dialog.getByRole("button", { name: /F35/ }).click();
    await expect(dialog.locator('input[type="time"]').first()).toHaveValue("08:15");
    await dialog.getByRole("button", { name: "Back", exact: true }).click();
    await expect(dialog.getByRole("heading", { name: "Choose their schedule", exact: true })).toBeFocused();
    await dialog.getByRole("button", { name: "Back", exact: true }).click();
    await expect(dialog.getByRole("heading", { name: "Request type", exact: true })).toBeFocused();
    await dialog.getByRole("button", { name: "Back", exact: true }).click();
    await expect(next).toBeEnabled(); await expect(next).toBeInViewport({ ratio: 1 });
    await next.click(); await expect(dialog.getByRole("button", { name: "Send request" })).toBeEnabled();
  });
}

for (const width of [320, 375, 390, 430, 1280]) {
  test(`custom Review design ${width}px`, async ({ page }) => {
    await page.setViewportSize({ width, height: width === 320 ? 568 : width === 375 ? 667 : width === 430 ? 932 : width === 1280 ? 800 : 844 });
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
    const back = dialog.getByRole("button", { name: "Back", exact: true });
    await expect(back).toHaveCount(1);
    const backBox = (await back.boundingBox())!;
    const progressBox = (await dialog.getByRole("list", { name: "Request progress" }).boundingBox())!;
    expect(backBox.x + backBox.width).toBeLessThanOrEqual(progressBox.x);
    await back.click();
    await expect(dialog.getByRole("heading", { name: "Give to whom?" })).toBeFocused();
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
    await dialog.getByText("Type of request", { exact: true }).scrollIntoViewIfNeeded();
    await expect(dialog.getByText("Your statistics", { exact: true })).toHaveCount(0);
    await expect(dialog.getByText("Give shift", { exact: true })).toBeVisible();
    await expect(dialog.getByRole("button", { name: "Send request", exact: true })).toBeInViewport({ ratio: 1 });
    expect(await dialog.evaluate(el => el.scrollWidth <= el.clientWidth && el.scrollHeight <= el.clientHeight + 1)).toBeTruthy();
    expect(await scroller.evaluate(el => el.scrollWidth <= el.clientWidth)).toBeTruthy();
    expect(state.offers).toHaveLength(0);
    await page.screenshot({ path: test.info().outputPath("review-custom.png") });
    await dialog.getByRole("button", { name: "Send request", exact: true }).click();
    expect(state.offers[0]).toMatchObject({ fromTime: "09:15", toTime: "16:30", requestKind: "give" });
  });
}


for (const width of [320, 375, 390, 430]) {
  test(`multiple return schedules and compact selection at ${width}px`, async ({ page }) => {
    await page.setViewportSize({ width, height: 844 });
    const state = { offers: [] as Offer[], multipleReturnSchedules: true };
    await mockApi(page, 1, state); await page.goto("/swap");
    const modal = page.locator("dialog:modal");
    await page.getByRole("button", { name: "Create offer", exact: true }).click();
    await modal.getByRole("button", { name: /F27/ }).click();
    await modal.getByRole("radio").click();
    await modal.getByRole("button", { name: "Confirm shift" }).click();
    const modalWidth = (await modal.boundingBox())!.width;
    const selected = modal.locator('[class*="nested"]');
    const button = await selected.getByRole("button", { name: "Change shift" }).boundingBox();
    const card = await selected.boundingBox();
    const copy = await selected.locator("strong").boundingBox();
    expect(button!.x).toBeGreaterThanOrEqual(copy!.x + copy!.width);
    expect(Math.abs(button!.y + button!.height / 2 - card!.y - card!.height / 2)).toBeLessThanOrEqual(1);
    await geometry(page, modalWidth, "Continue"); await diagnostic(page, "selected-shift-right-action");
    await modal.getByRole("button", { name: "Continue", exact: true }).click();
    await modal.getByRole("button", { name: "Specific employee", exact: true }).click();
    const list = modal.getByRole("group", { name: "Employees" });
    await expect(list.getByRole("button").first()).toContainText("Aaron");
    await list.getByRole("button", { name: /Target Worker/ }).click();
    await modal.getByRole("radio", { name: "Swap shifts", exact: true }).click();
    await modal.getByRole("button", { name: "Choose their shift", exact: true }).click();
    await expect(modal.getByRole("button", { name: /F99/ })).toBeVisible();
    await geometry(page, modalWidth, /F35/); await diagnostic(page, "return-schedules");
    await modal.getByRole("button", { name: /F35/ }).click();
    await expect(modal.getByRole("radio", { name: /05.11.2026/ })).toHaveCount(0);
    await modal.getByRole("radio", { name: /03.10.2026.*08:00.*18:00$/ }).click();
    await expect(modal.getByRole("button", { name: "Full shift", exact: true })).toBeEnabled();
    await modal.getByRole("button", { name: "Confirm selection" }).click();
    await modal.getByRole("button", { name: "Change", exact: true }).click();
    await expect(modal.getByRole("button", { name: /F35/ })).toHaveAttribute("aria-pressed", "true");
    await modal.getByRole("button", { name: /F99/ }).click();
    await expect(modal.getByRole("radio")).toHaveCount(1);
    await expect(modal.getByRole("radio")).toHaveAttribute("aria-checked", "false");
    await expect(modal.getByRole("button", { name: "Confirm selection" })).toBeDisabled();
    await modal.getByRole("radio").click();
    await modal.getByRole("button", { name: "Custom period", exact: true }).click();
    await modal.getByRole("button", { name: "Increase From" }).click();
    await expect(modal.locator('input[type="time"]').last()).toHaveValue("16:00");
    await modal.getByRole("button", { name: "Decrease To" }).click();
    await expect(modal.locator('input[type="time"]').first()).toHaveValue("10:15");
    await expect(modal.locator('input[type="time"]').last()).toHaveValue("15:45");
    await geometry(page, modalWidth, "Confirm selection"); await diagnostic(page, "independent-return-period");
    await modal.getByRole("button", { name: "Confirm selection" }).click();
    await modal.getByRole("button", { name: "Save selection" }).click();
    await expect(list.getByRole("button").first()).toContainText("Target WorkerSwap shifts");
    await modal.getByRole("searchbox").fill("Aaron");
    await expect(list.getByRole("button")).toHaveCount(1);
    await modal.getByRole("button", { name: "Clear search" }).click();
    await expect(list.getByRole("button").first()).toContainText("Target Worker");
    await list.getByRole("button", { name: /Target Worker/ }).click();
    await modal.getByRole("button", { name: "Change", exact: true }).click();
    await expect(modal.getByRole("button", { name: /F99/ })).toHaveAttribute("aria-pressed", "true");
    await modal.getByRole("button", { name: /F99/ }).click();
    await expect(modal.getByRole("radio")).toHaveAttribute("aria-checked", "true");
    await expect(modal.locator('input[type="time"]').first()).toHaveValue("10:15");
    await modal.getByRole("button", { name: "Confirm selection" }).click();
    await modal.getByRole("button", { name: "Save selection" }).click();
    await modal.getByRole("button", { name: "Continue", exact: true }).click();
    await expect(modal.getByRole("region", { name: "Employee shift (they give)" })).toContainText("5.5h");
    await modal.getByRole("button", { name: "Send request" }).click();
    await expect(modal).toHaveCount(0);
    expect(state.offers[0]).toMatchObject({ requestKind: "exchange", returnScheduleId: 30, returnScheduleSlotId: 999, fromTime: "09:00", toTime: "16:30", returnFromTime: "10:15", returnToTime: "15:45" });
  });
}


for (const width of [320, 375, 390, 430]) for (const language of ["en", "pl"] as const) {
  test(`contextual names and navigation ${language} at ${width}px`, async ({ page }) => {
    const pl = language === "pl";
    const ownerName = "Aleksandra Maria Kowalska-Wiśniewska";
    const targetName = "Anna Miniieiva Aleksandrowicz-Kowalska";
    const back = pl ? "Wstecz" : "Back", close = pl ? "Zamknij" : "Close";
    const next = pl ? "Kontynuuj" : "Continue";
    const ownSchedule = pl ? "Wybierz grafik" : "Choose a schedule";
    const ownShift = pl ? "Wybierz swoją zmianę" : "Select your shift";
    const theirSchedule = pl ? "Wybierz grafik tej osoby" : "Choose their schedule";
    const theirShift = pl ? "Wybierz zmianę tej osoby" : "Choose their shift";
    await page.setViewportSize({ width, height: 844 });
    await mockApi(page, 1, { offers: [], ownerName, targetName, language }); await page.goto("/swap");
    const launcher = page.getByRole("button", { name: pl ? "Utwórz ofertę" : "Create offer", exact: true });
    await launcher.click();
    const modal = page.locator("dialog:modal");
    const modalWidth = (await modal.boundingBox())!.width;
    const checkContext = async (name: string, own: boolean, text: string, action: string) => {
      const highlighted = modal.getByText(name, { exact: true });
      await expect(highlighted.locator("..")).toHaveText(text);
      await expect(highlighted).toHaveCSS("font-weight", "600");
      await expect(highlighted).toHaveCSS("color", own ? "rgb(36, 107, 255)" : "rgb(102, 117, 140)");
      await expect(highlighted.locator("..")).toHaveCSS("color", "rgb(102, 117, 140)");
      await geometry(page, modalWidth, action);
    };
    await expect(modal.getByRole("heading", { name: ownSchedule, exact: true })).toBeVisible();
    await checkContext(ownerName, true, pl ? `Wybierz grafik pracownika: ${ownerName}.` : `Select a schedule for ${ownerName}.`, next);
    await expect(modal.getByRole("button", { name: close, exact: true })).toHaveText("×");
    await diagnostic(page, "context-own-schedule");
    await modal.getByRole("button", { name: /F27/ }).click();
    await expect(modal.getByRole("heading", { name: ownShift, exact: true })).toBeFocused();
    await expect(modal.getByRole("button", { name: close, exact: true })).toHaveCount(0);
    await expect(modal.getByRole("button", { name: back, exact: true })).toHaveCount(1);
    await expect(modal.getByRole("button", { name: back, exact: true }).locator("svg")).toBeVisible();
    await checkContext(ownerName, true, pl ? `Wybierz zmianę z grafiku pracownika: ${ownerName}.` : `Choose a shift from ${ownerName}'s schedule.`, pl ? "Potwierdź zmianę" : "Confirm shift");
    await diagnostic(page, "context-own-shift");
    await modal.getByRole("radio").click();
    await modal.getByRole("button", { name: pl ? "Potwierdź zmianę" : "Confirm shift", exact: true }).click();
    await modal.getByRole("button", { name: next, exact: true }).click();
    await modal.getByRole("button", { name: pl ? "Wybrany pracownik" : "Specific employee", exact: true }).click();
    await modal.getByRole("button", { name: new RegExp(targetName) }).click();
    await expect(modal.getByRole("button", { name: back, exact: true })).toHaveCount(1);
    await modal.getByRole("radio", { name: pl ? "Wymień zmiany" : "Swap shifts", exact: true }).click();
    await modal.getByRole("button", { name: theirShift, exact: true }).click();
    await expect(modal.getByRole("heading", { name: theirSchedule, exact: true })).toBeFocused();
    await checkContext(targetName, false, pl ? `Wybierz grafik pracownika: ${targetName}.` : `Select a schedule for ${targetName}.`, back);
    await diagnostic(page, "context-their-schedule");
    await modal.getByRole("button", { name: /F35/ }).click();
    await expect(modal.getByRole("heading", { name: theirShift, exact: true })).toBeFocused();
    await checkContext(targetName, false, pl ? `Wybierz zmianę z grafiku pracownika: ${targetName}, aby zamienić się zmianami.` : `Select a shift from ${targetName}'s schedule to swap with yours.`, pl ? "Potwierdź wybór" : "Confirm selection");
    await diagnostic(page, "context-their-shift");
    await page.keyboard.press("Escape");
    await expect(modal.getByRole("heading", { name: theirSchedule, exact: true })).toBeFocused();
    await modal.getByRole("button", { name: back, exact: true }).click();
    await expect(modal.getByRole("heading", { name: pl ? "Rodzaj prośby" : "Request type", exact: true })).toBeFocused();
    await modal.getByRole("button", { name: back, exact: true }).click();
    await expect(modal.getByRole("button", { name: close, exact: true })).toHaveText("×");
    await modal.getByRole("button", { name: close, exact: true }).click();
    await expect(modal).toHaveCount(0); await expect(launcher).toBeFocused();
  });
}


for (const viewport of [{ width: 320, height: 568 }, { width: 375, height: 667 }, { width: 390, height: 844 },
  { width: 430, height: 932 }, { width: 1280, height: 800 }, { width: 667, height: 375 }]) {
  test(`compact offers and review details ${viewport.width}x${viewport.height}`, async ({ page }) => {
    await page.setViewportSize(viewport);
    const state = { offers: [offer({ id: 1, requestKind: "give", targetEmployeeName: null }),
      offer({ id: 2, requestKind: "exchange", targetEmployeeId: 2, returnFromTime: "10:00", returnToTime: "14:15", returnShiftHours: 4.25, returnShopName: "North Store" }),
      offer({ id: 3, requestKind: "give", targetEmployeeId: 2 })] };
    await mockApi(page, 2, state); await page.goto("/swap");
    const cards = page.locator('article[class*="offerCard_"]'); await expect(cards).toHaveCount(3);
    await expect(cards.first()).toHaveCSS("opacity", "1");
    const initialHeight = await cards.first().evaluate(el => el.offsetHeight);
    await page.screenshot({ path: test.info().outputPath("compact-give.png") });
    await expect(cards.nth(1).getByText("Swap shifts", { exact: true })).toBeVisible();
    await expect(cards.nth(1).getByText("F35", { exact: true })).toHaveCount(0);
    await cards.nth(1).scrollIntoViewIfNeeded();
    await page.locator('[data-swap-offers-scroll]').evaluate(el => { el.scrollTop += 120; });
    await page.screenshot({ path: test.info().outputPath("compact-exchange.png") });
    const area = page.locator('[data-swap-offers-scroll]');
    const launcher = cards.first().getByRole("button", { name: "View offer details for F27" });
    await launcher.focus();
    const scrollTop = await area.evaluate(el => el.scrollTop);
    await page.keyboard.press("Enter");
    const details = page.getByRole("dialog", { name: "Offer details", exact: true });
    await expect(details).toBeVisible(); await expect(page.locator("dialog:modal")).toHaveCount(1);
    await expect(details.getByRole("region", { name: "Shift you receive" })).toContainText("09:00–16:30");
    await expect(details.getByText("Public", { exact: true })).toBeVisible();
    await expect(details.getByRole("button", { name: "Accept shift" })).toBeInViewport({ ratio: 1 });
    await expect(area).toHaveCSS("overflow-y", "hidden");
    await page.screenshot({ path: test.info().outputPath("give-details.png") });
    await details.getByText("Status", { exact: true }).scrollIntoViewIfNeeded();
    await expect(details.getByRole("button", { name: "Accept shift" })).toBeInViewport({ ratio: 1 });
    await page.screenshot({ path: test.info().outputPath("give-details-bottom.png") });
    await page.keyboard.press("Tab");
    expect(await details.evaluate(el => el.contains(document.activeElement))).toBeTruthy();
    await page.keyboard.press("Escape"); await expect(details).toHaveCount(0); await expect(launcher).toBeFocused();
    expect(await area.evaluate(el => el.scrollTop)).toBe(scrollTop);
    expect(await cards.first().evaluate(el => el.offsetHeight)).toBe(initialHeight);
    await expect(cards.first()).not.toHaveAttribute("data-expanded");
    await cards.nth(1).getByRole("button", { name: "View offer details for F27" }).click();
    const sourceCard = details.getByRole("region", { name: "Shift you receive" });
    const returnCard = details.getByRole("region", { name: "Your shift (you give)" });
    await expect(sourceCard).toContainText("7.5h"); await expect(sourceCard.getByText("Receive", { exact: true })).toHaveCSS("color", "rgb(19, 128, 90)");
    await expect(returnCard).toContainText("F35"); await expect(returnCard).toContainText("North Store");
    await expect(returnCard).toContainText("4.25h"); await expect(returnCard.getByText("Give", { exact: true })).toHaveCSS("color", "rgb(36, 107, 255)");
    await expect(details.getByRole("button", { name: "Accept shift" })).toBeInViewport({ ratio: 1 });
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBeTruthy();
    await page.screenshot({ path: test.info().outputPath("exchange-details.png") });
    await returnCard.getByText("Shift hours: 4.25h", { exact: true }).scrollIntoViewIfNeeded();
    await expect(details.getByRole("button", { name: "Accept shift" })).toBeInViewport({ ratio: 1 });
    await page.screenshot({ path: test.info().outputPath("exchange-details-bottom.png") });
    await details.getByRole("button", { name: "Close", exact: true }).click();
    await cards.nth(2).getByRole("button", { name: "View offer details for F27" }).click();
    await expect(details.getByText("Private", { exact: true })).toBeVisible();
    await details.getByRole("button", { name: "Accept shift", exact: true }).click();
    await expect(page.locator("dialog:modal")).toHaveCount(0);
    await expect(page.getByRole("dialog", { name: "Accept this shift?" })).toBeVisible();
    await page.getByRole("button", { name: "Back", exact: true }).click();
  });
}

test("details lock eligibility, restore History and show creator transfer direction", async ({ page }) => {
  const state = { offers: [offer({ requestKind: "exchange", targetEmployeeId: 2, returnFromTime: "10:00", returnToTime: "14:15", returnShiftHours: 4.25 }),
    offer({ id: 2, status: "accepted", requestKind: "give" })] };
  await mockApi(page, 1, state); await page.goto("/swap");
  await page.getByRole("button", { name: "View offer details for F27" }).click();
  const details = page.getByRole("dialog", { name: "Offer details" });
  await expect(details.getByRole("region", { name: "Your shift (you give)" })).toContainText("F27");
  await expect(details.getByRole("region", { name: "Shift you receive" })).toContainText("F35");
  await expect(details.getByRole("button", { name: "Accept shift" })).toBeDisabled();
  await expect(details.getByRole("button", { name: "Cancel offer" })).toBeEnabled();
  await details.getByRole("button", { name: "Close", exact: true }).click();
  await page.getByRole("button", { name: "Swap history", exact: true }).click();
  const history = page.getByRole("dialog", { name: "Recent swap activity" });
  const historyLauncher = history.getByRole("button", { name: "View offer details for F27" });
  await historyLauncher.click(); await expect(page.locator("dialog:modal")).toHaveCount(2);
  await expect(details.getByRole("button", { name: "Accept shift" })).toHaveCount(0);
  await expect(details.getByRole("button", { name: "Cancel offer" })).toHaveCount(0);
  await page.keyboard.press("Escape"); await expect(details).toHaveCount(0);
  await expect(historyLauncher).toBeFocused(); await expect(history).toBeVisible();
  expect(await page.evaluate(() => document.body.style.overflow)).toBe("hidden");
  await page.keyboard.press("Escape"); await expect(history).not.toBeVisible();
});

test("locked offer details stay disabled and backdrop dismisses", async ({ page }) => {
  await mockApi(page, 2, { offers: [offer({ isScheduleLocked: true, acceptanceUnavailableReason: "Schedule is locked while a manager is editing it." })] });
  await page.goto("/swap"); await page.getByRole("button", { name: "View offer details for F27" }).click();
  const details = page.getByRole("dialog", { name: "Offer details" });
  await expect(details.getByRole("button", { name: "Accept shift" })).toBeDisabled();
  await expect(details.getByText("Schedule is locked while a manager is editing it.", { exact: true })).toBeVisible();
  await page.mouse.click(1, 1); await expect(details).toHaveCount(0);
});


test("Polish offer details wrap long names at doubled text size", async ({ page }) => {
  await page.setViewportSize({ width: 320, height: 568 });
  await mockApi(page, 2, { language: "pl", offers: [offer({ requestKind: "exchange", targetEmployeeId: 2,
    scheduleName: "Grafik Aleksandry Marii Kowalskiej-Wiśniewskiej", fromEmployeeName: "Aleksandra Maria Kowalska-Wiśniewska",
    targetEmployeeName: "Anna Aleksandrowicz-Kowalska", returnScheduleName: "Listopadowy grafik oddziału północnego",
    returnFromTime: "10:00", returnToTime: "14:15", returnShiftHours: 4.25 })] });
  await page.goto("/swap"); await page.getByRole("button", { name: /^Zobacz szczegóły oferty/ }).click();
  const details = page.getByRole("dialog", { name: "Szczegóły oferty" });
  await details.evaluate(dialog => {
    const nodes = [dialog, ...dialog.querySelectorAll<HTMLElement>("*")].filter((node): node is HTMLElement => node instanceof HTMLElement);
    const sizes = nodes.map(node => Number.parseFloat(getComputedStyle(node).fontSize));
    nodes.forEach((node, index) => { node.style.fontSize = `${sizes[index] * 2}px`; });
  });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBeTruthy();
  expect(await details.evaluate(dialog => dialog.scrollWidth <= dialog.clientWidth)).toBeTruthy();
  await details.getByText("Widoczność", { exact: true }).scrollIntoViewIfNeeded();
  await expect(details.getByRole("button", { name: "Przejmij zmianę", exact: true })).toBeInViewport({ ratio: 1 });
  await expect(details.getByRole("button", { name: "Zamknij", exact: true })).toBeInViewport({ ratio: 1 });
  await page.screenshot({ path: test.info().outputPath("polish-details-double-text.png") });
  await details.getByRole("button", { name: "Zamknij", exact: true }).click();
  await expect(details).toHaveCount(0);
});


for (const width of [320, 375, 390, 430]) {
  test(`premium offer list preserves History at ${width}px`, async ({ page }) => {
    await page.setViewportSize({ width, height: 844 });
    const state = { offers: [offer({ id: 1, scheduleName: "Demo", requestKind: "exchange",
      fromEmployeeName: "Oleh Protsun", fromEmployeeId: 2, targetEmployeeId: 1, isCreatedByCurrentEmployee: false, canAccept: true, canCancel: false, returnFromTime: "10:00", returnToTime: "14:15", returnShiftHours: 4.25 }),
      ...Array.from({ length: 6 }, (_, index) => offer({ id: index + 2, scheduleName: "8989", requestKind: "give",
        dayOfMonth: index + 2, fromEmployeeName: "Anna Minieieva", acceptanceUnavailableReason: "This is your own swap offer." }))] };
    await mockApi(page, 1, state);
    await page.route("**/api/employee-shift-swaps", route => route.fulfill({ contentType: "application/json", body: JSON.stringify(state.offers) }));
    await page.goto("/swap");
    const cards = page.locator('[data-swap-offers-scroll] article'); await expect(cards).toHaveCount(7);
    await expect(cards.first()).toHaveCSS("opacity", "1");
    await expect(page.getByText("7 offers", { exact: true })).toBeVisible();
    const date = cards.first().locator('[class*="offerDateLine"]');
    await expect(date).toHaveCSS("display", "flex");
    const icon = (await date.locator("svg").boundingBox())!;
    const text = (await date.locator("span").boundingBox())!;
    expect(Math.abs(icon.y + icon.height / 2 - text.y - text.height / 2)).toBeLessThanOrEqual(1);
    const warning = cards.nth(1).locator('[class*="offerUnavailableReason"]');
    await expect(warning).toHaveCSS("background-color", "rgb(255, 240, 242)");
    await expect(warning.locator("svg")).toHaveCount(1);
    await expect(cards.first().getByText("Can", { exact: true })).toBeVisible();
    const history = page.getByRole("button", { name: "Swap history", exact: true });
    const historyGeometry = await history.evaluate(button => {
      const rect = button.getBoundingClientRect(); const style = getComputedStyle(button);
      const stage = button.parentElement!; const backdrop = stage.querySelector("svg path");
      return { x: rect.x, y: rect.y, width: rect.width, height: rect.height, bottom: style.bottom,
        borderRadius: style.borderRadius, background: style.background, boxShadow: style.boxShadow, zIndex: style.zIndex,
        backdrop: backdrop?.getAttribute("d") };
    });
    await writeFile(test.info().outputPath("history-geometry.json"), JSON.stringify(historyGeometry, null, 2));
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBeTruthy();
    await expect(history).toBeInViewport({ ratio: 1 });
    await page.screenshot({ path: test.info().outputPath("premium-offers.png") });
    await cards.first().getByRole("button", { name: "Pin Demo swap" }).click();
    await expect(page.getByRole("dialog", { name: "Offer details" })).toHaveCount(0);
    await cards.first().getByRole("button", { name: "View offer details for Demo" }).click();
    await expect(page.getByRole("dialog", { name: "Offer details" })).toBeVisible();
    await page.getByRole("dialog", { name: "Offer details" }).getByRole("button", { name: "Close", exact: true }).click();
    const area = page.locator('[data-swap-offers-scroll]');
    await area.evaluate(el => { el.scrollTop = el.scrollHeight; });
    await expect(cards.last()).toBeInViewport(); await expect(history).toBeInViewport({ ratio: 1 });
    await expect.poll(() => area.evaluate(el => el.scrollTop)).toBeGreaterThan(0);
    const last = (await cards.last().boundingBox())!; const historyBox = (await history.boundingBox())!;
    expect(last.y + last.height).toBeLessThanOrEqual(historyBox.y);
    await page.screenshot({ path: test.info().outputPath("premium-offers-scrolled.png") });
    await area.evaluate(el => { el.scrollTop = 0; });
    await page.getByRole("searchbox").fill("Demo"); await expect(cards).toHaveCount(1);
    await page.getByRole("button", { name: "Clear swap search" }).click(); await expect(cards).toHaveCount(7);
  });
}


for (const viewport of [{ width: 320, height: 640 }, { width: 375, height: 812 }, { width: 390, height: 844 }, { width: 430, height: 932 }]) {
  test(`History viewport and independent scrolling ${viewport.width}x${viewport.height}`, async ({ page, browserName }) => {
    await page.setViewportSize(viewport);
    const state = { offers: [
      ...Array.from({ length: 12 }, (_, i) => offer({ id: i + 1, scheduleName: `Open ${i}`, scheduleSlotId: i + 800 })),
      ...Array.from({ length: 15 }, (_, i) => offer({ id: i + 100, scheduleName: i === 0 ? "Aleksandra Maria Kowalska-Wiśniewska — harmonogram listopadowy" : `History ${i}`,
        fromEmployeeName: "Aleksandra Maria Kowalska-Wiśniewska", targetEmployeeName: "Anna Aleksandrowicz-Kowalska",
        requestKind: i % 2 ? "give" : "exchange", status: i % 2 ? "cancelled" : "accepted", visibility: i % 3 ? "public" : "private",
        returnFromTime: "10:00", returnToTime: "14:15", returnShiftHours: 4.25 }))] };
    await mockApi(page, 1, state); await page.goto("/swap");
    const background = page.locator('[data-swap-offers-scroll]');
    await expect(background.locator("article")).toHaveCount(12);
    await background.evaluate(el => { el.scrollTop = 180; });
    const saved = await background.evaluate(el => ({ scroll: el.scrollTop, overflow: el.style.overflowY, x: el.getBoundingClientRect().x, width: el.getBoundingClientRect().width }));
    const launcher = page.getByRole("button", { name: "Swap history", exact: true }); await launcher.click();
    const history = page.getByRole("dialog", { name: "Recent swap activity" });
    const list = history.getByRole("region", { name: "Swap history", exact: true });
    await expect(history).toBeVisible(); await expect(history).toHaveCSS("overflow-y", "hidden");
    await expect(list).toHaveCSS("overflow-y", "auto");
    if (await page.evaluate(() => CSS.supports("overscroll-behavior-y", "none"))) await expect(list).toHaveCSS("overscroll-behavior-y", "none");
    else test.info().annotations.push({ type: "runtime", description: "Installed WebKit lacks overscroll-behavior support; fixed-body/root locks and scroll restoration are verified separately." });
    await expect(background).toHaveCSS("overflow-y", "hidden"); await expect(page.locator("body")).toHaveCSS("position", "fixed");
    const box = (await history.boundingBox())!;
    expect(box.y).toBeGreaterThanOrEqual(0); expect(box.y + box.height).toBeLessThanOrEqual(viewport.height);
    expect(box.height).toBeGreaterThanOrEqual(viewport.height - 80);
    const close = history.getByRole("button", { name: "Close", exact: true });
    await close.click({ trial: true });
    const closeBefore = (await close.boundingBox())!;
    const cards = list.locator("article"); await expect(cards).toHaveCount(15);
    expect(await cards.evaluateAll(nodes => nodes.every(node => {
      const badges = node.querySelector<HTMLElement>('[class*="offerBadges_"]')!;
      const title = node.querySelector<HTMLElement>('[class*="offerTitle_"]')!;
      return getComputedStyle(badges).justifyContent === "flex-start" && getComputedStyle(badges).flexWrap === "wrap" &&
        Math.abs(badges.getBoundingClientRect().left - title.getBoundingClientRect().left) < 1 &&
        [...badges.children].every(badge => badge.getBoundingClientRect().right <= node.getBoundingClientRect().right);
    }))).toBeTruthy();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBeTruthy();
    expect(await list.evaluate(el => el.scrollWidth <= el.clientWidth)).toBeTruthy();
    await page.screenshot({ path: test.info().outputPath("history-top.png") });
    const first = cards.first().getByRole("button", { name: /^View offer details for/ }); await first.click();
    const details = page.getByRole("dialog", { name: "Offer details" }); await expect(details).toBeVisible();
    await expect(page.locator("dialog:modal")).toHaveCount(2); await page.keyboard.press("Escape");
    await expect(details).toHaveCount(0); await expect(first).toBeFocused();
    await expect(background).toHaveCSS("overflow-y", "hidden");
    await list.evaluate(el => { el.scrollTop = el.scrollHeight; });
    await expect(cards.last()).toBeInViewport(); await expect(close).toBeInViewport({ ratio: 1 });
    expect((await close.boundingBox())!.y).toBeCloseTo(closeBefore.y, 0);
    if (!(browserName === "webkit" && test.info().project.use.isMobile)) {
      await list.hover(); await page.mouse.wheel(0, 800);
      await page.mouse.move(1, 1); await page.mouse.wheel(0, 800);
    }
    expect(await background.evaluate(el => el.scrollTop)).toBe(saved.scroll);
    await page.screenshot({ path: test.info().outputPath("history-bottom.png") });
    await close.click(); await expect(history).not.toBeVisible(); await expect(launcher).toBeFocused();
    expect(await background.evaluate(el => ({ scroll: el.scrollTop, overflow: el.style.overflowY, x: el.getBoundingClientRect().x, width: el.getBoundingClientRect().width }))).toEqual(saved);
    await launcher.click(); await expect(list).toBeVisible(); await page.keyboard.press("Escape"); await expect(history).not.toBeVisible();
    await expect(background).toHaveCSS("overflow-y", "auto");
  });
}


for (const count of [0, 1, 4]) {
  test(`short History uses natural card height with ${count} entries`, async ({ page }) => {
    await page.setViewportSize({ width: 320, height: 640 });
    await mockApi(page, 1, { offers: Array.from({ length: count }, (_, i) => offer({ id: i + 1, status: "accepted", scheduleName: "Long employee name — Aleksandra Maria Kowalska-Wiśniewska", canAccept: false, canCancel: false })) });
    await page.goto("/swap"); await page.getByRole("button", { name: "Swap history", exact: true }).click();
    const history = page.getByRole("dialog", { name: "Recent swap activity" });
    const list = history.getByRole("region", { name: "Swap history", exact: true });
    const close = history.getByRole("button", { name: "Close", exact: true });
    await close.click({ trial: true });
    const box = (await history.boundingBox())!;
    expect(box.y + box.height).toBeLessThanOrEqual(640);
    expect(await list.evaluate(el => el.scrollWidth <= el.clientWidth)).toBeTruthy();
    if (count === 0) expect(box.height).toBeLessThan(300);
    if (count === 1) expect(box.height).toBeLessThan(400);
    if (count > 0) {
      const card = list.locator("article").first();
      await expect(card).toHaveCSS("flex-grow", "0");
      expect((await card.boundingBox())!.height).toBeLessThanOrEqual((await card.locator('button[class*="offerOpenButton_"]').boundingBox())!.height + 2);
    }
    await list.evaluate(el => { el.scrollTop = el.scrollHeight; }); await expect(close).toBeInViewport({ ratio: 1 });
    await page.screenshot({ path: test.info().outputPath("short-history.png") });
    await close.click(); await expect(history).not.toBeVisible();
  });
}


for (const viewport of [{ width: 320, height: 640 }, { width: 375, height: 812 }, { width: 390, height: 844 }, { width: 430, height: 932 }, { width: 1280, height: 800 }]) {
  test(`stationary Swap and list-only gestures ${viewport.width}x${viewport.height}`, async ({ page, browserName }) => {
    await page.setViewportSize(viewport);
    await mockApi(page, 2, { offers: Array.from({ length: 16 }, (_, i) => offer({ id: i + 1, scheduleName: `Offer ${i + 1}`, scheduleSlotId: 800 + i })) });
    await page.goto("/swap");
    const list = page.locator("[data-swap-offers-scroll]"); await expect(list.locator("article")).toHaveCount(16);
    const header = page.locator('header[class*="swapHeader_"]');
    const toolbar = page.locator('[class*="offerToolbar_"]');
    const history = page.getByRole("button", { name: "Swap history", exact: true });
    const nav = page.locator('[data-employee-nav]').last();
    await expect(header).toHaveCSS("opacity", "1"); await expect(toolbar).toHaveCSS("opacity", "1");
    await expect(nav).toHaveCSS("opacity", "1");
    await page.getByRole("button", { name: "Create offer", exact: true }).click({ trial: true });
    const rect = async (locator: typeof list) => locator.evaluate(el => {
      const box = el.getBoundingClientRect(); return { x: Math.round(box.x), y: Math.round(box.y), width: Math.round(box.width), height: Math.round(box.height) };
    });
    const stationary = async () => ({ header: await rect(header), toolbar: await rect(toolbar), history: await rect(history), nav: await rect(nav) });
    const before = await stationary();
    const outer = () => page.evaluate(() => ({ window: scrollY, html: document.documentElement.scrollTop, body: document.body.scrollTop, root: document.getElementById("root")!.scrollTop }));
    const originalOuter = await outer();
    await expect(list).toHaveCSS("overflow-y", "auto");
    await expect(page.locator('main[class*="swapBody_"]')).toHaveCSS("overflow-y", "hidden");
    await expect(page.locator("body")).toHaveCSS("position", "fixed");
    const listBox = (await list.boundingBox())!;
    const x = listBox.x + listBox.width / 2;
    const gesture = async (x: number, y: number, distance: number) => {
      if (browserName === "chromium") {
        const cdp = await page.context().newCDPSession(page);
        await cdp.send("Input.dispatchTouchEvent", { type: "touchStart", touchPoints: [{ x, y }] });
        for (let step = 1; step <= 8; step++) {
          await cdp.send("Input.dispatchTouchEvent", { type: "touchMove", touchPoints: [{ x, y: y - distance * step / 8 }] });
          await page.waitForTimeout(16);
        }
        await cdp.send("Input.dispatchTouchEvent", { type: "touchEnd", touchPoints: [] }); await cdp.detach();
      } else if (test.info().project.use.isMobile) {
        test.info().annotations.push({ type: "runtime", description: "Mobile WebKit exposes no wheel/swipe input; keyboard scroll is exercised here, Chromium verifies native touch and desktop WebKit verifies wheel input." });
        if (x >= listBox.x && x <= listBox.x + listBox.width && y >= listBox.y && y <= listBox.y + listBox.height) await list.focus();
        else await history.focus();
        await page.keyboard.press(distance > 0 ? "PageDown" : "PageUp");
      } else { await page.mouse.move(x, y); await page.mouse.wheel(0, distance); }
    };
    await gesture(x, listBox.y + Math.min(listBox.height - 10, 230), 160);
    await expect.poll(() => list.evaluate(el => el.scrollTop)).toBeGreaterThan(0);
    expect(await stationary()).toEqual(before); expect(await outer()).toEqual(originalOuter);
    const search = (await toolbar.boundingBox())!;
    await gesture(search.x + search.width / 2, search.y + search.height / 2, 80);
    expect(await stationary()).toEqual(before); expect(await outer()).toEqual(originalOuter);
    await list.evaluate(el => { el.scrollTop = el.scrollHeight; });
    await gesture(x, listBox.y + Math.min(listBox.height - 10, 180), 120);
    expect(await stationary()).toEqual(before); expect(await outer()).toEqual(originalOuter);
    await list.evaluate(el => { el.scrollTop = 0; });
    await gesture(x, listBox.y + 10, -120);
    expect(await stationary()).toEqual(before); expect(await outer()).toEqual(originalOuter);
    await list.evaluate(el => { el.scrollTop = 160; });
    const saved = await list.evaluate(el => el.scrollTop);
    for (const kind of ["history", "details", "wizard"]) {
      if (kind === "history") await history.click();
      if (kind === "details") await list.locator("article").nth(2).getByRole("button", { name: /^View offer details for/ }).click();
      if (kind === "wizard") await page.getByRole("button", { name: "Create offer", exact: true }).click();
      await expect(page.locator("dialog:modal")).toHaveCount(1);
      await page.locator("dialog:modal").getByRole("button", { name: "Close", exact: true }).click();
      await expect(page.locator("dialog:modal")).toHaveCount(0);
      expect(await list.evaluate(el => el.scrollTop)).toBe(saved);
      expect(await outer()).toEqual(originalOuter);
    }
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth && document.documentElement.scrollHeight <= innerHeight)).toBeTruthy();
    const svg = page.locator('svg[class*="swapBackdrop_"]');
    expect(await svg.evaluate(el => Math.abs((el as SVGSVGElement).viewBox.baseVal.height - el.getBoundingClientRect().height) < 1)).toBeTruthy();
    await page.screenshot({ path: test.info().outputPath("stationary-swap.png") });
    await page.setViewportSize({ width: viewport.width, height: viewport.height - 100 });
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollHeight <= innerHeight)).toBeTruthy();
    await expect(history).toBeInViewport({ ratio: 1 });
    await nav.getByRole("link", { name: "Profile", exact: true }).click();
    await expect(page.locator("body")).not.toHaveCSS("position", "fixed");
    await expect(page.locator("html")).toHaveCSS("overflow-y", "auto");
  });
}
