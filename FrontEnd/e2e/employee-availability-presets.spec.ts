import { expect, test } from "@playwright/test";

for (const language of ["en", "pl"] as const) {
  test(`employee availability presets persist and fit the dialog: ${language}`, async ({ page }) => {
    const errors: string[] = [];
    page.on("pageerror", error => errors.push(error.message));
    let employeeId = 12;
    await page.addInitScript(() => localStorage.setItem("gf3.auth.access-token", "availability-fixture"));
    await page.route("**/*", async route => {
      const path = new URL(route.request().url()).pathname;
      if (!path.startsWith("/api/")) return route.continue();
      let body: unknown = [];
      if (path === "/api/auth/session") body = { role: "employee", employeeId, userName: `worker${employeeId}`, displayName: "Worker" };
      else if (path === "/api/account-language") body = { language };
      else if (path === "/api/employee-availability") body = [{
        id: 5, name: "Availability", year: 2026, month: 9, canSubmit: true, isEditLocked: false,
        visibleFromUtc: "2026-09-01T08:00:00Z", visibleToUtc: "2026-10-01T18:00:00Z", slots: [],
      }];
      else if (path.includes("negotiate")) return route.fulfill({ status: 503 });
      await route.fulfill({ contentType: "application/json", body: JSON.stringify(body) });
    });
    await page.goto("/availability");
    const edit = language === "en" ? "Edit preset 9:00 - 15:00" : "Edytuj szablon 9:00 - 15:00";
    const save = language === "en" ? "Save" : "Zapisz";
    const cancel = language === "en" ? "Cancel" : "Anuluj";
    const close = language === "en" ? "Close" : "Zamknij";
    const timeRange = language === "en" ? "Time range" : "Zakres godzin";
    const dayButton = () => page.getByRole("button").filter({ has: page.locator("span").filter({ hasText: /^1$/ }) }).first();

    for (const width of [320, 768, 1024, 1440]) {
      await page.setViewportSize({ width, height: 900 });
      await dayButton().click();
      const dialog = page.getByRole("dialog");
      await expect(dialog.getByRole("button", { name: edit, exact: true })).toBeVisible();
      await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(width);
      await dialog.getByRole("button", { name: edit, exact: true }).focus();
      await page.keyboard.press("Enter");
      const input = dialog.getByRole("textbox", { name: timeRange });
      await expect(input).toBeFocused();
      await input.fill("21:00 - 09:00");
      await expect(dialog.getByRole("button", { name: save, exact: true })).toBeDisabled();
      await input.fill("830-1630");
      await expect(dialog.getByRole("button", { name: save, exact: true })).toBeEnabled();
      await page.screenshot({ path: test.info().outputPath(`preset-editor-${width}.png`) });
      await dialog.getByRole("button", { name: cancel, exact: true }).click();
      await dialog.getByRole("button", { name: close, exact: true }).click();
    }

    await dayButton().click();
    await page.getByRole("button", { name: edit, exact: true }).click();
    await page.getByRole("textbox", { name: timeRange }).fill("830-1630");
    await page.getByRole("dialog").getByRole("button", { name: save, exact: true }).click();
    await page.getByRole("dialog").getByRole("button", { name: close, exact: true }).click();
    await page.reload();
    await dayButton().click();
    await expect(page.getByRole("dialog").getByRole("button", { name: "8:30 - 16:30", exact: true })).toBeVisible();
    await page.getByRole("dialog").getByRole("button", { name: "8:30 - 16:30", exact: true }).click();
    await page.getByRole("dialog").getByRole("button", { name: save, exact: true }).click();
    await expect(dayButton()).toContainText("08:30 - 16:30");
    employeeId = 13;
    await page.reload();
    await dayButton().click();
    await expect(page.getByRole("dialog").getByRole("button", { name: "9:00 - 15:00", exact: true })).toBeVisible();
    await expect(page.getByRole("dialog").getByRole("button", { name: "8:30 - 16:30", exact: true })).toHaveCount(0);
    expect(errors).toEqual([]);
  });
}
