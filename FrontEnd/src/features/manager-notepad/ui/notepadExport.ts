import type { ManagerNote } from "@entities/manager-notepad";
import { contentToEditorHtml, sanitizeRichText } from "./notepadRichText";

type ExportFormat = "txt" | "md";

function escapeMarkdown(value: string) {
  return value.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;")
    .replace(/[\\`*_{}[\]()#+.!|~-]/g, "\\$&");
}

function exportBody(content: string, format: ExportFormat, clipboard = false) {
  const template = document.createElement("template");
  template.innerHTML = clipboard ? content : contentToEditorHtml(content);
  const markdown = format === "md";

  function render(node: Node, depth = 0): string {
    if (node.nodeType === Node.TEXT_NODE) {
      return markdown ? escapeMarkdown(node.textContent ?? "") : node.textContent ?? "";
    }
    const element = node instanceof HTMLElement ? node : null;
    const text = () => [...node.childNodes].map(child => render(child, depth)).join("");
    switch (element?.tagName) {
      case "BR": return "\n";
      case "B":
      case "STRONG": return markdown && element.textContent?.trim() ? `**${text()}**` : text();
      case "I":
      case "EM": return markdown && element.textContent?.trim() ? `*${text()}*` : text();
      case "S":
      case "STRIKE": return markdown && element.textContent?.trim() ? `~~${text()}~~` : text();
      case "DIV":
      case "P":
      case "BLOCKQUOTE": return `${text()}\n`;
      case "UL":
      case "OL": {
        const checklist = element.dataset.checklist === "true";
        const items = [...element.children].filter(item => item.tagName === "LI");
        return `\n${items.map((item, index) => {
          const marker = checklist ? `- [${item.getAttribute("data-checked") === "true" ? "x" : " "}]`
            : element.tagName === "OL" ? `${index + 1}.` : "-";
          const descendants = [...item.childNodes].filter(child =>
            child.nodeValue?.trim() !== "" || child instanceof Element);
          const itemText = descendants.map(child => render(child, depth + 1)).join("").trimEnd();
          return `${"    ".repeat(depth)}${marker} ${itemText}\n`;
        }).join("")}`;
      }
      default: return text();
    }
  }

  const body = render(template.content);
  return clipboard ? body : body.trim();
}

export function formatNoteExport(note: Pick<ManagerNote, "title" | "content">, format: ExportFormat) {
  const title = note.title.replace(/\s+/g, " ").trim();
  const header = format === "md" ? `# ${escapeMarkdown(title)}` : title;
  return `${header}\n\n${exportBody(note.content, format)}\n`;
}

function copyWithTextarea(text: string, container: Element) {
  if (!container.isConnected) return false;
  const active = document.activeElement;
  const selection = document.getSelection();
  const ranges = selection ? Array.from({ length: selection.rangeCount }, (_, index) => selection.getRangeAt(index).cloneRange()) : [];
  const field = document.createElement("textarea");
  field.value = text;
  field.readOnly = true;
  field.tabIndex = -1;
  field.style.cssText = "position:fixed;top:0;left:0;width:1px;height:1px;opacity:0;font-size:16px";
  container.append(field);
  try {
    field.focus({ preventScroll: true });
    field.select();
    field.setSelectionRange(0, field.value.length);
    return document.execCommand("copy");
  } catch {
    return false;
  } finally {
    field.remove();
    if (active instanceof HTMLElement && active.isConnected) active.focus({ preventScroll: true });
    if (selection && ranges.length) {
      selection.removeAllRanges();
      ranges.forEach(range => selection.addRange(range));
    }
  }
}

export async function copyNoteToClipboard(title: string, editorHtml: string, mobileContainer?: Element) {
  const content = sanitizeRichText(editorHtml, true);
  const text = `${title}\n\n${exportBody(content, "txt", true)}`;
  const clipboard = navigator.clipboard;
  const writeRich = (clipboard: Clipboard) => {
    const template = document.createElement("template");
    template.innerHTML = content;
    // Use visible markers so checklist state survives paste into other editors.
    template.content.querySelectorAll<HTMLLIElement>('ul[data-checklist="true"] > li').forEach(item => {
      item.style.listStyleType = "none";
      item.prepend(item.dataset.checked === "true" ? "☑ " : "☐ ");
    });
    template.content.querySelectorAll("[data-checklist], [data-checked]").forEach(element => {
      element.removeAttribute("data-checklist");
      element.removeAttribute("data-checked");
    });
    const heading = document.createElement("h1");
    heading.textContent = title;
    template.content.prepend(heading);
    const html = new Blob([template.innerHTML], { type: "text/html" });
    const plain = new Blob([text], { type: "text/plain" });
    return clipboard.write([new ClipboardItem({
      "text/html": mobileContainer ? Promise.resolve(html) : html,
      "text/plain": mobileContainer ? Promise.resolve(plain) : plain,
    })]);
  };

  if (clipboard?.write && typeof ClipboardItem !== "undefined" &&
    (!ClipboardItem.supports || (ClipboardItem.supports("text/html") &&
      (!mobileContainer || ClipboardItem.supports("text/plain"))))) {
    try {
      await writeRich(clipboard);
      return "copied" as const;
    } catch (error) {
      if (!mobileContainer) throw error;
    }
  }

  try {
    if (!clipboard?.writeText) throw new Error("Clipboard writing is unavailable");
    await clipboard.writeText(text);
    return mobileContainer ? "text" as const : "copied" as const;
  } catch (error) {
    if (!mobileContainer) throw error;
    return copyWithTextarea(text, mobileContainer) ? "text" as const : { text };
  }
}

export function exportNote(note: ManagerNote, format: ExportFormat) {
  if (!note.canManage) return;
  const fileName = note.title.replace(/[^\p{L}\p{N} _-]/gu, "_").trim().slice(0, 100) || "note";
  const mime = format === "md" ? "text/markdown" : "text/plain";
  const url = URL.createObjectURL(new Blob([formatNoteExport(note, format)], { type: `${mime};charset=utf-8` }));
  const link = document.createElement("a");
  link.href = url;
  link.download = `${fileName}.${format}`;
  document.body.append(link);
  link.click();
  link.remove();
  window.setTimeout(() => URL.revokeObjectURL(url), 0);
}
