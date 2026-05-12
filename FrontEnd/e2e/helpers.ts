import { expect, type APIRequestContext, type Page } from "@playwright/test";

type LoginResponse = {
  accessToken: string;
};

type EmployeeResponse = {
  id: number;
};

export async function loginWithCredentials(page: Page, username: string, password: string) {
  await page.goto("/");

  const signInHeading = page.getByRole("heading", { name: "Sign in" });
  const signInButton = page.getByRole("button", { name: "Sign in" });
  const appShell = page.locator("aside:visible, nav[aria-label='Employee sections']:visible").first();
  const initialScreen = await Promise.race([
    appShell.waitFor({ state: "visible", timeout: 10000 }).then(() => "app"),
    signInHeading.waitFor({ state: "visible", timeout: 10000 }).then(() => "login"),
  ]);

  if (initialScreen === "login") {
    await page.getByRole("textbox", { name: "Username" }).fill(username);
    await page.getByRole("textbox", { name: "Password" }).fill(password);
    await expect(signInButton).toBeEnabled();
    await signInButton.click();
  }

  await expect(appShell).toBeVisible({ timeout: 10000 });
}

export async function loginAsManager(page: Page) {
  await loginWithCredentials(page, "manager", "123");
  await expect(page.getByRole("link", { name: "Home" })).toBeVisible({ timeout: 10000 });
}

export async function expectNoRequestError(page: Page) {
  await expect(page.getByText("Request Error")).toHaveCount(0);
  await expect(page.getByText(/Request failed with status/i)).toHaveCount(0);
}

export async function createEmployeeAccount(api: APIRequestContext, prefix = "e2e") {
  const suffix = `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
  const username = `${prefix}.${suffix}`;
  const password = "test-pass-123";
  const loginResponse = await api.post("/api/auth/login", {
    data: {
      username: "manager",
      password: "123",
    },
  });
  expect(loginResponse.ok()).toBe(true);

  const loginResult = await loginResponse.json() as LoginResponse;
  const createResponse = await api.post("/api/employees", {
    headers: {
      Authorization: `Bearer ${loginResult.accessToken}`,
    },
    data: {
      firstName: `E2E${suffix.slice(-4)}`,
      lastName: "Employee",
      email: `${username}@example.com`,
      phone: "555-0100",
      username,
      password,
    },
  });
  expect(createResponse.ok()).toBe(true);

  const employee = await createResponse.json() as EmployeeResponse;
  return {
    id: employee.id,
    username,
    password,
    managerAccessToken: loginResult.accessToken,
  };
}

export async function deleteEmployeeAccount(
  api: APIRequestContext,
  employeeId: number,
  managerAccessToken: string,
) {
  const deleteResponse = await api.delete(`/api/employees/${employeeId}`, {
    headers: {
      Authorization: `Bearer ${managerAccessToken}`,
    },
  });

  expect([204, 404]).toContain(deleteResponse.status());
}
