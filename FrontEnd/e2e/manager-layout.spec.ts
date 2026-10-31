import { expect, test } from "@playwright/test";
import { loginAsManager } from "./helpers";

test.describe("manager sidebar layout", () => {
  test("shows session actions only on manager profile", async ({ page }) => {
    await loginAsManager(page);

    await expect(page.getByRole("link", { name: "Open manager profile" })).toBeVisible();
    await expect(page.locator("aside").getByRole("button", { name: /Log out|Switch to Phone/ })).toHaveCount(0);
    await expect(page.getByText("Manager", { exact: true })).toBeVisible();

    await page.getByRole("link", { name: "Open manager profile" }).click();

    await expect(page).toHaveURL(/\/manager-profile$/);
    await expect(page.getByRole("heading", { name: "Manager profile" })).toBeVisible();
    const card = page.locator('section[aria-labelledby="manager-account-actions-heading"]');
    await page.getByRole("button", { name: "Collapse sidebar" }).click();
    for (const width of [390, 1280]) {
      await page.setViewportSize({ width, height: 900 });
      for (const name of ["Switch to Phone", "Log out"]) {
        const button = card.getByRole("button", { name, exact: true });
        await expect(button).toBeVisible();
        expect((await button.boundingBox())!.height).toBeGreaterThanOrEqual(44);
      }
      expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
    }
  });

  test("hides the sidebar scrollbar while keeping sidebar scrolling and collapse controls usable", async ({ page }) => {
    await loginAsManager(page);
    await page.setViewportSize({ width: 390, height: 720 });

    const sidebar = page.locator("aside").filter({ has: page.getByRole("button", { name: "Collapse sidebar", exact: true }) });
    await expect(sidebar).toBeVisible();

    await sidebar.hover();
    await page.mouse.wheel(0, 600);
    await expect(page.getByRole("button", { name: "Collapse sidebar" })).toBeVisible();

    await page.getByRole("button", { name: "Collapse sidebar" }).click();
    await expect(page.getByRole("button", { name: "Open sidebar" })).toBeVisible();

    await page.getByRole("button", { name: "Open sidebar" }).click();
    await expect(page.getByRole("link", { name: "Home" })).toBeVisible();
  });
});
