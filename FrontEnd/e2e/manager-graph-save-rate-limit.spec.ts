import { expect, test, type Page, type WebSocketRoute } from "@playwright/test";

const ids = [34, 36, 37, 38, 42];
const graphRoot = "/api/containers/7/graphs";
const editorUrl = (id: number) => "/container/7/graphs/" + id + "/edit?openGraphIds=" + ids.join(",");
const employeeIds = [1, 2, 3, 4, 5];
const employees = employeeIds.map(id => ({ id, firstName: "Worker", lastName: String(id) }));
const matrix = (page: Page) => page.locator('[class*="_scheduleMatrixCardShell_"] table');
const scheduleCell = (page: Page, key: string) => matrix(page).locator('[data-cell-key="' + key + '"]');
const recovery = (page: Page) => page.getByRole("dialog", { name: "Schedule save and recovery" });
const oldSummary = (page: Page) => page.getByRole("region", { name: "Schedule save and recovery" });

test.use({ serviceWorkers: "block" });

async function workspace(page: Page, baseURL: string, loseThirdBatch: boolean) {
  const origin = new URL(baseURL).origin;
  if (!["127.0.0.1", "localhost", "[::1]"].includes(new URL(origin).hostname)) {
    throw new Error("The mocked workspace requires a loopback frontend.");
  }
  const states = new Map(ids.map(id => [id, {
    graph: { id, containerId: 7, shopId: 1, name: "Graph " + id, year: 2026, month: 11, publicationStatus: "private",
      allowSwap: true, peoplePerShift: 1, shift1Time: "08:00 - 12:00", shift2Time: "12:00 - 16:00",
      maxHoursPerEmpMonth: 160, maxConsecutiveDays: 5, maxConsecutiveFull: 3, maxFullPerMonth: 10, note: "" },
    styles: new Map<string, Record<string, unknown>>(),
    slots: [{ id: 1, scheduleId: id, employeeId: 1, dayOfMonth: 12, slotNo: 1, status: 1, fromTime: "08:00", toTime: "12:00" }],
    versionCount: 0,
  }]));
  const traffic: Array<{ method: string; path: string; time: number }> = [];
  let socket: WebSocketRoute | undefined;
  let recording = false;
  let firstThrottle = !loseThirdBatch;
  let verifyOutage = false;
  let backgroundOutage = false;
  let backgroundThrottleCount = 0;
  let peakSaveReads = 0;
  let activeSaveReads = 0;
  let activeResourceReads = 0;
  let lastResourceRead = 0;
  const separator = "\u001e";
  const burst = (id: number) => {
    const payload = { containerId: 7, graphId: id, resourceType: "schedule", reason: "manager-schedule-cell-style-updated", changedAtUtc: new Date().toISOString() };
    for (const target of ["ScheduleChanged", "ManagerDataChanged", "ShiftSwapsChanged"]) {
      socket?.send(JSON.stringify({ type: 1, target, arguments: [payload] }) + separator);
    }
  };
  await page.routeWebSocket("**/api/realtime/presence**", connection => {
    socket = connection;
    connection.onMessage(raw => {
      for (const frame of raw.toString().split(separator).filter(Boolean)) {
        const { protocol, invocationId, arguments: args } = JSON.parse(frame);
        if (protocol) connection.send("{}" + separator);
        if (invocationId !== undefined) {
          const result = (args?.[0] ?? []).map((lock: object) => ({ ...lock, isLocked: true,
            lockedByManagerId: 9, changedAtUtc: "2026-11-09T10:00:00Z" }));
          connection.send(JSON.stringify({ type: 3, invocationId, result }) + separator);
        }
      }
    });
  });
  await page.addInitScript(() => localStorage.setItem("gf3.auth.access-token", "mock-manager"));
  await page.route("**/*", async route => {
    const request = route.request();
    const url = new URL(request.url());
    if (url.origin !== origin) return route.abort();
    const path = url.pathname;
    if (!path.startsWith("/api/")) return route.continue();
    const method = request.method();
    if (recording) traffic.push({ method, path, time: Date.now() });
    const json = (value: unknown, status = 200, headers?: Record<string, string>) => route.fulfill({ status, headers, json: value });
    const accepted = () => route.fulfill({ status: 204 });
    if (path === "/api/auth/session") return json({ role: "manager", managerId: 9, workspaceMode: "pc", userName: "fixture", displayName: "Manager", isSystemManager: true });
    if (path === "/api/account-language") return json({ language: "en" });
    if (path.endsWith("/presence/negotiate")) return json({
      connectionId: "fixture", connectionToken: "fixture", negotiateVersion: 1,
      availableTransports: [{ transport: "WebSockets", transferFormats: ["Text"] }],
    });
    if (path === "/api/containers/7") return json({ id: 7, name: "Container" });
    if (path === graphRoot) return json([...states.values()].map(state => state.graph));
    if (path === "/api/shops") return json([{ id: 1, name: "Shop" }]);
    if (path === "/api/employees") return json(employees);
    if (path === "/api/employment-contract-settings") return json({ automaticWeekdayAvailability: false });
    if (path.endsWith("highlight-setting")) return json({ highlightColor: "#DBEAFE" });
    if (path.includes("notepad")) return json({ notes: [], state: { isExpanded: false, isPinned: false, height: 700 } });
    const match = path.match(/^\/api\/containers\/7\/graphs\/(\d+)(.*)$/);
    if (!match) return json([]);
    const id = Number(match[1]);
    const state = states.get(id)!;
    const resource = match[2];
    const resourceRead = method === "GET" && ["", "/employees", "/slots", "/cell-styles"].includes(resource);
    const trackedRead = recording && resourceRead;
    if (resourceRead) activeResourceReads++;
    if (trackedRead) peakSaveReads = Math.max(peakSaveReads, ++activeSaveReads);
    try {
      if (resourceRead && id === 37 && resource === "/slots" && verifyOutage) {
        return await route.fulfill({ status: 429, contentType: "text/html", body: "<html>Too many requests</html>" });
      }
      if (resourceRead && id === 42 && resource === "/slots" && backgroundOutage) {
        backgroundThrottleCount++;
        return await json({ title: "Too many requests", status: 429 }, 429, { "Retry-After": "1" });
      }
      if (recording && resourceRead && id === 37 && resource === "/slots" && firstThrottle) {
        firstThrottle = false;
        return await json({ title: "Too many requests", status: 429 }, 429, { "Retry-After": "1" });
      }
      if (resource === "" && method === "PUT") {
        state.graph = { ...state.graph, ...request.postDataJSON() };
        burst(id);
        return await accepted();
      }
      if (resource === "/slots" && method === "PUT") {
        state.slots = request.postDataJSON().slots.map((slot: object, index: number) => ({ ...slot, id: index + 1, scheduleId: id }));
        burst(id);
        return await accepted();
      }
      if (resource === "/cell-styles/batch" && method === "PUT") {
        for (const patch of request.postDataJSON().changes) {
          const key = patch.employeeId + ":" + patch.dayOfMonth;
          const old = state.styles.get(key);
          state.styles.set(key, { id: old?.id ?? state.styles.size + 1, scheduleId: id,
            employeeId: patch.employeeId, dayOfMonth: patch.dayOfMonth,
            backgroundColorArgb: patch.changeBackgroundColor ? patch.backgroundColorArgb : old?.backgroundColorArgb ?? null,
            textColorArgb: patch.changeTextColor ? patch.textColorArgb : old?.textColorArgb ?? null });
        }
        burst(id);
        if (id === 37 && loseThirdBatch) {
          verifyOutage = true;
          return await route.abort("connectionreset");
        }
        return await accepted();
      }
      if (resource === "/versions") {
        if (method === "POST") state.versionCount++;
        return await json({ currentVersionId: state.versionCount || null,
          versions: Array.from({ length: state.versionCount }, (_, i) => ({ id: i + 1, scheduleId: id, createdAtUtc: "2026-11-09T10:00:00Z" })) });
      }
      if (resource === "/slots") return await json(state.slots);
      if (resource === "/cell-styles") return await json([...state.styles.values()]);
      if (resource === "/employees") return await json(employeeIds.map((employeeId, displayOrder) => ({ id: employeeId, employeeId, displayOrder, scheduleId: id, isPriority: false })));
      return await json(resource === "" ? state.graph : []);
    } finally {
      if (trackedRead) activeSaveReads--;
      if (resourceRead) {
        activeResourceReads--;
        lastResourceRead = Date.now();
      }
    }
  });
  return {
    states, traffic, burst,
    connected: () => Boolean(socket),
    resourceReadsSettled: () => activeResourceReads === 0 && Date.now() - lastResourceRead >= 300,
    startRecording: () => { traffic.length = 0; peakSaveReads = 0; recording = true; },
    peakSaveReads: () => peakSaveReads,
    failBackground: () => { backgroundOutage = true; burst(42); },
    backgroundThrottleCount: () => backgroundThrottleCount,
    recover: () => { verifyOutage = false; backgroundOutage = false; },
  };
}

async function editFive(page: Page, paintMany: boolean) {
  await page.goto(editorUrl(ids[0]));
  await page.getByRole("button", { name: "Collapse sidebar", exact: true }).click();
  for (const [index, id] of ids.entries()) {
    if (index) {
      await page.getByRole("tab", { name: "Graph " + id }).click();
      await expect(page).toHaveURL(url => url.pathname.endsWith("/" + id + "/edit"));
    }
    const first = scheduleCell(page, "1:1");
    await expect(first).toBeVisible();
    const day = (value: number) => matrix(page).locator('[data-matrix-target="day"][data-day-of-month="' + value + '"]');
    await day(1).click();
    if (paintMany) {
      for (let value = 2; value <= 10; value++) await day(value).click({ modifiers: ["Control"] });
      if (index === 0) await day(13).click({ modifiers: ["Control"] });
      if (index === 4) await scheduleCell(page, "1:13").click({ modifiers: ["Control"] });
    }
    await page.getByRole("button", { name: /^Fill #/ }).click();
    await day(2).click();
    await page.getByRole("button", { name: /^Text #/ }).click();
    const writeCell = async (key: string, value: string) => {
      const cell = scheduleCell(page, key);
      await cell.getByRole("button").dblclick();
      await cell.getByRole("textbox").fill(value);
      await cell.getByRole("textbox").press("Enter");
    };
    await writeCell("1:11", "Training");
    if (index === 0 || index === 4) await writeCell("1:12", "09:00 - 12:00");
  }
}

async function save(page: Page, beforeConfirm?: () => Promise<void>) {
  await page.getByRole("button", { name: "Save", exact: true }).click();
  const confirm = page.getByRole("dialog").getByRole("button", { name: "Save", exact: true });
  await expect(confirm).toBeVisible();
  await beforeConfirm?.();
  await confirm.click();
}

function desktopOnly(project: string) {
  test.skip(project === "mobile-chrome" || project === "webkit-iphone-x", "The full matrix stress scenario uses the desktop manager workspace.");
}

test("five graphs save 256 styles without adding a status panel or moving the matrix", async ({ page, baseURL }, testInfo) => {
  desktopOnly(testInfo.project.name);
  test.setTimeout(150_000);
  await page.setViewportSize({ width: 1450, height: 960 });
  const fixture = await workspace(page, baseURL!, false);
  await editFive(page, true);
  await expect.poll(fixture.connected).toBe(true);
  const before = await matrix(page).boundingBox();
  await save(page, async () => {
    await expect.poll(fixture.resourceReadsSettled).toBe(true);
    fixture.startRecording();
  });
  await expect(page.getByRole("button", { name: "Saving...", exact: true })).toBeDisabled();
  await expect(oldSummary(page)).toHaveCount(0);
  await expect(recovery(page)).toHaveCount(0);
  const during = await matrix(page).boundingBox();
  expect(Math.abs(during!.y - before!.y)).toBeLessThan(2);
  await expect(page).toHaveURL(url => url.pathname === "/container/7/graphs/42");
  await expect(page.getByText("All schedules saved", { exact: true })).toHaveCount(0);
  const writes = fixture.traffic.filter(entry => entry.path.startsWith(graphRoot + "/") && entry.method !== "GET");
  expect(writes.filter(entry => entry.path.endsWith("/cell-styles/batch")).map(entry => entry.path)).toEqual(ids.map(id => graphRoot + "/" + id + "/cell-styles/batch"));
  expect(writes.filter(entry => /\/cell-styles(?:\/\d+)?$/.test(entry.path))).toHaveLength(0);
  expect(writes.filter(entry => entry.path.endsWith("/versions"))).toHaveLength(5);
  expect(writes.filter(entry => entry.path.endsWith("/shift-swaps/manual"))).toHaveLength(0);
  const preflight = fixture.traffic.slice(0, fixture.traffic.indexOf(writes[0]));
  for (const id of ids) {
    expect(preflight.filter(entry => ["", "/employees", "/slots", "/cell-styles"].some(suffix => entry.path === graphRoot + "/" + id + suffix)))
      .toHaveLength(id === 37 ? 5 : 4);
  }
  const retried = preflight.filter(entry => entry.path === graphRoot + "/37/slots");
  expect(retried[1].time - retried[0].time).toBeGreaterThanOrEqual(950);
  expect(fixture.peakSaveReads()).toBeLessThanOrEqual(2);
  for (const state of fixture.states.values()) {
    expect(state.styles.size).toBeGreaterThanOrEqual(50);
    expect(state.versionCount).toBe(1);
  }
  expect([...fixture.states.values()].reduce((total, state) => total + state.styles.size, 0)).toBe(256);
  for (const id of [34, 42]) expect(fixture.states.get(id)?.slots.find(slot => slot.dayOfMonth === 12)?.fromTime).toBe("09:00");
  expect(await page.evaluate(() => sessionStorage.getItem("gf3:graph-drafts:9:7"))).toBeNull();
});

test("a lost batch and HTML 429 verification keep drafts and never replay confirmed writes", async ({ page, baseURL }, testInfo) => {
  desktopOnly(testInfo.project.name);
  test.setTimeout(90_000);
  await page.setViewportSize({ width: 1450, height: 960 });
  const fixture = await workspace(page, baseURL!, true);
  await editFive(page, false);
  await expect.poll(fixture.connected).toBe(true);
  fixture.startRecording();
  fixture.failBackground();
  await expect.poll(fixture.backgroundThrottleCount).toBe(2);
  expect(fixture.traffic.filter(entry => entry.path === graphRoot)).toHaveLength(1);
  await expect(oldSummary(page)).toHaveCount(0);
  await expect(scheduleCell(page, "1:11")).toContainText("Training");
  await page.getByRole("button", { name: "Review issue", exact: true }).click();
  await expect(recovery(page).getByText(/Schedule data could not be refreshed/)).toBeVisible();
  fixture.recover();
  await recovery(page).getByRole("button", { name: "Retry loading" }).click();
  await expect(recovery(page).getByText(/Schedule data could not be refreshed/)).toHaveCount(0);
  await recovery(page).getByRole("button", { name: "Close", exact: true }).click();
  const before = await matrix(page).boundingBox();
  fixture.startRecording();
  await save(page);
  await expect(page.getByText("Graph 37 · Cannot verify", { exact: true })).toHaveCount(0);
  await page.getByRole("button", { name: "Review issue", exact: true }).click({ timeout: 15_000 });
  await expect(oldSummary(page)).toHaveCount(0);
  expect(Math.abs((await matrix(page).boundingBox())!.y - before!.y)).toBeLessThan(2);
  for (const label of ["Graph 34 · Saved", "Graph 36 · Saved", "Graph 37 · Cannot verify", "Graph 38 · Pending", "Graph 42 · Pending"]) {
    await expect(recovery(page).getByText(label, { exact: true })).toBeVisible();
  }
  await expect(scheduleCell(page, "1:11")).toContainText("Training");
  expect(await page.evaluate(() => JSON.parse(sessionStorage.getItem("gf3:graph-drafts:9:7") ?? "[]")
    .map((draft: { graphId: number }) => draft.graphId))).toEqual(ids.slice(2));
  const lostWrite = fixture.traffic.findIndex(entry => entry.method === "PUT" && entry.path === graphRoot + "/37/cell-styles/batch");
  expect(lostWrite).toBeGreaterThanOrEqual(0);
  const verificationReads = fixture.traffic.slice(lostWrite + 1).filter(entry => entry.path === graphRoot + "/37/slots");
  expect(verificationReads).toHaveLength(3);
  expect(verificationReads[2].time - verificationReads[0].time).toBeGreaterThanOrEqual(5900);
  fixture.recover();
  await page.reload();
  await page.getByRole("button", { name: "Restore draft", exact: true }).click();
  await recovery(page).getByRole("button", { name: "Discard local draft", exact: true }).click();
  const discard = page.getByRole("dialog", { name: "Discard local draft" });
  await expect(discard).toBeVisible();
  await discard.getByRole("button", { name: "Cancel", exact: true }).click();
  await recovery(page).getByRole("button", { name: "Restore draft", exact: true }).click();
  await expect(recovery(page).getByText("Graph 37 · Cannot verify", { exact: true })).toBeVisible();
  await expect(scheduleCell(page, "1:11")).toContainText("Training");
  await recovery(page).getByRole("button", { name: "Review saved schedule" }).click();
  await expect(recovery(page).getByText(/Your draft is still here/)).toBeVisible();
  const batches = fixture.traffic.filter(entry => entry.method === "PUT" && entry.path.endsWith("/cell-styles/batch"));
  expect(batches.map(entry => entry.path)).toEqual(ids.slice(0, 3).map(id => graphRoot + "/" + id + "/cell-styles/batch"));
  expect(fixture.traffic.filter(entry => entry.method === "POST" && entry.path.endsWith("/versions"))).toHaveLength(2);
});

test("the compact recovery dialog fits a narrow manager viewport without moving the matrix", async ({ page, baseURL }, testInfo) => {
  test.skip(!["mobile-chrome", "webkit-iphone-x"].includes(testInfo.project.name), "This checks the narrow touch viewport.");
  const fixture = await workspace(page, baseURL!, true);
  await page.goto(testInfo.project.name === "mobile-chrome" ? "/container/7/graphs/42/edit" : editorUrl(42));
  await expect(matrix(page)).toBeVisible();
  const sidebar = page.getByRole("button", { name: "Collapse sidebar", exact: true });
  if (await sidebar.isVisible()) await sidebar.click();
  const before = await matrix(page).boundingBox();
  await expect.poll(fixture.connected).toBe(true);
  fixture.failBackground();
  await page.getByRole("button", { name: "Review issue", exact: true }).click();
  const dialog = await recovery(page).boundingBox();
  const viewport = page.viewportSize()!;
  expect(dialog!.x).toBeGreaterThanOrEqual(0);
  expect(dialog!.x + dialog!.width).toBeLessThanOrEqual(viewport.width);
  expect(dialog!.height).toBeLessThan(viewport.height);
  await expect(oldSummary(page)).toHaveCount(0);
  expect(Math.abs((await matrix(page).boundingBox())!.y - before!.y)).toBeLessThan(2);
  await recovery(page).getByRole("button", { name: "Close", exact: true }).click();
  await expect(matrix(page)).toBeVisible();
});
