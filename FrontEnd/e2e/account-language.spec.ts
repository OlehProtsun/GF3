import { expect, test } from "@playwright/test";

test.use({ reducedMotion: "reduce" });

test("account language defaults to Polish, persists explicit English and stays isolated", async ({ page }) => {
  test.setTimeout(90_000);
  let identity = { role: "manager", workspaceMode: "pc", managerId: 1 as number | null, employeeId: null as number | null, userName: "chief", displayName: "Home" };
  const languages = new Map<string, string>();
  const accountKey = () => `${identity.role}:${identity.managerId ?? identity.employeeId}`;
  const manager = () => ({ id: identity.managerId, userName: identity.userName, displayName: identity.displayName, recoveryEmail: "chief@example.com", isOnline: true, isSystem: false, createdAtUtc: "2026-09-01T12:00:00Z" });
  const errors: string[] = [];
  page.on("pageerror", error => errors.push(error.message));
  await page.addInitScript(() => localStorage.setItem("gf3.auth.access-token", "language-test-token"));
  await page.route("**/*", async route => {
    const path = new URL(route.request().url()).pathname;
    if (!path.startsWith("/api/")) return route.continue();
    let body: unknown = [];
    if (path === "/api/auth/session") body = identity;
    else if (path === "/api/account-language") {
      if (route.request().method() === "PUT") languages.set(accountKey(), route.request().postDataJSON().language);
      body = { language: languages.get(accountKey()) ?? "pl" };
    } else if (path === "/api/manager-profile/me") body = manager();
    else if (path === "/api/manager-profile/managers") body = [manager()];
    else if (path === "/api/employee-profile/me") body = { employeeId: identity.employeeId, username: identity.userName, displayName: identity.displayName, recoveryEmail: "worker@example.com", phone: "+48 123 456 789" };
    else if (path.includes("negotiate")) return route.fulfill({ status: 503 });
    else if (path.includes("notepad")) body = { notes: [], state: { isExpanded: false, isPinned: false, height: 400 } };
    await route.fulfill({ contentType: "application/json", body: JSON.stringify(body) });
  });
  const choose = async (current: "pl" | "en", next: "Polski" | "English") => {
    const collapse = page.getByRole("button", { name: current === "pl" ? "Zwiń pasek boczny" : "Collapse sidebar", exact: true });
    if (await collapse.isVisible()) await collapse.click();
    await page.getByRole("button", { name: current === "pl" ? "Język aplikacji" : "Application language", exact: true }).click();
    await page.getByRole("option", { name: next, exact: true }).click();
    await expect(page.locator("html")).toHaveAttribute("lang", next === "Polski" ? "pl" : "en");
  };
  await page.goto("/manager-profile");
  await expect(page.getByRole("heading", { name: "Profil menedżera", exact: true })).toBeVisible();
  await expect(page.locator("html")).toHaveAttribute("lang", "pl");
  await page.getByRole("button", { name: "Zwiń pasek boczny", exact: true }).click();
  await page.getByLabel("Nazwa wyświetlana", { exact: true }).first().fill("Unsaved draft");
  await choose("pl", "English");
  await expect(page.getByLabel("Display name", { exact: true }).first()).toHaveValue("Unsaved draft");
  await expect(page.getByRole("status")).toHaveText("Saved");
  await choose("en", "Polski");
  await expect(page.getByLabel("Nazwa wyświetlana", { exact: true }).first()).toHaveValue("Unsaved draft");
  await expect(page.getByRole("status")).toHaveText("Zapisano");
  await choose("pl", "English");
  await page.reload();
  await expect(page.getByRole("button", { name: "Application language", exact: true })).toHaveText("English");
  await expect(page.locator("html")).toHaveAttribute("lang", "en");
  await expect(page.getByLabel("Display name", { exact: true }).first()).toHaveValue("Home");
  identity = { ...identity, managerId: 2, userName: "second" };
  await page.reload();
  await expect(page.getByRole("button", { name: "Język aplikacji", exact: true })).toHaveText("Polski");
  identity = { ...identity, role: "employee", managerId: null, employeeId: 1, userName: "worker" };
  await page.goto("/profile");
  await expect(page.getByRole("button", { name: "Język aplikacji", exact: true })).toHaveText("Polski");
  await expect(page.getByRole("heading", { name: "Dane osobowe" })).toBeVisible();
  await choose("pl", "English");
  await page.reload();
  await expect(page.getByRole("button", { name: "Application language", exact: true })).toHaveText("English");
  await choose("en", "Polski");
  await page.reload();
  await expect(page.getByRole("button", { name: "Język aplikacji", exact: true })).toHaveText("Polski");
  identity = { ...identity, role: "manager", managerId: 1, employeeId: null, userName: "chief" };
  await page.goto("/manager-profile");
  await expect(page.getByRole("button", { name: "Application language", exact: true })).toHaveText("English");
  await choose("en", "Polski");
  expect(languages.get("manager:1")).toBe("pl");
  expect(languages.get("manager:2")).toBeUndefined();
  expect(languages.get("employee:1")).toBe("pl");
  expect(errors).toEqual([]);
});

test("guests see Polish login and recovery without requesting an account preference", async ({ page }) => {
  let preferenceRequests = 0;
  await page.route("**/*", async route => {
    const path = new URL(route.request().url()).pathname;
    if (!path.startsWith("/api/")) return route.continue();
    if (path === "/api/account-language") preferenceRequests++;
    await route.fulfill({ status: 401, contentType: "application/json", body: "{}" });
  });
  await page.goto("/login");
  await expect(page.locator("html")).toHaveAttribute("lang", "pl");
  await expect(page.getByRole("heading", { name: "Zaloguj się", exact: true })).toBeVisible();
  await page.goto("/password-recovery");
  await expect(page.locator("html")).toHaveAttribute("lang", "pl");
  await expect(page.getByRole("heading", { name: "Zresetuj hasło", exact: true })).toBeVisible();
  expect(preferenceRequests).toBe(0);
});
