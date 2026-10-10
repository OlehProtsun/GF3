import { expect, test } from "@playwright/test";

const pages = ['index', 'regulamin', 'polityka-prywatnosci', 'pliki-cookies', 'zasady-korzystania', 'podwykonawcy', 'bezpieczenstwo'];

test("all legal pages serve real anonymous HTML, cross-links and printable mobile text", async ({ page, request, isMobile }, testInfo) => {
  for (const name of pages) {
    const response = await page.goto(`/legal/${name}.html`);
    expect(response?.status()).toBe(200);
    expect(response?.headers()['content-type']).toContain('text/html');
    await expect(page.locator('html')).toHaveAttribute('lang', 'pl');
    await expect(page.locator('h1')).toBeVisible();
    expect(page.url()).toContain(`/legal/${name}.html`);
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
    for (const link of await page.locator('a[href^="/legal/"]').evaluateAll(links => [...new Set(links.map(link => link.getAttribute('href')!))])) {
      expect((await request.get(link)).status()).toBe(200);
    }
    await page.screenshot({ path: testInfo.outputPath(`${name}.png`), fullPage: true });
  }
  await page.emulateMedia({ media: 'print' });
  await expect(page.locator('h1')).toBeVisible();
  await expect(page.locator('header')).toBeHidden();
  await page.emulateMedia({ media: 'screen' });
  if (isMobile) await page.locator('.skip').focus();
  else await page.keyboard.press('Tab');
  await expect(page.locator('.skip')).toBeFocused();
  await page.keyboard.press('Enter');
  expect(page.url()).toContain('#tresc');
});

test("phone sign-in legal links are reachable in the card", async ({ page }) => {
  await page.addInitScript(() => {
    localStorage.clear();
    localStorage.setItem('gf3.auth.password-mode', 'phone');
  });
  await page.goto('/login');
  const links = page.getByRole('navigation', { name: 'Legal documents' });
  await expect(links).toBeVisible();
  for (const link of await links.getByRole('link').all()) {
    await link.scrollIntoViewIfNeeded();
    await expect(link).toBeInViewport();
  }
});

test("legal links can be reached on a short phone screen", async ({ page }) => {
  await page.setViewportSize({ width: 320, height: 568 });
  await page.addInitScript(() => {
    localStorage.clear();
    localStorage.setItem('gf3.auth.password-mode', 'phone');
  });
  await page.goto('/login');
  const nav = page.getByRole('navigation', { name: 'Legal documents' });
  await expect(nav).toBeVisible();
  for (const link of await nav.getByRole('link').all()) {
    await link.scrollIntoViewIfNeeded();
    await expect(link).toBeInViewport();
    await link.focus();
    await expect(link).toBeFocused();
  }
});

test("employee legal links survive collapsed mobile tabs", async ({ page, isMobile }) => {
  await page.addInitScript(() => localStorage.setItem('gf3.auth.access-token', 'synthetic-legal-ui'));
  await page.route('**/api/**', async route => {
    const path = new URL(route.request().url()).pathname;
    if (!path.startsWith('/api/')) return route.continue();
    let body: unknown = [];
    if (path === '/api/auth/session') body = { role: 'employee', employeeId: 1, userName: 'synthetic', displayName: 'Synthetic' };
    if (path === '/api/account-language') body = { language: 'en' };
    if (path === '/api/employee-ui-state') body = { readNotificationIds: [], scheduleColumnOrders: {}, pinnedSwapIds: [] };
    await route.fulfill({ contentType: 'application/json', body: JSON.stringify(body) });
  });
  await page.goto('/swap');
  if (isMobile) await page.getByRole('button', { name: 'Collapse navigation' }).click();
  const links = page.getByRole('navigation', { name: 'Legal documents' });
  await expect(links).toBeVisible();
  for (const link of await links.getByRole('link').all()) await expect(link).toBeInViewport();
});
