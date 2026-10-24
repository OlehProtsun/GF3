import { expect, test } from "@playwright/test";

test("production update requires a click and preserves session storage", async ({ page, request }) => {
  const response = await request.get("/version.json");
  expect(response.ok()).toBeTruthy();
  const initial = await response.json();
  let buildId: string = initial.buildId;
  let checks = 0;
  await page.route("**/version.json", route => {
    checks++;
    return route.fulfill({ contentType: "application/json", body: JSON.stringify({ buildId }) });
  });
  await page.route("**/api/**", route => route.fulfill({ status: 401, contentType: "application/json", body: "{}" }));
  await page.goto("/login");
  await expect.poll(() => checks).toBe(1);
  await expect(page.getByRole("button", { name: "Update now" })).toHaveCount(0);
  await page.evaluate(() => localStorage.setItem("release-test-session", "retained"));
  let navigations = 0;
  page.on("framenavigated", frame => { if (frame === page.mainFrame()) navigations++; });
  buildId = "e2e-next-release";
  await page.evaluate(() => window.dispatchEvent(new Event("focus")));
  const update = page.getByRole("button", { name: "Update now" });
  await expect(update).toBeVisible();
  expect(navigations).toBe(0);
  await Promise.all([page.waitForEvent("domcontentloaded"), update.click()]);
  expect(navigations).toBe(1);
  expect(await page.evaluate(() => localStorage.getItem("release-test-session"))).toBe("retained");
});

for (const failure of ["offline", "html", "500"]) {
  test(`production metadata ${failure} leaves app usable`, async ({ page }) => {
    await page.route("**/version.json", route => failure === "offline" ? route.abort() : route.fulfill({
      status: failure === "500" ? 500 : 200, contentType: failure === "html" ? "text/html" : "application/json", body: "{}",
    }));
    await page.route("**/api/**", route => route.fulfill({ status: 401, contentType: "application/json", body: "{}" }));
    await page.goto("/login");
    await expect(page.getByRole("button", { name: "Update now" })).toHaveCount(0);
    await expect(page.getByRole("button", { name: "Sign in", exact: true })).toBeVisible();
  });
}
