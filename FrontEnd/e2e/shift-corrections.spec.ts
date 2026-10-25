import { expect, test, type Page } from "@playwright/test";

async function correctionFixture(page: Page, language: string) {
  await page.addInitScript(() => localStorage.setItem("gf3.auth.access-token", "correction-ui-test"));
  const graph = { id: 3, containerId: 1, shopId: 4, name: "Schedule", note: "", year: 2026, month: 10,
    publicationStatus: "public", peoplePerShift: 1, shift1Time: "15:00 - 22:00", shift2Time: "08:00 - 14:00",
    maxHoursPerEmpMonth: 160, maxConsecutiveDays: 5, maxConsecutiveFull: 3, maxFullPerMonth: 10 };
  const requests = Array.from({ length: 25 }, (_, index) => ({
    id: index + 1, containerId: 1, scheduleId: 3, scheduleSlotId: 1,
    employeeId: 2, employeeName: index === 0 ? "Oleh Protsun" : index === 1 ? "Oleh Protsun With A Very Long Surname" : `Employee ${index}`,
    scheduleName: index === 1 ? "Long schedule name with multiple words" : "F35", shopName: "Central",
    year: 2026, month: 10, dayOfMonth: 21, originalFromTime: "15:00", originalToTime: "22:00",
    requestedFromTime: "14:00", requestedToTime: "22:00", createdAtUtc: "2026-10-21T12:26:00Z",
    status: index < 2 ? "pending" : index % 2 ? "approved" : "rejected", reviewedByManagerName: "Ada Manager",
  }));
  await page.route("**/api/**", async route => {
    const path = new URL(route.request().url()).pathname;
    if (!path.startsWith("/api/")) return route.continue();
    let body: unknown = [];
    if (path === "/api/auth/session") body = { role: "manager", workspaceMode: "pc", managerId: 1, displayName: "Manager", isSystemManager: true };
    else if (path === "/api/account-language") body = { language };
    else if (path === "/api/containers/1") body = { id: 1, name: "Container" };
    else if (path === "/api/containers/1/graphs") body = [graph];
    else if (path === "/api/containers/1/graphs/3") body = graph;
    else if (path === "/api/containers/1/graphs/3/employees") body = [{ id: 1, scheduleId: 3, employeeId: 2, displayOrder: 0 }];
    else if (path === "/api/containers/1/graphs/3/slots") body = [{ id: 1, scheduleId: 3, employeeId: 2, dayOfMonth: 21, slotNo: 1, fromTime: "15:00", toTime: "22:00", status: "ASSIGNED" }];
    else if (path === "/api/containers/1/graphs/3/shift-corrections") body = requests;
    else if (path === "/api/manager-shift-correction-settings") body = { highlightColor: "#FDE68A" };
    else if (path === "/api/employees") body = [{ id: 2, firstName: "Oleh", lastName: "Protsun" }];
    else if (path === "/api/shops") body = [{ id: 4, name: "Central" }];
    else if (path.includes("notepad")) body = { notes: [], state: { isExpanded: false, isPinned: false, height: 700 } };
    else if (path.includes("negotiate")) return route.fulfill({ status: 503 });
    await route.fulfill({ json: body });
  });
}

for (const language of ["en", "pl", "uk"] as const) {
  test(`compact correction history and details in ${language}`, async ({ page, isMobile }, info) => {
    test.setTimeout(60_000);
    await page.setViewportSize({ width: 1440, height: 1000 });
    await correctionFixture(page, language);
    await page.goto("/container/1/graphs/3/edit");
    const list = page.getByRole("list", { name: language === "en" ? "Shift correction requests" : language === "pl" ? "Wnioski o korektę zmiany" : "Запити на коригування змін" });
    const expand = page.getByRole("button", { name: /^(Expand Shift Corrections|Rozwiń Korekty zmian|Розгорнути Коригування змін)$/ });
    await expect(expand).toBeVisible({ timeout: 20_000 });
    const collapseNavigation = page.getByRole("button", { name: language === "en" ? "Collapse sidebar" : language === "pl" ? "Zwiń pasek boczny" : "Згорнути бічну панель", exact: true });
    if (await collapseNavigation.isVisible()) {
      await collapseNavigation.focus();
      await page.keyboard.press("Enter");
    }
    await page.mouse.move(1300, 100);
    // Keyboard activation avoids the app navigation's hover-to-open overlay.
    await expand.focus();
    await page.keyboard.press("Enter");
    await expect(list).toBeVisible();
    const shell = list.locator("..");
    for (const width of [320, 240]) {
      await shell.evaluate((element, value) => { (element as HTMLElement).style.width = `${value}px`; (element as HTMLElement).style.boxSizing = "border-box"; }, width);
      await list.getByRole("listitem").first().focus();
      await page.keyboard.press("Enter");
      const article = list.locator("article");
      await expect(article).toBeVisible();
      await expect(article.locator("svg")).toHaveCount(10);
      for (const icon of await article.locator("svg").all()) {
        await expect(icon).toBeVisible();
        const size = await icon.boundingBox();
        expect(size!.width).toBeGreaterThanOrEqual(12);
        expect(size!.height).toBeGreaterThanOrEqual(12);
      }
      await expect(article.locator("dt").first()).toContainText(language === "en" ? "Date" : language === "pl" ? "Data" : "Дата");
      const buttons = article.locator('button').filter({ has: page.locator('span[title]') });
      await expect(buttons).toHaveCount(2);
      for (const button of await buttons.all()) {
        expect(await button.evaluate(element => element.getBoundingClientRect().height)).toBe(42);
        expect(await button.evaluate(element => element.scrollHeight <= element.clientHeight + 1)).toBe(true);
      }
      const sizes = await buttons.evaluateAll(elements => elements.map(element => element.getBoundingClientRect().width));
      expect(Math.abs(sizes[0] - sizes[1])).toBeLessThan(1);
      expect(await article.evaluate(element => element.scrollWidth <= element.clientWidth + 1)).toBe(true);
      await article.screenshot({ path: info.outputPath(`pending-${width}.png`) });
      await article.getByRole("button").first().focus();
      await page.keyboard.press("Enter");
    }
    await list.getByRole("listitem").nth(1).focus();
    await page.keyboard.press("Enter");
    expect(await list.locator("article").evaluate(element => element.scrollWidth <= element.clientWidth + 1)).toBe(true);
    await list.locator("article").screenshot({ path: info.outputPath("long-names-240.png") });
    await list.locator("article button").first().focus();
    await page.keyboard.press("Enter");
    await shell.evaluate(element => { (element as HTMLElement).style.width = ""; });
    await page.setViewportSize({ width: 375, height: 812 });
    for (const index of [2, 3]) {
      await list.getByRole("listitem").nth(index).focus();
      await page.keyboard.press("Enter");
      await expect(list.locator("article em")).toBeVisible();
      await expect(list.locator("article svg")).toHaveCount(9);
      await expect(list.locator("article p svg")).toBeVisible();
      await expect(list.locator("article p")).toContainText("Ada Manager");
      expect(await list.locator("article button").count()).toBe(1);
      await list.locator("article").screenshot({ path: info.outputPath(`resolved-${index}.png`) });
      await list.locator("article button").focus();
      await page.keyboard.press("Enter");
    }
    expect(await list.evaluate(element => getComputedStyle(element).scrollbarWidth)).toBe("none");
    expect(await list.evaluate(element => getComputedStyle(element).touchAction)).toBe("auto");
    expect(await list.evaluate(element => element.scrollWidth <= element.clientWidth)).toBe(true);
    await list.focus();
    await expect(list).toBeFocused();
    expect(await list.evaluate(element => getComputedStyle(element).outlineStyle)).toBe("solid");
    await page.keyboard.press("End");
    await expect.poll(() => list.evaluate(element => element.scrollTop + element.clientHeight >= element.scrollHeight - 1)).toBe(true);
    const bottomSpace = await list.evaluate(element => element.getBoundingClientRect().bottom - element.lastElementChild!.getBoundingClientRect().bottom);
    expect(bottomSpace).toBeGreaterThanOrEqual(18);
    await shell.screenshot({ path: info.outputPath("history-bottom.png") });
    await page.keyboard.press("Home");
    await expect.poll(() => list.evaluate(element => element.scrollTop)).toBe(0);
    if (!isMobile) {
      await list.hover();
      await page.mouse.wheel(0, 180);
      await expect.poll(() => list.evaluate(element => element.scrollTop)).toBeGreaterThan(0);
    }
  });
}
