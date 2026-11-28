import { expect } from "@playwright/test";
import { test } from "./fixtures/isolatedBackend";
import {
  createEmployeeAccount,
  deleteEmployeeAccount,
  expectNoRequestError,
  loginWithCredentials,
} from "./helpers";

const employeeRoutes = [
  { path: "/", text: "Inbox" },
  { path: "/availability", text: "No active windows" },
  { path: "/schedule", text: "No published schedules assigned to you" },
  { path: "/swap", text: "Open swaps" },
  { path: "/profile", text: "Recovery email" },
] as const;

function createSyntheticPdf() {
  const contents = ["BT /F1 18 Tf 40 500 Td (Synthetic schedule page 1) Tj ET", "BT /F1 18 Tf 40 500 Td (Synthetic schedule page 2) Tj ET"];
  const objects = [
    "<< /Type /Catalog /Pages 2 0 R >>",
    "<< /Type /Pages /Kids [3 0 R 4 0 R] /Count 2 >>",
    "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /Font << /F1 5 0 R >> >> /Contents 6 0 R >>",
    "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /Font << /F1 5 0 R >> >> /Contents 7 0 R >>",
    "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
    ...contents.map(content => `<< /Length ${Buffer.byteLength(content)} >>\nstream\n${content}\nendstream`),
  ];
  let pdf = "%PDF-1.4\n";
  const offsets = [0];
  objects.forEach((object, index) => {
    offsets.push(Buffer.byteLength(pdf));
    pdf += `${index + 1} 0 obj\n${object}\nendobj\n`;
  });
  const xref = Buffer.byteLength(pdf);
  pdf += `xref\n0 ${objects.length + 1}\n0000000000 65535 f \n`;
  pdf += offsets.slice(1).map(offset => `${String(offset).padStart(10, "0")} 00000 n \n`).join("");
  pdf += `trailer\n<< /Size ${objects.length + 1} /Root 1 0 R >>\nstartxref\n${xref}\n%%EOF\n`;
  return Buffer.from(pdf);
}

test.describe("employee critical pages", () => {
  test("daily schedule distinguishes selected shifts and related assignments and keeps the rounded PDF controls", async ({ page }, info) => {
    await page.clock.setFixedTime(new Date("2026-08-12T10:00:00Z"));
    await page.emulateMedia({ reducedMotion: "reduce" });
    await page.addInitScript(() => {
      localStorage.setItem("gf3.auth.access-token", "daily-schedule-fixture");
      sessionStorage.setItem("gf3.pwa.install.dismissed", "1");
    });
    await page.route("**/api/**", route => {
      const path = new URL(route.request().url()).pathname;
      if (!path.startsWith("/api/")) return route.continue();
      let body: unknown = [];
      if (path === "/api/auth/session") body = { role: "employee", employeeId: 1, userName: "zoe", displayName: "Zoe Young" };
      else if (path === "/api/account-language") body = { language: "en" };
      else if (path === "/api/employee-ui-state") body = { scheduleColumnOrders: {}, readNotificationIds: [] };
      else if (path === "/api/employee-urlop/approved") body = [{ shopId: 1, employeeId: 3, employeeName: "Vera Away", date: "2026-08-12" }];
      else if (path === "/api/employee-schedules") body = [{
        id: 1, containerId: 1, containerName: "Synthetic", shopId: 1, shopName: "Central",
        name: "Selected graph", year: 2026, month: 8, publicationStatus: "public",
        employees: [["Zoe", "Young"], ["Casey", "Outside"], ["Vera", "Away"]].map(([firstName, lastName], index) => ({
          id: index + 1, employeeId: index + 1, firstName, lastName, displayName: firstName + " " + lastName, displayOrder: index,
        })),
        slots: [{ id: 1, employeeId: 1, dayOfMonth: 12, slotNo: 1, fromTime: "08:00", toTime: "12:00", status: "ASSIGNED" }],
        relatedScheduleAssignments: [[1, 12], [2, 12], [1, 13]].map(([employeeId, dayOfMonth]) => ({
          employeeId, dayOfMonth, scheduleId: 2, scheduleName: "Alternate graph",
        })),
      }];
      else if (path.includes("negotiate")) return route.fulfill({ status: 503 });
      return route.fulfill({ json: body });
    });
    await page.goto("/schedule");
    await expect(page.getByText("Alternate graph", { exact: true }).first()).toBeVisible();
    const eye = page.getByRole("button", { name: "Show daily schedule view" });
    await expect(eye).toHaveCSS("border-top-left-radius", "999px");
    await expect(page.getByRole("button", { name: "Export schedule to PDF" })).toHaveCSS("border-top-left-radius", "999px");
    await eye.click();
    const workers = page.getByRole("tabpanel", { name: "Wednesday 12" });
    const cards = workers.getByRole("article");
    await expect(cards).toHaveCount(2);
    await expect(cards.filter({ hasText: "Zoe Young" })).toContainText("08:00 - 12:00");
    await expect(cards.filter({ hasText: "Zoe Young" })).toContainText("Alternate graph");
    const relatedOnly = cards.filter({ hasText: "Casey Outside" });
    await expect(relatedOnly).toContainText("Alternate graph");
    await expect(relatedOnly).not.toContainText("08:00 - 12:00");
    await expect(workers.getByText(/Vera Away|^UR$/)).toHaveCount(0);
    await workers.screenshot({ path: info.outputPath("daily-selected-workers.png") });
    await page.getByRole("tab", { name: "Thursday 13, working day" }).click();
    const followingDay = page.getByRole("tabpanel", { name: "Thursday 13" }).getByRole("article");
    await expect(followingDay).toHaveCount(1);
    await expect(followingDay).toContainText("Zoe Young");
    await expect(followingDay).toContainText("Alternate graph");
    await expect(followingDay).not.toContainText("08:00 - 12:00");
    await page.getByRole("tab", { name: "Friday 14, day off" }).click();
    await expect(page.getByText("No one is scheduled.")).toBeVisible();
    const showMatrix = page.getByRole("button", { name: "Show schedule matrix view" });
    await expect(showMatrix).toHaveAttribute("aria-pressed", "true");
    await showMatrix.click();
    await expect(page.getByText("Alternate graph", { exact: true }).first()).toBeVisible();
    await expect(eye).toBeFocused();
  });

  for (const fileAction of ["share", "download", "activation"] as const) {
    test(`employee PDF ${fileAction} stays on the schedule without a preview`, async ({ page, request }) => {
      await page.addInitScript(action => {
        Reflect.set(window, "pdfSharedFiles", []);
        let attempts = 0;
        Object.defineProperty(navigator, "canShare", { configurable: true, value: () => action !== "download" });
        Object.defineProperty(navigator, "share", { configurable: true, value: async (data: ShareData) => {
          attempts++;
          if (action === "activation" && attempts === 1) throw new DOMException("Tap required", "NotAllowedError");
          Reflect.set(window, "pdfSharedFiles", data.files?.map(file => ({ name: file.name, type: file.type, size: file.size })));
        } });
      }, fileAction);
      const employee = await createEmployeeAccount(request, "employee.pdf.e2e");
      let exports = 0;
      const viewerRequests: string[] = [];
      page.on("request", event => { if (/pdfjs|pdf\.worker|EmployeeSchedulePdf\.worker/.test(event.url())) viewerRequests.push(event.url()); });
      try {
        await page.route("**/api/employee-schedules", route => route.fulfill({ json: [{
          id: 33001, containerId: 1, containerName: "Synthetic container", shopId: 1, shopName: "Synthetic shop",
          name: "Synthetic published schedule", year: 2026, month: 10, publicationStatus: "public",
          employees: [{ id: 1, employeeId: employee.id, firstName: "Synthetic", lastName: "Employee",
            displayName: "Synthetic Employee", displayOrder: 0, minHoursMonth: 0 }], slots: [], relatedScheduleAssignments: [],
        }] }));
        await page.route("**/api/employee-schedules/33001/export/pdf", route => {
          exports++;
          expect(route.request().headers().authorization).toMatch(/^Bearer /);
          return route.fulfill({ contentType: "application/pdf", headers: { "Content-Disposition": "attachment; filename=synthetic.pdf" }, body: createSyntheticPdf() });
        });
        await page.route(/\/api\/employee-urlop\/approved\?/, route => {
          const parameters = new URL(route.request().url()).searchParams;
          expect(Object.fromEntries(parameters)).toEqual({ shopId: "1", year: "2026", month: "10" });
          return route.fulfill({ json: [] });
        });
        await loginWithCredentials(page, employee.username, employee.password);
        await page.goto("/schedule");
        const trigger = page.getByRole("button", { name: "Export schedule to PDF" });
        await trigger.scrollIntoViewIfNeeded();
        let beforeScroll = await page.evaluate(() => window.scrollY);
        if (fileAction === "download") {
          const [download] = await Promise.all([page.waitForEvent("download"), trigger.click()]);
          expect(download.suggestedFilename()).toBe("synthetic.pdf");
        } else {
          await trigger.click();
          if (fileAction === "activation") {
            await expect(page.getByText("PDF is ready. Tap PDF again to share, or download it.")).toBeVisible();
            expect(await page.evaluate(() => window.scrollY)).toBe(beforeScroll);
            const share = page.getByRole("button", { name: "Share PDF" });
            await share.scrollIntoViewIfNeeded();
            beforeScroll = await page.evaluate(() => window.scrollY);
            await share.click();
          }
          await expect.poll(() => page.evaluate(() => Reflect.get(window, "pdfSharedFiles"))).toEqual([
            { name: "synthetic.pdf", type: "application/pdf", size: createSyntheticPdf().length },
          ]);
        }
        await expect(page.getByRole("button", { name: "Export schedule to PDF" })).toBeEnabled();
        await expect(page.getByRole("dialog")).toHaveCount(0);
        await expect(page).toHaveURL(/\/schedule$/);
        await expect(page.getByRole("button", { name: "Select schedule. Current schedule: Synthetic published schedule" })).toBeVisible();
        expect(await page.evaluate(() => window.scrollY)).toBe(beforeScroll);
        await page.getByRole("button", { name: "Show daily schedule view" }).click();
        await expect(page.getByRole("button", { name: "Show schedule matrix view" })).toBeVisible();
        const openNavigation = page.locator('button[aria-label="Open navigation"]:not([inert])');
        if (await openNavigation.count()) await openNavigation.click();
        await page.locator('nav a[href="/availability"]:visible').first().click();
        await expect(page).toHaveURL(/\/availability$/);
        expect(exports).toBe(1);
        expect(viewerRequests).toEqual([]);
        await expectNoRequestError(page);
      } finally {
        await deleteEmployeeAccount(request, employee.id, employee.managerAccessToken);
      }
    });
  }
  test("new employee can sign in and visit the employee workflow pages", async ({ page, request }) => {
    const employee = await createEmployeeAccount(request, "employee.e2e");

    try {
      await loginWithCredentials(page, employee.username, employee.password);
      await expect(page.getByRole("tab", { name: /^Inbox/ })).toBeVisible();

      for (const route of employeeRoutes) {
        await page.goto(route.path);
        await expect(page.getByText(route.text, { exact: true }).first()).toBeVisible();
        await expectNoRequestError(page);
      }
    } finally {
      await deleteEmployeeAccount(request, employee.id, employee.managerAccessToken);
    }
  });

  test("employee users are redirected away from manager-only routes", async ({ page, request }) => {
    const employee = await createEmployeeAccount(request, "employee.route.e2e");

    try {
      await loginWithCredentials(page, employee.username, employee.password);
      await page.goto("/database");

      await expect(page).toHaveURL(/\/$/);
      await expect(page.getByRole("tab", { name: /^Inbox/ })).toBeVisible();
      await expectNoRequestError(page);
    } finally {
      await deleteEmployeeAccount(request, employee.id, employee.managerAccessToken);
    }
  });
});
