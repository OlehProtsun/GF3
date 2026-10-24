import { expect, test, type Page } from "@playwright/test";

test.use({ actionTimeout: 10_000, launchOptions: process.platform === "win32" ? { ignoreDefaultArgs: ["--disable-accelerated-compositing"] } : {} });

async function fixture(page: Page, language: string, availableShiftCount?: number) {
  page.on("pageerror", error => console.error(error.message));
  const employees = [1, 2].map(id => ({ id, firstName: "Worker", lastName: `Test ${id}`, hasLoginAccount: true, isOnline: false }));
  const group = { id: 1, name: "February", year: 2026, month: 2 };
  let graph = { id: 1, containerId: 1, shopId: 1, name: "Generated shifts", year: 2026, month: 2, publicationStatus: "public", allowSwap: true,
    peoplePerShift: 2, shift1Time: "08:00 - 12:00", shift2Time: "12:00 - 16:00", maxHoursPerEmpMonth: 160,
    maxConsecutiveDays: 5, maxConsecutiveFull: 3, maxFullPerMonth: 10, availabilityGroupId: 1, note: "Visible note" };
  if (availableShiftCount !== undefined) {
    const columns = Array.from({ length: Math.ceil(availableShiftCount / 28) }, (_, index) => [
      index + 1, index ? `Do obsadzenia ${index + 1}` : "Do obsadzenia",
      Object.fromEntries(Array.from({ length: Math.min(28, availableShiftCount - index * 28) }, (_, day) => [String(day + 1), "08:00 - 12:00"])),
    ]);
    graph.note = `Visible note\n\n[[GF3_GRAPH_META:${JSON.stringify({ m: columns, g: columns.map(column => column[0]) })}]]`;
  }
  const slots: unknown[] = [];
  const offers: Record<string, unknown>[] = [];
  const publications: unknown[] = [];
  const assignments = employees.map(employee => ({ id: employee.id, scheduleId: 1, employeeId: employee.id, displayOrder: employee.id - 1 }));
  await page.addInitScript(() => localStorage.setItem("gf3.auth.access-token", "fixture-token"));
  await page.route("**/api/**", async route => {
    const request = route.request(); const path = new URL(request.url()).pathname; const method = request.method();
    if (!path.startsWith("/api/")) return route.continue();
    const json = (body: unknown, status = 200) => route.fulfill({ status, contentType: "application/json", body: JSON.stringify(body) });
    if (path === "/api/auth/session") return json({ role: "manager", workspaceMode: "pc", managerId: 1, userName: "manager", displayName: "Manager", isSystemManager: true });
    if (path === "/api/account-language") return json({ language });
    if (path.includes("negotiate")) return route.fulfill({ status: 503 });
    if (path === "/api/containers/1/graphs/generate-preview") {
      const generated = Array.from({ length: 28 }, (_, i) => [0, 1].flatMap(shift => [1, 2].map(employeeId => ({
        id: i * 4 + shift * 2 + employeeId, scheduleId: 1, dayOfMonth: i + 1, slotNo: employeeId,
        employeeId: i === 0 && shift === 0 ? null : employeeId, status: i === 0 && shift === 0 ? 0 : 1,
        fromTime: shift ? "12:00" : "08:00", toTime: shift ? "16:00" : "12:00",
      })))).flat();
      return json({ slots: generated });
    }
    if (path === "/api/containers/1/graphs/1" && method === "PUT") { graph = { ...graph, ...request.postDataJSON() }; return route.fulfill({ status: 204 }); }
    if (path === "/api/containers/1/graphs/1/slots" && method === "POST") {
      const slot = { ...request.postDataJSON(), id: slots.length + 1, scheduleId: 1 }; slots.push(slot); return json(slot, 201);
    }
    if (path === "/api/containers/1/graphs/1/shift-swaps/manual" && method === "POST") {
      const input = request.postDataJSON(); publications.push(input);
      const offer = { ...input, id: offers.length + 1, scheduleId: 1, isManagerCreated: true, status: "open", visibility: input.targetEmployeeId ? "private" : "public", year: 2026, month: 2 };
      offers.push(offer); return json(offer, 201);
    }
    if (path === "/api/containers/1/graphs/1/shift-swaps") return json(offers);
    if (/\/shift-swaps\/\d+\/cancel$/.test(path)) {
      const id = Number(path.split("/").at(-2));
      const index = offers.findIndex(offer => offer.id === id);
      if (index >= 0) offers.splice(index, 1);
      return route.fulfill({ status: 204 });
    }
    if (path === "/api/containers/1/graphs/1/versions") return json({ versions: [], currentVersionId: null });
    if (path === "/api/containers/1/graphs/1") return json(graph);
    if (path === "/api/containers/1/graphs") return json([graph]);
    if (path === "/api/containers/1") return json({ id: 1, name: "Container" });
    if (path === "/api/employees") return json(employees);
    if (path === "/api/shops") return json([{ id: 1, name: "Shop" }]);
    if (path === "/api/availability-groups") return json([group]);
    if (path === "/api/availability-groups/1/members") return json(employees.map(employee => ({ id: employee.id, employeeId: employee.id, displayOrder: employee.id - 1 })));
    if (path === "/api/containers/1/graphs/1/employees") return json(assignments);
    if (path === "/api/containers/1/graphs/1/slots") return json(slots);
    if (path.endsWith("highlight-setting")) return json({ acceptedSwapHighlightColor: "#DBEAFE" });
    if (path.includes("notepad")) return json({ notes: [], state: { isExpanded: false, isPinned: false, height: 700 } });
    return json([]);
  });
  return { publications };
}

for (const count of [0, 1, 5, 30, 62]) {
  test(`manual shifts bounded list with ${count} shifts`, async ({ page, browserName }, testInfo) => {
    test.setTimeout(60_000);
    await fixture(page, "en", count);
    await page.goto("/container/1/graphs/1/edit");
    const expand = page.getByRole("button", { name: "Expand Manual Columns", exact: true });
    await expect(expand).toBeVisible({ timeout: 20_000 });
    await page.getByRole("button", { name: "Collapse sidebar", exact: true }).click();
    await expand.click();
    const card = page.locator("section").filter({ has: page.getByText(/^Manual shifts/) }).last();
    const list = card.getByRole("region", { name: "Available shifts", exact: true });
    await expect(card.getByRole("button", { name: /Show all|Show less|Load more/ })).toHaveCount(0);
    if (!count) {
      await expect(list).toHaveCount(0);
      await expect(card.getByRole("button", { name: "Publish shift", exact: true })).toBeDisabled();
      return;
    }
    await expect(list.getByRole("button")).toHaveCount(count);
    for (const width of [1280, 375, 320]) {
      await page.setViewportSize({ width, height: 740 });
      const metrics = await list.evaluate(element => ({ height: element.clientHeight, content: element.scrollHeight, overflow: getComputedStyle(element).overflowY }));
      expect(metrics.height).toBeLessThanOrEqual(350);
      expect(metrics.overflow).toBe("auto");
      if (count >= 30) {
        expect(metrics.content).toBeGreaterThan(metrics.height);
        await list.scrollIntoViewIfNeeded();
        if (browserName === "webkit" && testInfo.project.use.isMobile) {
          await list.evaluate(element => element.scrollBy({ top: 650 }));
        } else {
          await list.hover();
          await page.mouse.wheel(0, 650);
        }
        await expect.poll(() => list.evaluate(element => element.scrollTop)).toBeGreaterThan(0);
      }
      const last = list.getByRole("button").last();
      await last.scrollIntoViewIfNeeded();
      if (testInfo.project.use.hasTouch) await last.tap();
      else await last.click();
      await expect(last).toHaveAttribute("aria-pressed", "true");
      await list.evaluate(element => { element.scrollTop = 0; });
      await expect(last).toHaveAttribute("aria-pressed", "true");
      expect(await card.evaluate(element => element.scrollWidth > element.clientWidth + 1)).toBe(false);
      const heading = card.getByText(/^Available shifts/);
      expect(await heading.evaluate(element => element.closest('[role="region"]'))).toBeNull();
      if (count === 62 && width === 375) await card.screenshot({ path: testInfo.outputPath("scrollable-manual-shifts-375.png") });
    }
  });
}

for (const language of ["en", "pl", "uk"]) {
  test(`generated shifts responsive flow ${language}`, async ({ page }, testInfo) => {
    test.setTimeout(90_000);
    const fixtureState = await fixture(page, language);
    await page.goto("/container/1/graphs/1/edit");
    // Use the shared language API so this checks switching as well as translated layout.
    await page.evaluate(async lang => {
      const module = await import(/* @vite-ignore */ "/src/shared/i18n/index.ts");
      module.setLanguage(lang);
    }, language);
    const titles = { en: "Manual shifts", pl: "Zmiany ręczne", uk: "Ручні зміни" };
    const generateLabels = { en: "Generate", pl: "Generuj", uk: "Згенерувати" };
    const generate = page.getByRole("button", { name: generateLabels[language as keyof typeof generateLabels], exact: true });
    const expandDetails = page.getByRole("button", { name: /Expand Schedule Details|Rozwiń Szczegóły grafiku|Розгорнути Дані графіка/, exact: true });
    await expect(expandDetails).toBeVisible({ timeout: 20_000 });
    await page.getByRole("button", { name: /Collapse sidebar|Zwiń pasek boczny|Згорнути бічну панель/, exact: true }).click();
    await expandDetails.click();
    await expect(generate).toBeVisible(); await generate.click();
    await expect(page.getByText("Do obsadzenia 2", { exact: true }).first()).toBeVisible();
    const card = page.locator("section").filter({ has: page.getByText(new RegExp(`^${titles[language as keyof typeof titles]}`)) }).last();
    await page.getByRole("button", { name: /Expand Manual Columns|Rozwiń Kolumny ręczne|Розгорнути Ручні стовпці/, exact: true }).click();
    await expect(card).toBeVisible();
    const rows = card.locator('button[aria-pressed]').filter({ hasText: "08:00–12:00" });
    await expect(rows).toHaveCount(2);
    for (const width of [1280, 375, 390, 430, 320]) {
      await page.setViewportSize({ width, height: 900 });
      await rows.nth(1).click(); await expect(rows.nth(1)).toHaveAttribute("aria-pressed", "true");
      const overflow = await card.evaluate(element => element.scrollWidth > element.clientWidth + 1);
      expect(overflow, `card overflow at ${width}`).toBe(false);
      await expect(rows.nth(1)).toBeVisible();
      if (width === 375 || width === 1280) await card.screenshot({ path: testInfo.outputPath(`manual-shifts-${width}.png`) });
    }
    await rows.nth(0).focus(); await page.keyboard.press("Space"); await expect(rows.nth(0)).toHaveAttribute("aria-pressed", "true");
    expect(fixtureState.publications).toEqual([]);
    if (language === "en") {
      await page.setViewportSize({ width: 1280, height: 900 });
      await card.getByRole("button", { name: "Publish shift", exact: true }).click();
      await expect(rows).toHaveCount(1);
      await card.getByRole("button", { name: "Specific employee", exact: true }).click();
      await card.getByRole("button", { name: "employee list", exact: true }).click();
      await page.getByRole("option", { name: "Worker Test 2", exact: true }).click();
      await card.getByRole("button", { name: "Publish shift", exact: true }).click();
      await expect(rows).toHaveCount(0);
      await expect(card.getByRole("button", { name: /Offered \(2\)/ })).toHaveAttribute("aria-expanded", "true");
      expect(fixtureState.publications).toEqual([]);
      await page.getByRole("button", { name: "Save", exact: true }).first().click();
      await page.getByRole("dialog", { name: "Save schedule", exact: true }).getByRole("button", { name: "Save", exact: true }).click();
      await expect.poll(() => fixtureState.publications.length).toBe(2);
      expect(fixtureState.publications).toEqual([
        { manualColumnId: 1, dayOfMonth: 1, fromTime: "08:00", toTime: "12:00", targetEmployeeId: null },
        { manualColumnId: 2, dayOfMonth: 1, fromTime: "08:00", toTime: "12:00", targetEmployeeId: 2 },
      ]);
      await page.waitForURL(/\/container\/1\/graphs\/1$/);
      await page.goto("/container/1/graphs/1/edit");
      await page.getByRole("button", { name: "Collapse sidebar", exact: true }).click();
      await page.getByRole("button", { name: "Expand Manual Columns", exact: true }).click({ timeout: 20_000 });
      await expect(card.getByText("Do obsadzenia 2", { exact: true })).toBeVisible();
      await expect(rows).toHaveCount(0);
      await card.getByRole("button", { name: /Offered \(2\)/ }).click();
      await card.getByRole("button", { name: "Cancel shift on 01 Feb", exact: true }).first().click();
      await expect(rows).toHaveCount(1);
    }
  });
}
