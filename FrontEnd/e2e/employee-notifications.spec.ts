import { expect, test } from '@playwright/test';

const now = Date.parse('2026-09-19T12:00:00Z');
for (const language of ['en', 'pl']) {
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
    const recent = page.locator('article').filter({hasText: 'RECENT-SCHEDULE'});
    const expiring = page.locator('article').filter({hasText: 'EXPIRING-SCHEDULE'});
    await expect(recent).toBeVisible();
    await expect(expiring).toBeVisible();
    await expect(page.locator('article').filter({hasText: 'EXPIRED-SCHEDULE'})).toHaveCount(0);
    await recent.locator('button').click();
    await expect(recent.locator('button')).toBeDisabled();
    await expect.poll(() => readIds.size).toBe(1);
    await page.reload();
    await expect(recent.locator('button')).toBeDisabled();
    await page.clock.fastForward(90_000);
    await expect(expiring).toHaveCount(0);
    await expect(recent).toBeVisible();
    expect(errors).toEqual([]);
  });
}
