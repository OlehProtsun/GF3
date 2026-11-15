import { expect, test } from '@playwright/test';

const now = Date.parse('2026-09-19T12:00:00Z');
const labels = {
  en: { unread: '3 unread', newsUnread: '2 unread', markAll: 'Mark all read', read: '2 read', inboxTab: 'Inbox', newsTab: 'News' },
  pl: { unread: 'Nieprzeczytane: 3', newsUnread: 'Nieprzeczytane: 2', markAll: 'Odczytaj wszystkie', read: 'Przeczytane: 2', inboxTab: 'Wiadomości', newsTab: 'Aktualności' },
  uk: { unread: 'Непрочитаних: 3', newsUnread: 'Непрочитаних: 2', markAll: 'Позначити всі прочитаними', read: 'Прочитаних: 2', inboxTab: 'Вхідні', newsTab: 'Новини' },
};
for (const language of ['en', 'pl', 'uk'] as const) {
  test('employee notification lifecycle in ' + language, async ({ page }) => {
    await page.clock.install({ time: new Date(now) });
    await page.addInitScript(() => localStorage.setItem('gf3.auth.access-token', 'notification-test'));
    const readIds = new Set<string>();
    let newsItems = [1, 2].map(id => ({ id, title: `Announcement ${id}`, body: 'News content', audience: 'all', imageUrl: null, videoUrl: null, createdAtUtc: new Date(now).toISOString(), isRead: false }));
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
      else if (path === '/api/system-news') body = newsItems;
      else if (path === '/api/system-news/read-all') {
        newsItems = newsItems.map(item => ({ ...item, isRead: true }));
        return route.fulfill({ status: 204 });
      }
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
    const assertHeaderRows = async (statusBadge: typeof badge, markReadButton: typeof markAll) => {
      const header = statusBadge.locator('xpath=ancestor::*[@data-employee-motion][1]');
      const [icon, tabs, count, action] = await Promise.all([
        header.locator('[class*="sectionIcon"]').boundingBox(),
        header.getByRole('tablist').boundingBox(),
        statusBadge.boundingBox(),
        markReadButton.boundingBox(),
      ]);
      expect(icon && tabs && count && action).toBeTruthy();
      expect(Math.abs(icon!.y + icon!.height / 2 - (tabs!.y + tabs!.height / 2))).toBeLessThan(5);
      expect(tabs!.x).toBeGreaterThanOrEqual(icon!.x + icon!.width - 1);
      expect(count!.y).toBeGreaterThan(tabs!.y + tabs!.height - 1);
      expect(Math.abs(count!.y + count!.height / 2 - (action!.y + action!.height / 2))).toBeLessThan(2);
      expect(count!.x + count!.width).toBeLessThan(action!.x);
    };
    for (const width of [320, 375, 430, 768]) {
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
      if (width <= 430) await assertHeaderRows(badge, markAll);
      if (width === 320) {
        await badge.locator('xpath=ancestor::*[@data-employee-motion][1]').screenshot({ path: test.info().outputPath(`alerts-${language}-${width}.png`) });
      }
    }
    await page.setViewportSize({ width: 320, height: 812 });
    await page.getByRole('tab', { name: labels[language].newsTab }).click();
    const newsBadge = page.getByRole('status', { name: labels[language].newsUnread });
    const markNewsAll = page.getByRole('button', { name: labels[language].markAll });
    await expect(newsBadge).toHaveText('2');
    await assertHeaderRows(newsBadge, markNewsAll);
    await markNewsAll.click();
    await expect(page.getByRole('status', { name: labels[language].read })).toHaveText('2');
    await expect(page.getByRole('button', { name: labels[language].markAll })).toHaveCount(0);
    await page.getByRole('tab', { name: labels[language].inboxTab }).click();
    await expect(page.getByRole('status', { name: labels[language].unread })).toHaveText('3');
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
    await expect(page.getByRole('button', { name: labels[language].markAll })).toHaveCount(0);
    expect(errors).toEqual([]);
  });
}
