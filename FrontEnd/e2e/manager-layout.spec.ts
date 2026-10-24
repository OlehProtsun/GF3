import { expect, test } from "@playwright/test";
import { loginAsManager } from "./helpers";

test.describe("manager sidebar layout", () => {
  test("uses shared nav-button styling for manager and logout actions", async ({ page }) => {
    await loginAsManager(page);

    await expect(page.getByRole("link", { name: "Open manager profile" })).toBeVisible();
    await expect(page.getByRole("button", { name: "Log out" })).toBeVisible();
    await expect(page.getByText("Manager", { exact: true })).toBeVisible();

    await page.getByRole("link", { name: "Open manager profile" }).click();

    await expect(page).toHaveURL(/\/manager-profile$/);
    await expect(page.getByRole("heading", { name: "Manager profile" })).toBeVisible();
  });

  test("hides the sidebar scrollbar while keeping sidebar scrolling and collapse controls usable", async ({ page }) => {
    await loginAsManager(page);
    await page.setViewportSize({ width: 390, height: 720 });

    const sidebar = page.locator("aside");
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
