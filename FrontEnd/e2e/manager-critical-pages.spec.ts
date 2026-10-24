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

test.describe("manager critical pages", () => {
  test("requires authentication for manager pages", async ({ page }) => {
    await page.goto("/manager-profile");

    await expect(page.getByRole("heading", { name: "Sign in" })).toBeVisible();
    await expect(page.getByRole("button", { name: "Sign in" })).toBeDisabled();
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
    await page.getByRole("button", { name: "Log out" }).click();

    await expect(page.getByRole("heading", { name: "Sign in" })).toBeVisible();

    await page.goto("/database");
    await expect(page.getByRole("heading", { name: "Sign in" })).toBeVisible();
  });
});
