import { expect, test } from "@playwright/test";
import { versions, communication, regulation } from "./fixtures/viewport-data";

for (const size of [{ width: 1280, height: 720 }, { width: 1440, height: 800 }, { width: 1024, height: 600 }, { width: 390, height: 664 }]) {
  test(`populated dialogs fit ${size.width}x${size.height}`, async ({ page }) => {
    test.setTimeout(90_000);
    await page.setViewportSize(size);
    const errors: string[] = [];
    page.on("pageerror", e => errors.push(e.message));
    await page.addInitScript(() => localStorage.setItem("gf3.auth.access-token", "viewport-fixture"));
    await page.route("**/*", async route => {
      const path = new URL(route.request().url()).pathname;
      if (!path.startsWith("/api/")) return route.continue();
      let data: unknown = [];
      if (path.endsWith("/versions")) data = versions;
      else if (path === "/api/communications/pending") data = [communication];
      else if (path === "/api/regulations/pending") data = [regulation];
      else if (path === "/api/auth/session") data = { role: "employee", employeeId: 1, managerId: null, userName: "viewport", displayName: "Test account" };
      await route.fulfill({ contentType: "application/json", body: JSON.stringify(data) });
    });
    for (const name of ["transferFull", "availabilityRelated", "versions", "history", "correction", "communication", "regulation", "saving", "manual"]) {
      await test.step(name, async () => {
        await page.goto(`/e2e/fixtures/viewport.html?case=${name}`);
        if (name === "manual") await page.locator('[class*="shiftPickerButton_"]').click();
        if (name === "versions") await expect(page.getByRole("button", { name: /Commit 25,/ })).toBeVisible();
        const surface = page.locator('[class*="surface_"]').filter({ visible: true }).first();
        await expect(surface).toBeVisible();
        const box = (await surface.boundingBox())!;
        expect(box.x).toBeGreaterThanOrEqual(0);
        expect(box.y).toBeGreaterThanOrEqual(0);
        expect(box.x + box.width).toBeLessThanOrEqual(size.width + 1);
        expect(box.y + box.height).toBeLessThanOrEqual(size.height + 1);
        await expect.poll(() => surface.evaluate(el => el.scrollWidth - el.clientWidth)).toBeLessThanOrEqual(1);
        if (name !== "saving") {
          const lastAction = surface.getByRole("button").last();
          await lastAction.scrollIntoViewIfNeeded();
          await expect(lastAction).toBeInViewport({ ratio: 0.98 });
          // Include clipping ancestors: an on-screen rect alone is insufficient.
          if (!(await lastAction.isDisabled())) await lastAction.click({ trial: true, timeout: 5000 });
        }
        if (name === "history") {
          await expect(surface.getByText("After", { exact: true })).toBeInViewport({ ratio: 0.98 });
          if (size.width > 1180) await expect(surface.getByText("Before", { exact: true })).toBeInViewport({ ratio: 0.98 });
        }
        if (name === "versions") {
          await page.getByRole("button", { name: /^Commit 1,/ }).click();
          await surface.getByRole("button", { name: "Switch", exact: true }).click();
          const confirmation = page.getByRole("dialog", { name: "Checkout commit #1", exact: true });
          await expect(confirmation.locator('[class*="surface_"]')).toBeInViewport({ ratio: 1 });
          await confirmation.getByRole("button", { name: "Cancel", exact: true }).click();
          await expect(confirmation).toHaveCount(0);
        }
        await page.screenshot({ path: test.info().outputPath(`${name}.png`) });
      });
    }
    expect(errors).toEqual([]);
  });
}
