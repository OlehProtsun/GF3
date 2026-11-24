import { expect, type APIRequestContext, type Page } from "@playwright/test";
import { test } from "./fixtures/isolatedBackend";
import { createEmployeeAccount, expectNoRequestError, loginAsManager } from "./helpers";

test.use({ actionTimeout: 10_000 });

type Fixture = { containerId: number; shopName: string; employeeName: string; groupName: string; headers: Record<string, string> };
const graphZero = /\/api\/containers\/\d+\/graphs\/0(?:\/|$)/;

async function withFixture(api: APIRequestContext, run: (fixture: Fixture) => Promise<void>) {
  const employee = await createEmployeeAccount(api, "graph-create");
  const headers = { Authorization: `Bearer ${employee.managerAccessToken}` };
  const suffix = `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
  const post = async (url: string, data: unknown) => {
    const response = await api.post(url, { headers, data });
    expect(response.ok(), await response.text()).toBe(true);
    return response.json();
  };
  const container = await post("/api/containers", { name: `E2E Graph ${suffix}` });
  const shopName = `E2E Shop ${suffix}`;
  await post("/api/shops", { name: shopName, address: "Test address" });
  const groupName = `E2E Availability ${suffix}`;
  const now = new Date();
  const group = await post("/api/availability-groups", { name: groupName, year: now.getFullYear(), month: now.getMonth() + 1 });
  const member = await post(`/api/availability-groups/${group.id}/members`, { employeeId: employee.id, displayOrder: 0 });
  await post(`/api/availability-groups/${group.id}/slots`, {
    availabilityGroupMemberId: member.id, dayOfMonth: 1, kind: 0,
  });
  const details = await api.get(`/api/employees/${employee.id}`, { headers });
  expect(details.ok()).toBe(true);
  const person = await details.json();
  await run({ containerId: container.id, shopName, groupName, employeeName: `${person.firstName} ${person.lastName}`, headers });
  // The isolated backend fixture removes the entire database after the test.
}

async function openDraft(page: Page, fixture: Fixture) {
  await loginAsManager(page);
  const requests: string[] = [];
  page.on("request", request => requests.push(request.url()));
  await page.goto(`/container/${fixture.containerId}/graphs/new`);
  await expect(page.getByRole("heading", { name: "Add Schedule", exact: true })).toBeVisible();
  const collapseSidebar = page.getByRole("button", { name: "Collapse sidebar", exact: true });
  if (await collapseSidebar.isVisible()) await collapseSidebar.click();
  await page.getByRole("button", { name: "Expand Schedule Details", exact: true }).click();
  await expect(page.locator("#graph-name")).toBeEditable();
  await page.locator("#graph-name").fill(`Created ${fixture.containerId}`);
  await page.getByRole("button", { name: "shop list", exact: true }).click();
  await page.getByRole("option", { name: new RegExp(fixture.shopName) }).click();
  await page.waitForLoadState("networkidle");
  expect(requests.filter(url => graphZero.test(new URL(url).pathname))).toEqual([]);
  await expectNoRequestError(page);
  return requests;
}

test("new graph makes no zero-ID requests and saves to a positive persisted graph", async ({ page, request }) => {
  await withFixture(request, async fixture => {
    const requests = await openDraft(page, fixture);
    await page.getByRole("button", { name: "Expand Employees", exact: true }).click();
    await page.getByRole("button", { name: "employee list", exact: true }).click();
    await page.getByRole("option", { name: fixture.employeeName, exact: true }).click();
    await page.locator("#graph-employee-select").locator("../..").getByRole("button", { name: "Add", exact: true }).click();
    if ((page.viewportSize()?.width ?? 0) <= 960) {
      await page.getByRole("button", { name: "Collapse Employees", exact: true }).click();
      await page.getByRole("button", { name: "Collapse Schedule Details", exact: true }).click();
    }
    await page.getByRole("button", { name: "Save", exact: true }).first().click();
    const saved = page.waitForResponse(response => response.request().method() === "POST"
      && new URL(response.url()).pathname === `/api/containers/${fixture.containerId}/graphs`);
    await page.getByRole("dialog", { name: "Save schedule", exact: true }).getByRole("button", { name: "Save", exact: true }).click();
    const response = await saved;
    expect(response.status()).toBe(201);
    const graph = await response.json();
    expect(graph.id).toBeGreaterThan(0);
    const persisted = await request.get(`/api/containers/${fixture.containerId}/graphs/${graph.id}`, { headers: fixture.headers });
    expect(persisted.status(), await persisted.text()).toBe(200);
    expect((await persisted.json()).name).toBe(`Created ${fixture.containerId}`);
    const graphDetail = page.waitForResponse(response => response.request().method() === "GET"
      && new URL(response.url()).pathname === `/api/containers/${fixture.containerId}/graphs/${graph.id}`);
    await page.goto(`/container/${fixture.containerId}/graphs/${graph.id}`, { waitUntil: "domcontentloaded" });
    const graphDetailResponse = await graphDetail;
    expect(graphDetailResponse.status(), await graphDetailResponse.text()).toBe(200);
    await expect(page.getByRole("heading", { name: "Schedule Profile", exact: true })).toBeVisible();
    await expect(page.locator('h2[class*="scheduleName_"]')).toHaveText(`Created ${fixture.containerId}`);
    await expectNoRequestError(page);
    await page.goto(`/container/${fixture.containerId}/graphs/${graph.id}/edit`, { waitUntil: "domcontentloaded" });
    await expect(page.getByRole("heading", { name: "Schedule Edit", exact: true })).toBeVisible();
    await expect.poll(() => requests.some(url => new URL(url).pathname.endsWith(`/graphs/${graph.id}/shift-swaps/highlight-setting`))).toBe(true);
    await expect.poll(() => requests.some(url => new URL(url).pathname.endsWith(`/graphs/${graph.id}/shift-corrections`))).toBe(true);
    expect(requests.filter(url => graphZero.test(new URL(url).pathname))).toEqual([]);
    await expectNoRequestError(page);
  });
});

test("unsaved generation uses the preview route with real availability", async ({ page, request }) => {
  await withFixture(request, async fixture => {
    const requests = await openDraft(page, fixture);
    await page.getByRole("button", { name: "availability groups", exact: true }).click();
    await page.getByRole("option", { name: new RegExp(fixture.groupName) }).click();
    const preview = page.waitForResponse(response => response.request().method() === "POST"
      && new URL(response.url()).pathname === `/api/containers/${fixture.containerId}/graphs/generate-preview`);
    await page.getByRole("button", { name: "Generate", exact: true }).click();
    const response = await preview;
    expect(response.ok(), await response.text()).toBe(true);
    expect(requests.filter(url => graphZero.test(new URL(url).pathname))).toEqual([]);
    await expectNoRequestError(page);
  });
});
