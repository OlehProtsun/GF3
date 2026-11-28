import type { ManagerNote, ManagerNoteVisibility } from "@entities/manager-notepad";
import { t } from "@shared/i18n";

export const lineSpacingValues = ["1", "1.15", "1.5", "1.75", "2"] as const;
export const spacingBlockSelector = "p, div, li, blockquote";
const spacingTags = new Set(["P", "DIV", "LI", "BLOCKQUOTE"]);

const allowedTags = new Set(["B", "STRONG", "I", "EM", "U", "S", "STRIKE", "P", "DIV", "BR", "UL", "OL", "LI", "SPAN", "FONT", "BLOCKQUOTE"]);
const blockedTags = new Set(["SCRIPT", "STYLE", "IFRAME", "OBJECT", "EMBED", "SVG", "MATH", "FORM", "INPUT", "BUTTON", "TEMPLATE"]);
const richTextPattern = /<(?:b|strong|i|em|u|s|strike|p|div|br|ul|ol|li|span|font|blockquote)(?=[\s/>])|&(?:amp|lt|gt|nbsp);/i;

function safeColor(value: string) {
  return /^(?:#[0-9a-f]{3,8}|rgba?\([\d .,/%]+\)|[a-z]+)$/i.test(value.trim());
}

export function plainTextToHtml(value: string) {
  return value.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/\r?\n/g, "<br>");
}

export function sanitizeRichText(html: string, preserveInlineEmphasis = false) {
  if (typeof document === "undefined") return plainTextToHtml(html);
  const template = document.createElement("template");
  template.innerHTML = html;
  const stripUnsafe = (node: ParentNode) => {
    [...node.childNodes].forEach(child => {
      if (child.nodeType === Node.COMMENT_NODE) {
        child.remove();
        return;
      }
      if (child.nodeType !== Node.ELEMENT_NODE) return;
      const element = child as HTMLElement;
      if (element.namespaceURI !== "http://www.w3.org/1999/xhtml" || blockedTags.has(element.tagName)) {
        element.remove();
        return;
      }
      stripUnsafe(element);
      if (!allowedTags.has(element.tagName)) {
        element.replaceWith(...element.childNodes);
        return;
      }

      const color = element.getAttribute("color") ?? element.style.color;
      const size = element.getAttribute("size");
      const { fontSize, textAlign, lineHeight } = element.style;
      const { fontWeight, fontStyle, textDecorationLine } = element.style;
      const checklist = element.getAttribute("data-checklist");
      const checked = element.getAttribute("data-checked");
      [...element.attributes].forEach(attribute => element.removeAttribute(attribute.name));

      if (element.tagName === "UL" && checklist === "true") element.setAttribute("data-checklist", "true");
      if (element.tagName === "LI" && (checked === "true" || checked === "false")) element.setAttribute("data-checked", checked);
      if (element.tagName === "FONT") {
        if (color && safeColor(color)) element.setAttribute("color", color);
        if (size && /^[1-7]$/.test(size)) element.setAttribute("size", size);
      } else if (color && safeColor(color)) {
        element.style.color = color;
      }
      if (/^\d+(?:\.\d+)?(?:px|rem|em|%)$/.test(fontSize)) element.style.fontSize = fontSize;
      if (spacingTags.has(element.tagName) && lineSpacingValues.some(value => value === lineHeight)) {
        element.style.lineHeight = lineHeight;
      }
      if (preserveInlineEmphasis) {
        if (/^(?:normal|bold|[1-9]00)$/.test(fontWeight)) element.style.fontWeight = fontWeight;
        if (fontStyle === "normal" || fontStyle === "italic" || fontStyle === "oblique") element.style.fontStyle = fontStyle;
        if (/^(?:none|underline|line-through|underline line-through|line-through underline)$/.test(textDecorationLine)) {
          element.style.textDecorationLine = textDecorationLine;
        }
      }
      if (["left", "center", "right", "justify"].includes(textAlign)) element.style.textAlign = textAlign;
    });
  };
  stripUnsafe(template.content);
  return template.innerHTML;
}

export function contentToEditorHtml(value: string) {
  return richTextPattern.test(value) ? sanitizeRichText(value) : plainTextToHtml(value);
}

export function richTextToPlainText(value: string) {
  if (!richTextPattern.test(value) || typeof document === "undefined") return value;
  const template = document.createElement("template");
  template.innerHTML = sanitizeRichText(value);
  template.content.querySelectorAll("br").forEach(br => br.replaceWith("\n"));
  template.content.querySelectorAll("p, div, ul, ol, li, blockquote").forEach(block => block.after("\n"));
  return (template.content.textContent ?? "").trimEnd();
}

export function notePreview(note: Pick<ManagerNote, "content">) {
  return richTextToPlainText(note.content).trim().replace(/\s+/g, " ") || t("Empty note");
}

export function visibilityLabel(visibility: ManagerNoteVisibility) {
  if (visibility === "custom") return t("Shared");
  return visibility === "public" ? t("Public") : t("Private");
}
