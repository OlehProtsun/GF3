import { spawn, type ChildProcess } from "node:child_process";
import { once } from "node:events";
import { mkdtemp, rm } from "node:fs/promises";
import { tmpdir } from "node:os";
import { join, resolve } from "node:path";
import { expect, request, test, type APIRequestContext, type APIResponse, type Page } from "@playwright/test";
import type { AvailabilityGroup, AvailabilitySlot } from "../src/entities/availability-groups/model/types";
import type { SaveAvailabilityGroupGraphDto, SaveAvailabilityGroupGraphResult } from "../src/entities/availability-groups/api/dto";

const root = "/api/availability-groups";
const saveGraphPath = root + "/save-graph";
const employees = Array.from({ length: 5 }, (_, index) => `AvailabilityFixture${index + 1}`);
const employeeName = (index: number) => `${employees[index]} Worker`;

let backend: ChildProcess | undefined;
let directory: string | undefined;
let backendUrl: string;
let api: APIRequestContext;
let token: string;
let employeeIds: number[];

test.setTimeout(60_000);

async function read<T>(response: APIResponse): Promise<T> {
  if (!response.ok()) throw new Error(`HTTP ${response.status()} for ${new URL(response.url()).pathname}: ${await response.text()}`);
  return response.json() as Promise<T>;
}

test.beforeAll(async () => {
  directory = await mkdtemp(join(tmpdir(), "gf3-availability-e2e-"));
  const apiProject = resolve("../GF3.WebApi");
  const password = "263819";
  backend = spawn("dotnet", [join(apiProject, "bin", "Debug", "net10.0", "WebApi.dll"), "--urls", "http://127.0.0.1:0"], {
    cwd: apiProject,
    env: {
      ...process.env,
      ASPNETCORE_ENVIRONMENT: "Testing",
      DOTNET_ENVIRONMENT: "Testing",
      ConnectionStrings__Default: `Data Source=${join(directory, "availability.db")}`,
      DatabaseStartup__RequireExistingDatabase: "false",
      GF3_BOOTSTRAP_MANAGER_PASSWORD: password,
      Jwt__SigningKey: "synthetic-availability-browser-fixture-only-signing-key",
      Logging__LogLevel__Default: "Warning",
      "Logging__LogLevel__Microsoft.Hosting.Lifetime": "Information",
    },
    windowsHide: true,
  });
  const child = backend;
  backendUrl = await new Promise<string>((ready, reject) => {
    let output = "";
    const deadline = setTimeout(() => reject(new Error(`Local Web API did not start:\n${output}`)), 25_000);
    child.on("error", error => { clearTimeout(deadline); reject(error); });
    child.on("exit", code => { clearTimeout(deadline); reject(new Error(`Local Web API exited with ${code}:\n${output}`)); });
    const collect = (chunk: Buffer) => {
      output = (output + chunk.toString()).slice(-6000);
      const match = /Now listening on: (http:\/\/127\.0\.0\.1:\d+)/.exec(output);
      if (match) {
        clearTimeout(deadline);
        ready(match[1]);
      }
    };
    child.stdout?.on("data", collect);
    child.stderr?.on("data", collect);
  });
  const auth = await request.newContext({ baseURL: backendUrl });
  try {
    const login = await read<{ accessToken: string }>(await auth.post("/api/auth/login", { data: { username: "manager", password } }));
    const pc = await read<{ accessToken: string }>(await auth.post("/api/auth/manager-mode", {
      headers: { Authorization: `Bearer ${login.accessToken}` }, data: { mode: "pc" },
    }));
    token = pc.accessToken;
  } finally {
    await auth.dispose();
  }
  api = await request.newContext({ baseURL: backendUrl, extraHTTPHeaders: { Authorization: `Bearer ${token}` } });
  await read(await api.put("/api/account-language", { data: { language: "en" } }));
  employeeIds = [];
  for (const firstName of employees) {
    const employee = await read<{ id: number }>(await api.post("/api/employees", { data: { firstName, lastName: "Worker" } }));
    employeeIds.push(employee.id);
  }
});

test.afterAll(async () => {
  await api?.dispose();
  if (backend?.pid && backend.exitCode === null && backend.signalCode === null) {
    const exited = once(backend, "exit");
    backend.kill();
    await exited;
  }
  if (directory) await rm(directory, { recursive: true, force: true, maxRetries: 3 });
});

type Failure = "rate-limit" | "lost-response";
type Mutation = { method: string; path: string; payload: SaveAvailabilityGroupGraphDto; status?: number; result?: SaveAvailabilityGroupGraphResult };

async function recordSaves(page: Page, baseURL: string) {
  const frontend = new URL(baseURL);
  if (!["localhost", "127.0.0.1", "[::1]"].includes(frontend.hostname)) {
    throw new Error("Availability save tests require a local frontend.");
  }

  let nextFailure: Failure | undefined;
  const writes: Mutation[] = [];
  await page.addInitScript(accessToken => localStorage.setItem("gf3.auth.access-token", accessToken), token);
  await page.routeWebSocket("**/api/**", socket => {
    const url = new URL(socket.url());
    url.host = new URL(backendUrl).host;
    url.protocol = "ws:";
    const server = new WebSocket(url);
    const queued: (string | Buffer)[] = [];
    socket.onMessage(message => {
      if (server.readyState === WebSocket.OPEN) server.send(message);
      else queued.push(message);
    });
    socket.onClose(() => server.close());
    server.addEventListener("open", () => queued.splice(0).forEach(message => server.send(message)));
    server.addEventListener("message", event => socket.send(String(event.data)));
    server.addEventListener("close", () => socket.close());
    server.addEventListener("error", () => socket.close());
  });

  await page.route(url => url.origin === frontend.origin && url.pathname.startsWith("/api/"), async route => {
    const { pathname, search } = new URL(route.request().url());
    const method = route.request().method();
    const write = pathname.startsWith(root) && ["POST", "PUT", "DELETE"].includes(method)
      ? { method, path: pathname, payload: route.request().postDataJSON() } as Mutation : undefined;
    if (write) writes.push(write);
    const failure = write ? nextFailure : undefined;
    if (write) nextFailure = undefined;
    if (failure === "rate-limit") {
      write!.status = 429;
      await route.fulfill({ status: 429, headers: { "Retry-After": "1" }, json: { status: 429, title: "Rate limited" } });
      return;
    }

    const response = await route.fetch({ url: backendUrl + pathname + search, maxRetries: 0 });
    if (write) {
      write.status = response.status();
      if (pathname === saveGraphPath && response.ok()) write.result = await response.json();
    }
    if (failure === "lost-response") await route.abort("failed");
    else await route.fulfill({ response });
  });

  return { writes, failNext: (failure: Failure) => { nextFailure = failure; } };
}

const list = async () => read<AvailabilityGroup[]>(await api.get(root));
const slots = async (id: number) => read<AvailabilitySlot[]>(await api.get(`${root}/${id}/slots`));
const saveButton = (page: Page) => page.getByRole("button", { name: "Save Changes", exact: true });
const nameField = (page: Page) => page.getByPlaceholder("Write availability name...");
const dayInput = (page: Page, day: number) => page.getByRole("textbox", { name: `${employeeName(0)} day ${day}`, exact: true });

async function createDraft(page: Page, name: string, count: number) {
  await page.goto("/availability/new");
  await nameField(page).fill(name);
  const month = page.getByLabel("availability month", { exact: true });
  await month.fill("12");
  await month.press("Tab");
  for (const firstName of employees.slice(0, count)) {
    await page.getByRole("button", { name: "employee list", exact: true }).press("Space");
    await page.getByRole("option", { name: new RegExp(firstName) }).click();
    await page.getByRole("button", { name: "Add", exact: true }).first().press("Enter");
  }
}

async function expectSaved(page: Page, writes: Mutation[]) {
  await expect(page).toHaveURL(/\/availability\/\d+$/);
  const id = Number(new URL(page.url()).pathname.split("/").at(-1));
  expect(writes.map(({ method, path, status }) => ({ method, path, status }))).toEqual([
    { method: "POST", path: saveGraphPath, status: 200 },
  ]);
  expect(writes[0].result?.id).toBe(id);
  return id;
}

test.describe("manager availability saves", () => {
  test.skip(({ isMobile }) => isMobile, "The manager availability editor is a PC workspace.");

  test("one write for one and five employees, double click, edits and CFA", async ({ page, baseURL }, testInfo) => {
    await page.setViewportSize({ width: 1500, height: 1000 });
    const { writes } = await recordSaves(page, baseURL!);
    await createDraft(page, "One employee", 1);
    await saveButton(page).dblclick();
    const oneId = await expectSaved(page, writes);
    const oneDays = await slots(oneId);
    expect(oneDays).toHaveLength(31);
    const traffic = [writes[0]];

    writes.length = 0;
    await createDraft(page, "Five employees", 5);
    for (const day of [6, 9, 12, 15, 20, 22]) await dayInput(page, day).fill("+");
    await saveButton(page).click();
    const fiveId = await expectSaved(page, writes);
    const fiveDays = await slots(fiveId);
    expect(fiveDays).toHaveLength(155);
    expect(new Set(fiveDays.map(day => `${day.availabilityGroupMemberId}:${day.dayOfMonth}`)).size).toBe(155);
    expect(writes[0].payload.members.map(member => member.employeeId)).toEqual(employeeIds);
    traffic.push(writes[0]);

    const year = (await list()).find(group => group.id === oneId)!.year;
    const source = await read<AvailabilityGroup>(await api.post(root, { data: { name: "CFA source", year, month: 12 } }));
    const member = await read<{ id: number }>(await api.post(`${root}/${source.id}/members`, { data: { employeeId: employeeIds[0], displayOrder: 0 } }));
    await read(await api.post(`${root}/${source.id}/slots`, {
      data: { availabilityGroupMemberId: member.id, dayOfMonth: 3, kind: 2, intervalStr: "09:00 - 15:00" },
    }));

    writes.length = 0;
    await page.goto(`/availability/${oneId}/edit`);
    await nameField(page).fill("Edited availability");
    for (const day of [4, 7, 10, 11, 19, 24]) await dayInput(page, day).fill("+");
    await page.getByRole("button", { name: `Choose availability from another schedule for ${employeeName(0)}` }).press("Enter");
    const transfer = page.getByRole("dialog", { name: /Move availability for/ });
    await transfer.getByRole("button", { name: `${employeeName(0)} day 3`, exact: true }).click();
    await transfer.getByRole("button", { name: "Add days to draft: 1" }).click();
    await expect(transfer).toHaveCount(0);
    expect(writes).toHaveLength(0);
    await saveButton(page).click();
    expect(await expectSaved(page, writes)).toBe(oneId);
    expect(writes[0].payload.transfers).toEqual([{ sourceGroupId: source.id, employeeId: employeeIds[0], dayOfMonths: [3] }]);
    expect(writes[0].result?.sourceGroupIds).toEqual([source.id]);
    const edited = await slots(oneId);
    expect(edited.map(day => day.id).sort()).toEqual(oneDays.map(day => day.id).sort());
    expect(edited.find(day => day.dayOfMonth === 3)).toMatchObject({ kind: 2, intervalStr: "09:00 - 15:00" });
    expect(await slots(source.id)).toMatchObject([{ dayOfMonth: 3, kind: 1, intervalStr: null }]);
    expect(await read<unknown[]>(await api.get(`${root}/${source.id}/transfer-hints`))).toMatchObject([{ targetGroupId: oneId, dayOfMonth: 3 }]);
    traffic.push(writes[0]);

    writes.length = 0;
    await page.goto(`/availability/${oneId}/edit`);
    await expect(nameField(page)).toHaveValue("Edited availability");
    await saveButton(page).click();
    await expectSaved(page, writes);
    expect(writes[0].result?.changed).toBe(false);
    traffic.push(writes[0]);
    await testInfo.attach("availability-save-traffic", {
      body: JSON.stringify(traffic.map(write => ({ path: write.path, method: write.method, status: write.status,
        members: write.payload.members.length, days: write.payload.members.reduce((total, member) => total + member.days.length, 0),
        ...write.result })), null, 2),
      contentType: "application/json",
    });
  });

  test("rate-limited save keeps the draft and lost response after commit stays unconfirmed", async ({ page, baseURL }) => {
    await page.setViewportSize({ width: 1500, height: 1000 });
    const { writes, failNext } = await recordSaves(page, baseURL!);
    await createDraft(page, "Rate limited fixture", 1);
    await dayInput(page, 1).fill("Training");
    failNext("rate-limit");
    await saveButton(page).click();
    await expect(page.getByText("Too many requests. Wait 1 s before saving again.", { exact: true })).toBeVisible();
    await expect(dayInput(page, 1)).toHaveValue("Training");
    await expect(page).toHaveURL(/\/availability\/new$/);
    expect((await list()).some(group => group.name.startsWith("Rate limited fixture"))).toBe(false);
    // Leave the retry window open to catch an automatic replay before the explicit second click.
    await page.waitForTimeout(1_100);
    expect(writes.map(write => [write.path, write.status])).toEqual([[saveGraphPath, 429]]);
    writes.length = 0;
    await saveButton(page).click();
    await expectSaved(page, writes);

    writes.length = 0;
    await createDraft(page, "Lost response fixture", 1);
    failNext("lost-response");
    await saveButton(page).click();
    await expect(page.getByText("Availability save could not be confirmed. Check the Availability list before trying again.", { exact: true }).last()).toBeVisible();
    await expect(nameField(page)).toHaveValue("Lost response fixture");
    await expect(page).toHaveURL(/\/availability\/new$/);
    const committed = (await list()).filter(group => group.name.startsWith("Lost response fixture"));
    expect(committed).toHaveLength(1);
    expect(await slots(committed[0].id)).toHaveLength(31);
    await page.goto("/availability");
    await expect(page.getByRole("button", { name: /^Open availability group Lost response fixture/ })).toBeVisible();
    expect(writes.map(write => [write.path, write.status])).toEqual([[saveGraphPath, 200]]);
  });
});

test("November and December tints survive pinning, hover, search and keyboard focus", async ({ page, baseURL, isMobile }) => {
  await recordSaves(page, baseURL!);
  for (const [name, month, year] of [
    ["Tint November 2026", 11, 2026], ["Tint November 2027", 11, 2027],
    ["Tint December 2026", 12, 2026], ["Tint December 2027", 12, 2027],
  ] as const) {
    await read(await api.post(root, { data: { name, year, month } }));
  }

  await page.goto("/availability");
  const november = page.getByRole("button", { name: "Open availability group Tint November 2026", exact: true });
  const december = page.getByRole("button", { name: "Open availability group Tint December 2026", exact: true });
  const green = "rgb(232, 246, 237)";
  const blue = "rgb(232, 242, 255)";
  await expect(november).toHaveCSS("background-color", green);
  await expect(december).toHaveCSS("background-color", blue);
  await expect(page.getByRole("button", { name: "Open availability group Tint November 2027", exact: true })).toHaveCSS("background-color", green);
  await expect(page.getByRole("button", { name: "Open availability group Tint December 2027", exact: true })).toHaveCSS("background-color", blue);
  await december.getByRole("button", { name: "Pin Tint December 2026" }).click();
  if (!isMobile) await december.hover();
  await expect(december).toHaveCSS("background-color", blue);
  await page.getByRole("textbox", { name: "Search availability", exact: true }).fill("Tint December");
  await expect(november).toHaveCount(0);
  await december.focus();
  await expect(december).toBeFocused();
  await expect(december).toHaveCSS("background-color", blue);
  await december.press("Enter");
  await expect(page).toHaveURL(/\/availability\/\d+$/);
});
