import { expect, test, type Locator } from "@playwright/test";

async function expectWithinViewport(locator: Locator, width: number, height: number) {
  await expect(locator).toBeVisible();
  const box = await locator.boundingBox();
  expect(box).not.toBeNull();
  expect(box!.x).toBeGreaterThanOrEqual(0);
  expect(box!.y).toBeGreaterThanOrEqual(0);
  expect(box!.x + box!.width).toBeLessThanOrEqual(width + 1);
  expect(box!.y + box!.height).toBeLessThanOrEqual(height + 1);
}

for (const size of [{ width: 1280, height: 720 }, { width: 1440, height: 800 }, { width: 1024, height: 600 }, { width: 390, height: 664 }]) {
  test(`dialogs and dropdowns fit ${size.width}x${size.height}`, async ({ page }) => {
    await page.setViewportSize(size);
    const errors: string[] = [];
    page.on("pageerror", error => errors.push(error.message));
    for (const name of ["confirm", "group", "transfer", "colors", "preset", "related", "highlight", "columns"]) {
      await page.goto(`/e2e/fixtures/viewport.html?case=${name}`);
      const surface = page.locator('[class*="surface_"]').first();
      await expectWithinViewport(surface, size.width, size.height);
      // The final action must remain reachable, including after long validation messages.
      const action = surface.getByRole("button").last();
      await action.scrollIntoViewIfNeeded();
      await expectWithinViewport(action, size.width, size.height);
      await expect(surface).toHaveJSProperty("scrollWidth", await surface.evaluate(el => el.clientWidth));
      await page.screenshot({ path: test.info().outputPath(`${name}.png`) });
    }
    for (const name of ["select", "presets", "date"]) {
      await page.goto(`/e2e/fixtures/viewport.html?case=${name}`);
      await page.getByRole("button").first().click();
      const popup = page.locator('[class*="dropdownPortal_"], [class*="datePortal_"]');
      await expectWithinViewport(popup, size.width, size.height);
      await page.setViewportSize({ ...size, height: size.height - 100 });
      await expectWithinViewport(popup, size.width, size.height - 100);
      await page.setViewportSize(size);
    }
    expect(errors).toEqual([]);
  });
}
