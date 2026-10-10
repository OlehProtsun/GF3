import { expect, test } from "@playwright/test";

test.use({ reducedMotion: "reduce" });

test("account language switches immediately, persists and stays isolated by role and account", async ({ page }) => {
  let identity = { role: "manager", managerId: 1 as number | null, employeeId: null as number | null, userName: "chief", displayName: "Home" };
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
      body = { language: languages.get(accountKey()) ?? "en" };
    } else if (path === "/api/manager-profile/me") body = manager();
    else if (path === "/api/manager-profile/managers") body = [manager()];
    else if (path === "/api/employee-profile/me") body = { employeeId: identity.employeeId, username: identity.userName, displayName: identity.displayName, recoveryEmail: "worker@example.com", phone: "+48 123 456 789" };
    else if (path.includes("negotiate")) return route.fulfill({ status: 503 });
    else if (path.includes("notepad")) body = { notes: [], state: { isExpanded: false, isPinned: false, height: 400 } };
    await route.fulfill({ contentType: "application/json", body: JSON.stringify(body) });
  });

  await page.goto("/manager-profile");
  await expect(page.getByRole("heading", { name: "Manager profile", exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Collapse sidebar", exact: true }).click();
  await page.getByLabel("Display name", { exact: true }).first().fill("Unsaved draft");
  await page.getByRole("button", { name: "Application language", exact: true }).click();
  await page.screenshot({ path: test.info().outputPath("language-dropdown.png"), fullPage: true });
  await page.getByRole("option", { name: "Polska" }).click();
  await expect(page.getByRole("heading", { name: "Profil menedżera", exact: true })).toBeVisible();
  await expect(page.getByLabel("Nazwa wyświetlana", { exact: true }).first()).toHaveValue("Unsaved draft");
  await expect(page.getByRole("status")).toHaveText("Zapisano");
  await expect(page.getByRole("status").locator("path")).toHaveCSS("animation-duration", "0.42s");
  await expect(page.getByRole("status").locator("svg").locator("..")).toHaveCSS("animation-duration", "2.4s");
  await expect(page.getByRole("status").locator("path")).toHaveCSS("stroke-dashoffset", "0px");
  await page.screenshot({ path: test.info().outputPath("manager-polish.png"), fullPage: true });
  await page.getByRole("button", { name: "Język aplikacji", exact: true }).click();
  await page.getByRole("option", { name: "English" }).click();
  await expect(page.getByRole("heading", { name: "Manager profile", exact: true })).toBeVisible();
  await expect(page.getByLabel("Display name", { exact: true }).first()).toHaveValue("Unsaved draft");
  await page.getByRole("button", { name: "Application language", exact: true }).click();
  await page.getByRole("option", { name: "Polska" }).click();
  await page.reload();
  await expect(page.getByRole("button", { name: "Język aplikacji", exact: true })).toHaveText("Polska");
  await expect(page.getByLabel("Nazwa wyświetlana", { exact: true }).first()).toHaveValue("Home");

  identity = { ...identity, managerId: 2, userName: "second" };
  await page.reload();
  await expect(page.getByRole("button", { name: "Application language", exact: true })).toHaveText("English");
  identity = { ...identity, role: "employee", managerId: null, employeeId: 1, userName: "worker" };
  await page.goto("/profile");
  await expect(page.getByRole("button", { name: "Application language", exact: true })).toHaveText("English");
  await page.getByRole("button", { name: "Application language", exact: true }).click();
  await page.screenshot({ path: test.info().outputPath("employee-language-dropdown.png"), fullPage: true });
  await page.getByRole("option", { name: "Polska" }).click();
  await expect(page.getByRole("button", { name: "Język aplikacji", exact: true })).toHaveText("Polska");
  await expect(page.getByRole("heading", { name: "Dane osobowe" })).toBeVisible();
  await expect(page.getByRole("status")).toHaveText("Zapisano");
  await expect(page.getByRole("status").locator("path")).toHaveCSS("animation-duration", "0.42s");
  await expect(page.getByRole("status").locator("svg").locator("..")).toHaveCSS("animation-duration", "2.4s");
  await expect(page.getByRole("status").locator("path")).toHaveCSS("stroke-dashoffset", "0px");
  await page.screenshot({ path: test.info().outputPath("employee-polish.png"), fullPage: true });
  await page.reload();
  await expect(page.getByRole("button", { name: "Język aplikacji", exact: true })).toHaveText("Polska");
  expect(languages.get("manager:1")).toBe("pl");
  expect(languages.get("manager:2")).toBeUndefined();
  expect(languages.get("employee:1")).toBe("pl");
  expect(errors).toEqual([]);
});
