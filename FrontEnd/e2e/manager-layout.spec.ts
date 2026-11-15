import { expect, test } from "@playwright/test";
import { loginAsManager, loginWithCredentials } from "./helpers";

test.describe("manager sidebar layout", () => {
  test("starts hidden and slides the overlay with no reserved page space", async ({ page }) => {
    await loginWithCredentials(page, "manager", "123456");
    await expect(page.locator("#manager-sidebar-navigation")).toBeHidden();
    await expect(page.locator("main")).toHaveCSS("padding-left", "0px");
    await page.getByRole("button", { name: "Open sidebar", exact: true }).click();
    const nav = page.locator("#manager-sidebar-navigation");
    const label = nav.getByText("Home", { exact: true });
    await expect(nav).toHaveCSS("width", "86px");
    await expect(label).toBeVisible();
    await expect(page.locator("main")).toHaveCSS("padding-left", "0px");
    for (let i = 0; i < 2; i++) {
      await page.getByRole("button", { name: "Collapse sidebar", exact: true }).first().click();
      await expect(nav).toBeHidden();
      expect(await nav.evaluate(el => el.getBoundingClientRect().bottom)).toBeLessThan(0);
      await expect(label).toBeHidden();
      await expect(page.locator("main")).toHaveCSS("padding-left", "0px");
      await expect(nav.getByRole("link", { name: "Home", exact: true })).toBeHidden();
      await page.getByRole("button", { name: "Open sidebar", exact: true }).click();
      await expect(nav).toHaveCSS("width", "86px");
      await expect(label).toBeVisible();
      await expect(page.locator("main")).toHaveCSS("padding-left", "0px");
    }
    await page.locator('[class*="backdropVisible"]').click({ position: { x: 300, y: 300 } });
    await expect(nav).toHaveAttribute("data-collapsed", "true");
    await page.getByRole("button", { name: "Open sidebar", exact: true }).click();
    await expect(label).toBeVisible();
  });
  test("keeps profile actions and content clear of the fixed rail", async ({ page }) => {
    await loginAsManager(page);
    const rail = page.locator("#manager-sidebar-navigation");
    await expect(rail).toBeVisible();
    await expect(rail.getByRole("button", { name: /Log out|Switch to Phone/ })).toHaveCount(0);
    await expect(page.getByRole("button", { name: "Collapse sidebar" }).first()).toBeVisible();
    await expect(rail.getByText("Manager", { exact: true })).toBeVisible();
    await page.getByRole("link", { name: "Open manager profile" }).click();
    await expect(page).toHaveURL(/\/manager-profile$/);
    await expect(page.getByRole("heading", { name: "Manager profile" })).toBeVisible();
    await page.getByRole("button", { name: "Collapse sidebar" }).first().click();
    await expect(rail).toBeHidden();
    const card = page.locator('section[aria-labelledby="manager-account-actions-heading"]');
    for (const width of [390, 960, 1280, 1440]) {
      await page.setViewportSize({ width, height: 900 });
      await expect(rail).toBeHidden();
      for (const name of ["Switch to Phone", "Log out"]) {
        const button = card.getByRole("button", { name, exact: true });
        await expect(button).toBeVisible();
        expect((await button.boundingBox())!.height).toBeGreaterThanOrEqual(44);
      }
      expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
      expect(await page.locator("main").evaluate(el => parseFloat(getComputedStyle(el).paddingLeft))).toBe(0);
    }
    await page.getByRole("button", { name: "Open sidebar", exact: true }).click();
    for (const name of ["Employee", "Shop", "Availability", "Urlop", "Container", "Message", "Settings", "Home"]) {
      const link = rail.getByRole("link", { name, exact: true });
      const href = await link.getAttribute("href"); await link.click();
      await expect(page).toHaveURL(new RegExp(href === "/" ? "/$" : `${href}$`));
      for (const width of [390, 960, 1280, 1440]) {
        await page.setViewportSize({ width, height: 900 });
        expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
      }
    }
    await page.getByRole("button", { name: "Collapse sidebar", exact: true }).first().click();
    await page.getByRole("button", { name: "Open system news", exact: true }).click();
    await expect(page.getByRole("complementary", { name: "System news", exact: true })).toBeVisible();
    await page.getByRole("button", { name: "Close system news", exact: true }).click();
    await page.getByRole("button", { name: "Open notepad", exact: true }).click();
    await expect(page.getByLabel("Manager notepad", { exact: true })).toBeVisible();
    await page.getByRole("button", { name: "Collapse notepad", exact: true }).click();
    await expect(page.getByLabel("Manager notepad", { exact: true })).toHaveAttribute("aria-hidden", "true");
  });

  test("scrolls only the icon list and keeps Profile pinned at short heights", async ({ page }) => {
    await loginAsManager(page);
    await page.setViewportSize({ width: 390, height: 720 });
    const rail = page.locator("#manager-sidebar-navigation");
    const profile = rail.getByRole("link", { name: "Open manager profile" });
    const before = await profile.boundingBox();
    const scroller = rail.locator('[class*="navScroll"]');
    await scroller.evaluate(el => el.scrollTop = el.scrollHeight);
    await expect(profile).toBeVisible(); expect(await profile.boundingBox()).toEqual(before);
    await expect(scroller).toHaveCSS("scrollbar-width", "none");
    await expect(page.locator('[class*="backdrop"]')).toHaveClass(/backdropVisible/);
    expect((await profile.boundingBox())!.height).toBeGreaterThanOrEqual(44);
    await page.setViewportSize({ width: 390, height: 480 });
    expect(await scroller.evaluate(el => el.scrollHeight > el.clientHeight)).toBe(true);
    const pinned = await profile.boundingBox();
    await scroller.evaluate(el => el.scrollTop = el.scrollHeight);
    expect(await profile.boundingBox()).toEqual(pinned);
  });
});
