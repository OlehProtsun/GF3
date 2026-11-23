import { expect, test, type Locator, type Page } from "@playwright/test";

const names = ["Ada Able", "Ben Baker", "Zoe Young"];
const savedOrderKey = "gf3:employee-schedule-column-order:v1:employee-1";

async function openSchedule(page: Page, longList = false, additionalNames: string[] = []) {
  await page.clock.setFixedTime(new Date("2026-08-12T12:00:00Z"));
  await page.emulateMedia({ reducedMotion: "reduce" });
  await page.addInitScript(() => {
    if (!localStorage.getItem("gf3.auth.access-token")) localStorage.setItem("gf3.auth.access-token", "column-order-a");
    sessionStorage.setItem("gf3.pwa.install.dismissed", "1");
  });

  const employeeNames = longList ? [...names, ...Array.from({ length: 18 }, (_, index) =>
    `Employee ${index} With A Long Name ${"W".repeat(38)}`)] : [...names, ...additionalNames];
  const employees = employeeNames.map((label, index) => {
    const [firstName, ...lastName] = label.split(" ");
    return { id: index + 1, employeeId: index + 1, firstName, lastName: lastName.join(" "), displayOrder: index };
  });
  const writes: { employeeId: number; order: number[] }[] = [];
  const saved = new Map<number, number[]>();
  await page.route("**/api/**", async route => {
    const request = route.request();
    const path = new URL(request.url()).pathname;
    if (!path.startsWith("/api/")) return route.continue();
    const employeeId = request.headers().authorization === "Bearer column-order-b" ? 2 : 1;
    let data: unknown = [];
    if (path === "/api/auth/session") data = { role: "employee", employeeId, userName: `employee-${employeeId}`, displayName: employeeNames[employeeId - 1] };
    else if (path === "/api/account-language") data = { language: "en" };
    else if (path === "/api/public-holiday-settings") data = { showPublicHolidays: false };
    else if (path === "/api/employee-ui-state") data = { scheduleColumnOrders: saved.has(employeeId) ? { "1": saved.get(employeeId) } : {}, readNotificationIds: [] };
    else if (path === "/api/employee-ui-state/schedule-columns/1" && request.method() === "PUT") {
      const { columnOrder } = request.postDataJSON();
      writes.push({ employeeId, order: columnOrder });
      saved.set(employeeId, columnOrder);
      return route.fulfill({ status: 204 });
    } else if (path === "/api/employee-schedules") data = [{
      id: 1, containerId: 2, containerName: "Team", shopId: 4, shopName: "Central", name: "August",
      year: 2026, month: 8, publicationStatus: "public", employees,
      slots: employees.map((employee, index) => ({ id: index + 1, dayOfMonth: 12, slotNo: index + 1,
        employeeId: employee.employeeId, fromTime: "08:00", toTime: "12:00", status: "ASSIGNED" })),
    }];
    else if (path.includes("negotiate")) return route.fulfill({ status: 503 });
    await route.fulfill({ json: data });
  });
  await page.goto("/schedule");
  return writes;
}

async function openDialog(page: Page) {
  await page.getByRole("button", { name: "Customize columns from Ada Able" }).click();
  const dialog = page.getByRole("dialog", { name: "Customize schedule" });
  await expect(dialog).toBeVisible();
  return dialog;
}

test("employee matrix names keep one font size while scrolling and changing phone orientation", async ({ page }, info) => {
  const employeeNames = [
    "Aleksandra Żółć-Nowak",
    "Анна-Марія Гринчук",
    "Jean-Claude Dupont",
    "Avery Anastasia Supercalifragilistic Expialidocious",
    "Oleh Protsun",
  ];
  await openSchedule(page, false, employeeNames);
  const headers = page.getByRole("button", { name: /^Customize columns from / });
  await expect(headers).toHaveCount(names.length + employeeNames.length);

  const readHeaderMetrics = () => headers.evaluateAll(elements => elements.map(element => {
    const style = getComputedStyle(element);
    return { fontSize: style.fontSize, lineHeight: style.lineHeight };
  }));
  const initial = await readHeaderMetrics();
  expect(new Set(initial.map(item => item.fontSize)).size).toBe(1);
  expect(initial[0].fontSize).toBe("12.48px");
  expect(new Set(initial.map(item => item.lineHeight)).size).toBe(1);
  const matrixCard = page.locator('[class*="openScheduleMatrix"]');
  const textSizeAdjust = await matrixCard.evaluate(element => {
    const style = getComputedStyle(element);
    const value = style.getPropertyValue("-webkit-text-size-adjust") || style.getPropertyValue("text-size-adjust") ||
      (style as CSSStyleDeclaration & { webkitTextSizeAdjust?: string }).webkitTextSizeAdjust || "";
    return { value, supported: CSS.supports("-webkit-text-size-adjust", "100%") || CSS.supports("text-size-adjust", "100%") };
  });
  if (textSizeAdjust.supported) expect(textSizeAdjust.value).toBe("100%");

  const matrix = matrixCard.locator("table");
  const scroller = matrix.locator("xpath=..");
  const dimensions = await scroller.evaluate(element => ({ clientWidth: element.clientWidth, scrollWidth: element.scrollWidth }));
  expect(dimensions.scrollWidth).toBeGreaterThan(dimensions.clientWidth);
  await scroller.evaluate(element => { element.scrollLeft = element.scrollWidth; });
  await expect.poll(readHeaderMetrics).toEqual(initial);

  if (info.project.name === "webkit-iphone-x" || info.project.name === "mobile-chrome") {
    await page.setViewportSize({ width: 812, height: 375 });
    await expect.poll(readHeaderMetrics).toEqual(initial);
  }
});

async function center(locator: Locator) {
  const box = await locator.boundingBox();
  if (!box) throw new Error("The drag handle did not have a visible box");
  return { x: box.x + box.width / 2, y: box.y + box.height / 2 };
}

async function sendTouch(handle: Locator, type: "touchstart" | "touchmove" | "touchend", point: { x: number; y: number }) {
  await handle.evaluate((element, { type, point }) => {
    const coordinates = { identifier: 1, target: element, clientX: point.x, clientY: point.y };
    const options = { bubbles: true, cancelable: true };
    let event: TouchEvent;
    try {
      const touch = new Touch(coordinates);
      event = new TouchEvent(type, { ...options, touches: type === "touchend" ? [] : [touch], changedTouches: [touch] });
    } catch {
      event = new TouchEvent(type, options);
      Object.defineProperties(event, {
        touches: { value: type === "touchend" ? [] : [coordinates] }, changedTouches: { value: [coordinates] },
      });
    }
    element.dispatchEvent(event);
  }, { type, point });
}

test("drag previews stay in the dialog and Apply persists one account's order", async ({ page, isMobile }, info) => {
  const errors: string[] = [];
  page.on("pageerror", error => errors.push(error.message));
  const writes = await openSchedule(page);
  const dialog = await openDialog(page);
  const list = dialog.getByRole("list", { name: "Schedule columns" });
  const handles = list.getByRole("button", { name: /Reorder/ });
  const handle = list.getByRole("button", { name: "Reorder Ben Baker" });
  const targetName = isMobile ? "Reorder Zoe Young" : "Reorder Ada Able";
  const start = await center(handle), target = await center(list.getByRole("button", { name: targetName }));
  if (isMobile) {
    await sendTouch(handle, "touchstart", start);
    await expect(handle).toHaveAttribute("aria-pressed", "true");
    await sendTouch(handle, "touchmove", target);
  } else {
    await page.mouse.move(start.x, start.y);
    await page.mouse.down();
    await page.mouse.move(start.x - 8, start.y);
    await page.mouse.move(target.x, target.y, { steps: 6 });
  }
  const dropTarget = dialog.getByText("Drop here");
  await expect(dialog.getByText(`Moving Ben Baker to position ${isMobile ? 3 : 1}.`, { exact: true })).toBeAttached();
  await expect(dropTarget).toBeVisible();
  await expect.poll(async () => {
    const labelBox = await dropTarget.boundingBox();
    const previewBox = await dialog.locator('[class*="dragPreview"]').boundingBox();
    return Boolean(labelBox && previewBox && labelBox.y >= previewBox.y + previewBox.height - 1);
  }).toBe(true);
  await dialog.screenshot({ path: info.outputPath("column-order-drag.png") });
  if (isMobile) await sendTouch(handle, "touchend", target);
  else await page.mouse.up();
  if (!isMobile) {
    await expect(handles.first()).toHaveAccessibleName("Reorder Ben Baker");
    const middle = await center(list.getByRole("button", { name: "Reorder Ada Able" }));
    const bottom = await center(list.getByRole("button", { name: "Reorder Zoe Young" }));
    await page.mouse.move(middle.x, middle.y);
    await page.mouse.down();
    await page.mouse.move(middle.x - 8, middle.y);
    await page.mouse.move(bottom.x, bottom.y, { steps: 6 });
    await expect(dialog.getByText("Moving Ada Able to position 3.", { exact: true })).toBeAttached();
    await page.mouse.up();
  }
  const order = isMobile ? [1, 3, 2] : [2, 3, 1];
  const handleNames = order.map(id => `Reorder ${names[id - 1]}`);
  await expect.poll(() => handles.evaluateAll(buttons => buttons.map(button => button.getAttribute("aria-label")))).toEqual(handleNames);
  if (!isMobile) {
    const reordered = await center(handle);
    const outsideList = await center(dialog.getByRole("button", { name: "Close column order dialog" }));
    await page.mouse.move(reordered.x, reordered.y);
    await page.mouse.down();
    await page.mouse.move(reordered.x - 8, reordered.y);
    await expect(handle).toHaveAttribute("aria-pressed", "true");
    await page.mouse.move(outsideList.x, outsideList.y, { steps: 4 });
    await expect(dialog.getByText("Drop here")).toHaveCount(0);
    await page.mouse.up();
    await expect(dialog.getByRole("button", { name: "Apply" })).toBeEnabled();
    await expect.poll(() => handles.evaluateAll(buttons => buttons.map(button => button.getAttribute("aria-label")))).toEqual(handleNames);
  }
  expect(writes).toHaveLength(0);
  await dialog.getByRole("button", { name: "Apply" }).click();
  await expect(dialog).toHaveCount(0);
  await expect.poll(() => writes).toEqual([{ employeeId: 1, order }]);
  expect(await page.evaluate(key => JSON.parse(localStorage.getItem(key) ?? "null"), savedOrderKey)).toEqual({ version: 1, scheduleOrders: { "1": order } });

  await page.evaluate(key => localStorage.removeItem(key), savedOrderKey);
  await page.reload();
  await openDialog(page);
  await expect.poll(() => handles.evaluateAll(buttons => buttons.map(button => button.getAttribute("aria-label")))).toEqual(handleNames);
  await dialog.getByRole("button", { name: "Reset default" }).click();
  await expect(handles.first()).toHaveAccessibleName("Reorder Ada Able");
  await dialog.getByRole("button", { name: "Cancel" }).click();
  expect(writes).toHaveLength(1);

  await page.evaluate(() => localStorage.setItem("gf3.auth.access-token", "column-order-b"));
  await page.reload();
  await openDialog(page);
  await expect(handles.first()).toHaveAccessibleName("Reorder Ada Able");
  await expect(dialog.getByRole("button", { name: "Apply" })).toBeDisabled();
  expect(errors).toEqual([]);
});

test("a narrow long list scrolls internally and Escape cancels before closing", async ({ page }, info) => {
  await page.setViewportSize({ width: 320, height: 550 });
  const writes = await openSchedule(page, true);
  const dialog = await openDialog(page);
  const list = dialog.getByRole("list", { name: "Schedule columns" });
  await expect(list.getByRole("listitem")).toHaveCount(21);
  const handle = list.getByRole("button", { name: "Reorder Ben Baker" });
  await expect(dialog).toHaveCSS("animation-name", "none");
  await expect(handle).toHaveCSS("touch-action", "none");
  await handle.focus();
  await page.keyboard.press("Space");
  await page.keyboard.press("ArrowUp");
  await expect(dialog.getByText("Drop here")).toBeVisible();
  await page.keyboard.press("Escape");
  await expect(dialog).toBeVisible();
  await expect(dialog.getByRole("button", { name: "Apply" })).toBeDisabled();
  await expect(list.getByRole("button").first()).toHaveAccessibleName("Reorder Ada Able");
  expect(await list.evaluate(node => ({
    fits: node.scrollWidth <= node.clientWidth + 1,
    scrolls: node.scrollHeight > node.clientHeight,
    touchAction: getComputedStyle(node).touchAction,
  }))).toEqual({ fits: true, scrolls: true, touchAction: "auto" });
  await list.getByRole("button").last().focus();
  await expect.poll(() => list.evaluate(node => node.scrollTop)).toBeGreaterThan(0);
  await expect(dialog.getByRole("button", { name: "Cancel" })).toBeInViewport();
  await dialog.screenshot({ path: info.outputPath("column-order-narrow.png") });
  await page.keyboard.press("Escape");
  await expect(dialog).toHaveCount(0);
  expect(writes).toHaveLength(0);
});
