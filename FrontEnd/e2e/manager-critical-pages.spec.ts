import { expect, test } from "@playwright/test";
import { expectNoRequestError, loginAsManager } from "./helpers";

const managerRoutes = [
  { path: "/", label: "Home" },
  { path: "/employee", label: "Employee" },
  { path: "/shop", label: "Shop" },
  { path: "/availability", label: "Availability" },
  { path: "/container", label: "Container" },
  { path: "/information", label: "Information" },
  { path: "/database", label: "DataBase" },
  { path: "/manager-profile", label: "Manager" },
] as const;
const signIn = /^(Sign in|Zaloguj się)$/;

test.describe("manager critical pages", () => {
  test("requires authentication for manager pages", async ({ page }) => {
    await page.goto("/manager-profile");

    await expect(page.getByRole("heading", { name: signIn })).toBeVisible();
    await expect(page.getByRole("button", { name: signIn })).toBeDisabled();
  });

  test("loads every manager sidebar route without request error", async ({ page }) => {
    await loginAsManager(page);

    for (const route of managerRoutes) {
      await page.goto(route.path);
      await expect(page.getByRole("link", { name: "Home" })).toBeVisible();
      await expect(page.getByText(route.label, { exact: true }).first()).toBeVisible();
      await expectNoRequestError(page);
    }
  });

  test("logs out and removes access to protected pages", async ({ page }) => {
    await loginAsManager(page);
    await page.goto("/manager-profile");
    const logout = page.getByRole("button", { name: "Log out" });
    await expect(logout).toBeVisible();
    const sidebar = page.getByRole("button", { name: "Collapse sidebar", exact: true });
    if (await sidebar.isVisible()) await sidebar.click();
    await logout.click();

    await expect(page.getByRole("heading", { name: signIn })).toBeVisible();

    await page.goto("/database");
    await expect(page.getByRole("heading", { name: signIn })).toBeVisible();
  });
});
