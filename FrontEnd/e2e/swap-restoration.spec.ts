import { expect, test } from '@playwright/test';

for (const language of ['en', 'pl']) {
  test(`restored swap renders and opens panels in ${language}`, async ({ page }) => {
    const errors: string[] = [];
    page.on('pageerror', error => errors.push(error.message));
    await page.addInitScript(() => localStorage.setItem('gf3.auth.access-token', 'swap-test'));
    await page.route('**/*', async route => {
      const path = new URL(route.request().url()).pathname;
      if (!path.startsWith('/api/')) return route.continue();
      let body: unknown = [];
      if (path === '/api/auth/session') body = { role: 'employee', employeeId: 1, managerId: null, userName: 'worker', displayName: 'Worker' };
      else if (path === '/api/account-language') body = { language };
      else if (path === '/api/employee-shift-swaps') body = Array.from({ length: 8 }, (_, index) => ({
        id: index + 1, scheduleId: 10, scheduleSlotId: 100, scheduleName: `Offer ${index + 1}`,
        containerName: 'Container', shopName: 'Shop', year: 2026, month: 9, dayOfMonth: index + 1,
        fromTime: '08:00', toTime: '16:00', fromEmployeeId: 2, fromEmployeeName: 'Test Worker',
        visibility: 'public', status: 'open', createdAtUtc: '2026-09-01T00:00:00Z', shiftHours: 8,
        canAccept: true, canCancel: false, isCreatedByCurrentEmployee: false,
      }));
      else if (path === '/api/employee-schedules') body = [{
        id: 10, containerId: 2, containerName: 'Container', shopId: 4, shopName: 'Shop',
        name: 'Test schedule', year: 2026, month: 9, publicationStatus: 'public', allowSwap: true,
        employees: [{ id: 1, employeeId: 1, firstName: 'Test', lastName: 'Worker', displayName: 'Test Worker', minHoursMonth: 0, displayOrder: 1 }],
        slots: [{ id: 100, dayOfMonth: 1, slotNo: 1, employeeId: 1, fromTime: '08:00', toTime: '16:00', status: 'ASSIGNED' }],
      }];
      else if (path.includes('negotiate')) return route.fulfill({ status: 503 });
      await route.fulfill({ contentType: 'application/json', body: JSON.stringify(body) });
    });
    await page.goto('/swap');
    await page.getByRole('button', { name: language === 'en' ? 'Create offer' : 'Utwórz ofertę', exact: true }).click();
    await expect(page.locator('#create-swap-panel')).toBeVisible();
    const dialog = page.getByRole('dialog');
    await expect(dialog).toBeVisible();
    expect(await dialog.evaluate(element => element.matches(':modal'))).toBeTruthy();
    expect(await dialog.evaluate(element => getComputedStyle(element, '::backdrop').backdropFilter)).toBe('blur(10px)');
    if (language === 'en') {
      await dialog.getByRole('button', { name: /Test schedule/ }).click();
      const picker = page.getByRole('dialog', { name: 'Choose shift', exact: true });
      await expect(picker).toBeVisible();
      expect(await picker.evaluate(element => element.matches(':modal'))).toBeTruthy();
      await page.keyboard.press('Escape');
      await expect(picker).not.toBeVisible();
      await expect(dialog).toBeVisible();
      await dialog.getByRole('button', { name: /Test schedule/ }).click();
      await picker.getByRole('button', { name: /08:00.*16:00/ }).click();
      await picker.getByRole('button', { name: 'Choose shift', exact: true }).click();
      await expect(picker).not.toBeVisible();
      await expect(dialog.getByText('Selected shift', { exact: true })).toBeVisible();
      await expect(dialog.getByRole('button', { name: 'Offer shift', exact: true })).toBeEnabled();
    }
    await page.keyboard.press('Escape');
    await expect(dialog).not.toBeVisible();
    await expect(page.getByRole('button', { name: language === 'en' ? 'Create offer' : 'Utwórz ofertę', exact: true })).toBeFocused();
    await page.locator('button[aria-controls="swap-history-panel"]').click();
    await expect(page.locator('#swap-history-panel')).toBeVisible();
    expect(await page.getByRole('dialog').evaluate(element => element.matches(':modal'))).toBeTruthy();
    await page.keyboard.press('Escape');
    await expect(page.locator('#swap-history-panel')).toHaveCount(0);
    for (const size of [{ width: 408, height: 873 }, { width: 320, height: 568 }, { width: 390, height: 844 }, { width: 844, height: 390 }]) {
      await page.setViewportSize(size);
      const stage = page.locator('[class*="swapStage"]').first();
      await expect.poll(async () => (await stage.boundingBox())?.width ?? 0).toBeGreaterThanOrEqual(size.width - 12);
      if (language === 'en' && size.width === 408) {
        const expandedHeight = (await stage.boundingBox())!.height;
        await page.getByRole('button', { name: 'Collapse navigation', exact: true }).click();
        await expect.poll(async () => (await stage.boundingBox())?.height ?? 0).toBeGreaterThan(expandedHeight + 60);
        await expect(page.locator('button[aria-controls="swap-history-panel"]')).toBeInViewport({ ratio: 1 });
        await page.getByRole('button', { name: 'Open navigation', exact: true }).click();
      }
      const history = page.locator('button[aria-controls="swap-history-panel"]');
      const scrollArea = page.locator('main[class*="swapBody"]');
      await scrollArea.evaluate(element => { element.scrollTop = element.scrollHeight; });
      const lastOffer = scrollArea.locator('article').last();
      await expect(lastOffer).toBeVisible();
      await expect.poll(async () => {
        const area = (await scrollArea.boundingBox())!;
        const card = (await lastOffer.boundingBox())!;
        return area.y + area.height - card.y - card.height;
      }).toBeGreaterThanOrEqual(200);
      await expect(history).toBeInViewport({ ratio: 1 });
      await expect.poll(() => page.evaluate(() => document.documentElement.scrollHeight <= window.innerHeight)).toBeTruthy();
      await expect.poll(() => page.locator('svg[viewBox]').filter({ has: page.locator('path') }).evaluateAll(elements => {
        const backdrop = elements.find(element => element.getAttribute('viewBox')?.startsWith('0 0 ') && element.classList.toString().includes('swapBackdrop')) as SVGSVGElement;
        return backdrop ? Math.abs(backdrop.viewBox.baseVal.height - backdrop.getBoundingClientRect().height) < 1 : false;
      })).toBeTruthy();
      if (size.width === 408) await page.screenshot({ path: test.info().outputPath('swap-408.png'), fullPage: true });
    }
    await page.locator('main[class*="swapBody"]').evaluate(element => { element.scrollTop = 0; });
    await expect(page.getByRole('button', { name: language === 'en' ? 'My offers' : 'Moje oferty', exact: true })).toBeVisible();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBeTruthy();
    expect(errors).toEqual([]);
  });
}
