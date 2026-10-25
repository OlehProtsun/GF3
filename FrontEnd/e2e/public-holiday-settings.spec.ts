import { expect, test, type Page } from "@playwright/test";

test.use({ timezoneId: "Europe/Warsaw", launchOptions: process.platform === "win32" ? { ignoreDefaultArgs: ["--disable-accelerated-compositing"] } : {} });

const group = {
  id: 5, name: "May team", year: 2026, month: 5, canSubmit: true, isEditLocked: false, slots: [],
  members: [{ employeeId: 12, firstName: "Worker", lastName: "Bee", displayOrder: 0, slots: [] }],
};
const graph = { id: 3, containerId: 1, shopId: 4, name: "May manager schedule", year: 2026, month: 5,
  publicationStatus: "private", peoplePerShift: 1, shift1Time: "08:00 - 16:00", shift2Time: "16:00 - 20:00",
  maxHoursPerEmpMonth: 160, maxConsecutiveDays: 5, maxConsecutiveFull: 3, maxFullPerMonth: 10 };

async function fixture(page: Page, state: { enabled: boolean; required?: boolean; reads?: number }, role = "manager", mode = "pc", language = "en") {
  const writes: boolean[] = [];
  await page.addInitScript(() => localStorage.setItem("gf3.auth.access-token", "holiday-fixture"));
  await page.route("**/api/**", async route => {
    const path = new URL(route.request().url()).pathname;
    if (!path.startsWith("/api/")) return route.continue();
    let data: unknown = [];
    const session = () => ({ role, workspaceMode: mode, managerId: 1, employeeId: 12, userName: "fixture", displayName: "Holiday fixture", isSystemManager: false });
    if (path === "/api/auth/session") data = session();
    else if (path === "/api/auth/manager-mode") {
      mode = route.request().postDataJSON().mode;
      data = { session: session(), accessToken: "holiday-fixture-mode", expiresAtUtc: "2099-01-01T00:00:00Z" };
    }
    else if (path === "/api/regulations/pending") data = state.required
      ? [{ id: 9, title: "Required rules", version: "1", message: "Read these rules", pdfFileName: "rules.pdf" }] : [];
    else if (path === "/api/regulations/9/accept") { state.required = false; data = { id: 1 }; }
    else if (path === "/api/account-language") data = { language };
    else if (path === "/api/public-holiday-settings") {
      state.reads = (state.reads ?? 0) + Number(route.request().method() === "GET");
      if (state.required) return route.fulfill({ status: 428, contentType: "application/problem+json", body: JSON.stringify({ detail: "Accept the currently published required documents to continue." }) });
      if (route.request().method() === "PUT") { state.enabled = route.request().postDataJSON().showPublicHolidays; writes.push(state.enabled); }
      data = { showPublicHolidays: state.enabled };
    }
    else if (path === "/api/employee-availability") data = [group];
    else if (path === "/api/employees") data = [{ id: 12, firstName: "Worker", lastName: "Bee" }];
    else if (path === "/api/containers") data = [{ id: 1, name: "Main" }];
    else if (path === "/api/containers/1") data = { id: 1, name: "Main" };
    else if (path === "/api/containers/1/graphs") data = [graph];
    else if (path === "/api/containers/1/graphs/3") data = graph;
    else if (path === "/api/containers/1/graphs/3/employees") data = [{ id: 1, scheduleId: 3, employeeId: 12, displayOrder: 0 }];
    else if (path === "/api/containers/1/graphs/3/slots") data = [{ id: 1, scheduleId: 3, employeeId: 12, dayOfMonth: 1, slotNo: 1, fromTime: "08:00", toTime: "16:00", status: "Working" }];
    else if (path === "/api/shops") data = [{ id: 4, name: "Central" }];
    else if (path === "/api/shops/4") data = { id: 4, name: "Central" };
    else if (path === "/api/availability-groups/5") data = { ...group, publicationStatus: "private" };
    else if (path === "/api/availability-groups/5/items") data = [{ memberId: 1, employeeId: 12, displayOrder: 0, dayId: 1, dayOfMonth: 1, kind: "Available" }];
    else if (path === "/api/employee-ui-state") data = { scheduleColumnOrders: {}, readNotificationIds: [], pinnedSwapIds: [] };
    else if (path === "/api/employee-ui-state/availability-time-presets") data = { presets: null };
    else if (path === "/api/employee-schedules") data = [{
      id: 1, containerId: 2, containerName: "Main", shopId: 4, shopName: "Central", name: "May schedule", year: 2026, month: 5,
      publicationStatus: "public", employees: [{ id: 1, employeeId: 12, firstName: "Worker", lastName: "Bee", displayOrder: 0 }],
      slots: [{ id: 1, dayOfMonth: 1, slotNo: 1, employeeId: 12, fromTime: "09:00", toTime: "15:00", status: "ASSIGNED" }],
    }];
    else if (path.includes("notepad")) data = { notes: [], state: { isExpanded: false, isPinned: false, height: 700 } };
    else if (path.includes("negotiate")) return route.fulfill({ status: 503 });
    await route.fulfill({ contentType: "application/json", body: JSON.stringify(data) });
  });
  return writes;
}

test("workspace selection waits for documents and mode before requesting holiday settings", async ({ page }) => {
  const state = { enabled: true, required: true, reads: 0 };
  await fixture(page, state, "manager", "choose");
  await page.goto("/");
  const document = page.getByRole("dialog", { name: "Required rules" });
  await expect(document).toBeVisible();
  expect(state.reads).toBe(0);
  await document.getByRole("checkbox").check();
  await document.getByRole("button", { name: "Zaakceptuj i kontynuuj" }).click();
  await expect(document).toHaveCount(0);
  await expect(page.getByRole("heading", { name: "Wybierz tryb pracy" })).toBeVisible();
  expect(state.reads).toBe(0);
  await page.getByRole("button", { name: /^PC/ }).click();
  await expect.poll(() => state.reads).toBeGreaterThan(0);
  await expect(page.getByText("Accept the currently published required documents to continue.", { exact: true })).toHaveCount(0);
});

for (const [width, language, viewLabel, closeLabel] of [
  [320, "en", "View public holidays", "Close"], [375, "pl", "Zobacz święta ustawowe", "Zamknij"],
  [390, "uk", "Переглянути свята", "Закрити"], [430, "en", "View public holidays", "Close"],
] as const) {
  test(`PC Settings and open dialog fit ${width}px in ${language}`, async ({ page }) => {
    await page.setViewportSize({ width, height: 700 });
    const writes = await fixture(page, { enabled: false }, "manager", "pc", language);
    await page.goto("/settings");
    await expect(page.getByRole("switch")).toBeEnabled();
    await expect(page.getByRole("switch")).not.toBeChecked();
    await page.getByRole("button", { name: language === "pl" ? "Zwiń pasek boczny" : language === "uk" ? "Згорнути бічну панель" : "Collapse sidebar" }).click();
    await expect.poll(() => page.locator("aside").first().evaluate(element => element.getBoundingClientRect().bottom)).toBeLessThanOrEqual(0);
    await page.screenshot({ path: test.info().outputPath(`settings-${width}-${language}.png`), fullPage: true });
    const opener = page.getByRole("button", { name: viewLabel });
    await opener.click();
    const dialog = page.getByRole("dialog");
    await expect(dialog.getByRole("button", { name: closeLabel, exact: true })).toBeFocused();
    await dialog.getByRole("spinbutton").fill("2026");
    await expect(dialog.getByRole("listitem")).toHaveCount(14);
    await expect(dialog.locator("time").nth(7)).toHaveText("04.06.2026");
    await expect(dialog.locator("a")).toHaveAttribute("href", "https://eli.gov.pl/eli/DU/2025/296/ogl/pol");
    expect(await dialog.evaluate(el => el.scrollWidth <= el.clientWidth)).toBe(true);
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
    await page.screenshot({ path: test.info().outputPath(`holidays-${width}-${language}.png`) });
    await page.keyboard.press("Escape");
    await expect(dialog).toHaveCount(0);
    await expect(opener).toBeFocused();
    const languageOpener = page.getByRole("button", { name: language === "pl" ? "Język Polski" : language === "uk" ? "Мова Українська" : "Language English", exact: true });
    await languageOpener.click();
    const languageDialog = page.getByRole("dialog");
    await expect(languageDialog.getByRole("radio")).toHaveCount(3);
    await expect(languageDialog.getByRole("radio", { name: language === "pl" ? "Polski" : language === "uk" ? "Українська" : "English", exact: true })).toBeChecked();
    expect(await languageDialog.evaluate(element => element.scrollWidth <= element.clientWidth)).toBe(true);
    await page.screenshot({ path: test.info().outputPath(`language-${width}-${language}.png`) });
    await languageDialog.getByRole("button", { name: closeLabel, exact: true }).click();
    await expect(languageOpener).toBeFocused();
    expect(writes).toEqual([]);
  });
}

for (const width of [768, 1280]) {
  test(`settings dialogs fit tablet and desktop at ${width}px`, async ({ page }) => {
    await page.setViewportSize({ width, height: 1000 });
    await fixture(page, { enabled: true });
    await page.goto("/settings");
    await page.getByRole("button", { name: "Collapse sidebar" }).click();
    await expect.poll(() => page.locator("aside").first().evaluate(element => element.getBoundingClientRect().bottom)).toBeLessThanOrEqual(0);
    for (const [name, screenshot] of [["View public holidays", "holidays"], ["Language English", "language"]]) {
      await page.getByRole("button", { name, exact: true }).click();
      const dialog = page.getByRole("dialog");
      await expect(dialog).toBeVisible();
      expect(await dialog.evaluate(element => { const bounds = element.getBoundingClientRect(); return element.scrollWidth <= element.clientWidth && bounds.left >= 0 && bounds.right <= innerWidth && bounds.top >= 0 && bounds.bottom <= innerHeight; })).toBe(true);
      await expect(dialog.getByRole("button", { name: "Close", exact: true }).locator("svg")).toBeVisible();
      await page.screenshot({ path: test.info().outputPath(`${screenshot}-${width}.png`) });
      await page.keyboard.press("Escape");
    }
  });
}

test("desktop Settings contains two compact cards and persists the iOS switch", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  const state = { enabled: true };
  await fixture(page, state);
  await page.goto("/settings");
  await page.getByRole("button", { name: "Collapse sidebar" }).click();
  await expect.poll(() => page.locator("aside").first().evaluate(element => element.getBoundingClientRect().bottom)).toBeLessThanOrEqual(0);
  await expect(page.getByRole("region", { name: "Public holidays", exact: true })).toBeVisible();
  await expect(page.getByRole("region", { name: "Language", exact: true })).toBeVisible();
  const toggle = page.getByRole("switch");
  const track = toggle.locator("..").locator("span").first();
  await expect(track).toHaveCSS("background-color", "rgb(36, 99, 255)");
  await page.screenshot({ path: test.info().outputPath("settings-desktop.png"), fullPage: true });
  await toggle.click();
  await expect(toggle).not.toBeChecked();
  await expect(track).toHaveCSS("background-color", "rgb(226, 229, 235)");
  await page.reload();
  await expect(page.getByRole("switch")).not.toBeChecked();
  await expect(page.getByRole("button", { name: "Language English" })).toBeVisible();
});

test("Phone manager has no Settings entry or route", async ({ page }) => {
  await fixture(page, { enabled: true }, "manager", "phone");
  await page.goto("/more");
  await expect(page.getByRole("button", { name: "Log out" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Settings", exact: true })).toHaveCount(0);
  await page.goto("/settings");
  await expect(page).toHaveURL(/\/$/);
  await expect(page.getByRole("switch")).toHaveCount(0);
});

for (const mode of ["pc", "phone"]) {
  test(`${mode} manager schedule and availability date headers share the global setting`, async ({ page }) => {
    const state = { enabled: true };
    await fixture(page, state, "manager", mode);
    for (const path of ["/container/1/graphs/3", "/availability/5"]) {
      await page.goto(path);
      const holiday = page.locator('th[title="Labour Day"]');
      await expect(holiday).toHaveCount(1);
      await expect(holiday.locator("span").first()).toHaveCSS("background-color", "rgb(255, 247, 214)");
      state.enabled = false;
      await page.evaluate(() => window.dispatchEvent(new Event("focus")));
      await expect(holiday).toHaveCount(0);
      state.enabled = true;
    }
  });
}

test("PC global toggle updates employee availability and schedule matrix, daily and summary dates", async ({ page, context }) => {
  const state = { enabled: true };
  const writes = await fixture(page, state);
  const employee = await context.newPage();
  await fixture(employee, state, "employee");
  await employee.goto("/availability");
  await employee.getByRole("button", { name: /^May team / }).click();
  const availabilityDate = employee.locator('[data-availability-day="1"] th');
  await expect(availabilityDate).toHaveAttribute("title", "Labour Day");
  await page.goto("/settings");
  await page.getByRole("button", { name: "Collapse sidebar" }).click();
  const toggle = page.getByRole("switch");
  await expect(toggle).toBeEnabled();
  await toggle.click();
  await expect(toggle).not.toBeChecked();
  await employee.bringToFront();
  await employee.evaluate(() => window.dispatchEvent(new Event("focus")));
  await expect(availabilityDate).not.toHaveAttribute("title");
  await expect(employee.locator('[data-availability-day="3"] th')).toHaveCSS("background-color", "rgb(255, 241, 242)");
  await employee.goto("/schedule");
  await expect(employee.getByRole("table", { name: "Schedule hours summary" })).toBeVisible();
  await expect(employee.getByTitle("Labour Day")).toHaveCount(0);
  await expect(toggle).toBeEnabled();
  await toggle.click();
  await expect(toggle).toBeChecked();
  await employee.bringToFront();
  await employee.evaluate(() => window.dispatchEvent(new Event("focus")));
  await expect(employee.getByTitle("Labour Day")).toHaveCount(2);
  const date = employee.locator('th[title="Labour Day"]');
  await expect(date.locator("span").first()).toHaveCSS("background-color", "rgb(255, 247, 214)");
  expect(await date.evaluate(element => {
    const cell = element.getBoundingClientRect();
    const label = element.querySelector("span span")!.getBoundingClientRect();
    const marker = element.querySelector("small")!.getBoundingClientRect();
    return marker.left >= label.right && marker.right <= cell.right;
  })).toBe(true);
  await employee.screenshot({ path: test.info().outputPath("schedule-holiday.png") });
  await employee.getByRole("button", { name: "Show daily schedule view" }).click();
  await expect(employee.getByRole("tab", { name: /Friday 1, working day.*Labour Day/ })).toHaveCSS("background-color", "rgb(255, 247, 214)");
  expect(writes).toEqual([false, true]);
  await employee.close();
});

for (const [language, label, summaryTitle, statisticsTitle] of [
  ["en", "Holiday Days", "Schedule Summary", "Container Statistics"],
  ["pl", "Dni świąteczne", "Podsumowanie grafiku", "Statystyki kontenera"],
  ["uk", "Святкові дні", "Підсумок графіка", "Статистика контейнера"],
] as const) {
  for (const mode of ["pc", "phone"]) {
    test(`${mode} manager holiday statistics toggle and fit in ${language}`, async ({ page }) => {
      await page.setViewportSize({ width: mode === "phone" ? 375 : 1440, height: 900 });
      const state = { enabled: true };
      await fixture(page, state, "manager", mode, language);
      await page.goto("/container/1/graphs/3");
      const section = page.locator("section").filter({ has: page.getByText(summaryTitle, { exact: true }) });
      const metric = mode === "phone" ? section.locator("summary dt").filter({ hasText: label }) : section.getByRole("columnheader", { name: label, exact: true });
      await expect(metric).toBeVisible();
      if (mode === "phone") {
        const summary = section.locator("summary");
        await expect(summary.locator("dd")).toHaveText(["1", "30", "8", "1"]);
        for (const width of [320, 375, 430]) {
          await page.setViewportSize({ width, height: 900 });
          expect(await summary.evaluate(el => el.scrollWidth <= el.clientWidth)).toBe(true);
          expect(await summary.locator("dl").evaluate(el => {
            const items = [...el.children].map(item => item.getBoundingClientRect());
            return items[0].top === items[1].top && items[2].top === items[3].top
              && items[2].top > items[0].top && items[0].left === items[2].left;
          })).toBe(true);
        }
        await page.setViewportSize({ width: 375, height: 900 });
        await summary.click();
        await expect(section.locator("details")).toHaveAttribute("open", "");
        await summary.click();
      } else {
        await expect(section.locator("tbody tr").first().locator("td").nth(4)).toHaveText("1");
        const scroll = section.locator("table").locator("..");
        const before = await metric.boundingBox();
        await scroll.evaluate(el => { el.scrollLeft = 900; });
        const after = await metric.boundingBox();
        expect(Math.abs(after!.x - before!.x)).toBeLessThan(2);
        expect(await scroll.evaluate(el => el.scrollLeft)).toBeGreaterThan(0);
        const cell = await section.locator("tbody tr").first().locator("td").nth(4).boundingBox();
        expect(Math.abs(cell!.x - after!.x)).toBeLessThan(2);
        expect(Math.abs(cell!.width - after!.width)).toBeLessThan(2);
        expect(await metric.evaluate(el => el.scrollHeight <= el.clientHeight && el.scrollWidth <= el.clientWidth)).toBe(true);
      }
      await page.screenshot({ path: test.info().outputPath(`statistics-${mode}-${language}.png`), fullPage: true });
      state.enabled = false;
      await page.evaluate(() => window.dispatchEvent(new Event("focus")));
      await expect(metric).toHaveCount(0);
      if (mode === "phone") await expect(section.locator("summary dd")).toHaveText(["1", "30", "8"]);
      else await expect(section.locator("tbody tr").first().locator("td").nth(4)).toHaveText("08:00");
      state.enabled = true;
      await page.evaluate(() => window.dispatchEvent(new Event("focus")));
      await expect(metric).toBeVisible();
    });
  }

  test(`container holiday statistics include employee and TOTAL in ${language}`, async ({ page }) => {
    await page.setViewportSize({ width: 1280, height: 900 });
    const state = { enabled: true };
    await fixture(page, state, "manager", "pc", language);
    await page.goto("/container?openContainerId=1");
    const section = page.locator("section").filter({ has: page.getByText(statisticsTitle, { exact: true }) });
    const heading = section.getByRole("columnheader", { name: label, exact: true });
    await expect(heading).toBeVisible();
    await expect(section.locator("tbody tr td:nth-child(4)")).toHaveText(["1", "1"]);
    expect(await heading.evaluate(el => el.scrollWidth <= el.clientWidth && el.scrollHeight <= el.clientHeight)).toBe(true);
    state.enabled = false;
    await page.evaluate(() => window.dispatchEvent(new Event("focus")));
    await expect(heading).toHaveCount(0);
    await expect(section.locator("tbody tr td:nth-child(4)")).toHaveText(["8", "8"]);
    state.enabled = true;
    await page.evaluate(() => window.dispatchEvent(new Event("focus")));
    await expect(heading).toBeVisible();
    await page.screenshot({ path: test.info().outputPath(`container-statistics-${language}.png`), fullPage: true });
  });
}

test("Settings persists holiday visibility across navigation back to manager statistics", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  const state = { enabled: true };
  const writes = await fixture(page, state);
  await page.goto("/container/1/graphs/3");
  const heading = page.getByRole("columnheader", { name: "Holiday Days", exact: true });
  await expect(heading).toBeVisible();
  await page.goto("/settings");
  await page.getByRole("button", { name: "Collapse sidebar" }).click();
  await page.getByRole("switch").click();
  await expect(page.getByRole("switch")).not.toBeChecked();
  await page.goBack();
  await expect(page.getByText("Schedule Summary", { exact: true })).toBeVisible();
  await expect(heading).toHaveCount(0);
  await page.goForward();
  await page.getByRole("button", { name: "Collapse sidebar" }).click();
  await page.getByRole("switch").click();
  await expect(page.getByRole("switch")).toBeChecked();
  await page.goBack();
  await expect(heading).toBeVisible();
  expect(writes).toEqual([false, true]);
});
