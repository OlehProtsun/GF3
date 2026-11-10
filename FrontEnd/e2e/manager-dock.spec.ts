import { expect, test, type Locator, type Page } from "@playwright/test";
import { loginAsManager } from "./helpers";

test.use({ launchOptions: process.platform === "win32" ? { ignoreDefaultArgs: ["--disable-accelerated-compositing"] } : {} });
const summaryPath = "**/api/manager-home/nav-badges";
async function summary(page: Page, initial = { totalPending: 100, graphPending: 99, urlopPending: 1 }) {
  let counts = initial;
  await page.route(summaryPath, route => route.fulfill({ json: counts }));
  return (next: typeof counts) => { counts = next; };
}
async function transform(anchor: Locator) {
  return anchor.locator("[data-manager-dock-face]").evaluate(el => {
    const matrix = new DOMMatrixReadOnly(getComputedStyle(el).transform);
    return { scale: Math.hypot(matrix.a, matrix.b), x: matrix.m41, y: matrix.m42 };
  });
}

test("restored sidebar retains badges, gloss and proximity motion after reopening", async ({ page, isMobile }) => {
  await summary(page); await loginAsManager(page); await page.setViewportSize({ width: 1280, height: 900 });
  const rail = page.locator("#manager-sidebar-navigation");
  for (const [name, count] of [["Home", "99+"], ["Container", "99"], ["Urlop", "1"]]) {
    await expect(rail.getByRole("link", { name, exact: true }).locator("[data-manager-dock-badge]")).toHaveText(count);
  }
  for (const name of ["Employee", "Shop", "Availability", "Message", "Settings"]) {
    await expect(rail.getByRole("link", { name, exact: true }).locator("[data-manager-dock-badge]")).toHaveCount(0);
  }
  await expect(page.getByText(/New approval requests:/)).toHaveCount(0);
  const focal = rail.getByRole("link", { name: "Shop", exact: true });
  const above = rail.getByRole("link", { name: "Employee", exact: true });
  const below = rail.getByRole("link", { name: "Availability", exact: true });
  for (const reopened of [false, true]) {
    if (reopened) {
      await page.getByRole("button", { name: "Collapse sidebar", exact: true }).first().click();
      await expect(rail).toBeHidden();
      await page.getByRole("button", { name: "Open sidebar", exact: true }).click();
    }
    await expect(rail).toHaveCSS("width", "86px");
    await expect(rail).toHaveCSS("transform", "matrix(1, 0, 0, 1, 0, 0)");
    await expect(focal).toHaveCSS("height", "56px");
    const hitbox = await focal.boundingBox();
    const finish = focal.locator('[class*="dockFaceInner"]');
    expect(await finish.evaluate(el => getComputedStyle(el).boxShadow)).not.toBe("none");
    expect(await finish.evaluate(el => getComputedStyle(el).backgroundImage)).toContain("gradient");
    expect(await finish.evaluate(el => getComputedStyle(el).color)).toBe("rgba(0, 0, 0, 0.85)");
    const label = focal.locator('..').locator('[class*="navLabel"]');
    await expect(label).toBeVisible();
    if (!isMobile) {
      await focal.hover();
      await expect.poll(async () => (await transform(focal)).scale).toBeGreaterThan(1.18);
      expect((await transform(focal)).scale).toBeLessThan(1.22);
      for (const neighbor of [above, below]) {
        expect((await transform(neighbor)).scale).toBeGreaterThan(1);
        expect((await transform(neighbor)).scale).toBeLessThan((await transform(focal)).scale);
      }
      expect((await transform(above)).y).toBeLessThan(-1);
      expect((await transform(below)).y).toBeGreaterThan(1);
      expect(await focal.boundingBox()).toEqual(hitbox);
      const face = (await focal.locator("[data-manager-dock-face]").boundingBox())!;
      expect(face.y + face.height).toBeLessThan((await label.boundingBox())!.y);
      const tooltip = page.getByRole("tooltip"); await expect(tooltip).toHaveText("Shop");
      const railBox = (await rail.boundingBox())!;
      expect((await tooltip.boundingBox())!.x).toBeGreaterThan(railBox.x + railBox.width);
      expect(await tooltip.evaluate(el => getComputedStyle(el, "::before").content)).not.toBe("none");
      for (const badge of await rail.locator("[data-manager-dock-badge]").all()) {
        const box = (await badge.boundingBox())!;
        expect(box.x).toBeGreaterThan(railBox.x);
        expect(box.x + box.width).toBeLessThan(railBox.x + railBox.width);
      }
      await page.screenshot({ path: `../tmp/sidebar-restored-${reopened ? "reopened" : "expanded"}.png` });
      await page.mouse.move(300, 300);
      await expect.poll(async () => (await transform(focal)).scale).toBe(1);
      await expect(page.getByRole("tooltip")).toHaveCount(0);
    }
  }
  await focal.focus(); await expect(page.getByRole("tooltip")).toHaveText("Shop");
  await page.keyboard.press("Enter"); await expect(page).toHaveURL(/\/shop$/);
  await expect(focal).toHaveAttribute("aria-current", "page");
  await expect(rail.getByRole("link", { name: "Home", exact: true })).not.toHaveAttribute("aria-current");
  if (!isMobile) {
    await page.setViewportSize({ width: 1280, height: 480 });
    const scroller = rail.locator('[class*="navScroll"]');
    for (const [name, scroll] of [["Home", 0], ["Settings", 9999]] as const) {
      await scroller.evaluate((el, y) => { el.scrollTop = y; }, scroll);
      const edge = rail.getByRole("link", { name, exact: true }); await edge.hover();
      await expect.poll(async () => (await transform(edge)).scale).toBeGreaterThan(1.18);
      const face = (await edge.locator("[data-manager-dock-face]").boundingBox())!;
      const clip = (await scroller.boundingBox())!;
      expect(face.y).toBeGreaterThanOrEqual(clip.y);
      expect(face.y + face.height).toBeLessThanOrEqual(clip.y + clip.height);
    }
  }
});
test("reduced motion and scrolling boundaries keep faces within the rail", async ({ page }) => {
  await summary(page); await loginAsManager(page);
  await page.emulateMedia({ reducedMotion: "reduce" });
  await page.setViewportSize({ width: 390, height: 720 });
  const rail = page.locator("#manager-sidebar-navigation");
  const home = rail.getByRole("link", { name: "Home", exact: true });
  const glow = await rail.evaluate(el => (el as HTMLElement).style.getPropertyValue("--dock-glow-y"));
  await home.hover(); await page.waitForTimeout(400);
  expect(await transform(home)).toEqual({ scale: 1, x: 0, y: 0 });
  expect(await rail.evaluate(el => (el as HTMLElement).style.getPropertyValue("--dock-glow-y"))).toBe(glow);
  expect(await home.locator('[class*="dockFaceInner"]').evaluate(el => getComputedStyle(el, "::after").display)).toBe("none");
  await page.getByRole("button", { name: "Collapse sidebar", exact: true }).first().click();
  await expect(rail).toBeHidden();
  await page.getByRole("button", { name: "Open sidebar", exact: true }).click();
  await expect(rail).toBeVisible();
  await expect(rail).toHaveCSS("transition-duration", "0s");
  await home.hover();
  expect(await transform(home)).toEqual({ scale: 1, x: 0, y: 0 });
  expect(await home.locator('[class*="dockFaceInner"]').evaluate(el => getComputedStyle(el, "::after").display)).toBe("none");
  const scroller = rail.locator('[class*="navScroll"]');
  for (const end of [0, 9999]) {
    await scroller.evaluate((el, y) => { el.scrollTop = y; }, end);
    await expect(rail.getByRole("link", { name: "Open manager profile" })).toBeVisible();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  }
  await rail.getByRole("link", { name: "Urlop", exact: true }).click(); await expect(page).toHaveURL(/\/urlop$/);
});

test("focus lifecycle refreshes a single summary and replaces category delta pills without remounting", async ({ page }) => {
  await page.clock.install();
  const update = await summary(page, { totalPending: 3, graphPending: 2, urlopPending: 1 });
  await loginAsManager(page);
  const rail = page.locator("#manager-sidebar-navigation");
  await expect(rail.getByRole("link", { name: "Home", exact: true }).locator("[data-manager-dock-badge]")).toHaveText("3");
  await expect(page.getByText(/New approval requests:/)).toHaveCount(0);
  await page.clock.pauseAt((await page.evaluate(() => Date.now())) + 1000);
  const refresh = async (next: { totalPending: number; graphPending: number; urlopPending: number }) => {
    await page.clock.runFor(31_000);
    update(next);
    await page.evaluate(() => window.dispatchEvent(new Event("focus")));
  };
  await refresh({ totalPending: 5, graphPending: 4, urlopPending: 1 });
  let pill = page.getByText("New approval requests: 2", { exact: true });
  await expect(pill).toHaveAttribute("data-anchor", "/container");
  await expect(page.locator('[aria-live="polite"]').filter({ hasText: /New approval requests/ })).toHaveCount(1);
  await page.clock.runFor(3500); await expect(pill).toHaveCount(0);
  await refresh({ totalPending: 6, graphPending: 4, urlopPending: 2 });
  pill = page.getByText("New approval requests: 1", { exact: true }); await expect(pill).toHaveAttribute("data-anchor", "/urlop");
  await page.clock.runFor(3500);
  await refresh({ totalPending: 2, graphPending: 1, urlopPending: 1 });
  await expect(rail.getByRole("link", { name: "Home", exact: true }).locator("[data-manager-dock-badge]")).toHaveText("2");
  await expect(page.getByText(/New approval requests:/)).toHaveCount(0);
});
