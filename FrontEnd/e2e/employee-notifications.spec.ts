import { expect, test } from '@playwright/test';

const now = Date.parse('2026-09-19T12:00:00Z');
const labels = {
  en: { unread: '3 unread', markAll: 'Mark all read', read: '2 read' },
  pl: { unread: 'Nieprzeczytane: 3', markAll: 'Odczytaj wszystkie', read: 'Przeczytane: 2' },
  uk: { unread: 'Непрочитаних: 3', markAll: 'Позначити всі прочитаними', read: 'Прочитаних: 2' },
};
for (const language of ['en', 'pl', 'uk'] as const) {
  test('employee notification lifecycle in ' + language, async ({ page }) => {
    await page.clock.install({ time: new Date(now) });
    await page.addInitScript(() => localStorage.setItem('gf3.auth.access-token', 'notification-test'));
    const readIds = new Set<string>();
    const errors: string[] = [];
    page.on('pageerror', error => errors.push(error.message));
    await page.route('**/*', async route => {
      const path = new URL(route.request().url()).pathname;
      if (!path.startsWith('/api/')) return route.continue();
      let body: unknown = [];
      if (path === '/api/auth/session') body = { role: 'employee', employeeId: 1, userName: 'worker', displayName: 'Worker' };
      else if (path === '/api/account-language') body = { language };
      else if (path === '/api/employee-ui-state') body = { scheduleColumnOrders: {}, pinnedSwapIds: [], readNotificationIds: [...readIds] };
      else if (path === '/api/employee-shift-swaps') body = [{
        id: 31, scheduleId: 2, scheduleName: 'LINKED-SWAP', containerName: 'Container', shopName: 'Shop',
        year: 2026, month: 9, dayOfMonth: 19, fromTime: '08:00', toTime: '16:00', fromEmployeeName: 'Coworker',
        status: 'open', visibility: 'public', createdAtUtc: new Date(now).toISOString(), isCreatedByCurrentEmployee: false,
        isManagerCreated: true, manualColumnId: 1,
      }];
      else if (path === '/api/employee-ui-state/notifications/read') {
        route.request().postDataJSON().notificationIds.forEach((id: string) => readIds.add(id));
        return route.fulfill({status: 204});
      }
      else if (path === '/api/employee-schedules') body = [
        { id: 1, name: 'EXPIRED-SCHEDULE', publishedAtUtc: new Date(now - 6 * 86_400_000).toISOString() },
        { id: 2, name: 'RECENT-SCHEDULE', publishedAtUtc: new Date(now - 86_400_000).toISOString() },
        { id: 3, name: 'EXPIRING-SCHEDULE', publishedAtUtc: new Date(now - 5 * 86_400_000 + 60_000).toISOString() },
      ].map(item => ({ ...item, containerId: 1, shopId: 1, shopName: 'Shop', containerName: 'Container', year: 2026, month: 9, publicationStatus: 'public', employees: [], slots: [] }));
      else if (path.includes('negotiate')) return route.fulfill({status: 503});
      return route.fulfill({contentType: 'application/json', body: JSON.stringify(body)});
    });
    await page.goto('/');
    await expect(page.locator('a[href="/swap?swapId=31"]')).toBeVisible();
    const recent = page.locator('article').filter({hasText: 'RECENT-SCHEDULE'});
    const expiring = page.locator('article').filter({hasText: 'EXPIRING-SCHEDULE'});
    await expect(recent).toBeVisible();
    await expect(expiring).toBeVisible();
    await expect(page.locator('article').filter({hasText: 'EXPIRED-SCHEDULE'})).toHaveCount(0);
    const badge = page.getByRole('status', { name: labels[language].unread });
    const markAll = page.getByRole('button', { name: labels[language].markAll });
    await expect(badge).toHaveText('3');
    for (const width of [320, 360, 390, 768]) {
      await page.setViewportSize({ width, height: 812 });
      const visibleWithinWidth = async (locator: typeof badge) => locator.evaluate(element => {
        const rect = element.getBoundingClientRect();
        return rect.left >= -1 && rect.right <= innerWidth + 1;
      });
      expect(await visibleWithinWidth(badge)).toBe(true);
      expect(await visibleWithinWidth(markAll)).toBe(true);
      expect(await badge.evaluate(element => {
        const header = element.closest('[data-employee-motion]')!;
        const panel = element.closest('section')!;
        return [header, panel].every(node => node.scrollWidth <= node.clientWidth + 1);
      })).toBe(true);
      if (width === 320) {
        await badge.locator('xpath=ancestor::*[@data-employee-motion][1]').screenshot({ path: test.info().outputPath(`alerts-${language}-${width}.png`) });
      }
    }
    const synchronized = page.waitForResponse(async response =>
      new URL(response.url()).pathname === '/api/employee-ui-state'
      && response.request().method() === 'GET'
      && (await response.json()).readNotificationIds.length === 1);
    await recent.locator('button').click();
    await expect(recent.locator('button')).toBeDisabled();
    await expect.poll(() => readIds.size).toBe(1);
    await (await synchronized).finished();
    await page.reload();
    await expect(recent.locator('button')).toBeDisabled();
    await page.clock.runFor(90_000);
    await expect(expiring).toHaveCount(0);
    await expect(recent).toBeVisible();
    await markAll.click();
    await expect(page.getByRole('status', { name: labels[language].read })).toHaveText('2');
    expect(errors).toEqual([]);
  });
}
