import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { ManagerNote } from "@entities/manager-notepad";
import { copyNoteToClipboard, exportNote, formatNoteExport } from "./notepadExport";

const title = "<Note> [Żółć] & Я";
const content = '<p>Українська<br><strong>Polski</strong></p><ul data-checklist="true">' +
  '<li data-checked="true">First<ul><li>Nested</li></ul></li><li data-checked="false">Second</li></ul>';
const initialClipboard = Object.getOwnPropertyDescriptor(navigator, "clipboard");
const initialExecCommand = Object.getOwnPropertyDescriptor(document, "execCommand");

function clipboard(value: Partial<Clipboard> | undefined) {
  Object.defineProperty(navigator, "clipboard", { configurable: true, value });
}

function modal() {
  const element = document.createElement("div");
  element.setAttribute("data-manager-notepad-modal", "");
  document.body.append(element);
  return element;
}

function readBlob(blob: Blob): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(String(reader.result));
    reader.onerror = () => reject(reader.error);
    reader.readAsText(blob);
  });
}

beforeEach(() => {
  vi.stubGlobal("ClipboardItem", class {
    static supports = () => true;
    constructor(public formats: Record<string, Blob | Promise<Blob>>) {}
  });
});
afterEach(() => {
  document.body.replaceChildren();
  if (initialClipboard) Object.defineProperty(navigator, "clipboard", initialClipboard);
  else Reflect.deleteProperty(navigator, "clipboard");
  if (initialExecCommand) Object.defineProperty(document, "execCommand", initialExecCommand);
  else Reflect.deleteProperty(document, "execCommand");
  vi.restoreAllMocks();
  vi.unstubAllGlobals();
});

describe("notepad export and clipboard", () => {
  it("renders readable multiline Unicode and checklist state in both formats", () => {
    const note = { title, content };
    expect(formatNoteExport(note, "txt")).toContain(`${title}\n\nУкраїнська\nPolski`);
    const markdown = formatNoteExport(note, "md");
    expect(markdown).toContain("# &lt;Note&gt; \\[Żółć\\] &amp; Я");
    expect(markdown).toContain("**Polski**");
    for (const format of ["txt", "md"] as const) {
      const output = formatNoteExport(note, format);
      expect(output).toContain("- [x] First\n    - Nested");
      expect(output).toContain("- [ ] Second");
    }
  });

  it.each([false, true])("writes sanitized rich and plain clipboard data (phone: %s)", async phone => {
    const write = vi.fn<Clipboard["write"]>().mockResolvedValue();
    const writeText = vi.fn<Clipboard["writeText"]>();
    clipboard({ write, writeText });
    const unsafe = '<img src="x" onerror="unsafe()"><script>unsafe()</script><a href="javascript:unsafe()">label</a>';
    expect(await copyNoteToClipboard(title, content + unsafe, phone ? modal() : undefined)).toBe("copied");
    expect(write).toHaveBeenCalledTimes(1);
    expect(writeText).not.toHaveBeenCalled();
    const item = write.mock.calls[0][0][0] as unknown as { formats: Record<string, Blob | Promise<Blob>> };
    expect(Object.keys(item.formats)).toEqual(["text/html", "text/plain"]);
    expect(item.formats["text/html"] instanceof Promise).toBe(phone);
    const html = await readBlob(await item.formats["text/html"]);
    expect(html).toContain("<h1>&lt;Note&gt; [Żółć] &amp; Я</h1>");
    expect(html).toContain("☑ First");
    expect(html).toContain("☐ Second");
    expect(html).not.toMatch(/<img|<script|onerror=|javascript:|data-checked/);
    const plain = await readBlob(await item.formats["text/plain"]);
    expect(plain).toContain("Українська\nPolski");
    expect(plain).toContain("- [x] First");
  });

  it("uses plain text if rich writes reject, then uses the active modal as the fallback", async () => {
    const container = modal();
    const write = vi.fn<Clipboard["write"]>().mockRejectedValue(new Error("Denied"));
    const writeText = vi.fn<Clipboard["writeText"]>().mockResolvedValueOnce().mockRejectedValue(new Error("Denied"));
    clipboard({ write, writeText });
    const field = document.createElement("button");
    field.textContent = "Focus";
    container.append(field);
    field.focus();
    const range = document.createRange();
    range.selectNodeContents(field);
    const selection = document.getSelection()!;
    selection.removeAllRanges();
    selection.addRange(range);
    expect(selection.toString()).toBe("Focus");
    const exec = vi.fn(() => {
      const fallback = container.querySelector("textarea");
      expect(fallback?.value).toBe(writeText.mock.calls[1][0]);
      expect(document.activeElement).toBe(fallback);
      return true;
    });
    Object.defineProperty(document, "execCommand", { configurable: true, value: exec });
    expect(await copyNoteToClipboard(title, content, container)).toBe("text");
    expect(exec).not.toHaveBeenCalled();
    expect(await copyNoteToClipboard(title, content, container)).toBe("text");
    expect(exec).toHaveBeenCalledExactlyOnceWith("copy");
    expect(container.querySelector("textarea")).toBeNull();
    expect(document.activeElement).toBe(field);
    expect(document.getSelection()?.toString()).toBe("Focus");
    exec.mockReturnValueOnce(false);
    expect(await copyNoteToClipboard(title, content, container)).toEqual({ text: writeText.mock.calls[2][0] });
    expect(container.querySelector("textarea")).toBeNull();
    await expect(copyNoteToClipboard(title, content)).rejects.toThrow("Denied");
  });

  it("exports only manageable notes with a bounded safe filename and cleans up the download", async () => {
    const nativeUrl = URL;
    const createObjectURL = vi.fn(() => "blob:note");
    const revokeObjectURL = vi.fn();
    vi.stubGlobal("URL", class extends nativeUrl {
      static createObjectURL = createObjectURL;
      static revokeObjectURL = revokeObjectURL;
    });
    const download = vi.spyOn(HTMLAnchorElement.prototype, "click").mockImplementation(function (this: HTMLAnchorElement) {
      expect(this.download).toBe(`_${"Ż".repeat(99)}.md`);
      expect(this.isConnected).toBe(true);
    });
    const note = { title: `/${"Ż".repeat(110)}`, content, canManage: false } as ManagerNote;
    exportNote(note, "md");
    expect(createObjectURL).not.toHaveBeenCalled();
    exportNote({ ...note, canManage: true }, "md");
    expect(download).toHaveBeenCalledTimes(1);
    expect(document.querySelector("a[download]")).toBeNull();
    expect(createObjectURL).toHaveBeenCalledWith(expect.objectContaining({ type: "text/markdown;charset=utf-8" }));
    await new Promise(resolve => window.setTimeout(resolve, 0));
    expect(revokeObjectURL).toHaveBeenCalledExactlyOnceWith("blob:note");
  });
});
