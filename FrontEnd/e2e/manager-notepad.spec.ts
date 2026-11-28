import { expect, type Page } from "@playwright/test";
import { test } from "./fixtures/isolatedBackend";
import { expectVisibleField, setVisualViewport } from "./fixtures/visualViewport";
import { loginAsManager, loginWithCredentials } from "./helpers";

const notesApi = "/api/manager-notepad/notes";
const panel = (page: Page) => page.getByRole("dialog", { name: "Manager notepad" });

async function managerHeaders(page: Page) {
  const token = await page.evaluate(() => localStorage.getItem("gf3.auth.access-token"));
  expect(token).toBeTruthy();
  return { Authorization: `Bearer ${token}` };
}

async function seedNote(page: Page, title: string, visibility = "private", content = "Initial body") {
  const response = await page.request.post(notesApi, { headers: await managerHeaders(page), data: { title, content, visibility } });
  expect(response.status()).toBe(201);
  return await response.json() as { id: number; revision: number };
}

async function getNote(page: Page, id: number) {
  const response = await page.request.get(`${notesApi}/${id}`, { headers: await managerHeaders(page) });
  expect(response.ok()).toBe(true);
  return await response.json();
}

async function openDesktop(page: Page) {
  const launcher = page.getByRole("button", { name: "Open notepad", exact: true });
  await launcher.click();
  await expect(panel(page)).toBeVisible();
}

async function openPhone(page: Page) {
  await loginWithCredentials(page, "manager", "123456");
  await page.goto("/manager-profile");
  await page.getByRole("button", { name: "Switch to Phone", exact: true }).click();
  await expect(page.locator("[data-manager-phone]")).toBeVisible();
  await page.goto("/");
  const launcher = page.getByRole("button", { name: "Notepad", exact: true });
  await launcher.click();
  await expect(panel(page)).toBeVisible();
  return launcher;
}

test("desktop private note CRUD, explicit flush and history survive reload", async ({ page }, info) => {
  test.skip(!["chromium", "webkit-laptop"].includes(info.project.name), "Desktop editor and panel controls");
  test.setTimeout(60_000);
  await page.clock.install();
  await loginAsManager(page);
  await openDesktop(page);
  const notepad = panel(page);
  await notepad.getByRole("button", { name: "New", exact: true }).click();
  await page.clock.pauseAt(await page.evaluate(() => Date.now() + 1000));
  await notepad.getByRole("textbox", { name: "Note title" }).fill("Release checklist");
  const content = notepad.getByRole("textbox", { name: "Note content" });
  await content.fill("Verify schedule");
  await content.evaluate(element => {
    const range = document.createRange();
    range.selectNodeContents(element);
    const selection = document.getSelection()!;
    selection.removeAllRanges();
    selection.addRange(range);
    element.dispatchEvent(new MouseEvent("mouseup", { bubbles: true }));
  });
  await notepad.getByRole("button", { name: "Bold", exact: true }).press("Enter");
  const created = page.waitForResponse(response => new URL(response.url()).pathname === notesApi && response.request().method() === "POST");
  await notepad.getByRole("button", { name: "Notes", exact: true }).press("Enter");
  await page.clock.resume();
  const response = await created;
  expect(response.status()).toBe(201);
  const { id } = await response.json();
  const initial = await getNote(page, id);
  expect(initial).toMatchObject({ title: "Release checklist", revision: 1, visibility: "private" });
  expect(initial.content).toMatch(/<(?:b|strong)>Verify schedule<\/(?:b|strong)>/);
  await page.reload();
  await expect(notepad).toBeVisible();
  await notepad.getByRole("button", { name: /Release checklist/ }).click();
  await expect(content).toContainText("Verify schedule");
  await notepad.getByRole("textbox", { name: "Note title" }).fill("Edited checklist");
  await expect.poll(async () => getNote(page, id)).toMatchObject({ revision: 2, title: "Edited checklist" });
  await notepad.getByRole("button", { name: "Note actions" }).click();
  await page.getByRole("button", { name: /^Version history/ }).click();
  await notepad.getByRole("button", { name: "Restore revision 1", exact: true }).click();
  await page.getByRole("dialog", { name: "Restore revision 1?" }).getByRole("button", { name: "Restore", exact: true }).click();
  await expect.poll(async () => (await getNote(page, id)).revision).toBe(3);
  await expect(notepad.getByRole("textbox", { name: "Note title" })).toHaveValue("Release checklist");
  await notepad.getByRole("button", { name: "Notes", exact: true }).click();
  await notepad.getByRole("button", { name: "Pin note", exact: true }).click();
  await expect(notepad.getByRole("button", { name: "Unpin note", exact: true })).toHaveAttribute("aria-pressed", "true");
  await expect.poll(async () => (await getNote(page, id)).isPinned).toBe(true);
  await notepad.getByRole("button", { name: /Release checklist/ }).click();
  await notepad.getByRole("button", { name: "Note actions" }).click();
  await page.getByRole("switch", { name: "Lock editing" }).click();
  await expect(content).toHaveAttribute("contenteditable", "false");
  await expect.poll(async () => (await getNote(page, id)).isLocked).toBe(true);
  await notepad.getByRole("button", { name: "Note actions" }).click();
  await page.getByRole("button", { name: /^Delete note/ }).click();
  await page.getByRole("dialog", { name: "Delete note?" }).getByRole("button", { name: "Delete", exact: true }).click();
  const headers = await managerHeaders(page);
  await expect.poll(async () => (await page.request.get(`${notesApi}/${id}`, { headers })).status()).toBe(404);
});

test("two manager tabs coordinate a public note lease", async ({ page, context }, info) => {
  test.skip(!["chromium", "webkit-laptop"].includes(info.project.name), "Desktop editor across tabs");
  test.setTimeout(60_000);
  await loginAsManager(page);
  const note = await seedNote(page, "Public handoff", "public");
  const state = page.waitForResponse(response => new URL(response.url()).pathname === "/api/manager-notepad/state" &&
    response.request().method() === "PUT");
  await openDesktop(page);
  expect((await state).status()).toBe(204);
  const other = await context.newPage();
  try {
    await other.goto("/");
    await expect(panel(other)).toBeVisible();
    for (const tab of [page, other]) {
      await panel(tab).getByRole("button", { name: /^Public 1$/ }).click();
      await panel(tab).getByRole("button", { name: /Public handoff/ }).click();
    }
    const first = panel(page).getByRole("textbox", { name: "Note content" });
    const second = panel(other).getByRole("textbox", { name: "Note content" });
    await expect(first).toHaveAttribute("contenteditable", "true");
    await expect(second).toHaveAttribute("contenteditable", "false");
    await first.fill("Saved by first tab");
    await expect.poll(async () => getNote(page, note.id)).toMatchObject({
      content: "Saved by first tab", revision: note.revision + 1,
    });
    await panel(page).getByRole("button", { name: "Notes", exact: true }).click();
    await expect.poll(async () => (await getNote(page, note.id)).activeEditor ?? null).toBeNull();
    await panel(other).getByRole("button", { name: "Try again", exact: true }).click();
    await expect(second).toHaveAttribute("contenteditable", "true");
    await expect(second).toHaveText("Saved by first tab");
    await other.route(`**${notesApi}/${note.id}`, async route => {
      if (route.request().method() !== "PUT") return route.continue();
      await route.fulfill({ status: 409, json: { code: "notepad_edit_session_conflict", title: "Lease lost" } });
    });
    const conflict = other.waitForResponse(response =>
      new URL(response.url()).pathname === `${notesApi}/${note.id}` && response.request().method() === "PUT");
    await second.fill("Keep the second draft");
    expect((await conflict).status()).toBe(409);
    await expect(second).toHaveText("Keep the second draft");
    await expect(second).toHaveAttribute("contenteditable", "false");
    expect((await getNote(other, note.id)).content).toBe("Saved by first tab");
    const history = await page.request.get(`${notesApi}/${note.id}/versions`, { headers: await managerHeaders(page) });
    expect(await history.json()).toHaveLength(1);
  } finally {
    await other.close();
  }
});

test.describe("manager phone notepad", () => {
  test.skip(({ isMobile }) => !isMobile, "Mobile native dialog and viewport");

  test("manager phone notepad is a real modal and uses explicit save", async ({ page }) => {
    const launcher = await openPhone(page);
    const notepad = panel(page);
    await expect(page.locator("dialog[data-manager-notepad-modal]:modal")).toBeVisible();
    await expect(page.locator("[data-manager-phone] > [inert] main")).toHaveCount(1);
    await expect.poll(() => page.evaluate(() => Boolean(document.activeElement?.closest("[data-manager-notepad-modal]")))).toBe(true);
    await page.keyboard.press("Tab");
    await expect.poll(() => page.evaluate(() => Boolean(document.activeElement?.closest("[data-manager-notepad-modal]")))).toBe(true);
    await notepad.getByRole("button", { name: "Close notepad" }).click();
    await expect(notepad).toHaveCount(0);
    await expect(launcher).toBeFocused();
    await page.getByRole("link", { name: "More", exact: true }).click();
    await page.getByRole("button", { name: "Notepad", exact: true }).click();
    await expect(page.locator("dialog[data-manager-notepad-modal]:modal")).toHaveCount(1);
  });

  test("phone keyboard focus uses the available viewport", async ({ page }) => {
    await openPhone(page);
    const notepad = panel(page);
    await notepad.getByRole("button", { name: "New", exact: true }).click();
    const title = notepad.getByRole("textbox", { name: "Note title" });
    await title.fill("Keyboard text");
    await setVisualViewport(page, { height: 380, top: 25 });
    const editor = notepad.getByRole("region", { name: "Note editor" });
    await expectVisibleField(title, editor);
    await expect(notepad.getByRole("button", { name: "Done", exact: true })).toHaveCount(0);
    const content = notepad.getByRole("textbox", { name: "Note content" });
    await content.fill("Writing area");
    await expectVisibleField(content, editor);
    expect(await content.evaluate(element => element.getBoundingClientRect().height)).toBeGreaterThan(64);
    expect(await page.evaluate(() => visualViewport!.scale)).toBe(1);
    await expect(notepad.getByRole("toolbar", { name: "Text formatting" })).toBeVisible();
    await page.keyboard.press("Escape");
    await setVisualViewport(page, null);
    await expect(title).toHaveValue("Keyboard text");
    await expect(content).toHaveText("Writing area");
  });

  test("phone unsaved changes and failed writes are recoverable", async ({ page }) => {
    await openPhone(page);
    const notepad = panel(page);
    const writes: string[] = [];
    await page.clock.install();
    await page.route(`**${notesApi}`, async route => {
      if (route.request().method() !== "POST") return route.continue();
      writes.push(route.request().postDataJSON().title);
      if (writes.length === 1) await route.fulfill({ status: 503, json: { title: "Offline" } });
      else await route.continue();
    });
    await notepad.getByRole("button", { name: "New", exact: true }).click();
    const title = notepad.getByRole("textbox", { name: "Note title" });
    await title.fill("Recover phone draft");
    await page.clock.fastForward(2_000);
    expect(writes).toEqual([]);
    await page.keyboard.press("Escape");
    await notepad.getByRole("button", { name: "Save", exact: true }).click();
    await expect(notepad.getByRole("button", { name: "Retry", exact: true })).toBeVisible();
    await expect(title).toHaveValue("Recover phone draft");
    await notepad.getByRole("button", { name: "Notes", exact: true }).click();
    const prompt = page.getByRole("dialog", { name: "Discard unsaved changes?" });
    await expect(prompt.getByRole("button", { name: "Save changes" })).toBeVisible();
    await prompt.getByRole("button", { name: "Keep editing" }).click();
    await expect(title).toHaveValue("Recover phone draft");
    await notepad.getByRole("button", { name: "Retry", exact: true }).click();
    await expect(notepad.getByRole("button", { name: "Saved", exact: true })).toBeDisabled();
    expect(writes).toEqual(["Recover phone draft", "Recover phone draft"]);
    await page.reload();
    await page.getByRole("button", { name: "Notepad", exact: true }).click();
    await expect(notepad.getByRole("button", { name: /Recover phone draft/ })).toBeVisible();
  });

  test("mobile clipboard success and fallback", async ({ page }) => {
    await openPhone(page);
    const notepad = panel(page);
    const title = "Żółć і Україна";
    await notepad.getByRole("button", { name: "Close notepad" }).click();
    await seedNote(page, title, "private", '<ul data-checklist="true"><li data-checked="true">Готово</li><li data-checked="false">Później</li></ul>');
    await page.reload();
    await page.getByRole("button", { name: "Notepad", exact: true }).click();
    await notepad.getByRole("button", { name: new RegExp(title) }).click();
    const copy = notepad.getByRole("button", { name: "Copy note" });
    await page.evaluate(() => {
      Object.defineProperty(navigator, "clipboard", { configurable: true, value: {
        write() { return Promise.reject(new Error("Unavailable in browser fixture")); },
        writeText(text: string) { document.documentElement.dataset.copied = text; return Promise.resolve(); },
      } });
    });
    await copy.click();
    await expect(page.locator("html")).toHaveAttribute("data-copied", /Żółć і Україна[\s\S]*\[x\] Готово[\s\S]*\[ \] Później/);
    await page.evaluate(() => {
      Object.defineProperty(navigator, "clipboard", { configurable: true, value: undefined });
      Object.defineProperty(document, "execCommand", { configurable: true, value: () => false });
    });
    await copy.click();
    const manual = notepad.getByRole("textbox", { name: "Select and copy the note manually." });
    await expect(manual).toHaveValue(/Żółć і Україна[\s\S]*\[x\] Готово[\s\S]*\[ \] Później/);
  });
});
