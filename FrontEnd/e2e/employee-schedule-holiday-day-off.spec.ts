import { expect, test } from "@playwright/test";

for (const enabled of [true, false]) {
  test(`daily schedule has four distinct day states, holidays ${enabled}`, async ({ page }, info) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await page.emulateMedia({ reducedMotion: "reduce" });
    await page.addInitScript(() => localStorage.setItem("gf3.auth.access-token", "holiday-day-off-fixture"));
    await page.route("**/api/**", async route => {
      const path = new URL(route.request().url()).pathname;
      let body: unknown = [];
      if (path === "/api/auth/session") body = { role: "employee", employeeId: 12, userName: "worker", displayName: "Worker" };
      else if (path === "/api/account-language") body = { language: "en" };
      else if (path === "/api/public-holiday-settings") body = { showPublicHolidays: enabled };
      else if (path === "/api/employee-ui-state") body = { scheduleColumnOrders: {}, readNotificationIds: [] };
      else if (path === "/api/employee-schedules") body = [{
        id: 1, containerId: 2, containerName: "Team", shopId: 4, shopName: "Central", name: "May", year: 2026, month: 5,
        publicationStatus: "public", employees: [{ id: 1, employeeId: 12, firstName: "Test", lastName: "Worker", displayOrder: 0 }],
        slots: [1, 4].map(day => ({ id: day, dayOfMonth: day, slotNo: 1, employeeId: 12,
          fromTime: "08:00", toTime: "16:00", status: "ASSIGNED" })),
      }];
      else if (path.includes("negotiate")) return route.fulfill({ status: 503 });
      await route.fulfill({ json: body });
    });
    await page.goto("/schedule");
    await page.getByRole("button", { name: "Show daily schedule view" }).click();
    const work = page.getByRole("tab", { name: /^Monday 4, working day/ });
    const off = page.getByRole("tab", { name: /^Saturday 2, day off/ });
    const holidayWork = page.getByRole("tab", { name: /^Friday 1, working day/ });
    const holidayOff = page.getByRole("tab", { name: /^Sunday 3, day off/ });
    await expect(work).toHaveCSS("background-color", "rgb(255, 255, 255)");
    await expect(work).toHaveCSS("background-image", "none");
    await expect(off).toHaveCSS("background-color", "rgba(255, 228, 230, 0.9)");
    await expect(off).toHaveCSS("background-image", "none");
    await expect(holidayWork).toHaveCSS("background-color", enabled ? "rgb(255, 247, 214)" : "rgb(255, 255, 255)");
    if (enabled) {
      const gradient = await holidayOff.evaluate(element => getComputedStyle(element).backgroundImage);
      expect(gradient).toMatch(/^linear-gradient\(90deg,/);
      expect(gradient).toContain("255, 247, 214");
      expect(gradient).toContain("255, 228, 230");
      expect(gradient.match(/50%/g)?.length).toBe(2);
      await expect(holidayOff).toHaveAttribute("title", "Constitution Day");
      await off.focus();
      await page.keyboard.press("Tab");
      await expect(holidayOff).toBeFocused();
      await expect(holidayOff).toHaveCSS("box-shadow", /rgba\(124, 58, 237, 0.3\).*3px/);
      await holidayOff.click();
      await expect(holidayOff).toHaveAttribute("aria-selected", "true");
      await expect(holidayOff).toHaveCSS("border-top-color", "rgb(124, 58, 237)");
      await expect(holidayOff).toHaveCSS("box-shadow", /rgba\(124, 58, 237, 0.3\).*3px/);
      await holidayOff.focus();
      await expect(holidayOff).toBeFocused();
      await expect(holidayOff).toHaveCSS("background-image", gradient);
    } else {
      await expect(holidayOff).toHaveCSS("background-color", "rgba(255, 228, 230, 0.9)");
      await expect(holidayOff).toHaveCSS("background-image", "none");
      await expect(holidayOff).not.toHaveAttribute("title");
    }
    await page.screenshot({ path: info.outputPath(`daily-holiday-off-${enabled}.png`), fullPage: true });
  });
}
