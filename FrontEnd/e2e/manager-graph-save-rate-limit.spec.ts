import { expect, test, type Page, type WebSocketRoute } from "@playwright/test";
import { MessageType } from "@microsoft/signalr";
import { readFileSync } from "node:fs";

type FixtureSlot = { dayOfMonth: number; slotNo: number; fromTime: string; toTime: string; employeeId: number | null; status: number | string };
type IncidentGraph = { graphId: number; before: { slots: FixtureSlot[]; employeeOrder: number[] }; after: { slots: FixtureSlot[]; employeeOrder: number[] } };

const ids = [34, 36, 37, 38, 42];
const graphRoot = "/api/containers/7/graphs";
const editorUrl = (id: number) => "/container/7/graphs/" + id + "/edit?openGraphIds=" + ids.join(",");
const employeeIds = [1, 2, 3, 4, 5];
const matrix = (page: Page) => page.locator('[class*="_scheduleMatrixCardShell_"] table');
const scheduleCell = (page: Page, key: string) => matrix(page).locator('[data-cell-key="' + key + '"]');
const recovery = (page: Page) => page.getByRole("dialog", { name: "Schedule save and recovery" });
const oldSummary = (page: Page) => page.getByRole("region", { name: "Schedule save and recovery" });

test.use({ serviceWorkers: "block" });

async function workspace(page: Page, baseURL: string, loseThirdBatch: boolean,
  options: { graphIds?: number[]; roster?: number[]; throttle?: boolean; enforceLimits?: boolean;
    incident?: IncidentGraph[]; loseCommittedReadback?: boolean; availabilityRosters?: Record<number, number[]>;
    unlinked?: boolean; period?: { year: number; month: number } } = {}) {
  const graphIds = options.graphIds ?? ids;
  const roster = options.roster ?? employeeIds;
  const period = options.period ?? { year: 2026, month: 11 };
  const fixtureEmployees = roster.map(id => ({ id, firstName: "Worker", lastName: String(id) }));
  const origin = new URL(baseURL).origin;
  if (!["127.0.0.1", "localhost", "[::1]"].includes(new URL(origin).hostname)) {
    throw new Error("The mocked workspace requires a loopback frontend.");
  }
  const states = new Map(graphIds.map(id => [id, {
    graph: { id, containerId: 7, shopId: 1, name: "Graph " + id, ...period, publicationStatus: "private",
      allowSwap: true, peoplePerShift: 1, shift1Time: "08:00 - 12:00", shift2Time: "12:00 - 16:00",
      maxHoursPerEmpMonth: 160, maxConsecutiveDays: 5, maxConsecutiveFull: 3, maxFullPerMonth: 10, note: "",
      availabilityGroupId: options.availabilityRosters && !options.unlinked ? id : null },
    styles: new Map<string, Record<string, unknown>>(),
    slots: [{ id: 1, scheduleId: id, employeeId: 1 as number | null, dayOfMonth: 12, slotNo: 1, status: 1, fromTime: "08:00", toTime: "12:00" }],
    versionCount: 0,
  }]));
  for (const graph of options.incident ?? []) states.get(graph.graphId)!.slots = graph.before.slots.map((slot, i) => ({
    ...slot, status: slot.employeeId ? 1 : 0, id: i + 1, scheduleId: graph.graphId,
  }));
  const traffic: Array<{ method: string; path: string; time: number }> = [];
  let socket: WebSocketRoute | undefined;
  let recording = false;
  let firstThrottle = !loseThirdBatch && options.throttle !== false;
  let verifyOutage = false;
  let backgroundOutage = false;
  let committedReadbackOutage = options.loseCommittedReadback;
  let backgroundThrottleCount = 0;
  let activeRequests = 0;
  let peakConcurrency = 0;
  let rateLimits = 0;
  let tokens = 60;
  let tokenTime = 0;
  let windowTime = 0;
  let windowRequests = 0;
  let pause: { id: number; method: string } | undefined;
  let paused = false;
  let release: (() => void) | undefined;
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
        const { protocol, invocationId, arguments: args, type } = JSON.parse(frame);
        if (protocol) connection.send("{}" + separator);
        if (type === MessageType.Ping) connection.send(frame + separator);
        if (invocationId !== undefined) {
          const result = (args?.[0] ?? []).map((lock: object) => ({ ...lock, isLocked: true,
            lockedByManagerId: 9, changedAtUtc: "2026-11-09T10:00:00Z" }));
          connection.send(JSON.stringify({ type: 3, invocationId, result }) + separator);
        }
      }
    });
  });
  await page.addInitScript(() => {
    localStorage.setItem("gf3.auth.access-token", "mock-manager");
    const counter = { recording: false, active: 0, peak: 0 };
    Object.defineProperty(window, "__gf3SaveHttp", { value: counter });
    const send = window.fetch.bind(window);
    window.fetch = async (input, init) => {
      const url = new URL(input instanceof Request ? input.url : String(input), location.href);
      if (!location.pathname.endsWith("/edit")) counter.recording = false;
      const tracked = counter.recording && url.pathname.startsWith("/api/containers/7/graphs/");
      if (tracked) counter.peak = Math.max(counter.peak, ++counter.active);
      try { return await send(input, init); }
      finally { if (tracked) counter.active--; }
    };
  });
  await page.route("**/*", async route => {
    const request = route.request();
    const url = new URL(request.url());
    if (url.origin !== origin) return route.abort();
    const path = url.pathname;
    if (!path.startsWith("/api/")) return route.continue();
    const method = request.method();
    if (recording && !new URL(page.url()).pathname.endsWith("/edit")) recording = false;
    if (recording) traffic.push({ method, path, time: Date.now() });
    const json = (value: unknown, status = 200, headers?: Record<string, string>) => {
      if (recording && status === 429) rateLimits++;
      return route.fulfill({ status, headers, json: value });
    };
    const accepted = () => route.fulfill({ status: 204 });
    if (recording && options.enforceLimits) {
      const now = Date.now();
      tokens = Math.min(60, tokens + (now - tokenTime) / 100);
      tokenTime = now;
      if (now - windowTime >= 60000) { windowTime = now; windowRequests = 0; }
      if (tokens < 1 || ++windowRequests > 240) return json({ status: 429, title: "Too many requests" }, 429, { "Retry-After": "1" });
      tokens--;
    }
    if (path === "/api/auth/session") return json({ role: "manager", managerId: 9, workspaceMode: "pc", userName: "fixture", displayName: "Manager", isSystemManager: true });
    if (path === "/api/account-language") return json({ language: "en" });
    if (path.endsWith("/presence/negotiate")) return json({
      connectionId: "fixture", connectionToken: "fixture", negotiateVersion: 1,
      availableTransports: [{ transport: "WebSockets", transferFormats: ["Text"] }],
    });
    if (path === "/api/containers/7") return json({ id: 7, name: "Container" });
    if (path === graphRoot) return json([...states.values()].map(state => state.graph));
    if (path === "/api/shops") return json([{ id: 1, name: "Shop" }]);
    if (path === "/api/employees") return json(fixtureEmployees);
    if (options.availabilityRosters) {
      if (path === "/api/availability-groups") return json(graphIds.map(id => ({ id, ...period, name: "Availability " + id })));
      const availability = path.match(/^\/api\/availability-groups\/(\d+)\/members$/);
      if (availability) return json((options.availabilityRosters[Number(availability[1])] ?? [])
        .map((employeeId, displayOrder) => ({ id: employeeId, employeeId, displayOrder, availabilityGroupId: Number(availability[1]) })));
    }
    if (path === "/api/employment-contract-settings") return json({ automaticWeekdayAvailability: false });
    if (path.endsWith("highlight-setting")) return json({ highlightColor: "#DBEAFE" });
    if (path.includes("notepad")) return json({ notes: [], state: { isExpanded: false, isPinned: false, height: 700 } });
    const match = path.match(/^\/api\/containers\/7\/graphs\/(\d+)(.*)$/);
    if (!match) return json([]);
    const id = Number(match[1]);
    const state = states.get(id)!;
    const resource = match[2];
    const resourceRead = method === "GET" && ["", "/employees", "/slots", "/cell-styles"].includes(resource);
    if (resourceRead) activeResourceReads++;
    const tracked = recording;
    if (tracked) peakConcurrency = Math.max(peakConcurrency, ++activeRequests);
    try {
      if (id === 39 && state.versionCount === 1 && resourceRead && resource === "/slots" && committedReadbackOutage)
        return await route.abort("connectionreset");
      if (pause?.id === id && pause.method === method && (method === "GET" ? resource === "" : resource === "/cell-styles/batch")) {
        paused = true; await new Promise<void>(resolve => { release = resolve; }); pause = undefined;
      }
      if (resourceRead && id === 37 && resource === "/slots" && verifyOutage) {
        if (recording) rateLimits++;
        return await route.fulfill({ status: 429, headers: { "Retry-After": "1" }, contentType: "text/html", body: "<html>Too many requests</html>" });
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
        // C# parity is exercised separately against SQLite; this HTTP fixture must also renumber.
        const groups = new Map<string, typeof state.slots>();
        for (const slot of state.slots) {
          const key = `${slot.dayOfMonth}/${slot.fromTime}/${slot.toTime}`;
          groups.set(key, [...groups.get(key) ?? [], slot]);
        }
        state.slots = [...groups.values()].flatMap(rows => rows.sort((a, b) => a.slotNo - b.slotNo ||
          (a.employeeId ?? Infinity) - (b.employeeId ?? Infinity)).map((slot, i) => ({ ...slot, slotNo: i + 1, status: slot.employeeId ? 1 : 0 })));
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
        const baseline = options.incident ? id === 36 ? 4 : id === 39 ? 20 : 1 : 0;
        return await json({ currentVersionId: baseline + state.versionCount || null,
          versions: Array.from({ length: baseline + state.versionCount }, (_, i) => ({ id: i + 1, versionNumber: i + 1,
            scheduleId: id, createdAtUtc: "2026-11-09T10:00:00Z" })) });
      }
      if (resource === "/slots") return await json(state.slots);
      if (resource === "/cell-styles") return await json([...state.styles.values()]);
      if (resource === "/employees") return await json((options.incident?.find(graph => graph.graphId === id)?.after.employeeOrder ?? options.availabilityRosters?.[id] ?? roster)
        .map((employeeId, displayOrder) => ({ id: employeeId, employeeId, displayOrder, scheduleId: id, isPriority: false })));
      return await json(resource === "" ? state.graph : []);
    } finally {
      if (tracked) activeRequests--;
      if (resourceRead) {
        activeResourceReads--;
        lastResourceRead = Date.now();
      }
    }
  });
  return {
    states, traffic, burst,
    pause: (id: number, method: string) => { pause = { id, method }; paused = false; },
    paused: () => paused,
    release: () => release?.(),
    metrics: () => {
      const save = traffic.filter(row => row.path.startsWith(graphRoot + "/"));
      return {
        GET: save.filter(row => row.method === "GET").length,
        PUT: save.filter(row => row.method === "PUT").length,
        POST: save.filter(row => row.method === "POST").length,
        peakStartsPerSecond: Math.max(0, ...save.map(row => save.filter(other => other.time >= row.time && other.time < row.time + 1000).length)),
        peakConcurrency, rateLimits,
        dirtyGraphs: [...states.values()].filter(state => state.versionCount > 0).length,
        styles: [...states.values()].reduce((count, state) => count + state.styles.size, 0),
        versions: [...states.values()].reduce((count, state) => count + state.versionCount, 0),
      };
    },
    connected: () => Boolean(socket),
    resourceReadsSettled: () => activeResourceReads === 0 && Date.now() - lastResourceRead >= 300,
    startRecording: async () => {
      traffic.length = 0; peakConcurrency = 0; rateLimits = 0;
      tokens = 60; tokenTime = windowTime = Date.now(); windowRequests = 0; recording = true;
      await page.evaluate(() => Object.assign((window as unknown as { __gf3SaveHttp: object }).__gf3SaveHttp,
        { recording: true, active: 0, peak: 0 }));
    },
    httpMetrics: () => page.evaluate(() => {
      return (window as unknown as { __gf3SaveHttp: { peak: number } }).__gf3SaveHttp.peak;
    }),
    failBackground: () => { backgroundOutage = true; burst(42); },
    backgroundThrottleCount: () => backgroundThrottleCount,
    recover: () => { verifyOutage = false; backgroundOutage = false; committedReadbackOutage = false; },
  };
}

async function editFive(page: Page, paintMany: boolean) {
  await page.goto(editorUrl(ids[0]));
  const collapseSidebar = page.getByRole("button", { name: "Collapse sidebar", exact: true });
  if (await collapseSidebar.isVisible()) await collapseSidebar.click();
  for (const [index, id] of ids.entries()) {
    if (index) {
      await page.getByRole("tab", { name: "Graph " + id }).click();
      await expect(page).toHaveURL(url => url.pathname.endsWith("/" + id + "/edit"));
    }
    const first = scheduleCell(page, "1:1");
    await expect(first).toBeVisible({ timeout: 30000 });
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
    await editScheduleCell(page, "1:11", "Training");
    if (index === 0 || index === 4) await editScheduleCell(page, "1:12", "09:00 - 12:00");
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

async function editScheduleCell(page: Page, key: string, value: string) {
  const cell = scheduleCell(page, key);
  await cell.getByRole("button").dblclick();
  await cell.getByRole("textbox").fill(value);
  await cell.getByRole("textbox").press("Enter");
}

const tabGraphIds = [42, 35, 31];
const tabRosters = { 42: [1, 2, 3], 35: [2, 4], 31: [3, 5] };
const tabEditorUrl = (id: number) => `/container/7/graphs/${id}/edit?openGraphIds=${tabGraphIds.join(",")}`;
const graphTab = (page: Page, id: number) => page.getByRole("tab", { name: "Graph " + id, exact: true });
const matrixState = (page: Page) => matrix(page).locator("[data-cell-key]").evaluateAll(elements =>
  elements.map(element => [element.getAttribute("data-cell-key"), element.querySelector("button")?.textContent, element.getAttribute("style")]));

async function openTabFixture(page: Page, baseURL: string, options: { unlinked?: boolean; period?: { year: number; month: number } } = {}) {
  await page.setViewportSize({ width: 1600, height: 960 });
  const fixture = await workspace(page, baseURL, false, { graphIds: tabGraphIds, availabilityRosters: tabRosters, throttle: false, ...options });
  for (const [id, state] of fixture.states) {
    state.slots = tabRosters[id as keyof typeof tabRosters].map((employeeId, i) => ({
      id: i + 1, scheduleId: id, employeeId, dayOfMonth: 12, slotNo: i + 1, status: 1,
      fromTime: "08:00", toTime: "12:00",
    }));
  }
  const go = async (id: number) => {
    await graphTab(page, id).click();
    await expect(page).toHaveURL(url => url.pathname.endsWith("/" + id + "/edit"));
    await expect(matrix(page)).toBeVisible();
    await expect.poll(fixture.resourceReadsSettled).toBe(true);
  };
  return { ...fixture, go };
}

for (const throttle of [false, true]) test(`editor tab round trips preserve a text draft (429=${throttle})`, async ({ page, baseURL }, testInfo) => {
  desktopOnly(testInfo.project.name);
  const fixture = await openTabFixture(page, baseURL!);
  const employeeReads: number[] = [];
  if (throttle) await page.route("**/api/containers/7/graphs/35/employees", async route => {
    employeeReads.push(Date.now());
    if (employeeReads.length > 1) return route.fallback();
    await route.fulfill({ status: 429, headers: { "Retry-After": "1" }, json: { title: "Too many requests" } });
  });
  const { go } = fixture;
  await page.goto(tabEditorUrl(42));
  await expect(scheduleCell(page, "1:12")).toContainText("08:00 - 12:00");
  await expect(scheduleCell(page, "2:12")).toContainText("Graph 35");
  await expect(scheduleCell(page, "3:12")).toContainText("Graph 31");
  const clean = await matrixState(page);
  await fixture.startRecording();
  await go(35);
  await go(42);
  expect(await matrixState(page)).toEqual(clean);
  const textCell = scheduleCell(page, "1:11");
  await editScheduleCell(page, "1:11", "Training");
  await expect(textCell).toContainText("Training");
  const dirty = await matrixState(page);
  await go(35);
  await go(31);
  await go(42);
  const reads = fixture.traffic.filter(row => row.method === "GET");
  const shared = reads.filter(row => ["/api/employees", "/api/shops", "/api/availability-groups"].includes(row.path));
  console.log(`tab traffic (429=${throttle}): ${reads.length} GETs; ${shared.length} shared GETs`);
  expect(await matrixState(page)).toEqual(dirty);
  expect(shared).toHaveLength(0);
  if (throttle) {
    expect(employeeReads).toHaveLength(2);
    expect(employeeReads[1] - employeeReads[0]).toBeGreaterThanOrEqual(1_000);
  }
});

for (const { year, month, today, unlinked } of [
  { year: 2026, month: 10, today: "2026-11-26" },
  { year: 2026, month: 11, today: "2026-11-26" },
  { year: 2026, month: 12, today: "2026-11-26" },
  { year: 2026, month: 12, today: "2026-12-15" },
  { year: 2026, month: 12, today: "2027-01-15" },
  { year: 2027, month: 1, today: "2026-11-26" },
  { year: 2026, month: 12, today: "2026-11-26", unlinked: true },
]) test(`editor tab round trips preserve linked rosters for ${year}-${month} on ${today} (unlinked=${Boolean(unlinked)})`, async ({ page, baseURL }, testInfo) => {
  desktopOnly(testInfo.project.name);
  await page.clock.setFixedTime(new Date(today + "T12:00:00Z"));
  const fixture = await openTabFixture(page, baseURL!, { period: { year, month }, unlinked });
  const original = fixture.states.get(42)!;
  const lastDay = new Date(Date.UTC(year, month, 0)).getUTCDate();
  const metadata = { m: [[1, "Manual demand", { [lastDay]: "10:00 - 14:00" }]], o: [3, -1, 1, 2], t: [[2, lastDay, "Induction"]] };
  original.graph.note = "Fixture metadata\n\n[[GF3_GRAPH_META:b64:" + Buffer.from(JSON.stringify(metadata)).toString("base64") + "]]";
  original.styles.set("1:12", { id: 1, scheduleId: 42, employeeId: 1, dayOfMonth: 12, backgroundColorArgb: -65536 });
  original.slots.push({ id: 4, scheduleId: 42, employeeId: 3, dayOfMonth: lastDay, slotNo: 1, status: 1, fromTime: "09:00", toTime: "15:00" });
  await page.goto(tabEditorUrl(42));
  await expect(scheduleCell(page, `3:${lastDay}`)).toContainText("09:00 - 15:00");
  await expect(scheduleCell(page, `-1:${lastDay}`)).toContainText("10:00 - 14:00");
  await expect(scheduleCell(page, `2:${lastDay}`)).toContainText("Induction");
  await editScheduleCell(page, "1:11", "Training");
  const before = await matrixState(page);
  await fixture.go(35);
  await fixture.go(42);
  expect(await matrixState(page)).toEqual(before);
});

for (const failure of ["500", "timeout", "canceled"] as const) test(`editor tab keeps its draft during a ${failure} on another graph`, async ({ page, baseURL }, testInfo) => {
  desktopOnly(testInfo.project.name);
  const fixture = await openTabFixture(page, baseURL!);
  const resource = "**/api/containers/7/graphs/35/employees";
  let attempts = 0;
  let release!: () => void;
  const delayedResponse = new Promise<void>(resolve => { release = resolve; });
  await page.route(resource, async route => {
    attempts++;
    if (failure === "500") return route.fulfill({ status: 500, json: { title: "Server error" } });
    if (failure === "timeout") return route.abort("timedout");
    await delayedResponse;
    await route.fallback().catch(() => {});
  });
  await page.goto(tabEditorUrl(42));
  await expect(scheduleCell(page, "1:12")).toBeVisible();
  await editScheduleCell(page, "1:11", "Training");
  const original = await matrixState(page);
  await fixture.startRecording();
  await graphTab(page, 35).click();
  await expect(page.getByRole("button", { name: "Save", exact: true })).toBeDisabled();
  if (failure !== "canceled") {
    await expect(page.getByText("Could not load this schedule editor.", { exact: true })).toBeVisible();
    expect(attempts).toBe(2);
    await fixture.go(42);
  } else {
    await expect.poll(() => attempts).toBe(1);
    await fixture.go(31);
    await fixture.go(42);
    release();
  }
  expect(await matrixState(page)).toEqual(original);
  expect(fixture.traffic.every(row => row.method === "GET" || row.path.endsWith("/presence/negotiate"))).toBe(true);
});

test("50 switches and realtime refresh preserve text, time and color drafts through one Save", async ({ page, baseURL }, testInfo) => {
  desktopOnly(testInfo.project.name);
  test.setTimeout(65_000);
  await page.clock.setFixedTime(new Date("2026-11-26T12:00:00Z"));
  const fixture = await openTabFixture(page, baseURL!);
  await page.goto(tabEditorUrl(42));
  await expect.poll(fixture.connected).toBe(true);
  await editScheduleCell(page, "1:11", "Training");
  const first = await matrixState(page);
  await fixture.go(35);
  await editScheduleCell(page, "4:12", "09:00 - 12:00");
  const second = await matrixState(page);
  await fixture.go(31);
  await matrix(page).locator('[data-matrix-target="day"][data-day-of-month="1"]').click();
  await page.getByRole("button", { name: /^Fill #/ }).click();
  const third = await matrixState(page);
  const expected = new Map([[42, first], [35, second], [31, third]]);
  await fixture.startRecording();
  for (let index = 0; index < 50; index++) {
    const id = tabGraphIds[index % tabGraphIds.length];
    await graphTab(page, id).dispatchEvent("click");
    await expect(page).toHaveURL(url => url.pathname.endsWith("/" + id + "/edit"));
    await expect(matrix(page)).toBeVisible();
  }
  expect(fixture.traffic.filter(row => !row.path.endsWith("/shift-corrections"))).toHaveLength(0);
  expect(fixture.traffic.length).toBeLessThanOrEqual(tabGraphIds.length);
  fixture.burst(35);
  for (const [id, state] of expected) {
    if (!page.url().includes(`/graphs/${id}/edit`)) await fixture.go(id);
    expect(await matrixState(page)).toEqual(state);
  }
  await save(page);
  await expect(page).toHaveURL(url => !url.pathname.endsWith("/edit"), { timeout: 30_000 });
  expect([...fixture.states.values()].map(state => state.versionCount)).toEqual([1, 1, 1]);
  expect(fixture.states.get(42)?.slots).toHaveLength(3);
  expect(fixture.states.get(42)?.graph.note).toContain("GF3_GRAPH_META");
  expect(fixture.states.get(35)?.slots).toEqual(expect.arrayContaining([
    expect.objectContaining({ employeeId: 4, fromTime: "09:00", toTime: "12:00" }),
    expect.objectContaining({ employeeId: 2, fromTime: "08:00", toTime: "12:00" }),
  ]));
  expect(fixture.states.get(31)?.styles.size).toBeGreaterThan(0);
  await page.goto(tabEditorUrl(42));
  await expect(scheduleCell(page, "1:11")).toContainText("Training");
  await expect(scheduleCell(page, "2:12")).toContainText("08:00 - 12:00");
  await fixture.go(35);
  await expect(scheduleCell(page, "4:12")).toContainText("09:00 - 12:00");
});

for (const loseCommittedReadback of [false, true]) test(`seven-graph slot normalization incident completes all tabs (retry=${loseCommittedReadback})`, async ({ page, baseURL }, testInfo) => {
  desktopOnly(testInfo.project.name);
  test.setTimeout(180_000);
  const seven = [36, 39, 38, 34, 37, 40, 41];
  const privateGraphs = process.env.GF3_SAVE_PRIVATE_FIXTURE
    ? (JSON.parse(readFileSync(process.env.GF3_SAVE_PRIVATE_FIXTURE, "utf8")) as { graphs: IncidentGraph[] }).graphs : [];
  const roster = [...new Set([...Array.from({ length: 17 }, (_, i) => i + 1), ...privateGraphs.flatMap(graph => graph.after.employeeOrder)])];
  const incident = seven.map(graphId => {
    const existing = privateGraphs.find(graph => graph.graphId === graphId);
    if (existing) return existing;
    const count = graphId === 36 ? 91 : graphId === 39 ? 201 : 20;
    const slots: FixtureSlot[] = Array.from({ length: count }, (_, i) => ({ employeeId: i % 17 + 1,
      dayOfMonth: Math.floor(i / 17) + 1, slotNo: i % 17 + 3, status: 1, fromTime: "08:00", toTime: "12:00" }));
    const added = [{ employeeId: 1, dayOfMonth: 20, slotNo: 1, status: 1, fromTime: "09:00", toTime: "15:00" },
      { employeeId: 2, dayOfMonth: 21, slotNo: 1, status: 1, fromTime: "09:00", toTime: "15:00" }];
    return { graphId, before: { slots, employeeOrder: roster }, after: { slots: [...slots, ...added], employeeOrder: roster } };
  });
  await page.setViewportSize({ width: 1600, height: 960 });
  const fixture = await workspace(page, baseURL!, false, { graphIds: seven, roster, incident, loseCommittedReadback, throttle: false });
  await page.goto(`/container/7/graphs/36/edit?openGraphIds=${seven.join(",")}`);
  const collapseSidebar = page.getByRole("button", { name: "Collapse sidebar", exact: true });
  if (await collapseSidebar.isVisible()) await collapseSidebar.click();
  const cells = (slots: FixtureSlot[]) => {
    const map = new Map<string, string[]>();
    for (const slot of slots) if (slot.employeeId) {
      const key = `${slot.employeeId}:${slot.dayOfMonth}`;
      map.set(key, [...map.get(key) ?? [], `${slot.fromTime} - ${slot.toTime}`]);
    }
    return new Map([...map].map(([key, values]) => [key, values.sort().join(", ")]));
  };
  for (const [index, graph] of incident.entries()) {
    if (index) await page.getByRole("tab", { name: `Graph ${graph.graphId}`, exact: true }).click();
    const before = cells(graph.before.slots), after = cells(graph.after.slots);
    for (const key of new Set([...before.keys(), ...after.keys()])) {
      if (before.get(key) === after.get(key)) continue;
      const cell = scheduleCell(page, key);
      await cell.getByRole("button").dblclick();
      await cell.getByRole("textbox").fill(after.get(key) ?? "");
      await cell.getByRole("textbox").press("Enter");
    }
    await matrix(page).locator('[data-matrix-target="day"][data-day-of-month="1"]').click();
    await page.getByRole("button", { name: /^Fill #/ }).click();
  }
  await expect.poll(fixture.resourceReadsSettled).toBe(true);
  await fixture.startRecording(); await save(page);
  if (loseCommittedReadback) {
    await expect(page.getByRole("button", { name: "Try again", exact: true })).toBeVisible({ timeout: 60000 });
    expect([...fixture.states.values()].filter(state => state.versionCount)).toHaveLength(2);
    fixture.recover(); await page.getByRole("button", { name: "Try again", exact: true }).click();
  }
  await expect(page).toHaveURL(url => url.pathname === "/container/7/graphs/41", { timeout: 90000 });
  const writes = fixture.traffic.filter(row => row.method !== "GET");
  for (const resource of ["/slots", "/cell-styles/batch", "/versions"])
    expect(writes.filter(row => row.path.endsWith(resource)).map(row => Number(row.path.split("/")[5]))).toEqual(seven);
  expect(fixture.states.get(36)!.slots).toHaveLength(93);
  expect(fixture.states.get(39)!.slots).toHaveLength(203);
  expect(fixture.states.get(39)!.versionCount).toBe(1);
  await testInfo.attach("seven-graph-save-traffic", { body: JSON.stringify(fixture.metrics()), contentType: "application/json" });
  await page.goto(`/container/7/graphs/39/edit?openGraphIds=${seven.join(",")}`);
  if (await collapseSidebar.isVisible()) await collapseSidebar.click();
  await expect(matrix(page)).toBeVisible();
  await expect(page.getByRole("button", { name: "Restore draft" })).toHaveCount(0);
  // A second edit after reopening must save normally and leave no stale checkpoint/inert UI.
  await matrix(page).locator('[data-matrix-target="day"][data-day-of-month="2"]').click();
  await page.getByRole("button", { name: /^Text #/ }).click();
  await save(page);
  await expect(page).toHaveURL(url => url.pathname === "/container/7/graphs/39", { timeout: 60000 });
  expect(fixture.states.get(39)!.versionCount).toBe(2);
});

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
    await fixture.startRecording();
  });
  await expect(page.getByRole("dialog").getByRole("button", { name: "Saving...", exact: true })).toBeDisabled();
  await expect(oldSummary(page)).toHaveCount(0);
  await expect(recovery(page)).toHaveCount(0);
  const during = await matrix(page).boundingBox();
  expect(Math.abs(during!.y - before!.y)).toBeLessThan(2);
  await expect(page).toHaveURL(url => url.pathname === "/container/7/graphs/42", { timeout: 60000 });
  await expect(page.getByText("All schedules saved", { exact: true })).toHaveCount(0);
  const writes = fixture.traffic.filter(entry => entry.path.startsWith(graphRoot + "/") && entry.method !== "GET");
  expect(writes.filter(entry => entry.path.endsWith("/cell-styles/batch")).map(entry => entry.path)).toEqual(ids.map(id => graphRoot + "/" + id + "/cell-styles/batch"));
  expect(writes.filter(entry => /\/cell-styles(?:\/\d+)?$/.test(entry.path))).toHaveLength(0);
  expect(writes.filter(entry => entry.path.endsWith("/versions"))).toHaveLength(5);
  expect(writes.filter(entry => entry.path.endsWith("/shift-swaps/manual"))).toHaveLength(0);
  const preflight = fixture.traffic.slice(0, fixture.traffic.indexOf(writes[0]));
  for (const id of ids) {
    for (const resource of ["", "/employees", "/cell-styles"]) {
      expect(preflight.filter(entry => entry.path === graphRoot + "/" + id + resource)).toHaveLength(1);
    }
    // The editor's existing related-roster batch may already be reading slots when Save begins.
    expect(preflight.filter(entry => entry.path === graphRoot + "/" + id + "/slots").length).toBeGreaterThanOrEqual(id === 37 ? 2 : 1);
  }
  const retried = preflight.filter(entry => entry.path === graphRoot + "/37/slots");
  expect(retried[1].time - retried[0].time).toBeGreaterThanOrEqual(950);
  for (const state of fixture.states.values()) {
    expect(state.styles.size).toBeGreaterThanOrEqual(50);
    expect(state.versionCount).toBe(1);
  }
  expect([...fixture.states.values()].reduce((total, state) => total + state.styles.size, 0)).toBe(256);
  for (const id of [34, 42]) expect(fixture.states.get(id)?.slots.find(slot => slot.dayOfMonth === 12)?.fromTime).toBe("09:00");
  expect(await page.evaluate(() => sessionStorage.getItem("gf3:graph-drafts:9:7"))).toBeNull();
  await testInfo.attach("five-graph-save-traffic", { body: JSON.stringify({ ...fixture.metrics(),
    peakConcurrency: await fixture.httpMetrics() }), contentType: "application/json" });
});

test("lost batch and HTML 429 verification keep edits usable and retry only missing work", async ({ page, baseURL }, testInfo) => {
  desktopOnly(testInfo.project.name); test.setTimeout(150000);
  await page.setViewportSize({ width: 1450, height: 960 });
  const fixture = await workspace(page, baseURL!, true);
  await editFive(page, false);
  await expect.poll(fixture.resourceReadsSettled).toBe(true);
  await fixture.startRecording(); await save(page);
  const dialog = page.getByRole("dialog", { name: "Save schedule session" });
  await expect(dialog.getByRole("button", { name: "Try again" })).toBeVisible({ timeout: 60000 });
  await expect(recovery(page)).toHaveCount(0);
  await expect(oldSummary(page)).toHaveCount(0);
  expect(fixture.traffic.filter(entry => entry.method === "POST" && entry.path.endsWith("/versions"))).toHaveLength(2);
  await dialog.getByRole("button", { name: "Return to editor" }).click();
  await expect(scheduleCell(page, "1:11")).toContainText("Training");
  expect(await page.evaluate(() => sessionStorage.getItem("gf3:graph-drafts:9:7"))).toBeNull();
  fixture.recover(); await save(page);
  await expect(page).toHaveURL(url => url.pathname === "/container/7/graphs/42", { timeout: 60000 });
  const batches = fixture.traffic.filter(entry => entry.method === "PUT" && entry.path.endsWith("/cell-styles/batch"));
  expect(batches.map(entry => entry.path)).toEqual(ids.map(id => graphRoot + "/" + id + "/cell-styles/batch"));
  expect(fixture.traffic.filter(entry => entry.method === "POST" && entry.path.endsWith("/versions"))).toHaveLength(5);
  await testInfo.attach("save-retry-traffic", { body: JSON.stringify(fixture.metrics()), contentType: "application/json" });
});

test("the same Save dialog fits a narrow viewport during progress and safe Cancel", async ({ page, baseURL }, testInfo) => {
  test.skip(!["mobile-chrome", "webkit-iphone-x"].includes(testInfo.project.name), "This checks the narrow touch viewport.");
  const fixture = await workspace(page, baseURL!, false, { throttle: false });
  await page.goto(editorUrl(42));
  await expect(matrix(page)).toBeVisible();
  const sidebar = page.getByRole("button", { name: "Collapse sidebar", exact: true });
  if (await sidebar.isVisible()) await sidebar.click();
  await matrix(page).locator('[data-matrix-target="day"][data-day-of-month="1"]').click();
  await page.getByRole("button", { name: /^Fill #/ }).click();
  const before = await matrix(page).boundingBox();
  fixture.pause(42, "GET"); await save(page);
  await expect.poll(fixture.paused).toBe(true);
  const dialog = page.getByRole("dialog", { name: /Save schedule/ });
  await expect(dialog.getByRole("progressbar", { name: "Verified save progress" })).toBeVisible();
  const box = await dialog.locator('[class*="_dialog_"]').boundingBox();
  const viewport = page.viewportSize()!;
  expect(box!.x).toBeGreaterThanOrEqual(0);
  expect(box!.x + box!.width).toBeLessThanOrEqual(viewport.width);
  expect(box!.height).toBeLessThan(viewport.height);
  expect(Math.abs((await matrix(page).boundingBox())!.y - before!.y)).toBeLessThan(2);
  await dialog.getByRole("button", { name: "Cancel saving" }).click(); fixture.release();
  await expect(dialog).toHaveCount(0);
  await expect(scheduleCell(page, "1:1")).toBeVisible();
  expect([...fixture.states.values()].every(state => state.versionCount === 0)).toBe(true);
  await expect(page.getByRole("button", { name: "Save", exact: true })).toBeEnabled();
});

test("ten graphs save 5000 styled cells with bounded traffic and verified reload", async ({ page, baseURL }, testInfo) => {
  desktopOnly(testInfo.project.name); test.setTimeout(240000);
  const ten = [34, 36, 37, 38, 42, 43, 44, 45, 46, 47];
  const fixture = await workspace(page, baseURL!, false, { graphIds: ten, roster: Array.from({ length: 20 }, (_, i) => i + 1), throttle: false, enforceLimits: true });
  await page.setViewportSize({ width: 1600, height: 1000 });
  await page.goto(`/container/7/graphs/34/edit?openGraphIds=${ten.join(",")}`);
  const collapseSidebar = page.getByRole("button", { name: "Collapse sidebar", exact: true });
  if (await collapseSidebar.isVisible()) await collapseSidebar.click();
  for (const [index, id] of ten.entries()) {
    if (index) await page.getByRole("tab", { name: `Graph ${id}`, exact: true }).click();
    await expect(scheduleCell(page, "1:1")).toBeVisible({ timeout: 30000 });
    await matrix(page).locator('[data-matrix-target="day"][data-day-of-month="1"]').click();
    await matrix(page).locator('[data-matrix-target="day"][data-day-of-month="25"]').click({ modifiers: ["Shift"] });
    await page.getByRole("button", { name: /^Fill #/ }).click();
    await page.getByRole("button", { name: /^Text #/ }).click();
    for (const [key, value] of [["1:11", "Training"], ["1:12", "09:00 - 12:00"]]) {
      const cell = scheduleCell(page, key); await cell.getByRole("button").dblclick();
      await cell.getByRole("textbox").fill(value); await cell.getByRole("textbox").press("Enter");
    }
  }
  await expect.poll(fixture.resourceReadsSettled).toBe(true);
  await fixture.startRecording(); await save(page);
  await expect(page.getByRole("progressbar", { name: "Verified save progress" })).toBeVisible();
  await expect(page).toHaveURL(url => url.pathname === "/container/7/graphs/47", { timeout: 90000 });
  const metrics = { ...fixture.metrics(), peakRouteHandlers: fixture.metrics().peakConcurrency,
    peakConcurrency: await fixture.httpMetrics() };
  await testInfo.attach("ten-graph-save-traffic", { body: JSON.stringify({ ...metrics, cellsChanged: 5000, slotSets: 10,
    baselineBatchWrites: 10, actualBatchWrites: 10 }), contentType: "application/json" });
  expect(metrics).toMatchObject({ PUT: 30, POST: 10, rateLimits: 0, dirtyGraphs: 10, styles: 5000, versions: 10 });
  expect(metrics.GET).toBeGreaterThanOrEqual(90);
  // These totals also include ordinary related-roster queries. The Save-only two-request
  // cap and 350ms spacing are tested with a deterministic clock in graphSavePacer.test.ts;
  // this fixture enforces the configured rate budgets across all HTTP activity.
  // The configured authenticated budget is 240/min; the initial Nginx burst allowance is 60.
  expect(metrics.GET + metrics.PUT + metrics.POST).toBeLessThan(240);
  expect(metrics.peakStartsPerSecond).toBeLessThan(60);
  for (const state of fixture.states.values()) {
    expect(state.styles.size).toBe(500); expect(state.versionCount).toBe(1);
    expect(state.slots.find(slot => slot.dayOfMonth === 12)?.fromTime).toBe("09:00");
    const encoded = /\[\[GF3_GRAPH_META:b64:([\s\S]*?)\]\]$/.exec(state.graph.note)?.[1];
    expect(encoded).toBeTruthy();
    const metadata = JSON.parse(Buffer.from(encoded!, "base64url").toString("utf8"));
    expect(metadata.t).toContainEqual([1, 11, "Training"]);
  }
  await page.goto(`/container/7/graphs/47/edit?openGraphIds=${ten.join(",")}`);
  await expect(scheduleCell(page, "1:11")).toContainText("Training");
  await expect(scheduleCell(page, "1:12")).toContainText("09:00");
  await expect(page.getByRole("button", { name: "Restore draft" })).toHaveCount(0);
});
