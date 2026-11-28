import { expect } from "@playwright/test";
import { test } from "./fixtures/isolatedBackend";
import { createEmployeeAccount, loginAsManager } from "./helpers";

test("matrix actions stay in their graph drafts until one explicit save", async ({ page, isMobile }) => {
  test.skip(isMobile, "Manager PC editor");
  test.setTimeout(100_000);
  await page.setViewportSize({ width: 1600, height: 960 });
  const worker = await createEmployeeAccount(page.request, "matrix-actions");
  const headers = { Authorization: "Bearer " + worker.managerAccessToken };
  const api = async (method: "GET" | "POST", path: string, data?: object) => {
    const response = await page.request.fetch(path, { method, headers, data });
    expect(response.ok(), method + " " + path + ": " + await response.text()).toBe(true);
    return response.json();
  };
  const { id: workerId, firstName } = await api("GET", "/api/employees/" + worker.id);
  const { id: secondId } = await api("POST", "/api/employees", { firstName: "Second", lastName: "Worker" });
  const { id: containerId } = await api("POST", "/api/containers", { name: "Matrix actions" });
  const { id: shopId } = await api("POST", "/api/shops", { name: "Matrix shop", address: "Test address" });
  const root = "/api/containers/" + containerId + "/graphs";
  const draft = {
    year: 2026, month: 12, shopId, publicationStatus: "public", allowSwap: true,
    shift1Time: "08:00 - 12:00", shift2Time: "12:00 - 16:00", peoplePerShift: 1,
    maxHoursPerEmpMonth: 160, maxConsecutiveDays: 5, maxConsecutiveFull: 3, maxFullPerMonth: 10,
  };
  const note = "Test note\n\n[[GF3_GRAPH_META:" + JSON.stringify({
    m: [[1, "Generated coverage", { "1": "08:00 - 12:00" }]], g: [1],
  }) + "]]";
  const { id: graphId } = await api("POST", root, { ...draft, name: "First schedule", note });
  const { id: otherId } = await api("POST", root, { ...draft, name: "Second schedule" });
  const graphRoute = root + "/" + graphId;
  for (const id of [graphId, otherId]) {
    await api("POST", root + "/" + id + "/employees", { employeeId: workerId, displayOrder: 0 });
    await api("POST", root + "/" + id + "/employees", { employeeId: secondId, displayOrder: 1 });
  }
  await api("POST", graphRoute + "/shift-swaps/manual", {
    manualColumnId: 1, dayOfMonth: 1, fromTime: "08:00", toTime: "12:00", allowPartialAcceptance: false,
  });

  await loginAsManager(page);
  await page.goto("/container/" + containerId + "/graphs/" + graphId + "/edit?openGraphIds=" + graphId + "," + otherId);
  const sidebar = page.getByRole("button", { name: "Collapse sidebar", exact: true });
  if (await sidebar.isVisible()) await sidebar.press("Enter");
  await page.mouse.move(1050, 80);
  const matrix = page.locator('[class*="_scheduleMatrixCardShell_"] table');
  const source = matrix.getByRole("columnheader", { name: firstName + " Employee" });
  const second = matrix.getByRole("columnheader", { name: "Second Worker" });
  await source.dragTo(second);
  await expect(page.getByRole("menu")).toHaveCount(0);
  const handle = second.locator('[class*="_columnResizeHandle_"]');
  const grab = await handle.boundingBox();
  expect(grab).not.toBeNull();
  await page.mouse.move(grab!.x + grab!.width / 2, grab!.y + grab!.height / 2);
  await page.mouse.down();
  await page.mouse.move(grab!.x + 36, grab!.y + grab!.height / 2);
  await page.mouse.up();
  await expect(page.getByRole("menu")).toHaveCount(0);

  const remove = async () => {
    await source.getByRole("button", { name: /Actions for/ }).click();
    await page.getByRole("menuitem", { name: "Remove from schedule" }).click();
    return page.getByRole("dialog", { name: "Remove employee", exact: true });
  };
  let confirmation = await remove();
  await confirmation.getByRole("button", { name: "Cancel" }).click();
  await expect(source).toBeVisible();
  confirmation = await remove();
  await confirmation.getByRole("button", { name: "Remove", exact: true }).click();
  await expect(source).toHaveCount(0);
  expect(await api("GET", graphRoute + "/employees")).toHaveLength(2);

  await page.getByRole("button", { name: "Add manual column on the left" }).click();
  await matrix.getByRole("textbox", { name: "Manual column 2 header" }).fill("Left demand");
  await page.getByRole("button", { name: "Add manual column on the right" }).click();
  await matrix.getByRole("textbox", { name: "Manual column 3 header" }).fill("Right demand");
  const order = () => matrix.getByRole("columnheader").evaluateAll(headers =>
    headers.map(header => header.getAttribute("aria-label")).slice(1));
  await expect.poll(order).toEqual(["Generated coverage", "Left demand", "Second Worker", "Right demand"]);
  const leftCell = matrix.locator('[data-cell-key="-2:2"]');
  await leftCell.getByRole("button").dblclick();
  await leftCell.getByRole("textbox").fill("09:00 - 13:00");
  await leftCell.getByRole("textbox").press("Enter");
  await matrix.getByRole("button", { name: "Actions for manual column Generated coverage" }).click();
  await page.getByRole("menuitem", { name: "Delete manual column" }).click();
  await expect(page.getByText("Cancel open manual offers before changing their demand.", { exact: true })).toBeVisible();
  await matrix.getByRole("columnheader", { name: "Right demand" }).getByRole("button", { name: "Delete manual column" }).click();
  await page.getByRole("dialog", { name: "Delete manual column?" }).getByRole("button", { name: "Delete column" }).click();
  await expect(matrix.getByRole("textbox", { name: "Manual column 3 header" })).toHaveCount(0);

  await page.getByRole("tab", { name: "Second schedule" }).click();
  await expect(matrix.getByRole("columnheader", { name: firstName + " Employee" })).toBeVisible();
  await page.getByRole("tab", { name: "First schedule" }).click();
  await expect.poll(order).toEqual(["Generated coverage", "Left demand", "Second Worker"]);
  await expect(leftCell).toContainText("09:00 - 13:00");
  await page.getByRole("button", { name: "Save", exact: true }).click();
  await page.getByRole("dialog", { name: "Save schedule" }).getByRole("button", { name: "Save", exact: true }).click();
  await expect(page).toHaveURL(url => !url.pathname.endsWith("/edit"), { timeout: 45_000 });
  expect(await api("GET", graphRoute + "/employees")).toEqual([expect.objectContaining({ employeeId: secondId })]);
  expect(await api("GET", "/api/employees/" + workerId)).toMatchObject({ id: workerId, firstName });
  await page.goto("/container/" + containerId + "/graphs/" + graphId + "/edit");
  await expect.poll(order).toEqual(["Generated coverage", "Left demand", "Second Worker"]);
  await expect(leftCell).toContainText("09:00 - 13:00");
});
