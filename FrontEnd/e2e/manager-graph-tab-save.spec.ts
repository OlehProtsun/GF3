import { expect, type Page } from "@playwright/test";
import { test } from "./fixtures/isolatedBackend";
import { loginAsManager } from "./helpers";

const cell = (page: Page, key: string) =>
  page.locator('[class*="_scheduleMatrixCardShell_"] table').locator(`[data-cell-key="${key}"]`);

async function editCell(page: Page, key: string, value: string) {
  await cell(page, key).getByRole("button").dblclick();
  const textbox = cell(page, key).getByRole("textbox");
  await textbox.fill(value);
  await textbox.press("Enter");
}

test("three linked schedule drafts save and reopen with their own SQLite data", async ({ page }, testInfo) => {
  test.skip(testInfo.project.name === "mobile-chrome" || testInfo.project.name === "webkit-iphone-x", "Manager PC editor");
  test.setTimeout(100_000);
  await page.setViewportSize({ width: 1600, height: 960 });
  await loginAsManager(page);
  const token = await page.evaluate(() => localStorage.getItem("gf3.auth.access-token"));
  const headers = { Authorization: `Bearer ${token}` };
  const api = async (method: "GET" | "POST" | "PUT", url: string, data?: object) => {
    const response = await page.request.fetch(url, { method, headers, data });
    expect(response.ok(), `${method} ${url}: ${await response.text()}`).toBe(true);
    return response.status() === 204 ? undefined : response.json();
  };
  const { id: container } = await api("POST", "/api/containers", { name: "Tab regression container" });
  const { id: shop } = await api("POST", "/api/shops", { name: "Tab regression shop", address: "Test address" });
  const employees: number[] = [];
  for (const firstName of ["Ada", "Blake", "Casey"]) {
    const { id } = await api("POST", "/api/employees", { firstName, lastName: "Test" });
    employees.push(id);
  }
  const [ada, blake, casey] = employees;
  const rosters = [[ada, blake], [blake, casey], [casey, ada]];
  const period = { year: 2026, month: 12 };
  const root = `/api/containers/${container}/graphs`;
  const graphIds: number[] = [];
  for (const [index, roster] of rosters.entries()) {
    const { id: availabilityGroupId } = await api("POST", "/api/availability-groups", { ...period, name: `Availability ${index + 1}` });
    for (const [displayOrder, employeeId] of roster.entries()) {
      await api("POST", `/api/availability-groups/${availabilityGroupId}/members`, { employeeId, displayOrder });
    }
    const { id } = await api("POST", root, {
      ...period, availabilityGroupId, name: `Schedule ${index + 1}`, note: `Fixture ${index + 1}`, shopId: shop,
      publicationStatus: "private", peoplePerShift: 1, shift1Time: "08:00 - 12:00", shift2Time: "12:00 - 16:00",
      maxHoursPerEmpMonth: 160, maxConsecutiveDays: 5, maxConsecutiveFull: 3, maxFullPerMonth: 10,
    });
    graphIds.push(id);
    for (const [displayOrder, employeeId] of roster.entries()) {
      await api("POST", `${root}/${id}/employees`, { employeeId, displayOrder });
    }
    await api("PUT", `${root}/${id}/slots`, {
      slots: roster.map((employeeId, index) => ({
        employeeId, dayOfMonth: index + 12, slotNo: 1, status: 1, fromTime: "08:00", toTime: "12:00",
      })),
    });
  }
  const read = async (suffix: string) => {
    const parts = [];
    for (const id of graphIds) parts.push(await api("GET", `${root}/${id}${suffix}`));
    return parts;
  };
  const readGraphs = async () => (await read("")).map(graph => ({ ...graph, lastUpdatedAtUtc: undefined }));
  const beforeRows = await read("/employees");
  const beforeSlots = await read("/slots");
  const beforeGraphs = await readGraphs();
  const search = new URLSearchParams({ openGraphIds: graphIds.join(",") });
  const url = (index: number) => `/container/${container}/graphs/${graphIds[index]}/edit?${search}`;
  const go = async (index: number) => {
    await page.getByRole("tab", { name: `Schedule ${index + 1}`, exact: true }).click();
    await expect(page).toHaveURL(url(index));
  };
  await page.goto(url(0));
  await editCell(page, `${ada}:11`, "Training");
  await go(1);
  await editCell(page, `${blake}:12`, "");
  await go(2);
  await cell(page, `${casey}:12`).click();
  await page.getByRole("button", { name: /^Fill #/ }).click();
  await go(0);
  await expect(cell(page, `${ada}:11`)).toContainText("Training");
  await expect(cell(page, `${ada}:12`)).toContainText("08:00 - 12:00");
  await expect(cell(page, `${blake}:13`)).toContainText("08:00 - 12:00");
  const writes: Array<{ path: string; body: unknown }> = [];
  page.on("request", request => {
    const path = new URL(request.url()).pathname;
    if (path.startsWith(root) && request.method() === "PUT") writes.push({ path, body: request.postDataJSON() });
  });
  await page.getByRole("button", { name: "Save", exact: true }).click();
  await page.getByRole("dialog").getByRole("button", { name: "Save", exact: true }).click();
  await expect(page).toHaveURL(url => !url.pathname.endsWith("/edit"), { timeout: 35_000 });
  expect(writes.filter(write => write.path.endsWith("/slots"))).toEqual([{
    path: `${root}/${graphIds[1]}/slots`,
    body: { slots: [expect.objectContaining({ employeeId: casey, dayOfMonth: 13, fromTime: "08:00", toTime: "12:00" })] },
  }]);
  expect(await read("/employees")).toEqual(beforeRows);
  const afterSlots = await read("/slots");
  expect(afterSlots[0]).toEqual(beforeSlots[0]);
  expect(afterSlots[2]).toEqual(beforeSlots[2]);
  expect(afterSlots[1]).toEqual([expect.objectContaining({ employeeId: casey, dayOfMonth: 13,
    fromTime: beforeSlots[1][1].fromTime, toTime: beforeSlots[1][1].toTime })]);
  const afterGraphs = await readGraphs();
  expect(afterGraphs[0]).toEqual({ ...beforeGraphs[0], note: expect.stringMatching(/^Fixture 1\s+\[\[GF3_GRAPH_META:/) });
  expect(afterGraphs.slice(1)).toEqual(beforeGraphs.slice(1));
  expect((await read("/cell-styles"))[2]).toEqual([expect.objectContaining({ employeeId: casey, dayOfMonth: 12 })]);
  await page.goto(url(0));
  await expect(cell(page, `${ada}:11`)).toContainText("Training");
  await go(1);
  await expect(cell(page, `${casey}:13`)).toContainText("08:00 - 12:00");
  await expect(cell(page, `${blake}:12`)).not.toContainText("08:00 - 12:00");
});
