import { expect, type Locator, type Page } from "@playwright/test";

// Emulate keyboard geometry without resizing the layout viewport.
export async function setVisualViewport(page: Page, bounds: { height: number; top: number } | null) {
  await page.evaluate(bounds => {
    const viewport = window.visualViewport!;
    if (bounds) {
      Object.defineProperties(viewport, {
        height: { configurable: true, value: bounds.height },
        offsetTop: { configurable: true, value: bounds.top },
      });
    } else {
      Reflect.deleteProperty(viewport, "height");
      Reflect.deleteProperty(viewport, "offsetTop");
    }
    viewport.dispatchEvent(new Event("resize"));
    viewport.dispatchEvent(new Event("scroll"));
  }, bounds);
}

export async function expectVisibleField(field: Locator, scroller: Locator) {
  await expect(field).toBeFocused();
  await expect.poll(async () => {
    const scrollRect = (await scroller.boundingBox())!;
    return field.evaluate((element, bounds) => {
      const rect = element.getBoundingClientRect();
      const viewport = window.visualViewport!;
      return rect.height > 0 && rect.top >= Math.max(bounds.y, viewport.offsetTop) - 1 &&
        rect.bottom <= Math.min(bounds.y + bounds.height, viewport.offsetTop + viewport.height) + 1;
    }, scrollRect);
  }).toBe(true);
  expect(await field.evaluate(element => parseFloat(getComputedStyle(element).fontSize))).toBeGreaterThanOrEqual(16);
}
