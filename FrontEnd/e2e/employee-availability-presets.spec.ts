import { expect, test, type Page } from "@playwright/test";
import { readFileSync } from "node:fs";
const pl = JSON.parse(readFileSync(new URL("../src/shared/i18n/pl.json", import.meta.url), "utf8"));
const uk = JSON.parse(readFileSync(new URL("../src/shared/i18n/uk.json", import.meta.url), "utf8"));

for (const language of ["en", "pl", "uk"] as const) {
  test(`employee bottom sheet and database presets across devices: ${language}`, async ({ page, browser }) => {
    test.setTimeout(90_000);
    const dictionary: Record<string, string> = language === "pl" ? pl : language === "uk" ? uk : {};
    const tr = (key: string) => dictionary[key] ?? key;
    const starts = (key: string) => new RegExp("^" + tr(key).replace(/[.*+?^${}()|[\]\\]/g, "\\$&"));
    const errors: string[] = [];
    const server = new Map<number, string[]>();
    let employeeId = 12;
    let availabilityWrites = 0;
    async function install(target: Page) {
      target.on("pageerror", error => errors.push(error.message));
      await target.addInitScript(() => localStorage.setItem("gf3.auth.access-token", "preset-fixture"));
      await target.route("**/*", async route => {
        const path = new URL(route.request().url()).pathname;
        if (!path.startsWith("/api/")) return route.continue();
        let body: unknown = [];
        if (path === "/api/auth/session") body = { role: "employee", employeeId, userName: `worker${employeeId}`, displayName: "Worker" };
        else if (path === "/api/account-language") body = { language };
        else if (path === "/api/employee-ui-state/availability-time-presets") {
          if (route.request().method() === "PUT") {
            const payload = route.request().postDataJSON();
            expect(Object.keys(payload).sort()).toEqual(["onlyIfUnset", "presets"]);
            if (!payload.onlyIfUnset || !server.has(employeeId)) server.set(employeeId, payload.presets);
          }
          body = { presets: server.get(employeeId) ?? null };
        } else if (path === "/api/employee-availability") body = [{
          id: 5, name: "Availability", year: 2026, month: 9, canSubmit: true, isEditLocked: false,
          visibleFromUtc: "2026-09-01T08:00:00Z", visibleToUtc: "2026-10-01T18:00:00Z", slots: [],
          members: [{ employeeId, firstName: "Worker", lastName: "Bee", displayOrder: 0, slots: [] }],
        }];
        else if (path === "/api/employee-availability/5/slots") { availabilityWrites++; body = {}; }
        else if (path.includes("negotiate")) return route.fulfill({ status: 503 });
        await route.fulfill({ contentType: "application/json", body: JSON.stringify(body) });
      });
      await target.goto("/availability");
      await target.getByRole("button", { name: /^Availability / }).click();
    }
    async function openDay(target: Page) {
      await target.locator('[data-own-availability-cell="1"]').click();
      return target.getByRole("dialog");
    }
    await install(page);
    for (const width of [320, 375, 390, 430]) {
      await page.setViewportSize({ width, height: 640 });
      const dialog = await openDay(page);
      await expect(dialog.getByRole("radio")).toHaveCount(3);
      await expect(dialog.getByRole("radio", { name: starts("Not available") })).toBeChecked();
      await expect(dialog.getByRole("region", { name: tr("Other time") })).toHaveCount(0);
      await dialog.getByRole("radio", { name: starts("Custom time") }).click();
      await expect(dialog.getByRole("region", { name: tr("Other time") })).toBeVisible();
      await dialog.getByRole("button", { name: "09:00 – 21:00", exact: true }).click();
      await expect(dialog.getByLabel(tr("From"), { exact: true })).toHaveValue("09:00");
      await expect(dialog.getByLabel(tr("To"), { exact: true })).toHaveValue("21:00");
      await dialog.getByRole("radio", { name: starts("Available (whole day)") }).click();
      await expect(dialog.getByRole("region", { name: tr("Other time") })).toHaveCount(0);
      await dialog.getByRole("radio", { name: starts("Custom time") }).click();
      await dialog.getByLabel(tr("From"), { exact: true }).fill("22:00");
      await expect(dialog.getByRole("button", { name: tr("Save"), exact: true })).toBeDisabled();
      await dialog.getByLabel(tr("From"), { exact: true }).fill("09:00");
      await expect(dialog.getByRole("button", { name: tr("Save"), exact: true })).toBeEnabled();
      await page.screenshot({ path: test.info().outputPath(`sheet-${width}.png`), animations: "disabled" });
      await page.setViewportSize({ width, height: 400 });
      await expect(dialog.getByRole("button", { name: tr("Cancel"), exact: true })).toBeInViewport();
      await expect(dialog.getByRole("button", { name: tr("Save"), exact: true })).toBeInViewport();
      await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(width + 1);
      await dialog.getByRole("button", { name: tr("Cancel"), exact: true }).click();
      await expect(page.locator('[data-own-availability-cell="1"]')).toHaveText("−");
    }
    await page.setViewportSize({ width: 390, height: 844 });
    let dialog = await openDay(page);
    await dialog.getByRole("radio", { name: starts("Custom time") }).click();
    await dialog.getByRole("button", { name: tr("Edit presets"), exact: true }).click();
    const editName = (value: string) => tr("Edit preset {0}").replace("{0}", value);
    await dialog.getByRole("button", { name: editName("09:00 - 15:00"), exact: true }).click();
    let editor = dialog.getByRole("form", { name: tr("Edit preset") });
    await editor.getByLabel(tr("From"), { exact: true }).fill("20:00");
    await expect(editor.getByRole("button", { name: tr("Save preset"), exact: true })).toBeDisabled();
    await editor.getByLabel(tr("From"), { exact: true }).fill("08:30");
    await editor.getByLabel(tr("To"), { exact: true }).fill("16:30");
    await editor.getByRole("button", { name: tr("Save preset"), exact: true }).click();
    await expect(dialog.getByRole("button", { name: editName("08:30 - 16:30"), exact: true })).toBeVisible();
    await expect(dialog.getByLabel(tr("From"), { exact: true })).toHaveValue("09:00");
    await dialog.getByRole("button", { name: tr("Add preset"), exact: true }).click();
    editor = dialog.getByRole("form", { name: tr("Edit preset") });
    await editor.getByLabel(tr("From"), { exact: true }).fill("10:00");
    await editor.getByLabel(tr("To"), { exact: true }).fill("18:00");
    await editor.getByRole("button", { name: tr("Save preset"), exact: true }).click();
    await dialog.getByRole("button", { name: tr("Remove preset {0}").replace("{0}", "10:00 - 18:00"), exact: true }).click();
    await expect.poll(() => server.get(12)?.length).toBe(3);
    await dialog.getByRole("button", { name: tr("Done"), exact: true }).click();
    await dialog.getByRole("button", { name: "08:30 – 16:30", exact: true }).click();
    await expect(dialog.getByLabel(tr("From"), { exact: true })).toHaveValue("08:30");
    await dialog.getByRole("button", { name: tr("Cancel"), exact: true }).click();
    expect(availabilityWrites).toBe(0);

    const secondDevice = await browser.newContext({ viewport: { width: 390, height: 640 } });
    const secondPage = await secondDevice.newPage();
    await install(secondPage);
    expect(await secondPage.evaluate(() => localStorage.getItem("gf3:employee-availability:time-presets:12"))).toBeNull();
    const secondDialog = await openDay(secondPage);
    await secondDialog.getByRole("radio", { name: starts("Custom time") }).click();
    await expect(secondDialog.getByRole("button", { name: "08:30 – 16:30", exact: true })).toBeVisible();
    await secondDevice.close();

    await page.reload(); await page.getByRole("button", { name: /^Availability / }).click();
    dialog = await openDay(page);
    await dialog.getByRole("radio", { name: starts("Custom time") }).click();
    await expect(dialog.getByRole("button", { name: "08:30 – 16:30", exact: true })).toBeVisible();
    employeeId = 13;
    await page.reload(); await page.getByRole("button", { name: /^Availability / }).click();
    dialog = await openDay(page);
    await dialog.getByRole("radio", { name: starts("Custom time") }).click();
    await expect(dialog.getByRole("button", { name: "09:00 – 15:00", exact: true })).toBeVisible();
    await expect(dialog.getByRole("button", { name: "08:30 – 16:30", exact: true })).toHaveCount(0);
    expect(errors).toEqual([]);
  });
}
