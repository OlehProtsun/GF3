import { expect, test, type APIRequestContext, type Page } from "@playwright/test";
import { createEmployeeAccount, deleteEmployeeAccount, expectNoRequestError, loginAsManager } from "./helpers";

test.use({ actionTimeout: 10_000 });

type Fixture = { containerId: number; shopName: string; employeeName: string; groupName: string; headers: Record<string, string> };
const graphZero = /\/api\/containers\/\d+\/graphs\/0(?:\/|$)/;

async function withFixture(api: APIRequestContext, run: (fixture: Fixture) => Promise<void>) {
  const employee = await createEmployeeAccount(api, "graph-create");
  const headers = { Authorization: `Bearer ${employee.managerAccessToken}` };
  const suffix = `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
  let containerId: number | undefined;
  let shopId: number | undefined;
  let groupId: number | undefined;
  const post = async (url: string, data: unknown) => {
    const response = await api.post(url, { headers, data });
    expect(response.ok(), await response.text()).toBe(true);
    return response.json();
  };
  try {
    const container = await post("/api/containers", { name: `E2E Graph ${suffix}` });
    containerId = container.id;
    const shopName = `E2E Shop ${suffix}`;
    const shop = await post("/api/shops", { name: shopName, address: "Test address" });
    shopId = shop.id;
    const groupName = `E2E Availability ${suffix}`;
    const now = new Date();
    const group = await post("/api/availability-groups", { name: groupName, year: now.getFullYear(), month: now.getMonth() + 1 });
    groupId = group.id;
    const member = await post(`/api/availability-groups/${groupId}/members`, { employeeId: employee.id, displayOrder: 0 });
    await post(`/api/availability-groups/${groupId}/slots`, {
      availabilityGroupMemberId: member.id, dayOfMonth: 1, kind: 0,
    });
    const details = await api.get(`/api/employees/${employee.id}`, { headers });
    expect(details.ok()).toBe(true);
    const person = await details.json();
    await run({ containerId: container.id, shopName, groupName, employeeName: `${person.firstName} ${person.lastName}`, headers });
  } finally {
    if (containerId) {
      const graphs = await api.get(`/api/containers/${containerId}/graphs`, { headers });
      expect(graphs.ok()).toBe(true);
      for (const graph of await graphs.json())
        expect((await api.delete(`/api/containers/${containerId}/graphs/${graph.id}`, { headers })).ok()).toBe(true);
      expect((await api.delete(`/api/containers/${containerId}`, { headers })).ok()).toBe(true);
    }
    if (groupId) expect((await api.delete(`/api/availability-groups/${groupId}`, { headers })).ok()).toBe(true);
    if (shopId) expect((await api.delete(`/api/shops/${shopId}`, { headers })).ok()).toBe(true);
    await deleteEmployeeAccount(api, employee.id, employee.managerAccessToken);
  }
}

async function openDraft(page: Page, fixture: Fixture) {
  await loginAsManager(page);
  const requests: string[] = [];
  page.on("request", request => requests.push(request.url()));
  await page.goto(`/container/${fixture.containerId}/graphs/new`);
  await expect(page.getByRole("heading", { name: "Add Schedule", exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Collapse sidebar", exact: true }).click();
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
    const saved = page.waitForResponse(response => response.request().method() === "POST"
      && new URL(response.url()).pathname === `/api/containers/${fixture.containerId}/graphs`);
    await page.getByRole("button", { name: "Save", exact: true }).first().click();
    await page.getByRole("dialog", { name: "Save schedule", exact: true }).getByRole("button", { name: "Save", exact: true }).click();
    const response = await saved;
    expect(response.status()).toBe(201);
    const graph = await response.json();
    expect(graph.id).toBeGreaterThan(0);
    await page.goto(`/container/${fixture.containerId}/graphs/${graph.id}`);
    await expect(page.getByRole("heading", { name: `Created ${fixture.containerId}`, exact: true })).toBeVisible();
    await expectNoRequestError(page);
    await page.goto(`/container/${fixture.containerId}/graphs/${graph.id}/edit`);
    await expect(page.getByRole("heading", { name: "Schedule Edit", exact: true })).toBeVisible();
    await page.waitForLoadState("networkidle");
    expect(requests.some(url => new URL(url).pathname.endsWith(`/graphs/${graph.id}/shift-swaps/highlight-setting`))).toBe(true);
    expect(requests.some(url => new URL(url).pathname.endsWith(`/graphs/${graph.id}/shift-corrections`))).toBe(true);
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
