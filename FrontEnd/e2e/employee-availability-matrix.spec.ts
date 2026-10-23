import { expect, test, type Page } from "@playwright/test";
import type { EmployeeAvailabilityGroup } from "../src/entities/employee-availability/model/types";
const group: EmployeeAvailabilityGroup = {
  id: 5, name: "May team", year: 2026, month: 5, canSubmit: true, isEditLocked: false, slots: [],
  visibleFromUtc: "2026-05-01T08:00:00Z", visibleToUtc: "2026-05-31T18:00:00Z",
  members: [
    { employeeId: 13, firstName: "Anna", lastName: "First", displayOrder: 0, slots: [{ id: 1, availabilityGroupMemberId: 51, dayOfMonth: 1, kind: 2, intervalStr: "10:00 - 14:00" }] },
    { employeeId: 12, firstName: "Worker", lastName: "Bee", displayOrder: 5, slots: [] },
    { employeeId: 14, firstName: "Bob", lastName: "Last", displayOrder: 1, slots: [{ id: 2, availabilityGroupMemberId: 52, dayOfMonth: 1, kind: 0, intervalStr: null }] },
  ],
};
async function fixture(page: Page, groups = [structuredClone(group)], expandFirst = true, language = "en") {
  const puts: { slots: { dayOfMonth: number; kind: number; intervalStr: string | null }[] }[] = [];
  const errors: string[] = [];
  page.on("pageerror", error => errors.push(error.message));
  await page.addInitScript(() => localStorage.setItem("gf3.auth.access-token", "matrix-fixture"));
  await page.route("**/*", async route => {
    const path = new URL(route.request().url()).pathname;
    if (!path.startsWith("/api/")) return route.continue();
    let body: unknown = [];
    if (path === "/api/auth/session") body = { role: "employee", employeeId: 12, userName: "worker", displayName: "Worker Bee" };
    else if (path === "/api/account-language") body = { language };
    else if (path === "/api/employee-availability") body = groups;
    else if (path === "/api/employee-ui-state/availability-time-presets") body = { presets: null };
    else if (/^\/api\/employee-availability\/\d+\/slots$/.test(path)) {
      expect(route.request().method()).toBe("PUT");
      const payload = route.request().postDataJSON(); puts.push(payload);
      const current = groups.find(item => item.id === Number(path.split("/")[3]))!;
      current.slots = payload.slots.map((slot: object, index: number) => ({ ...slot, id: index + 1, availabilityGroupMemberId: 50 }));
      current.employeeLastModifiedAtUtc = "2026-05-01T10:00:00Z";
      body = current;
    } else if (path.includes("negotiate")) return route.fulfill({ status: 503 });
    await route.fulfill({ contentType: "application/json", body: JSON.stringify(body) });
  });
  await page.goto("/availability");
  await expect(page.getByRole("button", { name: /^May team / })).toHaveAttribute("aria-expanded", "false");
  await expect(page.getByRole("table")).toHaveCount(0);
  if (expandFirst) {
    await page.getByRole("button", { name: /^May team / }).click();
    await expect(page.getByRole("table")).toBeVisible();
  }
  return { puts, errors };
}
const own = (page: Page, day: number) => page.locator(`[data-own-availability-cell="${day}"]`);
async function holdRange(page: Page, anchor: number, target: number) {
  await page.getByRole("region", { name: "Team availability" }).evaluate(element => element.scrollIntoView({ block: "center" }));
  const from = await own(page, anchor).boundingBox(); const to = await own(page, target).boundingBox();
  if (!from || !to) throw new Error("Range cells unavailable");
  await page.mouse.move(from.x + from.width / 2, from.y + from.height / 2);
  await page.mouse.down(); await page.waitForTimeout(400);
  await page.mouse.move(to.x + to.width / 2, to.y + to.height / 2, { steps: 4 });
  await page.mouse.up();
}
for (const width of [320, 375, 390, 430]) {
  test(`matrix reads team, edits only self and fits ${width}px`, async ({ page }) => {
    await page.setViewportSize({ width, height: 640 });
    const { puts, errors } = await fixture(page);
    await expect(page.getByRole("region", { name: "Estimated working hours" })).toHaveCount(0);
    await expect(page.getByRole("row")).toHaveCount(32);
    await page.screenshot({ path: test.info().outputPath(`matrix-${width}.png`), animations: "disabled" });
    await expect(page.getByRole("columnheader")).toHaveText(["Day", "WBWorker BeeYou", "AFAnna First", "BLBob Last"]);
    const first = page.locator('[data-availability-day="1"]');
    const holidayDay = first.getByRole("rowheader");
    await expect(holidayDay).toHaveCSS("background-color", "rgb(255, 247, 214)");
    await expect(holidayDay).toHaveAttribute("title", "Labour Day");
    await expect(holidayDay.getByText("Holiday", { exact: true })).toBeVisible();
    const holidaySunday = page.locator('[data-availability-day="3"]').getByRole("rowheader");
    await expect(holidaySunday).toHaveCSS("background-color", "rgb(255, 247, 214)");
    await expect(holidaySunday).toHaveAttribute("title", "Constitution Day");
    const regularSunday = page.locator('[data-availability-day="10"]').getByRole("rowheader");
    await expect(regularSunday).toHaveCSS("background-color", "rgb(255, 241, 242)");
    await expect(regularSunday.getByText("Holiday", { exact: true })).toHaveCount(0);
    await expect(page.locator('[data-availability-day="4"]').getByText("Holiday", { exact: true })).toHaveCount(0);
    await expect(first.locator("td").first()).not.toHaveCSS("background-color", "rgb(255, 247, 214)");
    expect(await holidayDay.evaluate(element => element.scrollWidth <= element.clientWidth)).toBe(true);
    await expect(first.locator("button")).toHaveCount(1);
    await expect(first.getByText("10:00 - 14:00")).toBeAttached();
    await own(page, 1).click();
    await page.getByRole("dialog").getByRole("radio", { name: /^Available/ }).click();
    await page.getByRole("dialog").getByRole("button", { name: "Save", exact: true }).click();
    expect(puts).toHaveLength(0);
    await expect(own(page, 1)).toHaveText("+");
    await expect(page.getByRole("region", { name: "Estimated working hours" })).toHaveCount(0);
    await holdRange(page, 2, 4);
    await expect(page.getByRole("dialog", { name: "3 days selected" })).toBeVisible();
    await page.getByRole("dialog").getByRole("radio", { name: /^Custom time/ }).click();
    await page.getByRole("dialog").getByRole("button", { name: "09:00 – 15:00", exact: true }).click();
    await page.getByRole("dialog").getByRole("button", { name: "Save", exact: true }).click();
    await holdRange(page, 4, 2);
    await page.getByRole("dialog").getByRole("button", { name: "15:00 – 21:00", exact: true }).click();
    await page.getByRole("dialog").getByRole("button", { name: "Save", exact: true }).click();
    await own(page, 5).click();
    await page.getByRole("dialog").getByRole("radio", { name: /^Not available/ }).click();
    await page.getByRole("dialog").getByRole("button", { name: "Save", exact: true }).click();
    await page.getByRole("button", { name: "Save availability", exact: true }).click();
    await expect.poll(() => puts.length).toBe(1);
    expect(Object.keys(puts[0])).toEqual(["slots"]);
    expect(puts[0].slots).toHaveLength(31);
    expect(puts[0].slots[0]).toEqual({ dayOfMonth: 1, kind: 0, intervalStr: null });
    expect(puts[0].slots.slice(1, 4).map(slot => slot.intervalStr)).toEqual(Array(3).fill("15:00 - 21:00"));
    expect(puts[0].slots[4].kind).toBe(1);
    await page.getByRole("button", { name: "Close success message" }).click();
    const summary = page.getByRole("region", { name: "Estimated working hours" });
    await expect(summary).toContainText("~15–25 h");
    await expect(summary).toContainText("Actual scheduled hours may differ.");
    await summary.scrollIntoViewIfNeeded();
    expect(await summary.evaluate(element => element.scrollWidth <= element.clientWidth)).toBe(true);
    await page.screenshot({ path: test.info().outputPath(`estimate-${width}.png`), animations: "disabled" });
    const viewport = page.getByRole("region", { name: "Team availability" });
    const sticky = await viewport.evaluate(element => {
      const date = element.querySelector("thead th")!; const self = element.querySelector("thead th:nth-child(2)")!;
      const before = [date.getBoundingClientRect().x, self.getBoundingClientRect().x];
      element.scrollLeft = element.scrollWidth; element.scrollTop = element.scrollHeight;
      return { before, after: [date.getBoundingClientRect().x, self.getBoundingClientRect().x], left: element.scrollLeft, top: element.scrollTop };
    });
    expect(sticky.after).toEqual(sticky.before); expect(sticky.left).toBeGreaterThan(0); expect(sticky.top).toBeGreaterThan(0);
    await expect(own(page, 31)).toBeInViewport();
    await own(page, 31).click();
    const dialog = page.getByRole("dialog");
    await dialog.getByRole("radio", { name: /^Custom time/ }).click();
    await dialog.getByRole("button", { name: "Use custom value" }).click();
    await dialog.getByRole("textbox", { name: "Custom" }).fill("08:00 - 16:00");
    await page.setViewportSize({ width, height: 400 });
    await expect.poll(async () => {
      const sheet = await dialog.locator('[class*="dayDialog_"]').boundingBox();
      return sheet ? sheet.y >= 0 && sheet.y + sheet.height <= 401 : false;
    }).toBe(true);
    await page.keyboard.press("Escape");
    await expect(dialog).toHaveCount(0);
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(width + 1);
    const save = page.getByRole("button", { name: "Save availability", exact: true });
    await save.scrollIntoViewIfNeeded();
    const saveBox = await save.boundingBox(); const nav = await page.locator("nav").first().boundingBox();
    if (nav && saveBox) expect(saveBox.y + saveBox.height).toBeLessThanOrEqual(nav.y + 1);
    await page.reload();
    await page.getByRole("button", { name: /^May team / }).click();
    await expect(page.getByRole("region", { name: "Estimated working hours" })).toContainText("~15–25 h");
    expect(errors).toEqual([]);
  });
}
test("closed group remains readable and switching dirty groups asks for confirmation", async ({ page }) => {
  await fixture(page, [structuredClone(group), { ...structuredClone(group), id: 6, name: "Closed team", canSubmit: false }]);
  await own(page, 1).click();
  await page.getByRole("dialog").getByRole("radio", { name: /^Available/ }).click();
  await page.getByRole("dialog").getByRole("button", { name: "Save", exact: true }).click();
  await page.getByRole("button", { name: /^Closed team / }).click();
  await expect(page.getByRole("dialog")).toBeVisible();
  await page.getByRole("dialog").getByRole("button", { name: "Leave page" }).click();
  await expect(own(page, 1)).toBeDisabled();
  await expect(page.getByRole("button", { name: "Save availability", exact: true })).toBeDisabled();
  await expect(page.getByText("10:00 - 14:00", { exact: true })).toBeAttached();
});

for (const width of [320, 375, 390, 430]) {
  test(`accordion cards start collapsed, protect edits and flow below expanded content at ${width}px`, async ({ page }) => {
    await page.setViewportSize({ width, height: 640 });
    const groups = [structuredClone(group), { ...structuredClone(group), id: 6, name: "Closed team", canSubmit: false }, { ...structuredClone(group), id: 7, name: "Locked team", isEditLocked: true }];
    const { errors, puts } = await fixture(page, groups, false);
    const toggles = page.locator("button[aria-controls^='employee-availability-content-']");
    await expect(toggles).toHaveCount(3);
    await expect(page.getByRole("combobox")).toHaveCount(0);
    for (const toggle of await toggles.all()) await expect(toggle).toHaveAttribute("aria-expanded", "false");
    await page.getByRole("button", { name: /^May team / }).click();
    await own(page, 1).click();
    await page.getByRole("dialog").getByRole("radio", { name: /^Available/ }).click();
    await page.getByRole("dialog").getByRole("button", { name: "Save", exact: true }).click();
    await page.getByRole("button", { name: /^May team / }).click();
    await expect(page.getByRole("dialog", { name: "Leave without saving?" })).toBeVisible();
    await page.getByRole("button", { name: "Stay here" }).click();
    await expect(own(page, 1)).toHaveText("+");
    await page.getByRole("button", { name: /^May team / }).click();
    await page.getByRole("button", { name: "Leave page" }).click();
    await expect(page.getByRole("table")).toHaveCount(0);
    await page.getByRole("button", { name: /^Locked team / }).click();
    await expect(page.getByRole("table")).toBeVisible();
    await expect(own(page, 1)).toBeDisabled();
    await expect(page.getByRole("button", { name: "Save availability", exact: true })).toBeDisabled();
    const cards = page.locator("section[aria-labelledby^='employee-availability-toggle-']");
    await expect(cards.first().getByRole("button", { expanded: true })).toHaveAccessibleName(/^Locked team /);
    const expandedBox = await cards.first().boundingBox();
    const collapsedBox = await cards.nth(1).boundingBox();
    expect(collapsedBox!.y).toBeGreaterThanOrEqual(expandedBox!.y + expandedBox!.height - 1);
    await expect(page.getByRole("table")).toHaveCount(1);
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(width + 1);
    await page.screenshot({ path: test.info().outputPath(`accordion-${width}.png`), fullPage: true, animations: "disabled" });
    expect(puts).toHaveLength(0); expect(errors).toEqual([]);
  });
}

test("accordion reuses Polish and Ukrainian labels and localized month names", async ({ page }) => {
  for (const [language, heading, month] of [["pl", "Okresy udostępnienia", "maj"], ["uk", "Відкриті періоди", "травень"]]) {
    const saved = { ...structuredClone(group), employeeLastModifiedAtUtc: "2026-05-01T10:00:00Z", slots: Array.from({ length: 31 }, (_, index) => ({ id: index + 1, availabilityGroupMemberId: 50, dayOfMonth: index + 1, kind: index < 20 ? 2 : 1, intervalStr: index < 20 ? "09:00 - 15:00" : null })) };
    await fixture(page, [saved], false, language);
    await expect(page.getByRole("heading", { name: heading, level: 1 })).toBeVisible();
    await expect(page.getByRole("button", { name: /^May team / })).toContainText(new RegExp(month, "i"));
    await page.getByRole("button", { name: /^May team / }).click();
    await expect(page.getByRole("table")).toBeVisible();
    const holiday = page.locator('[data-availability-day="1"]').getByRole("rowheader");
    await expect(holiday.getByText(language === "pl" ? "Święto" : "Свято", { exact: true })).toBeVisible();
    await expect(holiday).toHaveAttribute("title", language === "pl" ? "Święto Pracy" : "День праці");
    const summary = page.getByRole("region", { name: language === "pl" ? "Szacowana liczba godzin pracy" : "Орієнтовна кількість робочих годин" });
    await expect(summary).toContainText(language === "pl" ? "~70–120 h" : "~70–120 год");
    await summary.scrollIntoViewIfNeeded();
    expect(await summary.evaluate(element => element.scrollWidth <= element.clientWidth)).toBe(true);
    await page.unroute("**/*");
  }
});
