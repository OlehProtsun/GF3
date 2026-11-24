import { expect } from "@playwright/test";
import { test } from "./fixtures/isolatedBackend";
import { expectNoRequestError, loginAsManager } from "./helpers";

const managerRoutes = [
  { path: "/", label: "Home", heading: "Home" },
  { path: "/employee", label: "Employee", heading: "Employee List" },
  { path: "/shop", label: "Shop", heading: "Shop List" },
  { path: "/availability", label: "Availability", heading: "Availability List" },
  { path: "/urlop", label: "Urlop", heading: "Urlop" },
  { path: "/container", label: "Container", heading: "Container List" },
  { path: "/information", label: "Information", heading: "Information" },
  { path: "/communications", label: "Message", heading: "Communications" },
  { path: "/settings", label: "Settings", heading: "Settings" },
  { path: "/manager-profile", label: "Open manager profile", heading: "Manager profile" },
] as const;
const signIn = /^(Sign in|Zaloguj się|Увійти)$/;

test.describe("manager critical pages", () => {
  test("requires authentication for manager pages", async ({ page }) => {
    await page.goto("/manager-profile");

    await expect(page.getByRole("heading", { name: signIn })).toBeVisible();
    await expect(page.getByRole("button", { name: signIn })).toBeDisabled();
  });

  test("loads every manager sidebar route without request error", async ({ page }) => {
    await loginAsManager(page);
    const sidebar = page.getByRole("navigation", { name: "Manager navigation", exact: true });
    for (const route of managerRoutes) {
      const link = sidebar.getByRole("link", { name: route.label, exact: true });
      await expect(link).toHaveAttribute("href", route.path);
      await link.click();
      await expect(page).toHaveURL(url => url.pathname === route.path);
      await expect(page.getByRole("heading", { name: route.heading, exact: true })).toBeVisible();
      await expectNoRequestError(page);
    }
    await sidebar.getByRole("link", { name: "DataBase", exact: true }).click();
    await expect(page).toHaveURL(url => url.pathname === "/database");
    await expect(page.getByRole("dialog", { name: "Unlock Database", exact: true })).toBeVisible();
    await expectNoRequestError(page);
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
