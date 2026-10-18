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
    fromEmployeeHoursBefore: 7.5, fromEmployeeHoursAfter: 7.5, canAccept: false, canCancel: true, isCreatedByCurrentEmployee: true, isManagerCreated: false,
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
    const background = page.locator('main[class*="swapBody_"]');
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
      await background.hover(); await page.mouse.wheel(0, 100);
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
    await expect(dialog.locator('input[type="time"]').first()).toHaveValue("08:15");
    await expect(dialog.getByRole("button", { name: "Confirm shift" })).toBeInViewport({ ratio: 1 });
    expect(await dialog.evaluate(el => el.scrollWidth <= el.clientWidth && el.scrollHeight <= el.clientHeight + 1)).toBeTruthy();
    await page.screenshot({ path: test.info().outputPath("picker-custom.png") });
    await dialog.getByRole("button", { name: "Confirm shift" }).click();
    await expect(dialog.getByText(/Thu 01 Oct.*09:15/)).toBeVisible();
    await dialog.getByRole("button", { name: "Change shift" }).click();
    await expect(dialog.locator('input[type="time"]').first()).toHaveValue("08:15");
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
