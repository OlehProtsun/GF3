import { test, expect, type Page } from "@playwright/test";
import { SCENES } from "../src/promo/storyboard";

function watch(page: Page) {
  const errors: string[] = [];
  const api: string[] = [];
  page.on("pageerror", error => errors.push(error.message));
  page.on("console", message => { if (message.type() === "error") errors.push(message.text()); });
  page.on("request", request => { const url = new URL(request.url()); if (url.pathname.startsWith("/api/") || !["localhost", "127.0.0.1", "[::1]"].includes(url.hostname)) api.push(request.url()); });
  return { errors, api };
}

async function capture(page: Page) {
  await page.setViewportSize({ width: 1920, height: 1080 });
  await page.goto("/promo.html?capture=1");
  await page.waitForFunction(() => window.__GF3_PROMO__?.ready === true);
  await page.evaluate(() => document.fonts.ready);
}

async function seek(page: Page, seconds: number) {
  await page.evaluate(async time => {
    window.__GF3_PROMO__!.seek(time);
    await new Promise<void>(resolve => requestAnimationFrame(() => requestAnimationFrame(() => resolve())));
  }, seconds);
}

test("standalone promo and original app entry remain independent", async ({ page, request }) => {
  const observed = watch(page);
  await page.goto("/promo.html");
  await expect(page).toHaveTitle(/GF3/);
  await expect(page.locator('[data-promo-scene="pain"]')).toBeVisible();
  await expect(page.locator('[data-promo-scene="pain"]')).toContainText("GF3");
  expect(await page.evaluate(() => window.__GF3_PROMO__)).toBeUndefined();
  const original = await request.get("/");
  expect(original.ok()).toBe(true);
  expect(await original.text()).toContain('id="root"');
  expect(await original.text()).not.toContain('id="promo-root"');
  expect(observed).toEqual({ errors: [], api: [] });
});

test("capture contract, scene seeks, boundaries and final hold", async ({ page }) => {
  const observed = watch(page);
  await capture(page);
  await expect(page.locator("[data-promo-controls]")).toHaveCount(0);
  const stage = page.locator("[data-promo-stage]");
  expect(await stage.boundingBox()).toMatchObject({ width: 1920, height: 1080 });
  expect(await page.evaluate(() => {
    const { duration, width, height, fps, ready } = window.__GF3_PROMO__!;
    return { duration, width, height, fps, ready };
  })).toEqual({ duration: 75, width: 1920, height: 1080, fps: 30, ready: true });
  const times = [0, 3, 9, 17, 28, 38, 49, 59, 66, 72, 74.9, 75];
  for (const time of times) {
    await seek(page, time);
    const spec = SCENES.find(scene => time < scene.end) ?? SCENES[8];
    const scene = page.locator(`[data-promo-scene="${spec.id}"]`);
    await expect(scene.locator("[data-promo-headline]")).toBeVisible();
    await expect(scene.locator("[data-promo-headline]")).toHaveText(spec.headline);
    expect(await scene.evaluate(node => Number(getComputedStyle(node).opacity))).toBeGreaterThan(.5);
    if (["manager", "availability", "schedule", "swap", "more"].includes(spec.id)) {
      await expect(scene.locator(`[data-promo-real-shot="${spec.id}"]`)).toBeVisible();
      expect(await scene.locator("img").evaluateAll(images => images.every(image => (image as HTMLImageElement).naturalWidth > 0))).toBe(true);
    }
    expect(await stage.screenshot()).not.toHaveLength(0);
  }
  for (const time of [6, 12, 23, 33, 44, 55, 63, 69]) {
    await seek(page, time);
    expect(await page.locator("[data-promo-scene]").evaluateAll(nodes =>
      nodes.some(node => getComputedStyle(node).visibility === "visible" && Number(getComputedStyle(node).opacity) >= .5),
    )).toBe(true);
  }
  await seek(page, 75);
  await expect(page.locator("[data-promo-cta]")).toBeVisible();
  expect(await page.evaluate(() => {
    const film = window.__GF3_PROMO__!;
    const invalid = [NaN, Infinity, -Infinity].map(value => { try { film.seek(value); return false; } catch (error) { return error instanceof TypeError; } });
    film.seek(-10); const low = film.getTime(); film.seek(100); return { invalid, low, high: film.getTime() };
  })).toEqual({ invalid: [true, true, true], low: 0, high: 75 });
  for (const feature of ["manager", "availability", "schedule", "swap", "more"]) {
    expect(await page.locator(`[data-promo-scene="${feature}"] img`).evaluateAll(images => images.every(image => (image as HTMLImageElement).complete && (image as HTMLImageElement).naturalWidth > 0))).toBe(true);
  }
  expect(observed).toEqual({ errors: [], api: [] });
});

test("capture pixels do not depend on seek order, also while offline", async ({ page, context }) => {
  await capture(page);
  await context.setOffline(true);
  const stage = page.locator("[data-promo-stage]");
  const frames = new Map<number, Buffer>();
  for (const time of [49, 3, 28, 49, 28]) {
    await seek(page, time);
    const frame = await stage.screenshot({ animations: "disabled" });
    if (frames.has(time)) expect(frame.equals(frames.get(time)!)).toBe(true);
    frames.set(time, frame);
  }
  for (const time of [28, 49]) {
    await seek(page, time);
    const original = await stage.screenshot({ animations: "disabled" });
    await seek(page, 3);
    await seek(page, 49);
    await seek(page, time);
    expect((await stage.screenshot({ animations: "disabled" })).equals(original)).toBe(true);
  }
  await seek(page, 66);
  for (const feature of ["manager", "availability", "schedule", "swap"]) {
    await expect(page.locator(`[data-promo-scene="connected"] [data-promo-real-shot="${feature}"]`)).toBeVisible();
  }
});

test("preview playback, pause, scrub and keyboard controls stay local", async ({ page }) => {
  const observed = watch(page);
  await page.goto("/promo.html");
  const play = page.getByRole("button", { name: "Odtwórz", exact: true });
  const slider = page.getByRole("slider", { name: "Pozycja prezentacji" });
  await play.click();
  await expect.poll(async () => Number(await slider.inputValue())).toBeGreaterThan(.2);
  await page.getByRole("button", { name: "Pauza" }).click();
  const paused = await slider.inputValue();
  await page.waitForTimeout(200);
  expect(await slider.inputValue()).toBe(paused);
  await slider.fill("49");
  await expect(page.locator('[data-promo-scene="swap"]')).toBeVisible();
  await slider.focus(); await page.keyboard.press("ArrowLeft");
  await expect(slider).toHaveValue("48");
  await page.keyboard.press("ArrowRight"); await expect(slider).toHaveValue("49");
  await page.keyboard.press("End"); await expect(slider).toHaveValue("75");
  await page.keyboard.press("Home"); await expect(slider).toHaveValue("0");
  await slider.evaluate(node => (node as HTMLElement).blur());
  await page.keyboard.press("Space"); await expect(page.getByRole("button", { name: "Pauza" })).toBeVisible();
  await page.keyboard.press("Space"); await expect(play).toBeVisible();
  expect(page.url()).toMatch(/\/promo\.html$/);
  expect(await page.evaluate(() => window.__GF3_PROMO__)).toBeUndefined();
  expect(observed).toEqual({ errors: [], api: [] });
});

test("narrow preview keeps the whole composition and accessible controls", async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto("/promo.html");
  const box = await page.locator("[data-promo-stage]").boundingBox();
  expect(box!.width).toBeCloseTo(390, 0);
  expect(box!.height).toBeCloseTo(390 * 1080 / 1920, 0);
  expect(box!.x).toBeGreaterThanOrEqual(0);
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
  await expect(page.getByRole("button", { name: "Odtwórz", exact: true })).toBeEnabled();
  await page.keyboard.press("Tab");
  await expect(page.getByRole("button", { name: "Odtwórz", exact: true })).toBeFocused();
  await page.keyboard.press("Tab");
  await expect(page.getByRole("slider")).toBeFocused();
});

test("reduced motion preview is static and capture still uses the full timeline", async ({ page }) => {
  await page.emulateMedia({ reducedMotion: "reduce" });
  await page.goto("/promo.html");
  await expect(page.getByRole("status").filter({ hasText: "Podgląd bez ruchu" })).toBeVisible();
  const slider = page.getByRole("slider");
  const initial = await slider.inputValue();
  await page.waitForTimeout(200);
  expect(await slider.inputValue()).toBe(initial);
  await page.getByRole("button", { name: "Następna scena" }).click();
  await expect(page.locator('[data-promo-scene="reveal"]')).toBeVisible();
  await slider.fill("49");
  await expect(page.locator('[data-promo-scene="swap"] [data-promo-real-shot="swap"]')).toBeVisible();
  await capture(page);
  await seek(page, 16);
  await expect(page.locator('[data-promo-scene="manager"]')).toBeVisible();
  expect(await page.evaluate(() => window.__GF3_PROMO__!.getTime())).toBe(16);
});
