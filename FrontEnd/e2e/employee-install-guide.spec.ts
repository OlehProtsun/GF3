import { expect, test } from "@playwright/test";

test("employee guide remembers language and navigates phone instructions, swaps and corrections", async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.addInitScript(() => {
    localStorage.setItem("gf3.auth.access-token", "guide-fixture-only");
    sessionStorage.setItem("gf3.pwa.install.dismissed", "1");
  });
  const missingImages: string[] = [];
  page.on("response", response => {
    if (response.request().resourceType() === "image" && !response.ok()) missingImages.push(new URL(response.url()).pathname);
  });
  await page.route("**/*", async route => {
    const url = new URL(route.request().url());
    if (!["localhost", "127.0.0.1", "[::1]"].includes(url.hostname)) return route.abort("blockedbyclient");
    if (!url.pathname.startsWith("/api/")) return route.continue();
    if (url.pathname.endsWith("/negotiate")) return route.fulfill({ status: 503 });
    if (url.pathname === "/api/employee-launches") return route.fulfill({ status: 204 });
    const body = url.pathname === "/api/auth/session"
      ? { role: "employee", employeeId: 1, userName: "worker", displayName: "Fixture Worker" }
      : url.pathname === "/api/account-language" ? { language: "en" } : [];
    return route.fulfill({ json: body });
  });

  await page.goto("/install");
  const chooser = page.getByRole("dialog", { name: "Choose your language" });
  await chooser.getByRole("radio", { name: /English/ }).check();
  await chooser.getByRole("button", { name: "Continue" }).click();
  const topics = page.getByRole("navigation", { name: "Guide topics" });
  await expect(topics.getByRole("link")).toHaveCount(8);
  for (const [title, topic] of [["Getting Started", "getting-started"], ["Shift Swaps", "shift-swaps"], ["Shift Corrections", "shift-corrections"]]) {
    const link = topics.getByRole("link", { name: new RegExp(`^${title}`) });
    await expect(link).toHaveAttribute("href", `/install?topic=${topic}`);
    await link.click();
    const article = page.getByRole("article", { name: title });
    await expect(article).toBeVisible();
    const capture = article.locator('img[src^="/guide/"]').first();
    await capture.scrollIntoViewIfNeeded();
    await expect.poll(() => capture.evaluate(image => (image as HTMLImageElement).naturalWidth)).toBeGreaterThan(0);
    expect(await article.evaluate(element => element.scrollWidth <= element.clientWidth + 1)).toBe(true);
    await article.getByRole("link", { name: "All topics" }).click();
  }

  await page.getByRole("button", { name: /Change guide language/ }).click();
  await chooser.getByRole("radio", { name: /Українська/ }).check();
  await chooser.getByRole("button", { name: "Continue" }).click();
  await expect(page.locator('article[lang="uk"]')).toBeVisible();
  await page.reload();
  await expect(chooser.getByRole("radio", { name: /Українська/ })).toBeChecked();
  expect(missingImages).toEqual([]);
});
