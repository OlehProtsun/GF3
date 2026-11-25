import { expect, test, type Locator } from "@playwright/test";

const png = Buffer.from("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aFxsAAAAASUVORK5CYII=", "base64");
const gif = Buffer.from("47494638396101000100800000000000FFFFFF2C00000000010001000002024401002C00000000010001000002024401003B", "hex");
const title = "Fixture Photo and GIF";
const message = "Fixture text in the middle";

test("ordered photo, text and GIF survive composer preview, save and employee dismissal", async ({ page }) => {
  await page.emulateMedia({ reducedMotion: "no-preference" });
  await page.addInitScript(() => {
    localStorage.setItem("gf3.auth.access-token", "communications-fixture-only");
    sessionStorage.setItem("gf3.pwa.install.dismissed", "1");
  });
  let employee = false;
  let dismissed = false;
  let expired = false;
  let saved: { id: number; title: string; body: string; blocks: { type: string; id: string; text?: string }[]; visibleFromUtc: string;
    deadlineAtUtc: string; createdAtUtc: string; createdByManagerName: string; isActive: boolean } | null = null;
  await page.route(/^https?:\/\//, async route => {
    const request = route.request();
    const url = new URL(request.url());
    if (!["localhost", "127.0.0.1", "[::1]"].includes(url.hostname)) return route.abort("blockedbyclient");
    const path = url.pathname;
    if (!path.startsWith("/api/")) return route.continue();
    if (path.endsWith("/negotiate")) return route.fulfill({ status: 503 });
    if (path === "/api/employee-launches") return route.fulfill({ status: 204 });
    if (path === "/api/auth/session") return route.fulfill({ json: employee
      ? { role: "employee", employeeId: 2, userName: "worker", displayName: "Fixture Employee" }
      : { role: "manager", workspaceMode: "pc", managerId: 1, isSystemManager: true, userName: "manager", displayName: "Fixture Manager" } });
    if (path === "/api/account-language") return route.fulfill({ json: { language: "en" } });
    if (path.includes("notepad")) return route.fulfill({ json: { notes: [], state: { isExpanded: false, isPinned: false, height: 700 } } });
    if (path === "/api/communications/with-gif" && request.method() === "POST") {
      const form = await new Response(new Uint8Array(request.postDataBuffer()!), { headers: { "Content-Type": request.headers()["content-type"] } }).formData();
      const blocks = (JSON.parse(String(form.get("Blocks"))) as NonNullable<typeof saved>["blocks"]).map(block => {
        if (block.type === "text") return block;
        const file = form.get(`Block-${block.id}`) as File;
        expect(file).toBeInstanceOf(File);
        expect(file.type).toBe(block.type === "photo" ? "image/png" : "image/gif");
        return { id: block.id, type: block.type, media: { key: block.id, fileName: file.name, size: file.size, contentType: file.type } };
      });
      saved = { id: 7, title: String(form.get("title")), body: String(form.get("body")), visibleFromUtc: String(form.get("visibleFromUtc")),
        deadlineAtUtc: String(form.get("deadlineAtUtc")), createdAtUtc: new Date().toISOString(), createdByManagerName: "Fixture Manager", isActive: true, blocks };
      return route.fulfill({ json: saved });
    }
    if (path === "/api/communications") return route.fulfill({ json: saved ? [saved] : [] });
    if (path === "/api/communications/pending") return route.fulfill({ json: saved && !dismissed && !expired ? [saved] : [] });
    if (path === "/api/communications/7/dismiss") {
      expect(request.method()).toBe("POST");
      dismissed = true;
      return route.fulfill({ status: 204 });
    }
    if (path.startsWith("/api/communications/7/photo/")) return route.fulfill({ body: png, contentType: "image/png" });
    if (path.startsWith("/api/communications/7/gif/")) return route.fulfill({ body: gif, contentType: "image/gif" });
    return route.fulfill({ json: [] });
  });

  await page.goto("/communications");
  await page.getByRole("textbox", { name: "Title" }).fill(title);
  await page.getByRole("textbox", { name: "Text", exact: true }).fill(message);
  await page.getByLabel("Add photo").setInputFiles({ name: "demo.png", mimeType: "image/png", buffer: png });
  await page.getByLabel("Add GIF").setInputFiles({ name: "demo.gif", mimeType: "image/gif", buffer: gif });
  const editor = page.getByRole("list", { name: "Content order" });
  await editor.getByRole("listitem").nth(1).getByRole("button", { name: "Move up" }).click();
  await page.getByRole("button", { name: "Preview", exact: true }).click();
  const preview = page.getByRole("dialog", { name: "Employee Preview" });
  await assertOrder(preview);
  await preview.getByRole("button", { name: "Close communication" }).click();
  await page.getByRole("button", { name: "Create", exact: true }).click();
  await expect.poll(() => saved?.blocks.map(block => block.type)).toEqual(["photo", "text", "gif"]);
  await expect(page.getByRole("textbox", { name: "Text", exact: true })).toHaveValue("");

  employee = true;
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto("/schedule");
  const announcement = page.getByRole("dialog", { name: title });
  await assertOrder(announcement);
  await announcement.getByRole("checkbox", { name: "Don't show this again" }).check();
  await announcement.getByRole("button", { name: "Close", exact: true }).click();
  await expect.poll(() => dismissed).toBe(true);
  await expect(announcement).toHaveCount(0);
  await page.reload();
  await expect(page.locator("[data-employee-content]")).toBeVisible();
  await expect(announcement).toHaveCount(0);
  expired = true;
  dismissed = false;
  await page.reload();
  await expect(page.locator("[data-employee-content]")).toBeVisible();
  await expect(announcement).toHaveCount(0);
});

async function assertOrder(dialog: Locator) {
  const photo = dialog.getByRole("img", { name: "Photo attachment", exact: true });
  const text = dialog.getByText(message, { exact: true });
  const motion = dialog.getByRole("img", { name: "Animated demonstration" });
  await expect(photo).toBeVisible();
  await expect(text).toBeVisible();
  await expect(motion).toBeVisible();
  expect((await photo.boundingBox())!.y).toBeLessThan((await text.boundingBox())!.y);
  expect((await text.boundingBox())!.y).toBeLessThan((await motion.boundingBox())!.y);
}
