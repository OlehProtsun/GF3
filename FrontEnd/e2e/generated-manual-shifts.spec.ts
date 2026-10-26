import { expect, test, type Page } from "@playwright/test";

test.use({ actionTimeout: 10_000, launchOptions: process.platform === "win32" ? { ignoreDefaultArgs: ["--disable-accelerated-compositing"] } : {} });

test("real backend manual acceptance persists and refreshes both colored cells", async ({ browser }, testInfo) => {
  test.skip(!process.env.GF3_LIVE_URL, "Run through ManualShiftHighlightTests with GF3_RUN_LIVE_BROWSER=1 for an isolated authenticated SQLite backend.");
  test.setTimeout(90_000);
  const url = process.env.GF3_LIVE_URL!;
  const graphId = Number(process.env.GF3_LIVE_GRAPH);
  const containerId = Number(process.env.GF3_LIVE_CONTAINER);
  const day = testInfo.project.name === "chromium" ? 10 : 11;
  const graphPath = `/api/containers/${containerId}/graphs/${graphId}`;
  const manager = await browser.newContext({ viewport: { width: 1280, height: 900 } });
  const employee = await browser.newContext({ viewport: { width: 390, height: 844 } });
  try {
    for (const [context, token] of [[manager, process.env.GF3_LIVE_MANAGER_TOKEN!], [employee, process.env.GF3_LIVE_EMPLOYEE_TOKEN!]] as const) {
      await context.addInitScript(value => localStorage.setItem("gf3.auth.access-token", value), token);
    }
    const managerPage = await manager.newPage();
    await managerPage.goto(`${url}/container/${containerId}/graphs/${graphId}/edit`);
    await managerPage.getByRole("button", { name: "Collapse sidebar", exact: true }).click();
    await managerPage.getByRole("button", { name: "Expand Manual shifts", exact: true }).click();
    const swatch = managerPage.getByRole("button", { name: "Accepted manual shift highlight color", exact: true });
    await swatch.click();
    await managerPage.getByRole("textbox", { name: "Accepted manual shift highlight hex color" }).fill("#99F6E4");
    await managerPage.getByRole("button", { name: "Use color", exact: true }).click();
    await expect(managerPage.getByRole("dialog", { name: "Choose accepted shift color" })).toHaveCount(0);
    const headers = managerPage.locator('table thead th');
    await expect(headers.nth(1).locator("input")).toHaveValue("Generated");
    await expect(headers.nth(1)).toHaveAttribute("draggable", "false");
    // The editor holds the edit lock; publish through the same authenticated manager session after leaving it.
    await managerPage.goto(`${url}/container/${containerId}/graphs/${graphId}`);
    const auth = { Authorization: `Bearer ${process.env.GF3_LIVE_MANAGER_TOKEN}` };
    const offered = await manager.request.post(`${url}${graphPath}/shift-swaps/manual`, { headers: auth,
      data: { manualColumnId: 7, dayOfMonth: day, fromTime: "09:00", toTime: "17:00" } });
    expect(offered.ok(), await offered.text()).toBe(true);
    const employeePage = await employee.newPage();
    await employeePage.goto(`${url}/swap`);
    await employeePage.getByRole("button", { name: "View offer details for Source" }).click();
    await employeePage.getByRole("dialog", { name: "Offer details" }).getByRole("button", { name: "Accept shift", exact: true }).click();
    await employeePage.getByRole("dialog", { name: "Accept this shift?" }).getByRole("button", { name: "Accept shift", exact: true }).click();
    await expect(employeePage.getByRole("dialog", { name: "Accept this shift?" })).toHaveCount(0);
    const cell = managerPage.getByRole("button", { name: `Generated day ${day}`, exact: true });
    await expect(cell.locator("xpath=ancestor::td")).toHaveCSS("background-color", "rgb(153, 246, 228)", { timeout: 15_000 });
    const recipient = managerPage.getByRole("button", { name: `Worker2 Test day ${day}`, exact: true });
    await expect(recipient.locator("xpath=ancestor::td")).toHaveCSS("background-color", "rgb(153, 246, 228)");
    await managerPage.reload();
    await expect(cell.locator("xpath=ancestor::td")).toHaveCSS("background-color", "rgb(153, 246, 228)");
    const detail = await manager.request.get(`${url}${graphPath}`, { headers: auth });
    const graph = await detail.json();
    const encoded = graph.note.split("[[GF3_GRAPH_META:b64:")[1].slice(0, -2);
    const meta = JSON.parse(Buffer.from(encoded, "base64url").toString("utf8"));
    expect(meta.s.find((entry: number[]) => entry[0] === -7 && entry[1] === day)[2]).toBe(-6687004);
  } finally { await manager.close(); await employee.close(); }
});

async function fixture(page: Page, language: string, availableShiftCount?: number, claimFlow = false, previewRaw?: string, workspaceMode = "pc") {
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
  if (claimFlow) graph = { ...graph, peoplePerShift: 1, shift1Time: "09:00 - 15:00", shift2Time: "15:00 - 21:00" };
  if (previewRaw) graph.note = `Visible note\n\n[[GF3_GRAPH_META:${JSON.stringify({ m: [[1, "do obstawienia", { "1": previewRaw }]], g: [1] })}]]`;
  let actor = 0;
  const claims: unknown[] = [];
  const slots: unknown[] = [];
  const offers: Record<string, unknown>[] = [];
  const publications: unknown[] = [];
  let manualHighlightColor = "#BAE6FD";
  const assignments = employees.map(employee => ({ id: employee.id, scheduleId: 1, employeeId: employee.id, displayOrder: employee.id - 1 }));
  await page.addInitScript(() => localStorage.setItem("gf3.auth.access-token", "fixture-token"));
  await page.route("**/api/**", async route => {
    const request = route.request(); const path = new URL(request.url()).pathname; const method = request.method();
    if (!path.startsWith("/api/")) return route.continue();
    const json = (body: unknown, status = 200) => route.fulfill({ status, contentType: "application/json", body: JSON.stringify(body) });
    if (path === "/api/auth/session") return json(actor ? { role:"employee", employeeId:actor, userName:`employee${actor}`,displayName:`Worker ${actor}` } : { role: "manager", workspaceMode, managerId: 1, userName: "manager", displayName: "Manager", isSystemManager: true });
    if (path === "/api/account-language") return json({ language });
    if (path.includes("negotiate")) return route.fulfill({ status: 503 });
    if (path === "/api/containers/1/graphs/generate-preview") {
      const generated = Array.from({ length: 28 }, (_, i) => [0, 1].flatMap(shift => [1, 2].map(employeeId => ({
        id: i * 4 + shift * 2 + employeeId, scheduleId: 1, dayOfMonth: i + 1, slotNo: employeeId,
        employeeId: i === 0 && (claimFlow ? employeeId === 1 : shift === 0) ? null : employeeId, status: i === 0 && (claimFlow ? employeeId === 1 : shift === 0) ? 0 : 1,
        fromTime: claimFlow ? shift ? "15:00" : "09:00" : shift ? "12:00" : "08:00", toTime: claimFlow ? shift ? "21:00" : "15:00" : shift ? "16:00" : "12:00",
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
    const employeeDto = (offer: Record<string, unknown>) => {
      const partialFree = actor === 2 && offer.fromTime === "09:00" && offer.toTime === "21:00";
      const canAcceptFull = !partialFree && !(actor === 2 && offer.fromTime === "15:00");
      const canAccept = offer.status === "open" && (canAcceptFull || Boolean(offer.allowPartialAcceptance) && partialFree);
      return { scheduleSlotId: 100, scheduleName: graph.name, containerName: "Container",shopName:"Shop",fromEmployeeId:null,fromEmployeeName:"do obstawienia",manualColumnName:"do obstawienia",
        createdAtUtc:"2026-02-01T00:00:00Z", shiftHours:offer.fromTime === "09:00" && offer.toTime === "21:00" ? 12 : 6,
        currentEmployeeHoursBefore:actor===2?6:0,currentEmployeeHoursAfter:12,currentEmployeeWorkDaysBefore:1,currentEmployeeWorkDaysAfter:1,
        currentEmployeeFreeDaysBefore:27,currentEmployeeFreeDaysAfter:27,fromEmployeeHoursBefore:0,fromEmployeeHoursAfter:0,
        isCreatedByCurrentEmployee:false,isScheduleLocked:false,canCancel:false,...offer,canAccept,canAcceptFull,
        eligibleClaimWindows:canAccept ? [{fromTime:String(offer.fromTime),toTime:partialFree?"15:00":String(offer.toTime)}] : [],
        acceptanceUnavailableReason:canAccept?null:"You already work during this time." };
    };
    if (path === "/api/employee-schedules") return json([{...graph,containerName:"Container",shopName:"Shop",employees:[],slots:actor===2?[{id:90,scheduleId:1,dayOfMonth:1,slotNo:1,employeeId:2,status:"ASSIGNED",fromTime:"15:00",toTime:"21:00"}]:[]}]);
    if (path === "/api/employee-shift-swaps") return json(offers.map(employeeDto));
    if (path === "/api/employee-ui-state") return json({scheduleColumnOrders:{},readNotificationIds:[],pinnedSwapIds:[]});
    if (/^\/api\/employee-shift-swaps\/\d+\/accept$/.test(path)) {
      const input=request.postData()?request.postDataJSON():null; claims.push(input);
      const offer=offers.find(item=>item.id===Number(path.split("/").at(-2)))!;
      if (offer.fromTime==="09:00" && offer.toTime==="21:00") {
        expect(input).toEqual({fromTime:"09:00",toTime:"15:00"});
        offers.push({...offer,id:2,fromTime:"15:00",toTime:"21:00",status:"open"});
        offer.toTime="15:00";
      }
      offer.status="accepted";offer.acceptedByEmployeeId=actor;offer.acceptedByEmployeeName=`Worker ${actor}`;
      return json(employeeDto(offer));
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
    if (path.endsWith("/shift-swaps/manual-highlight-setting")) {
      if (method === "PUT") manualHighlightColor = request.postDataJSON().highlightColor;
      return json({ highlightColor: manualHighlightColor });
    }
    if (path.endsWith("highlight-setting")) return json({ highlightColor: "#DBEAFE" });
    if (path.includes("notepad")) return json({ notes: [], state: { isExpanded: false, isPinned: false, height: 700 } });
    return json([]);
  });
  return { publications, claims, offers, getNote: () => graph.note, useEmployee: (id: number) => { actor = id; } };
}

test("compact manual demand survives editor, profile and employee phone", async ({ page }, testInfo) => {
  test.setTimeout(60_000);
  const raw = "09:00 - 15:00, 15:00 - 21:00, 09:00 - 15:00";
  const state = await fixture(page, "en", undefined, false, raw);
  const originalNote = state.getNote();
  await page.setViewportSize({ width: 1280, height: 900 });
  for (const path of ["/container/1/graphs/1/edit", "/container/1/graphs/1"]) {
    await page.goto(path);
    await page.getByRole("button", { name: "Collapse sidebar", exact: true }).click();
    const more = page.getByRole("button", { name: "More: Shifts", exact: true });
    await expect(more).toHaveCount(1, { timeout: 20_000 });
    await expect(more).toHaveCSS("color", "rgb(37, 99, 235)");
    const cell = more.locator("xpath=ancestor::td");
    await expect(cell).toHaveText("09:00 - 15:00, More");
    // The table may distribute spare space; verify the automatic content width before stretching.
    expect(await page.locator("col").nth(1).evaluate(el => Number((el as HTMLElement).style.getPropertyValue("--matrix-column-width").match(/\d+/)?.[0]))).toBeLessThan(300);
    await more.hover();
    const popup = page.getByRole("tooltip");
    await expect(popup).toBeVisible();
    await expect(popup.locator(":scope > div")).toHaveText(["09:00 - 15:00", "15:00 - 21:00"]);
    await popup.hover(); await expect(popup).toBeVisible();
    await page.keyboard.press("Escape"); await expect(popup).toHaveCount(0);
  }
  state.useEmployee(2);
  await page.goto("/schedule");
  for (const width of [320, 375, 390]) {
    await page.setViewportSize({ width, height: 740 });
    const more = page.getByRole("button", { name: "More: Shifts", exact: true });
    await more.scrollIntoViewIfNeeded();
    if (testInfo.project.use.hasTouch) await more.tap(); else await more.click();
    const popup = page.getByRole("tooltip");
    await expect(popup).toBeVisible();
    await expect(popup.locator(":scope > div")).toHaveText(["09:00 - 15:00", "15:00 - 21:00"]);
    expect(await popup.evaluate(el => {
      const r = el.getBoundingClientRect();
      return el.parentElement === document.body && r.left >= 0 && r.right <= innerWidth && r.top >= 0 && r.bottom <= innerHeight && el.scrollWidth <= el.clientWidth;
    })).toBe(true);
    expect(await more.evaluate(el => el.scrollWidth <= el.clientWidth)).toBe(true);
    await page.screenshot({ path: testInfo.outputPath(`manual-preview-${width}.png`) });
    const scroll = more.locator("xpath=ancestor::table/..");
    await scroll.evaluate(el => { el.scrollLeft = 0; el.dispatchEvent(new Event("scroll")); });
    await expect(popup).toHaveCount(0);
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  }
  expect(state.publications).toEqual([]); expect(state.claims).toEqual([]);
  expect(state.getNote()).toBe(originalNote);
});

test("manager phone keeps compact More visible under automatic width cap", async ({ page }) => {
  await fixture(page, "en", undefined, false, Array(30).fill("09:00 - 15:00").join(", "), "phone");
  await page.setViewportSize({ width: 375, height: 740 });
  await page.goto("/container/1/graphs/1");
  const more = page.getByRole("button", { name: "More: Shifts", exact: true });
  await expect(more).toBeVisible({ timeout: 20_000 });
  await more.scrollIntoViewIfNeeded();
  await expect(more).toBeVisible();
  expect(await more.evaluate(el => el.scrollWidth <= el.clientWidth)).toBe(true);
  await more.click();
  const popup = page.getByRole("tooltip");
  await expect(popup).toBeVisible();
  await expect(popup.locator(":scope > div")).toHaveCount(29);
  expect(await popup.evaluate(el => el.scrollHeight > el.clientHeight && el.getBoundingClientRect().bottom <= innerHeight)).toBe(true);
  await popup.evaluate(el => { el.scrollTop = 200; });
  await expect(popup).toBeVisible();
  await page.keyboard.press("Escape"); await expect(popup).toHaveCount(0);
});

test("manual color retains confirmed value when the server rejects saving", async ({ page }) => {
  await fixture(page, "en", 0);
  await page.route("**/shift-swaps/manual-highlight-setting", route => route.request().method() === "PUT"
    ? route.fulfill({ status: 409, contentType: "application/problem+json", body: JSON.stringify({ title: "Color conflict", detail: "Color conflict" }) })
    : route.fallback());
  await page.goto("/container/1/graphs/1/edit");
  await page.getByRole("button", { name: "Collapse sidebar", exact: true }).click();
  await page.getByRole("button", { name: "Expand Manual shifts", exact: true }).click();
  const swatch = page.getByRole("button", { name: "Accepted manual shift highlight color", exact: true });
  await swatch.click();
  await page.getByRole("textbox", { name: "Accepted manual shift highlight hex color" }).fill("#AABBCC");
  await page.getByRole("button", { name: "Use color", exact: true }).click();
  await expect(page.getByRole("main").getByText("Color conflict", { exact: true })).toBeVisible();
  await expect(page.getByRole("dialog", { name: "Choose accepted shift color" })).toBeVisible();
  await page.keyboard.press("Escape");
  await expect(page.getByRole("dialog", { name: "Choose accepted shift color" })).toHaveCount(0);
  await expect(swatch.locator("span")).toHaveCSS("background-color", "rgb(186, 230, 253)");
});

for (const count of [0, 1, 5, 30, 62]) {
  test(`manual shifts bounded list with ${count} shifts`, async ({ page, browserName }, testInfo) => {
    test.setTimeout(60_000);
    await fixture(page, "en", count);
    await page.goto("/container/1/graphs/1/edit");
    const expand = page.getByRole("button", { name: "Expand Manual shifts", exact: true });
    await expect(expand).toBeVisible({ timeout: 20_000 });
    await page.getByRole("button", { name: "Collapse sidebar", exact: true }).click();
    const labels = await page.locator('button[aria-label^="Expand "]').evaluateAll(buttons => buttons.map(button => button.getAttribute("aria-label")));
    expect(labels.filter(label => label !== "Expand header")).toEqual(["Expand Schedule Details", "Expand Employees", "Expand Publication", "Expand Manual shifts", "Expand Shift Corrections", "Expand Bind Information"]);
    await expect(page.locator("#graph-note")).toHaveCount(0);
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
    await expect(page.getByText("do obstawienia", { exact: true }).first()).toBeVisible();
    const card = page.locator("section").filter({ has: page.getByText(new RegExp(`^${titles[language as keyof typeof titles]}`)) }).last();
    await page.getByRole("button", { name: /Expand Manual shifts|Rozwiń Zmiany ręczne|Розгорнути Ручні зміни/, exact: true }).click();
    await expect(card).toBeVisible();
    const colorLabels = {
      en: { swatch: "Accepted manual shift highlight color", input: "Accepted manual shift highlight hex color", use: "Use color" },
      pl: { swatch: "Kolor wyróżnienia przyjętej ręcznej zmiany", input: "Kolor HEX wyróżnienia przyjętej ręcznej zmiany", use: "Zastosuj kolor" },
      uk: { swatch: "Колір виділення прийнятої ручної зміни", input: "HEX-колір виділення прийнятої ручної зміни", use: "Застосувати колір" },
    }[language as "en" | "pl" | "uk"];
    await card.getByRole("button", { name: colorLabels.swatch }).click();
    await page.getByRole("textbox", { name: colorLabels.input }).fill("#99F6E4");
    await page.getByRole("button", { name: colorLabels.use, exact: true }).click();
    await expect(card.getByRole("button", { name: colorLabels.swatch }).locator("span")).toHaveCSS("background-color", "rgb(153, 246, 228)");
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
        { manualColumnId: 1, dayOfMonth: 1, fromTime: "08:00", toTime: "12:00", targetEmployeeId: null, allowPartialAcceptance: false },
        { manualColumnId: 1, dayOfMonth: 1, fromTime: "08:00", toTime: "12:00", targetEmployeeId: 2, allowPartialAcceptance: false },
      ]);
      await page.waitForURL(/\/container\/1\/graphs\/1$/);
      await page.goto("/container/1/graphs/1/edit");
      await page.getByRole("button", { name: "Collapse sidebar", exact: true }).click();
      await page.getByRole("button", { name: "Expand Manual shifts", exact: true }).click({ timeout: 20_000 });
      await expect(card.getByText("do obstawienia", { exact: true })).toBeVisible();
      await expect(rows).toHaveCount(0);
      await card.getByRole("button", { name: /Offered \(2\)/ }).click();
      await card.getByRole("button", { name: "Cancel shift on 01 Feb", exact: true }).first().click();
      await expect(rows).toHaveCount(1);
    }
  });
}

for (const flexible of [true, false]) {
  test(`manager combined offer and occupied recipient ${flexible ? "partial and final claim" : "strict overlap"}`, async ({ page }, testInfo) => {
    test.setTimeout(90_000);
    const employeeViewport = page.viewportSize();
    await page.setViewportSize({ width: 1280, height: 900 });
    const state = await fixture(page, "en", undefined, true);
    await page.goto("/container/1/graphs/1/edit");
    await page.getByRole("button", {name:"Collapse sidebar",exact:true}).click();
    await page.getByRole("button", {name:"Expand Schedule Details",exact:true}).click();
    await page.getByRole("button", {name:"Generate",exact:true}).click();
    await expect(page.getByText("do obstawienia",{exact:true}).first()).toBeVisible();
    await page.getByRole("button", {name:"Expand Manual shifts",exact:true}).click();
    const card=page.locator("section").filter({has:page.getByText(/^Manual shifts/)}).last();
    await card.getByRole("region",{name:"Available shifts",exact:true}).getByRole("button",{name:/09:00–21:00/}).click();
    if(flexible) await card.getByRole("button",{name:"Allow custom time",exact:true}).click();
    await card.getByRole("button",{name:"Publish shift",exact:true}).click();
    await page.getByRole("button",{name:"Save",exact:true}).first().click();
    await page.getByRole("dialog",{name:"Save schedule",exact:true}).getByRole("button",{name:"Save",exact:true}).click();
    await expect.poll(()=>state.publications.length).toBe(1);
    expect(state.publications[0]).toEqual({manualColumnId:1,dayOfMonth:1,fromTime:"09:00",toTime:"21:00",targetEmployeeId:null,allowPartialAcceptance:flexible});
    await page.waitForURL(/\/container\/1\/graphs\/1$/);
    if (employeeViewport) await page.setViewportSize(employeeViewport);
    state.useEmployee(2); await page.goto("/swap");
    await page.getByRole("button",{name:"View offer details for Generated shifts"}).click();
    const details=page.getByRole("dialog",{name:"Offer details"});
    if(!flexible) {
      await expect(details).toContainText("You already work during this time.");
      await expect(details.getByRole("button",{name:"Accept shift",exact:true})).toBeDisabled();return;
    }
    await expect(details).toContainText("you can claim 09:00–15:00");
    await details.screenshot({ path: testInfo.outputPath("claim-period.png") });
    await expect(details.getByRole("button",{name:"Full shift",exact:true})).toHaveCount(0);
    await details.getByRole("button",{name:"Accept shift",exact:true}).click();
    const confirm=page.getByRole("dialog",{name:"Accept this shift?"});await expect(confirm).toContainText("09:00–15:00 (6h)");
    await confirm.getByRole("button",{name:"Accept shift",exact:true}).click();
    await expect(confirm).toHaveCount(0);await expect.poll(()=>state.claims.length).toBe(1);
    await expect(page.getByRole("button",{name:"View offer details for Generated shifts"})).toHaveCount(1);
    state.useEmployee(3);await page.goto("/swap");
    await page.getByRole("button",{name:"View offer details for Generated shifts"}).click();
    await expect(details).toContainText("15:00–21:00");
    await details.getByRole("button",{name:"Accept shift",exact:true}).click();
    await confirm.getByRole("button",{name:"Accept shift",exact:true}).click();
    await expect.poll(()=>state.claims.length).toBe(2);expect(state.offers.every(offer=>offer.status==="accepted")).toBe(true);
  });
}
