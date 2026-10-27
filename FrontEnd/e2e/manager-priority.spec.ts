import { expect, test } from "@playwright/test";

test.use({ reducedMotion: "reduce" });

test("priority settings and graph flags survive preview, save and reload at 320px", async ({ page }) => {
  test.setTimeout(60_000); // Both editor visits include the existing eight-second edit-lock fallback.
  let settings = { shiftHours: 8, weekendsOff: true };
  const employees = [1, 2].map(id => ({ id, firstName: id === 1 ? "Ada" : "Grace", lastName: "Synthetic", hasLoginAccount: false, isOnline: false }));
  let assignments = employees.map((employee, index) => ({ id: employee.id, scheduleId: 1, employeeId: employee.id, displayOrder: index, minHoursMonth: 70, isPriority: false }));
  let graph = { id: 1, containerId: 1, shopId: 1, name: "Priority fixture", year: 2026, month: 4, publicationStatus: "private", allowSwap: true,
    peoplePerShift: 1, shift1Time: "09:00 - 15:00", shift2Time: "15:00 - 21:00", maxHoursPerEmpMonth: 160, maxConsecutiveDays: 5,
    maxConsecutiveFull: 3, maxFullPerMonth: 10, availabilityGroupId: 1, note: null as string | null };
  let slots: unknown[] = [];
  let previewFlags: boolean[] = [];
  const errors: string[] = [];
  page.on("pageerror", error => errors.push(error.message));
  await page.addInitScript(() => localStorage.setItem("gf3.auth.access-token", "synthetic-priority-token"));
  await page.route("**/api/**", async route => {
    const path = new URL(route.request().url()).pathname;
    if (!path.startsWith("/api/")) return route.continue();
    const method = route.request().method();
    let body: unknown = [];
    if (path === "/api/auth/session") body = { role: "manager", workspaceMode: "pc", managerId: 1, userName: "synthetic", displayName: "Synthetic" };
    else if (path === "/api/account-language") body = { language: "en" };
    else if (path === "/api/public-holiday-settings") body = { showPublicHolidays: true };
    else if (path === "/api/priority-generation-settings") {
      if (method === "PUT") settings = route.request().postDataJSON();
      body = settings;
    }
    else if (path === "/api/employees") body = employees;
    else if (path === "/api/shops") body = [{ id: 1, name: "Synthetic shop", address: "Synthetic" }];
    else if (path === "/api/availability-groups") body = [{ id: 1, name: "Synthetic availability", year: 2026, month: 4, publicationStatus: "private" }];
    else if (path === "/api/availability-groups/1/members") body = employees.map((e, index) => ({ id: e.id, availabilityGroupId: 1, employeeId: e.id, displayOrder: index }));
    else if (path === "/api/containers/1") body = { id: 1, name: "Synthetic container" };
    else if (path === "/api/containers/1/graphs") body = [graph];
    else if (path === "/api/containers/1/graphs/1") {
      if (method === "PUT") graph = { ...graph, ...route.request().postDataJSON() };
      body = graph;
    }
    else if (path === "/api/containers/1/graphs/1/employees") body = assignments;
    else if (/\/graphs\/1\/employees\/\d+$/.test(path) && method === "PUT") {
      const id = Number(path.split("/").at(-1));
      assignments = assignments.map(a => a.id === id ? { ...a, ...route.request().postDataJSON() } : a);
      return route.fulfill({ status: 204 });
    }
    else if (path === "/api/containers/1/graphs/1/slots") {
      if (method === "PUT") slots = route.request().postDataJSON().slots.map((slot: object, index: number) => ({ ...slot, id: index + 1, scheduleId: 1 }));
      body = slots;
    }
    else if (path === "/api/containers/1/graphs/generate-preview") {
      previewFlags = route.request().postDataJSON().employees.map((e: { isPriority: boolean }) => e.isPriority);
      body = { slots: [{ id: 0, scheduleId: 1, dayOfMonth: 1, slotNo: 1, employeeId: 1, status: 1, fromTime: "09:00", toTime: "17:00" },
        { id: 0, scheduleId: 1, dayOfMonth: 1, slotNo: 1, employeeId: null, status: 0, fromTime: "15:00", toTime: "21:00" }] };
    }
    else if (path.includes("negotiate")) return route.fulfill({ status: 503 });
    else if (path.includes("notepad")) body = { notes: [], state: { isExpanded: false, isPinned: false, height: 400 } };
    else if (path === "/api/manager-profile/me") body = { id: 1, userName: "synthetic", displayName: "Synthetic", createdAtUtc: "2026-01-01T00:00:00Z" };
    await route.fulfill({ contentType: "application/json", body: JSON.stringify(body) });
  });
  await page.setViewportSize({ width: 320, height: 900 });
  await page.waitForLoadState("networkidle");
  await page.goto("/settings");
  const collapse = page.getByRole("button", { name: "Collapse sidebar", exact: true });
  const duration = page.getByRole("textbox", { name: "Priority shift duration", exact: true });
  expect(errors).toEqual([]);
  await expect(duration).toBeEnabled();
  if (await collapse.isVisible()) await collapse.click();
  await duration.fill("10"); await duration.press("Enter");
  await expect.poll(() => settings.shiftHours).toBe(10);
  await expect(duration).toHaveValue("10");
  await expect(duration).toBeEnabled();
  expect(errors, "duration save").toEqual([]);
  await page.getByRole("switch", { name: "Weekends off for priority employees" }).click();
  await expect.poll(() => settings.weekendsOff).toBe(false);
  await expect(page.getByRole("switch", { name: "Weekends off for priority employees" })).not.toBeChecked();
  await expect(page.getByRole("switch", { name: "Weekends off for priority employees" })).toBeEnabled();
  expect(errors, "weekend save").toEqual([]);
  await page.waitForLoadState("networkidle");
  await page.reload();
  await expect(duration).toHaveValue("10");
  if (await collapse.isVisible()) await collapse.click();
  await expect(page.getByRole("switch", { name: "Weekends off for priority employees" })).not.toBeChecked();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.waitForLoadState("networkidle");
  await page.goto("/container/1/graphs/1/edit");
  await page.getByRole("button", { name: "Expand Schedule Details", exact: true }).waitFor({ state: "visible", timeout: 15_000 });
  if (await collapse.isVisible()) await collapse.click();
  await page.getByRole("button", { name: "Expand Schedule Details", exact: true }).click();
  const ada = page.getByRole("checkbox", { name: "Priority: Ada Synthetic" });
  const grace = page.getByRole("checkbox", { name: "Priority: Grace Synthetic" });
  await expect(ada).toBeVisible();
  if (await collapse.isVisible()) await collapse.click();
  await ada.focus(); await page.keyboard.press("Space");
  await expect(ada).toBeChecked(); await expect(ada).toBeFocused();
  await grace.check(); await expect(grace).toBeChecked(); await expect(ada).toBeChecked();
  expect(assignments.every(a => !a.isPriority)).toBe(true);
  await expect(page.getByRole("textbox", { name: "Ada Synthetic min hours per month" })).toHaveValue("70");
  const control = await ada.locator("..").boundingBox();
  expect(control!.width).toBeGreaterThanOrEqual(44); expect(control!.height).toBeGreaterThanOrEqual(44);
  expect(control!.x).toBeGreaterThanOrEqual(0); expect(control!.x + control!.width).toBeLessThanOrEqual(320);
  await page.getByRole("button", { name: "Generate", exact: true }).click();
  await expect.poll(() => previewFlags).toEqual([true, true]);
  const collapseDetails = page.getByRole("button", { name: "Collapse Schedule Details", exact: true });
  await collapseDetails.focus(); await collapseDetails.press("Enter");
  await page.getByRole("button", { name: "Save", exact: true }).first().click();
  await page.getByRole("dialog").getByRole("button", { name: "Save", exact: true }).click();
  await expect.poll(() => assignments.every(a => a.isPriority)).toBe(true);
  await expect(page.getByRole("dialog")).toHaveCount(0);
  await expect(page).toHaveURL(/\/container\/1\/graphs\/1(?:\?|$)/);
  await expect(page.getByRole("heading", { name: "Schedule Profile", exact: true })).toBeVisible();
  await page.goto("/container/1/graphs/1/edit");
  await page.getByRole("button", { name: "Expand Schedule Details", exact: true }).waitFor({ state: "visible", timeout: 15_000 });
  if (await collapse.isVisible()) await collapse.click();
  await page.getByRole("button", { name: "Expand Schedule Details", exact: true }).click();
  await expect(ada).toBeChecked(); await expect(grace).toBeChecked();
  if (await collapse.isVisible()) await collapse.click();
  await page.waitForLoadState("networkidle");
  expect(errors).toEqual([]);
});

test("phone workspace exposes no priority write controls", async ({ page }) => {
  let writes = 0;
  await page.addInitScript(() => localStorage.setItem("gf3.auth.access-token", "synthetic-phone-token"));
  await page.route("**/api/**", async route => {
    const path = new URL(route.request().url()).pathname;
    if (!path.startsWith("/api/")) return route.continue();
    let body: unknown = [];
    if (path === "/api/auth/session") body = { role: "manager", workspaceMode: "phone", managerId: 1, userName: "synthetic", displayName: "Synthetic" };
    if (path === "/api/account-language") body = { language: "en" };
    if (path === "/api/priority-generation-settings" && route.request().method() === "PUT") writes++;
    if (path.includes("negotiate")) return route.fulfill({ status: 503 });
    await route.fulfill({ contentType: "application/json", body: JSON.stringify(body) });
  });
  await page.goto("/settings");
  await expect(page.getByRole("heading", { name: "Coming Soon", exact: true })).toBeVisible();
  await expect(page.getByRole("textbox", { name: "Priority shift duration", exact: true })).toHaveCount(0);
  expect(writes).toBe(0);
});
