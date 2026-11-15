import { expect, test, type Page } from "@playwright/test";
import pl from "../src/shared/i18n/pl.json" with { type: "json" };
import uk from "../src/shared/i18n/uk.json" with { type: "json" };

test.use({ reducedMotion: "reduce" });

async function profileFixture(page: Page, language = "en", system = false) {
  let profile = { id: 3, displayName: "test", userName: "test", recoveryEmail: "manager@example.com", isSystem: system, isOnline: true, createdAtUtc: "2026-09-01T12:00:00Z" };
  let managers = [
    { ...profile, id: 1, displayName: "OlehProtsun", userName: "manager", isSystem: true, isOnline: false, lastLoginAtUtc: "2026-10-22T10:00:00Z" },
    { ...profile, id: 2, displayName: "", userName: "manager2", recoveryEmail: "", isSystem: false, isOnline: false, lastLoginAtUtc: "2026-07-16T10:00:00Z" },
    profile,
  ];
  const updates: unknown[] = [];
  const creations: unknown[] = [];
  const deletions: number[] = [];
  let downloads = 0;
  const identity = () => ({ role: "manager", workspaceMode: "pc", managerId: 3, userName: profile.userName, displayName: profile.displayName, isSystemManager: system });
  await page.addInitScript(() => localStorage.setItem("gf3.auth.access-token", "profile-test-token"));
  await page.route("**/*", async route => {
    const path = new URL(route.request().url()).pathname;
    if (!path.startsWith("/api/")) return route.continue();
    let body: unknown = [];
    if (path === "/api/auth/session") body = identity();
    else if (path === "/api/account-language") body = { language };
    else if (path === "/api/public-holiday-settings") body = { showPublicHolidays: true };
    else if (path === "/api/manager-profile/me") {
      if (route.request().method() === "PUT") {
        const payload = route.request().postDataJSON();
        updates.push(payload);
        profile = { ...profile, displayName: payload.displayName, userName: payload.userName, recoveryEmail: payload.recoveryEmail };
        managers = managers.map(manager => manager.id === 3 ? profile : manager);
        body = { profile, session: identity(), accessToken: "updated-profile-token", expiresAtUtc: "2099-01-01T00:00:00Z" };
      } else body = profile;
    } else if (path === "/api/manager-profile/managers") {
      if (route.request().method() === "POST") {
        const payload = route.request().postDataJSON();
        creations.push(payload);
        const created = { ...profile, ...payload, id: 4, isSystem: false, isOnline: false };
        managers.push(created);
        body = created;
      } else body = managers;
    } else if (path.match(/^\/api\/manager-profile\/managers\/\d+$/)) {
      const id = Number(path.split("/").at(-1));
      deletions.push(id);
      managers = managers.filter(manager => manager.id !== id);
      return route.fulfill({ status: 204 });
    } else if (path === "/api/regulations/history/me") body = [{ id: 1, regulationDocumentId: 4, regulationTitle: "Actual manager document.pdf", regulationVersion: "v3", pdfSha256: "0123456789abcdef", acceptedAtUtc: "2026-10-23T12:00:00Z" }];
    else if (path === "/api/regulations/4/pdf") {
      downloads++;
      return route.fulfill({ contentType: "application/pdf", body: "%PDF-1.4\n%%EOF" });
    } else if (path.includes("negotiate")) return route.fulfill({ status: 503 });
    else if (path.includes("notepad")) body = { notes: [], state: { isExpanded: false, isPinned: false, height: 400 } };
    await route.fulfill({ contentType: "application/json", body: JSON.stringify(body) });
  });
  await page.goto("/manager-profile");
  await expect(page.locator("html")).toHaveAttribute("lang", language);
  const collapseLabels: Record<string, string> = { en: "Collapse sidebar", pl: "Zwiń pasek boczny", uk: "Згорнути бічну панель" };
  const collapse = page.getByRole("button", { name: collapseLabels[language], exact: true });
  if (await collapse.isVisible()) {
    await collapse.click();
    await expect.poll(() => page.locator("aside").first().evaluate(element => element.getBoundingClientRect().bottom)).toBeLessThanOrEqual(0);
  }
  return { updates, creations, deletions, downloads: () => downloads };
}

test("profile retains editing, numeric passwords, team permissions, confirmation and document actions", async ({ page }) => {
  const state = await profileFixture(page);
  await expect(page.getByRole("heading", { name: "Account details", exact: true })).toBeVisible();
  const details = page.locator("div").filter({ has: page.getByRole("heading", { name: "Account details", exact: true }) }).filter({ has: page.getByRole("button", { name: "Save changes", exact: true }) }).last();
  const create = page.locator("div").filter({ has: page.getByRole("heading", { name: "Add new manager", exact: true }) }).filter({ has: page.getByRole("button", { name: "Create manager", exact: true }) }).last();
  await expect(details.getByLabel("Display name", { exact: true })).toHaveValue("test");
  await expect(details.getByLabel("New password", { exact: true })).toHaveValue("");
  await details.getByLabel("New password", { exact: true }).fill("12x");
  await expect(details.getByLabel("New password", { exact: true })).toHaveValue("12");
  await page.getByRole("button", { name: "Save changes", exact: true }).click();
  await expect(page.getByText("Password must contain exactly 6 digits.", { exact: true })).toBeVisible();
  expect(state.updates).toHaveLength(0);
  await details.getByLabel("New password", { exact: true }).fill("654321");
  await details.getByLabel("Display name", { exact: true }).fill("Updated Manager");
  await page.getByRole("button", { name: "Save changes", exact: true }).click();
  await expect(page.getByText("Manager profile updated.", { exact: true })).toBeVisible();
  expect(state.updates).toEqual([{ displayName: "Updated Manager", userName: "test", recoveryEmail: "manager@example.com", newPassword: "654321" }]);
  await expect(details.getByLabel("New password", { exact: true })).toHaveValue("");
  await create.getByLabel("Display name", { exact: true }).fill("New Manager");
  await create.getByLabel("Username", { exact: true }).fill("manager.new");
  await create.getByLabel("Recovery email", { exact: true }).fill("new@example.com");
  await create.getByLabel("Temporary password", { exact: true }).fill("123456");
  await page.getByRole("button", { name: "Create manager", exact: true }).click();
  await expect(page.locator("article").filter({ hasText: "@manager.new" }).filter({ has: page.getByRole("button", { name: "Delete", exact: true }) })).toBeVisible();
  expect(state.creations).toEqual([{ displayName: "New Manager", userName: "manager.new", recoveryEmail: "new@example.com", password: "123456" }]);
  const systemRow = page.locator("article").filter({ hasText: "OlehProtsun" }).filter({ hasText: "System" });
  await expect(systemRow.getByRole("button")).toHaveCount(0);
  await expect(page.getByText("You", { exact: true })).toBeVisible();
  const otherRow = page.locator("article").filter({ hasText: "@manager2" }).filter({ has: page.getByRole("button", { name: "Delete", exact: true }) });
  await otherRow.getByRole("button").click();
  await page.getByRole("dialog").getByRole("button", { name: "Cancel", exact: true }).click();
  expect(state.deletions).toHaveLength(0);
  await otherRow.getByRole("button").click();
  await page.getByRole("dialog").getByRole("button", { name: "Delete", exact: true }).click();
  await expect(otherRow).toHaveCount(0);
  expect(state.deletions).toEqual([2]);
  await page.reload();
  await expect(details.getByLabel("Display name", { exact: true })).toHaveValue("Updated Manager");
  await page.getByRole("button", { name: "Collapse sidebar", exact: true }).click();
  await expect.poll(() => page.locator("aside").first().evaluate(element => element.getBoundingClientRect().bottom)).toBeLessThanOrEqual(0);
  await page.getByRole("button", { name: /Actual manager document.pdf/ }).click();
  await expect.poll(state.downloads).toBe(1);
});

test("system account retains restricted username and password", async ({ page }) => {
  await profileFixture(page, "en", true);
  await expect(page.getByLabel("Username", { exact: true }).first()).toBeDisabled();
  await expect(page.getByLabel("New password", { exact: true })).toBeDisabled();
  await expect(page.getByLabel("Display name", { exact: true }).first()).toBeEditable();
});

for (const [width, language] of [[320, "uk"], [390, "pl"], [768, "en"], [1366, "en"], [1920, "uk"]] as const) {
  test(`profile is responsive at ${width}px in ${language}`, async ({ page }, testInfo) => {
    await page.setViewportSize({ width, height: 1000 });
    await profileFixture(page, language);
    const dictionary: Record<string, string> = language === "pl" ? pl : language === "uk" ? uk : {};
    const label = (key: string) => dictionary[key] ?? key;
    await expect(page.getByRole("heading", { name: label("Manager team"), exact: true })).toBeVisible();
    await expect(page.getByText(label("Online now"), { exact: true })).toBeVisible();
    await expect(page.locator("article").filter({ hasText: "OlehProtsun" }).first()).toContainText(label("Last online {0}").split("{0}")[0]);
    await expect(page.getByRole("button", { name: /Actual manager document.pdf/ })).toBeVisible();
    await expect(page.getByRole("button", { name: /Application language|Język aplikacji|Мова застосунку/ })).toHaveCount(0);
    const content = page.locator("section").filter({ has: page.getByRole("heading", { name: label("Manager team"), exact: true }) }).first();
    await expect(content.locator("img, input[type=file]")).toHaveCount(0);
    expect(await content.locator("article").evaluateAll(rows => rows.every(row => row.querySelector("span")?.textContent?.match(/^[A-ZА-ЯІЇЄ]{1,2}$/)))).toBe(true);
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
    expect(await content.evaluate(element => [...element.querySelectorAll("input, article, button")].flatMap(node => {
      const rect = node.getBoundingClientRect();
      return rect.left >= 0 && rect.right <= innerWidth ? [] : [{ tag: node.tagName, text: node.textContent, left: rect.left, right: rect.right }];
    }))).toEqual([]);
    await page.screenshot({ path: testInfo.outputPath(`profile-${width}-${language}.png`), fullPage: true });
  });
}
