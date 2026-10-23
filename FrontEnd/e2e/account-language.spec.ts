import { expect, test } from "@playwright/test";

test.use({ reducedMotion: "reduce" });

test("account language defaults to Polish, persists English and Ukrainian and stays isolated", async ({ page }) => {
  test.setTimeout(120_000);
  let identity = { role: "manager", workspaceMode: "pc", managerId: 1 as number | null, employeeId: null as number | null, userName: "chief", displayName: "Home" };
  const languages = new Map<string, string>();
  let showPublicHolidays = true;
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
    else if (path === "/api/auth/logout") return route.fulfill({ status: 204 });
    else if (path === "/api/auth/login") {
      identity.workspaceMode = "choose";
      body = { session: identity, accessToken: "language-login-token", expiresAtUtc: "2099-01-01T00:00:00Z" };
    }
    else if (path === "/api/auth/manager-mode") {
      identity.workspaceMode = route.request().postDataJSON().mode;
      body = { session: identity, accessToken: "language-mode-token", expiresAtUtc: "2099-01-01T00:00:00Z" };
    }
    else if (path === "/api/public-holiday-settings") {
      if (route.request().method() === "PUT") showPublicHolidays = route.request().postDataJSON().showPublicHolidays;
      body = { showPublicHolidays };
    }
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
  const languageCodes = { Polski: "pl", English: "en", Українська: "uk" } as const;
  const languageLabels = { pl: "Język aplikacji", en: "Application language", uk: "Мова застосунку" };
  const collapseLabels = { pl: "Zwiń pasek boczny", en: "Collapse sidebar", uk: "Згорнути бічну панель" };
  const settingsLanguageLabels = { pl: "Język Polski", en: "Language English", uk: "Мова Українська" };
  const choose = async (current: "pl" | "en" | "uk", next: keyof typeof languageCodes) => {
    const collapse = page.getByRole("button", { name: collapseLabels[current], exact: true });
    if (await collapse.isVisible()) await collapse.click();
    if (identity.role === "manager") {
      await page.getByRole("button", { name: settingsLanguageLabels[current], exact: true }).click();
      await page.getByRole("radio", { name: next, exact: true }).click();
      await expect(page.getByRole("radio", { name: next, exact: true })).toBeChecked();
      await expect(page.locator("html")).toHaveAttribute("lang", languageCodes[next]);
      await page.keyboard.press("Escape");
      return;
    }
    await page.getByRole("button", { name: languageLabels[current], exact: true }).click();
    if (new URL(page.url()).pathname === "/profile") {
      const listbox = page.getByRole("listbox", { name: languageLabels[current], exact: true });
      await expect(listbox).toBeVisible();
      expect(await listbox.evaluate(element => {
        const dropdown = element.parentElement!;
        const bounds = dropdown.getBoundingClientRect();
        return dropdown.scrollWidth <= dropdown.clientWidth && bounds.left >= 0 && bounds.right <= innerWidth;
      })).toBe(true);
    }
    await page.getByRole("option", { name: next, exact: true }).click();
    await expect(page.locator("html")).toHaveAttribute("lang", languageCodes[next]);
  };
  await page.goto("/manager-profile");
  await expect(page.getByRole("heading", { name: "Profil menedżera", exact: true })).toBeVisible();
  await expect(page.locator("html")).toHaveAttribute("lang", "pl");
  await expect(page.getByRole("button", { name: "Język aplikacji", exact: true })).toHaveCount(0);
  await page.goto("/settings");
  await expect(page.getByRole("button", { name: "Język Polski", exact: true })).toBeVisible();
  await choose("pl", "English");
  await expect(page.getByRole("heading", { name: "Settings", exact: true })).toBeVisible();
  await page.getByRole("switch").click();
  await expect(page.getByRole("switch")).not.toBeChecked();
  await choose("en", "Українська");
  await expect(page.getByRole("heading", { name: "Налаштування", exact: true })).toBeVisible();
  await choose("uk", "Polski");
  await expect(page.getByRole("heading", { name: "Ustawienia", exact: true })).toBeVisible();
  await choose("pl", "English");
  await page.reload();
  await expect(page.getByRole("button", { name: "Language English", exact: true })).toBeVisible();
  await expect(page.locator("html")).toHaveAttribute("lang", "en");
  await expect(page.getByRole("switch")).not.toBeChecked();
  await choose("en", "Українська");
  await page.reload();
  await expect(page.getByRole("button", { name: "Мова Українська", exact: true })).toBeVisible();
  await expect(page.locator("html")).toHaveAttribute("lang", "uk");
  await choose("uk", "English");
  await page.getByRole("button", { name: "Open sidebar", exact: true }).click();
  await page.getByRole("button", { name: "Log out", exact: true }).click();
  await expect(page.getByRole("heading", { name: "Zaloguj się", exact: true })).toBeVisible();
  await page.getByRole("textbox", { name: "Nazwa użytkownika", exact: true }).fill("chief");
  await page.getByRole("textbox", { name: "Hasło", exact: true }).fill("123456");
  await page.getByRole("button", { name: "Zaloguj się", exact: true }).click();
  await page.getByRole("button", { name: /^PC/ }).click();
  await page.goto("/settings");
  await expect(page.getByRole("button", { name: "Language English", exact: true })).toBeVisible();
  await expect(page.getByRole("switch")).not.toBeChecked();
  identity = { ...identity, managerId: 2, userName: "second" };
  await page.reload();
  await expect(page.getByRole("button", { name: "Język Polski", exact: true })).toBeVisible();
  await expect(page.getByRole("switch")).not.toBeChecked();
  identity = { ...identity, role: "employee", managerId: null, employeeId: 1, userName: "worker" };
  await page.goto("/profile");
  await expect(page.getByRole("button", { name: "Język aplikacji", exact: true })).toHaveText("Polski");
  await expect(page.getByRole("heading", { name: "Dane konta" })).toBeVisible();
  await choose("pl", "English");
  await page.reload();
  await expect(page.getByRole("button", { name: "Application language", exact: true })).toHaveText("English");
  await choose("en", "Українська");
  await page.reload();
  await expect(page.getByRole("button", { name: "Мова застосунку", exact: true })).toHaveText("Українська");
  await expect(page.getByRole("heading", { name: "Дані акаунту", exact: true })).toBeVisible();
  await choose("uk", "Polski");
  await page.reload();
  await expect(page.getByRole("button", { name: "Język aplikacji", exact: true })).toHaveText("Polski");
  identity = { ...identity, role: "manager", managerId: 1, employeeId: null, userName: "chief" };
  await page.goto("/settings");
  await expect(page.getByRole("button", { name: "Language English", exact: true })).toBeVisible();
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
