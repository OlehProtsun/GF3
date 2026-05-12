import { expect, test } from "@playwright/test";
import {
  createEmployeeAccount,
  deleteEmployeeAccount,
  expectNoRequestError,
  loginWithCredentials,
} from "./helpers";

const employeeRoutes = [
  { path: "/", text: "Inbox" },
  { path: "/availability", text: "No active windows" },
  { path: "/schedule", text: "No published schedules" },
  { path: "/swap", text: "Open swaps" },
  { path: "/profile", text: "Recovery email" },
] as const;

test.describe("employee critical pages", () => {
  test("new employee can sign in and visit the employee workflow pages", async ({ page, request }) => {
    const employee = await createEmployeeAccount(request, "employee.e2e");

    try {
      await loginWithCredentials(page, employee.username, employee.password);
      await expect(page.getByRole("heading", { name: "Inbox" })).toBeVisible();

      for (const route of employeeRoutes) {
        await page.goto(route.path);
        await expect(page.getByText(route.text, { exact: true }).first()).toBeVisible();
        await expectNoRequestError(page);
      }
    } finally {
      await deleteEmployeeAccount(request, employee.id, employee.managerAccessToken);
    }
  });

  test("employee users are redirected away from manager-only routes", async ({ page, request }) => {
    const employee = await createEmployeeAccount(request, "employee.route.e2e");

    try {
      await loginWithCredentials(page, employee.username, employee.password);
      await page.goto("/database");

      await expect(page).toHaveURL(/\/$/);
      await expect(page.getByRole("heading", { name: "Inbox" })).toBeVisible();
      await expectNoRequestError(page);
    } finally {
      await deleteEmployeeAccount(request, employee.id, employee.managerAccessToken);
    }
  });
});
