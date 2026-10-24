import { chromium, expect } from "@playwright/test";
import { mkdir, writeFile, readFile } from "node:fs/promises";
import { fileURLToPath } from "node:url";
import path from "node:path";
import { createHash } from "node:crypto";

const root = fileURLToPath(new URL("../../", import.meta.url));
const origin = new URL(process.env.PROMO_APP_URL || process.env.PROMO_BASE_URL || "http://localhost:5173");
if (!["localhost", "127.0.0.1", "[::1]"].includes(origin.hostname)) throw new Error("Capture requires a local synthetic demonstration origin; remote hosts are rejected.");
const destination = process.env.PROMO_CAPTURE_DIR || path.join(root, "src/promo/real-ui/captures");
const dataset = "gf3-film-october-2026";
const demoRecipe = process.env.PROMO_DEMO_DATASET ? JSON.parse(await readFile(process.env.PROMO_DEMO_DATASET, "utf8")) : null;
if (demoRecipe && demoRecipe.synthetic !== true) throw new Error("PROMO_DEMO_DATASET must explicitly declare synthetic:true. Real customer datasets are prohibited.");
if ((process.env.PROMO_MANAGER_STATE || process.env.PROMO_EMPLOYEE_STATE) && !demoRecipe) throw new Error("Optional storageState requires PROMO_DEMO_DATASET declaring synthetic:true; never supply a real customer session.");
const sourcePaths = {
  manager: ["src/entities/containers/ui/ContainerGraphMatrix.tsx", "src/entities/containers/ui/ContainerGraphMatrix.module.css"],
  availability: ["src/pages/employee-availability/ui/EmployeeAvailabilityPage.tsx"],
  schedule: ["src/pages/employee-schedule/ui/EmployeeSchedulePage.tsx"],
  swap: ["src/pages/employee-swap/ui/EmployeeSwapPage.tsx", "src/entities/shift-swaps/api/shiftSwapsApi.ts"],
  more: ["src/pages/employee-schedule/ui/EmployeeSchedulePage.tsx"],
};
const routes = { manager: "/container", availability: "/availability", schedule: "/schedule", swap: "/swap", more: "/schedule" };
const browser = await chromium.launch({ headless: true });
const provenance = [];
const blocked = [];

const schedule = {
  id: 10, containerId: 2, containerName: "Sklep Centrum", shopId: 4, shopName: "Sklep Centrum",
  name: "Październik 2026", year: 2026, month: 10, publicationStatus: "public", allowSwap: true,
  publishedAtUtc: "2026-10-08T08:00:00Z",
  employees: ["Kasia", "Marta", "Tomek"].map((name, index) => ({ id: index + 1, employeeId: index + 1, firstName: name, lastName: "", displayName: name, minHoursMonth: 0, displayOrder: index })),
  slots: [12, 13, 14, 15, 16, 17].flatMap(day => [1, 2, 3].filter(employeeId => !(day === 16 && employeeId === 1)).map(employeeId => ({
    id: day * 10 + employeeId, dayOfMonth: day, slotNo: 1, employeeId,
    fromTime: employeeId === 2 ? "12:00" : "09:00", toTime: employeeId === 2 ? "20:00" : "17:00", status: "ASSIGNED",
  }))),
};
const offer = { id: 1, scheduleId: 10, scheduleSlotId: 162, scheduleName: "Październik 2026", containerName: "Sklep Centrum", shopName: "Sklep Centrum", year: 2026, month: 10, dayOfMonth: 16, fromTime: "12:00", toTime: "20:00", fromEmployeeId: 2, fromEmployeeName: "Marta", visibility: "public", status: "open", createdAtUtc: "2026-10-08T08:00:00Z", shiftHours: 8, canAccept: true, canCancel: false, isCreatedByCurrentEmployee: false, isManagerCreated: false, isScheduleLocked: false, currentEmployeeHoursBefore: 40, currentEmployeeHoursAfter: 48, currentEmployeeWorkDaysBefore: 5, currentEmployeeWorkDaysAfter: 6, currentEmployeeFreeDaysBefore: 26, currentEmployeeFreeDaysAfter: 25, fromEmployeeHoursBefore: 48, fromEmployeeHoursAfter: 40 };
if (schedule.slots.some(slot => slot.employeeId === 1 && slot.dayOfMonth === offer.dayOfMonth) || !schedule.slots.some(slot => slot.id === offer.scheduleSlotId && slot.employeeId === offer.fromEmployeeId && slot.fromTime === offer.fromTime && slot.toTime === offer.toTime)) throw new Error("Synthetic swap must match its real fixture shift and the recipient's free day.");

async function ready(page) {
  await page.evaluate(async () => { await document.fonts.ready; await Promise.all([...document.images].map(image => image.decode().catch(() => {}))); });
  await page.waitForTimeout(900);
}

async function screenshot(page, feature, state, target) {
  await ready(page);
  const file = path.join(destination, feature, `${state}.png`);
  await mkdir(path.dirname(file), { recursive: true });
  if (target) await target.screenshot({ path: file, animations: "disabled", caret: "hide" });
  else {
    const clips = {
      manager: { x: 100, y: 110, width: 1240, height: 360 },
      availability: state === "interaction" ? { x: 470, y: 200, width: 500, height: 510 } : { x: 510, y: 75, width: 740, height: 660 },
      swap: state === "outcome" ? { x: 430, y: 280, width: 580, height: 350 } : state === "interaction" ? { x: 20, y: 300, width: 900, height: 280 } : { x: 0, y: 60, width: 1440, height: 400 },
    };
    await page.screenshot({ path: file, animations: "disabled", caret: "hide", ...(clips[feature] ? { clip: clips[feature] } : {}) });
  }
  return { file: `${feature}/${state}.png`, sha256: createHash("sha256").update(await readFile(file)).digest("hex") };
}

try {
  for (const feature of Object.keys(sourcePaths)) {
    const storageState = feature === "manager" ? process.env.PROMO_MANAGER_STATE : process.env.PROMO_EMPLOYEE_STATE;
    const context = await browser.newContext({ viewport: { width: 1440, height: 900 }, deviceScaleFactor: 1, locale: "pl-PL", timezoneId: "Europe/Warsaw", colorScheme: "light", reducedMotion: "reduce", serviceWorkers: "block", ...(storageState ? { storageState } : {}) });
    const page = await context.newPage();
    const errors = [];
    page.on("pageerror", error => errors.push(error.message));
    try {
      await page.clock.setFixedTime(new Date("2026-10-12T08:00:00+02:00"));
      // Reuse the repo's E2E fixture interception mechanism: no real session/backend.
      await page.addInitScript(() => localStorage.setItem("gf3.auth.access-token", "synthetic-film-fixture"));
      await page.route("**/*", async route => {
        const url = new URL(route.request().url());
        if (url.origin !== origin.origin) return route.abort();
        const pathname = url.pathname;
        if (!pathname.startsWith("/api/")) return route.continue();
        let body = [];
        if (pathname === "/api/auth/session") body = { role: "employee", employeeId: 1, managerId: null, userName: "synthetic-kasia", displayName: "Kasia" };
        else if (pathname === "/api/account-language") body = { language: "pl" };
        else if (pathname === "/api/employee-ui-state") body = { scheduleColumnOrders: {}, readNotificationIds: [], pinnedSwapIds: [] };
        else if (pathname === "/api/employee-schedules") body = [schedule];
        else if (pathname === "/api/employee-shift-swaps") body = [offer];
        else if (pathname === "/api/employee-shift-swaps/employees") body = [{ employeeId: 1, displayName: "Kasia" }, { employeeId: 2, displayName: "Marta" }];
        else if (pathname === "/api/employee-availability") body = [{ id: 5, name: "Październik 2026", year: 2026, month: 10, canSubmit: true, isEditLocked: false, visibleFromUtc: "2026-10-01T08:00:00Z", visibleToUtc: "2026-11-01T18:00:00Z", slots: [] }];
        else if (pathname.includes("negotiate")) return route.fulfill({ status: 503 });
        if (route.request().method() !== "GET" && !["/api/employee-ui-state", "/api/account-language"].includes(pathname)) {
          return route.fulfill({ status: 405, contentType: "application/json", body: JSON.stringify({ message: "Film fixture is read-only; no server outcome is fabricated." }) });
        }
        return route.fulfill({ contentType: "application/json", body: JSON.stringify(body) });
      });
      const assets = [];
      if (feature === "manager") {
        await page.goto(new URL("/promo.html?productShot=manager", origin).href);
        await expect(page.getByText("Grafik zespołu", { exact: true })).toBeVisible();
        const cell = page.locator('[data-employee-id="1"][data-day-of-month="12"]').first();
        await cell.scrollIntoViewIfNeeded();
        await page.locator('[class*="tableScroll"]').first().evaluate(element => { element.scrollTop = 310; });
        assets.push(await screenshot(page, feature, "beginning"));
        await cell.dblclick();
        const editor = page.locator('[data-matrix-editor="true"]').first();
        await expect(editor).toBeVisible(); await editor.fill("10:00 - 18:00");
        assets.push(await screenshot(page, feature, "interaction"));
        await editor.press("Enter");
        await expect(cell).toContainText("10:00");
        assets.push(await screenshot(page, feature, "outcome"));
      } else if (feature === "availability") {
        await page.goto(new URL(routes[feature], origin).href);
        const day = page.getByRole("button").filter({ has: page.locator("span").filter({ hasText: /^12$/ }) }).first();
        await expect(day).toBeVisible();
        assets.push(await screenshot(page, feature, "beginning"));
        await day.click(); await expect(page.getByRole("dialog")).toBeVisible();
        await page.getByRole("dialog").getByRole("button", { name: "9:00 - 21:00", exact: true }).click();
        assets.push(await screenshot(page, feature, "interaction"));
        await page.getByRole("dialog").getByRole("button", { name: "Zapisz", exact: true }).click();
        await expect(day).toContainText("09:00 - 21:00");
        assets.push(await screenshot(page, feature, "outcome"));
      } else if (feature === "swap") {
        await page.goto(new URL(routes[feature], origin).href);
        const card = page.locator("article").filter({ hasText: "Październik 2026" }).first();
        await expect(card).toBeVisible();
        assets.push(await screenshot(page, feature, "beginning", card));
        await card.click(); await expect(card).toHaveAttribute("data-expanded", "true");
        assets.push(await screenshot(page, feature, "interaction", card));
        await card.getByRole("button", { name: "Akceptuj", exact: true }).click();
        await expect(page.getByRole("dialog")).toBeVisible();
        assets.push(await screenshot(page, feature, "outcome", page.getByRole("dialog").locator(":scope > div").first()));
      } else {
        await page.goto(new URL("/schedule", origin).href);
        await expect(page.getByRole("region", { name: "Twoje najbliższe zmiany" })).toBeVisible();
        if (feature === "more") {
          const summary = page.locator("section").filter({ has: page.locator('[role="table"][aria-label]') }).last();
          await expect(summary).toBeVisible(); await summary.scrollIntoViewIfNeeded();
          assets.push(await screenshot(page, feature, "beginning", summary));
          await summary.locator('[role="table"]').evaluate(element => { element.scrollTop = 360; });
          assets.push(await screenshot(page, feature, "interaction", summary));
          assets.push(await screenshot(page, feature, "outcome", summary));
        } else {
          const view = page.locator('[class*="scheduleViewStage"]').first();
          await expect(view).toBeVisible(); await view.scrollIntoViewIfNeeded();
          assets.push(await screenshot(page, feature, "beginning", view));
          const day = page.getByRole("tab").filter({ has: page.locator("strong").filter({ hasText: /^14$/ }) }).first();
          if (await day.count()) await day.click();
          else await page.getByRole("button", { name: /widok/i }).first().click();
          assets.push(await screenshot(page, feature, "interaction", view));
          const focusDay = page.getByRole("tab").filter({ has: page.locator("strong").filter({ hasText: /^14$/ }) }).first();
          if (await focusDay.count()) await focusDay.click();
          assets.push(await screenshot(page, feature, "outcome", view));
        }
      }
      if (errors.length) throw new Error(errors.join("\n"));
      const hashes = await Promise.all(sourcePaths[feature].map(async source => ({ path: source, sha256: createHash("sha256").update(await readFile(path.join(root, source))).digest("hex") })));
      provenance.push({ id: feature, acquisition: feature === "manager" ? "production-component" : "actual-app-capture", sourceRoute: routes[feature], sourcePaths: hashes, dataSet: dataset, assets, synthetic: true, mechanism: "production DOM + repository E2E interception, all API requests fulfilled locally", capturedAt: new Date().toISOString() });
      console.log(`Captured authentic ${feature}: ${assets.length} states`);
    } catch (error) { blocked.push(`${feature}: ${error.message}`); }
    finally { await context.close(); }
  }
  await mkdir(destination, { recursive: true });
  await writeFile(path.join(destination, "capture-provenance.json"), JSON.stringify({ dataSet: dataset, shots: provenance, blocked }, null, 2));
  if (blocked.length) throw new Error(`Blocked authentic sources:\n${blocked.join("\n")}`);
} finally { await browser.close(); }
