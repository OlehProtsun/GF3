import { expect, test } from "@playwright/test";

const longName = "VeryLongUnbrokenAccountName".repeat(12);

for (const role of ["manager", "employee"]) {
  test(`${role} profile truncates long identity text inside its section`, async ({ page }) => {
    await page.addInitScript(() => localStorage.setItem("gf3.auth.access-token", "responsive-fixture"));
    await page.route("**/*", async route => {
      const path = new URL(route.request().url()).pathname;
      if (!path.startsWith("/api/")) return route.continue();
      let body: unknown = [];
      const profile = { id: 1, employeeId: 1, username: longName, userName: longName, displayName: longName, recoveryEmail: `${longName}@example.com`, phone: "123456789".repeat(20), isSystem: false };
      if (path === "/api/auth/session") body = { role, managerId: role === "manager" ? 1 : null, employeeId: role === "employee" ? 1 : null, userName: longName, displayName: longName };
      else if (path === "/api/account-language") body = { language: "en" };
      else if (path.endsWith("profile/me")) body = profile;
      else if (path.endsWith("profile/managers")) body = [profile];
      else if (path.includes("notepad")) body = { notes: [], state: { isExpanded: false, isPinned: false, height: 400 } };
      else if (path.includes("negotiate")) return route.fulfill({ status: 503 });
      await route.fulfill({ json: body });
    });
    for (const width of [320, 375, 768]) {
      await page.setViewportSize({ width, height: 900 });
      await page.goto(role === "manager" ? "/manager-profile" : "/profile");
      const name = role === "manager" ? page.locator('[class*="profileTitle_"] strong') : page.locator('h1[class*="profileName_"]');
      await expect(name).toHaveText(longName);
      await expect(name).toHaveCSS("text-overflow", "ellipsis");
      await expect(name).toHaveCSS("white-space", "nowrap");
      await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(width);
      const overflow = await page.locator("main section").evaluateAll(elements => elements.filter(element => {
        const box = element.getBoundingClientRect();
        const parent = element.parentElement!.getBoundingClientRect();
        return box.width > 0 && (box.right > parent.right + 2 || box.left < parent.left - 2);
      }).map(element => element.className));
      expect(overflow, `Sections at ${width}px`).toEqual([]);
      await page.screenshot({ path: test.info().outputPath(`${role}-${width}.png`) });
    }
  });
}

test("shared buttons and cards keep long labels inside narrow containers", async ({ page }) => {
  for (const width of [320, 375, 768]) {
    await page.setViewportSize({ width, height: 900 });
    await page.goto("/e2e/fixtures/viewport.html?case=longText");
    const button = page.getByRole("button");
    await expect(button).toBeVisible();
    await expect(button.locator("span").last()).toHaveCSS("text-overflow", "ellipsis");
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(width);
    expect(await page.locator('button, article, section, [class*="title_"], [class*="description_"], [class*="metaItem_"], [class*="metaValue_"]').evaluateAll(elements => elements.flatMap(element => {
      const box = element.getBoundingClientRect();
      const parent = element.parentElement!.getBoundingClientRect();
      return box.right > parent.right + 2 ? [element.className] : [];
    }))).toEqual([]);
    await page.screenshot({ path: test.info().outputPath(`components-${width}.png`) });
  }
});
