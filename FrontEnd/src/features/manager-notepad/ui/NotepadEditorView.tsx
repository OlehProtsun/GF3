import { useCallback, useEffect, useEffectEvent, useLayoutEffect, useMemo, useRef, useState, type ClipboardEvent, type MouseEvent, type ReactNode } from "react";
import type { ManagerNote } from "@entities/manager-notepad";
import { getLocale, t } from "@shared/i18n";
import { createPortal } from "react-dom";
import { useDropdownPosition } from "@shared/lib/useDropdownPosition";
import { CheckIcon, CopyLinkIcon, SaveIcon } from "@shared/ui/icons";
import { copyNoteToClipboard } from "./notepadExport";
import { lineSpacingValues, plainTextToHtml, sanitizeRichText, spacingBlockSelector, visibilityLabel } from "./notepadRichText";
import { NotepadIcon } from "./NotepadIcon";
import { NotepadSettingsMenu } from "./NotepadSettingsMenu";
import { NotepadVisibilityControl } from "./NotepadVisibilityControl";
import { MAX_NOTE_CONTENT, type NoteAccess } from "./useNotepadAutosave";
import styles from "./ManagerNotepad.module.css";
import views from "./NotepadViews.module.css";

const textColors = [
  { value: "#0f172a", label: "Ink" }, { value: "#475569", label: "Slate" },
  { value: "#2563eb", label: "Blue" }, { value: "#7c3aed", label: "Violet" },
  { value: "#db2777", label: "Pink" }, { value: "#dc2626", label: "Red" },
  { value: "#ea580c", label: "Orange" }, { value: "#ca8a04", label: "Gold" },
  { value: "#16a34a", label: "Green" }, { value: "#0891b2", label: "Teal" },
  { value: "#ffffff", label: "White" },
];
const spacingLabels = { "1": "Single", "1.15": "Compact", "1.5": "Normal", "1.75": "Relaxed", "2": "Double" };
const formattingMenus = {
  spacing: { label: "Line spacing", icon: "lineSpacing", width: 220 },
  size: { label: "Text size", icon: "textSize", width: 246 },
  color: { label: "Text color", icon: "palette", width: 236 },
  more: { label: "More formatting actions", icon: "more", width: 232 },
} as const;
const fontSizes = ["12", "14", "16", "20", "24", "32"];
const inlineCommands = ["bold", "italic", "underline", "strikeThrough"] as const;
type ListFormat = "checklist" | "insertUnorderedList" | "insertOrderedList";
type FormatCommand = typeof inlineCommands[number] | ListFormat;
type ToolbarMenu = keyof typeof formattingMenus;
type LineSpacing = typeof lineSpacingValues[number];

function readTextColor() {
  const color = document.createElement("span").style;
  color.color = document.queryCommandValue?.("foreColor") ?? "";
  const selected = color.color;
  return textColors.find(({ value }) => {
    color.color = value;
    return color.color === selected;
  })?.value ?? null;
}

function closestBlock(node: Node | null | undefined, selector: string, editor: HTMLElement) {
  const element = node instanceof Element ? node : node?.parentElement;
  const block = element?.closest<HTMLElement>(selector);
  return block && block !== editor && editor.contains(block) ? block : null;
}

function listFormatAt(node: Node, editor: HTMLElement): ListFormat | null {
  const list = closestBlock(node, "li", editor)?.parentElement;
  if (list?.tagName === "OL") return "insertOrderedList";
  if (list?.tagName === "UL") {
    return list.dataset.checklist === "true" ? "checklist" : "insertUnorderedList";
  }
  return null;
}

function selectedListFormat(range: Range, editor: HTMLElement): ListFormat | null {
  if (range.collapsed) return listFormatAt(range.startContainer, editor);

  // Use the selected leaf content so nested lists and mixed list/paragraph selections stay distinct.
  const walk = editor.ownerDocument.createTreeWalker(range.commonAncestorContainer, NodeFilter.SHOW_ALL);
  let format: ListFormat | undefined;
  for (let node: Node | null = walk.currentNode; node; node = walk.nextNode()) {
    const element = node instanceof Element ? node : node.parentElement;
    if (element?.closest("button[data-checklist-toggle]")) continue;
    if (node instanceof Text) {
      if (!range.intersectsNode(node)) continue;
      const start = node === range.startContainer ? range.startOffset : 0;
      const end = node === range.endContainer ? range.endOffset : node.length;
      if (start >= end || (!node.data.slice(start, end).trim() && element?.matches("ul, ol"))) continue;
      if (!node.data.trim() && element === editor) continue;
    } else if (!(node instanceof HTMLBRElement) || !intersectsContents(range, node)) {
      continue;
    }
    const current = listFormatAt(node, editor);
    if (!current || (format && current !== format)) return null;
    format = current;
  }
  return format ?? null;
}

function hasChecklistContent(fragment: ParentNode) {
  fragment.querySelectorAll("button[data-checklist-toggle]").forEach(button => button.remove());
  return Boolean(fragment.textContent?.trim() || fragment.querySelector("ul, ol"));
}

function intersectsContents(range: Range, first: Node, last = first) {
  const bounds = range.cloneRange();
  bounds.setStartBefore(first);
  bounds.setEndAfter(last);
  if (range.collapsed) return bounds.isPointInRange(range.startContainer, range.startOffset);
  // Compare collapsed endpoints to exclude blocks merely touching the selection.
  const start = range.cloneRange();
  const end = range.cloneRange();
  start.collapse(true);
  end.collapse(false);
  const blockStart = bounds.cloneRange();
  bounds.collapse(false);
  blockStart.collapse(true);
  return start.compareBoundaryPoints(Range.START_TO_START, bounds) < 0 &&
    end.compareBoundaryPoints(Range.START_TO_START, blockStart) > 0;
}

function spacingBlocks(editor: HTMLElement, range: Range, createParagraphs = false) {
  const current = closestBlock(range.startContainer, spacingBlockSelector, editor);
  if (range.collapsed && current) return [current];

  const selected = Array.from(editor.querySelectorAll<HTMLElement>(spacingBlockSelector))
    .filter(block => intersectsContents(range, block));
  const blocks = selected.filter(block => !selected.some(other => other !== block && block.contains(other)));
  if (!createParagraphs) return blocks;

  // Older notes can contain unwrapped text and soft breaks at the editor root.
  const children = Array.from(editor.childNodes);
  let run: Node[] = [];
  const wrapRun = () => {
    if (run.length && intersectsContents(range, run[0], run[run.length - 1])) {
      const paragraph = editor.ownerDocument.createElement("p");
      editor.insertBefore(paragraph, run[0]);
      paragraph.append(...run);
      blocks.push(paragraph);
    }
    run = [];
  };
  for (const child of children) {
    if (child instanceof Element && child.matches(`${spacingBlockSelector}, ul, ol`)) wrapRun();
    else run.push(child);
  }
  wrapRun();
  if (!blocks.length && !editor.firstChild) {
    const paragraph = editor.ownerDocument.createElement("p");
    paragraph.append(editor.ownerDocument.createElement("br"));
    editor.append(paragraph);
    blocks.push(paragraph);
  }
  return blocks;
}

type Props = {
  note: ManagerNote | null;
  title: string;
  content: string;
  canEdit: boolean;
  workspaceReadOnly?: boolean;
  sessionBlocked?: boolean;
  dirty: boolean;
  isSaving: boolean;
  saveFailed: boolean;
  canSave: boolean;
  contentTooLong: boolean;
  isPhone?: boolean;
  focusEditing?: boolean;
  onStartFocusEditing?: () => void;
  onLeaveWritingArea?: () => void;
  onTitle: (title: string) => void;
  onContent: (content: string) => void;
  onSave: () => void;
  access?: NoteAccess;
  expanded: boolean;
  onAccessChange: (access: NoteAccess) => void;
  onBack: () => void;
  onDelete: () => void;
  onVersions: () => void;
  onLockChange: (locked: boolean) => void;
  collapseButton: ReactNode;
  children: ReactNode;
};

function ToolbarButton({ label, children, active, popupOpen, menuKind, onClick, rememberSelection }: {
  label: string; children: ReactNode; active?: boolean; popupOpen?: boolean; menuKind?: ToolbarMenu;
  onClick: (event: MouseEvent<HTMLButtonElement>) => void; rememberSelection: () => void;
}) {
  return (
    <button type="button" className={active ? `${styles.toolbarButton} ${styles.toolbarButtonActive}` : styles.toolbarButton}
      aria-label={label} aria-pressed={popupOpen === undefined ? active : undefined} title={label}
      aria-haspopup={popupOpen === undefined ? undefined : "dialog"} aria-expanded={popupOpen} data-menu-kind={menuKind}
      onPointerDown={event => { event.preventDefault(); rememberSelection(); }} onClick={onClick}>
      {children}
    </button>
  );
}

function decorateChecklist(editor: HTMLElement) {
  editor.querySelectorAll<HTMLLIElement>('ul[data-checklist="true"] > li').forEach(item => {
    const checked = item.dataset.checked === "true";
    item.dataset.checked = String(checked);
    let button = item.querySelector<HTMLButtonElement>(":scope > button[data-checklist-toggle]");
    if (!button) {
      button = editor.ownerDocument.createElement("button");
      button.type = "button";
      button.dataset.checklistToggle = "";
      button.contentEditable = "false";
      button.className = views.checkToggle;
      button.setAttribute("role", "checkbox");
      item.prepend(button);
    }
    button.setAttribute("aria-checked", String(checked));
    button.setAttribute("aria-label", checked ? t("Mark item incomplete") : t("Mark item complete"));

  });
  editor.querySelectorAll("button[data-checklist-toggle]").forEach(button => {
    if (!button.parentElement?.matches('ul[data-checklist="true"] > li')) button.remove();
  });
}

function revealMobileCaret(editor: HTMLDivElement | null) {
  if (!editor?.isContentEditable || document.activeElement !== editor ||
    Math.abs((window.visualViewport?.scale ?? 1) - 1) >= 0.01) return;
  const selection = document.getSelection();
  if (!selection?.isCollapsed || !selection.rangeCount || !editor.contains(selection.anchorNode)) return;
  const caret = selection.getRangeAt(0).getClientRects()[0];
  if (!caret?.height || editor.clientHeight < caret.height + 16) return;
  const visibleTop = editor.getBoundingClientRect().top + editor.clientTop;
  const visibleBottom = visibleTop + editor.clientHeight;
  if (caret.top < visibleTop + 8) editor.scrollTop += caret.top - visibleTop - 8;
  else if (caret.bottom > visibleBottom - 8) editor.scrollTop += caret.bottom - visibleBottom + 8;
}

export function NotepadEditorView({
  note, title, content, canEdit, workspaceReadOnly, sessionBlocked, dirty, isSaving, saveFailed, canSave, contentTooLong, isPhone,
  focusEditing, onStartFocusEditing, onLeaveWritingArea, onTitle, onContent, onSave, access, expanded, onAccessChange, onBack, onDelete, onVersions, onLockChange, collapseButton, children,
}: Props) {
  const editorRef = useRef<HTMLDivElement>(null);
  const [initialContent] = useState(content);
  const savedSelection = useRef<Range | null>(null);
  const splitChecklistItem = useRef<{
    item: HTMLElement; previous: Element | null; atStart: boolean; checked: boolean;
  } | null>(null);
  const [lineSpacing, setLineSpacing] = useState<string | null>(null);
  const popover = useRef<HTMLDivElement>(null);
  const layout = useRef<HTMLDivElement>(null);
  const caretFrame = useRef(0);
  const [layoutSize, setLayoutSize] = useState({ width: Infinity, height: 0 });
  const toolbarWidth = layoutSize.width;

  useEffect(() => {
    const container = layout.current;
    if (!container) return;
    let previousHeight = container.clientHeight;
    const measure = () => {
      const { clientWidth: width, clientHeight: height } = container;
      if (isPhone && height !== previousHeight) revealMobileCaret(editorRef.current);
      previousHeight = height;
      setLayoutSize(previous => previous.width === width && previous.height === height ? previous : { width, height });
    };
    measure();
    const observer = new ResizeObserver(measure);
    observer.observe(container);
    return () => {
      observer.disconnect();
      cancelAnimationFrame(caretFrame.current);
    };
  }, [isPhone]);
  const [actions, setActions] = useState<HTMLButtonElement | null>(null);
  const [formattingMenu, setFormattingMenu] = useState<{
    kind: ToolbarMenu; anchor: HTMLButtonElement; keyboard: boolean;
  } | null>(null);
  useEffect(() => {
    if (isPhone && !focusEditing) setFormattingMenu(null);
  }, [focusEditing, isPhone]);
  const toolbarMenu = formattingMenu?.kind;
  const menuPresentation = toolbarMenu && formattingMenus[toolbarMenu];
  const menuOpen = Boolean(canEdit && expanded && formattingMenu?.anchor.isConnected &&
    (toolbarWidth >= 444 || (formattingMenu.anchor.dataset.menuKind !== "size" && formattingMenu.anchor.dataset.menuKind !== "color")));
  const menuAnchor = useMemo(() => ({ current: formattingMenu?.anchor ?? null }), [formattingMenu, layoutSize]);
  const menuPosition = useDropdownPosition(menuOpen, menuAnchor, popover, "up", menuPresentation?.width, "end");
  const [textColor, setTextColor] = useState<string | null>(null);
  const [textSize, setTextSize] = useState<string | null>(null);
  const [canUndo, setCanUndo] = useState(false);
  const [copyStatus, setCopyStatus] = useState<"copied" | "text" | "error" | null>(null);
  const [manualCopy, setManualCopy] = useState<string | null>(null);
  const copyPending = useRef(false);
  const copyTimeout = useRef<number | undefined>(undefined);
  useEffect(() => () => {
    copyPending.current = false;
    window.clearTimeout(copyTimeout.current);
  }, []);
  const [active, setActive] = useState<Set<FormatCommand>>(new Set());

  const attachEditor = useCallback((element: HTMLDivElement | null) => {
    editorRef.current = element;
    if (!element) return;
    element.innerHTML = sanitizeRichText(initialContent);
    if (element.isContentEditable || element.getAttribute("contenteditable") === "true") decorateChecklist(element);
  }, [initialContent]);

  useLayoutEffect(() => {
    if (!menuOpen || !formattingMenu?.keyboard) return;
    const first = popover.current?.querySelector<HTMLButtonElement>('button[aria-pressed="true"]') ??
      popover.current?.querySelector<HTMLButtonElement>("button");
    first?.focus({ preventScroll: true });
  }, [menuOpen, formattingMenu]);

  useEffect(() => {
    if (!menuOpen || !formattingMenu) return;
    const dismiss = (event: PointerEvent) => {
      if (event.target instanceof Node && !popover.current?.contains(event.target) &&
        !formattingMenu.anchor.contains(event.target)) setFormattingMenu(null);
    };
    const escape = (event: KeyboardEvent) => {
      if (event.key !== "Escape") return;
      event.preventDefault();
      event.stopPropagation();
      setFormattingMenu(null);
      if (formattingMenu.keyboard) formattingMenu.anchor.focus({ preventScroll: true });
      else {
        const editor = editorRef.current;
        const selection = window.getSelection();
        const range = savedSelection.current;
        editor?.focus({ preventScroll: true });
        if (selection && range && editor?.contains(range.commonAncestorContainer)) {
          selection.removeAllRanges();
          selection.addRange(range);
        }
      }
    };
    document.addEventListener("pointerdown", dismiss);
    document.addEventListener("keydown", escape, true);
    return () => {
      document.removeEventListener("pointerdown", dismiss);
      document.removeEventListener("keydown", escape, true);
    };
  }, [menuOpen, formattingMenu]);

  useEffect(() => {
    if (!menuOpen && formattingMenu) setFormattingMenu(null);
  }, [menuOpen, formattingMenu]);

  useEffect(() => {
    const editor = editorRef.current;
    if (!editor) return;
    if (canEdit) decorateChecklist(editor);
    else editor.querySelectorAll("button[data-checklist-toggle]").forEach(button => button.remove());
  }, [canEdit]);

  const syncContent = () => {
    const editor = editorRef.current;
    if (!editor) return;
    decorateChecklist(editor);
    const html = sanitizeRichText(editor.innerHTML);
    onContent(html === "<br>" ? "" : html);
  };

  const rememberSelection = () => {
    const selection = window.getSelection();
    if (!selection?.rangeCount) return;
    const range = selection.getRangeAt(0);
    if (editorRef.current?.contains(range.commonAncestorContainer)) savedSelection.current = range.cloneRange();
  };

  const updateFormats = () => {
    const editor = editorRef.current;
    if (!editor) return;
    if (editor.ownerDocument.activeElement === editor) {
      setCanUndo(Boolean(document.queryCommandEnabled?.("undo")));
    }
    const selection = window.getSelection();
    if (!selection?.rangeCount) return;
    const range = selection.getRangeAt(0);
    if (!editor.contains(range.commonAncestorContainer)) return;
    if (document.activeElement === editor) setTextColor(readTextColor());
    const next = new Set<FormatCommand>(inlineCommands.filter(command => document.queryCommandState?.(command)));
    const list = selectedListFormat(range, editor);
    if (list) next.add(list);
    setActive(current => current.size === next.size && [...next].every(command => current.has(command)) ? current : next);
  };

  const refreshFormats = useEffectEvent(updateFormats);
  useEffect(() => {
    const onSelectionChange = () => refreshFormats();
    document.addEventListener("selectionchange", onSelectionChange);
    return () => document.removeEventListener("selectionchange", onSelectionChange);
  }, []);

  const restoreSelection = () => {
    const editor = editorRef.current;
    if (!editor || !canEdit) return;
    if (isPhone) editor.focus({ preventScroll: true });
    else editor.focus();
    const selection = window.getSelection();
    const range = savedSelection.current;
    if (selection && range && editor.contains(range.commonAncestorContainer)) {
      selection.removeAllRanges();
      selection.addRange(range);
    }
    return editor;
  };

  const runCommand = (command: string, value?: string) => {
    if (!restoreSelection()) return;
    document.execCommand(command, false, value);
    syncContent();
    rememberSelection();
    updateFormats();
    setFormattingMenu(null);
  };

  const toggleChecklist = () => {
    if (!canEdit || !editorRef.current) return;
    const editor = editorRef.current;
    const activeNode = savedSelection.current?.commonAncestorContainer;
    const element = activeNode instanceof Element ? activeNode : activeNode?.parentElement;
    const before = element?.closest("ul");
    if (before && editor.contains(before)) {
      if (before.getAttribute("data-checklist") === "true") {
        before.removeAttribute("data-checklist");
        [...before.children].forEach(item => item.removeAttribute("data-checked"));
      } else {
        before.setAttribute("data-checklist", "true");
      }
    } else {
      runCommand("insertUnorderedList");
      const selected = window.getSelection()?.anchorNode;
      const list = (selected instanceof Element ? selected : selected?.parentElement)?.closest("ul");
      if (list && editor.contains(list)) list.setAttribute("data-checklist", "true");
    }
    syncContent();
    updateFormats();
  };

  const handleChecklistInput = useEffectEvent((event: InputEvent) => {
    const editor = editorRef.current;
    const selection = window.getSelection();
    if (!canEdit || !editor || !selection?.rangeCount || event.isComposing || event.defaultPrevented || !event.cancelable) return;
    const range = selection.getRangeAt(0);
    const selector = 'ul[data-checklist="true"] > li';
    const item = closestBlock(range.startContainer, selector, editor);
    const lastItem = closestBlock(range.endContainer, selector, editor);
    if (!item || !lastItem || item.parentElement !== lastItem.parentElement) return;

    const deleteBack = event.inputType === "deleteContentBackward";
    const deleteKey = deleteBack || event.inputType === "deleteContentForward";
    const enter = event.inputType === "insertParagraph";
    if ((!deleteKey && !enter) || (enter && item !== lastItem)) return;
    const before = range.cloneRange();
    before.selectNodeContents(item);
    before.setEnd(range.startContainer, range.startOffset);
    const after = range.cloneRange();
    after.selectNodeContents(lastItem);
    after.setStart(range.endContainer, range.endOffset);
    const hasBefore = hasChecklistContent(before.cloneContents());
    if (hasBefore || hasChecklistContent(after.cloneContents())) {
      if (enter) {
        splitChecklistItem.current = {
          item, previous: item.previousElementSibling, atStart: !hasBefore, checked: item.dataset.checked === "true",
        };
      }
      return;
    }
    if (range.collapsed && !enter && !deleteBack) return;

    event.preventDefault();
    const list = item.parentElement!;
    const previous = item.previousElementSibling;
    const next = lastItem.nextElementSibling;
    if (deleteKey && !range.collapsed && (next || previous)) {
      let selected: Element | null = item;
      while (selected && selected !== next) {
        const following: Element | null = selected.nextElementSibling;
        selected.remove();
        selected = following;
      }
      if (next) {
        const toggle = next.querySelector(":scope > button[data-checklist-toggle]");
        selection.collapse(next, toggle === next.firstChild ? 1 : 0);
      } else if (previous) {
        selection.collapse(previous, previous.childNodes.length);
      }
    } else {
      const paragraph = editor.ownerDocument.createElement("p");
      const tail = list.cloneNode(false) as HTMLElement;
      paragraph.style.lineHeight = item.style.lineHeight;
      paragraph.append(editor.ownerDocument.createElement("br"));
      while (lastItem.nextSibling) tail.append(lastItem.nextSibling);
      list.after(paragraph);
      if (tail.firstElementChild) paragraph.after(tail);
      while (item.nextSibling) item.nextSibling.remove();
      item.remove();
      if (!list.firstElementChild) list.remove();
      selection.collapse(paragraph, 0);
    }
    syncContent();
    rememberSelection();
    updateFormats();
  });

  useEffect(() => {
    const editor = editorRef.current;
    if (!editor) return;
    const beforeInput = (event: InputEvent) => handleChecklistInput(event);
    editor.addEventListener("beforeinput", beforeInput);
    return () => editor.removeEventListener("beforeinput", beforeInput);
  }, []);

  const handleEditorInput = () => {
    const editor = editorRef.current;
    const split = splitChecklistItem.current;
    splitChecklistItem.current = null;
    if (editor && split) {
      const item = closestBlock(window.getSelection()?.anchorNode, 'ul[data-checklist="true"] > li', editor);
      if (item && split.atStart && item.previousElementSibling !== split.previous) {
        const newItem = item.previousElementSibling as HTMLElement | null;
        if (newItem && !hasChecklistContent(newItem.cloneNode(true) as HTMLElement)) {
          newItem.dataset.checked = "false";
          item.dataset.checked = String(split.checked);
        }
      } else if (item && item !== split.item) {
        item.dataset.checked = "false";
      }
    }
    syncContent();
    rememberSelection();
    updateFormats();
    if (isPhone) {
      cancelAnimationFrame(caretFrame.current);
      caretFrame.current = requestAnimationFrame(() => revealMobileCaret(editorRef.current));
    }
  };

  const readPickerValues = () => {
    const size = document.queryCommandValue?.("fontSize") ?? "";
    const legacySize = Number(size);
    setTextSize(fontSizes[legacySize - 2] ?? fontSizes.find(value => `${value}px` === size) ?? null);

    setTextColor(readTextColor());

    const editor = editorRef.current;
    const range = savedSelection.current;
    const blocks = editor && range && editor.contains(range.commonAncestorContainer) ? spacingBlocks(editor, range) : [];
    const value = blocks[0]?.style.lineHeight;
    setLineSpacing(value && blocks.every(block => block.style.lineHeight === value) ? value : null);
  };

  const toggleFormattingMenu = (kind: ToolbarMenu, event: MouseEvent<HTMLButtonElement>) => {
    if (formattingMenu?.anchor === event.currentTarget && (toolbarMenu === kind || kind === "more")) {
      setFormattingMenu(null);
      return;
    }
    if (kind !== "more") readPickerValues();
    setFormattingMenu({ kind, anchor: event.currentTarget, keyboard: event.detail === 0 });
  };

  const openOverflowPicker = (kind: "size" | "color") => {
    if (!formattingMenu) return;
    readPickerValues();
    setFormattingMenu({ ...formattingMenu, kind });
  };

  const applySpacing = (value: LineSpacing) => {
    const editor = restoreSelection();
    const selection = window.getSelection();
    if (!editor || !selection?.rangeCount) return;
    const range = selection.getRangeAt(0);
    if (!editor.contains(range.commonAncestorContainer)) return;
    const collapsed = range.collapsed;
    const bookmark = (node: Node, offset: number) => ({
      node, offset, next: node === editor ? node.childNodes[offset] : undefined,
      previous: node === editor && offset > 0 ? node.childNodes[offset - 1] : undefined,
    });
    const start = bookmark(range.startContainer, range.startOffset);
    const end = bookmark(range.endContainer, range.endOffset);
    const blocks = spacingBlocks(editor, range, true);
    blocks.forEach(block => { block.style.lineHeight = value; });
    const restorePoint = (point: ReturnType<typeof bookmark>) => {
      const boundary = editor.ownerDocument.createRange();
      if (point.next?.parentNode) boundary.setStartBefore(point.next);
      else if (point.previous?.parentNode) boundary.setStartAfter(point.previous);
      else if (point.node !== editor) boundary.setStart(point.node, point.offset);
      else boundary.setStart(blocks[0] ?? editor, 0);
      return boundary;
    };
    const restoredStart = restorePoint(start);
    const restoredEnd = collapsed ? restoredStart : restorePoint(end);
    selection.setBaseAndExtent(restoredStart.startContainer, restoredStart.startOffset,
      restoredEnd.startContainer, restoredEnd.startOffset);
    syncContent();
    rememberSelection();
    setFormattingMenu(null);
  };

  const paste = (event: ClipboardEvent<HTMLDivElement>) => {
    event.preventDefault();
    const html = event.clipboardData.getData("text/html");
    const safe = html ? sanitizeRichText(html) : plainTextToHtml(event.clipboardData.getData("text/plain"));
    if (safe) {
      rememberSelection();
      runCommand("insertHTML", safe);
    }
  };

  const format = (command: FormatCommand, label: string, glyph: ReactNode) => (
    <ToolbarButton label={label} active={active.has(command)} rememberSelection={rememberSelection}
      onClick={() => runCommand(command)}>{glyph}</ToolbarButton>
  );
  const formatMenuItems = [
    ...(toolbarWidth < 370 ? [
      { id: "underline", label: t("Underline"), onSelect: () => runCommand("underline") },
      { id: "strike", label: t("Strikethrough"), onSelect: () => runCommand("strikeThrough") },
    ] : []),
    ...(toolbarWidth < 444 ? [
      { id: "size", label: t("Text size"), onSelect: () => openOverflowPicker("size") },
      { id: "color", label: t("Text color"), onSelect: () => openOverflowPicker("color") },
    ] : []),
    ...(toolbarWidth < 298 ? [
      { id: "bullets", label: t("Bulleted list"), onSelect: () => runCommand("insertUnorderedList") },
      { id: "numbered", label: t("Numbered list"), onSelect: () => runCommand("insertOrderedList") },
    ] : []),
    { id: "clear", label: t("Clear formatting"), icon: <NotepadIcon name="clearFormat" size={18} />, description: t("Remove text styles"), onSelect: () => runCommand("removeFormat") },
  ];

  const copyNote = async () => {
    const editor = editorRef.current;
    if (!editor || copyPending.current) return;
    copyPending.current = true;
    window.clearTimeout(copyTimeout.current);
    setCopyStatus(null);
    setManualCopy(null);
    let result: "copied" | "text" | "error" = "error";
    let fallback: string | null = null;
    try {
      const container = isPhone ? editor.closest("[data-manager-notepad-modal]") ?? editor.parentElement ?? undefined : undefined;
      const copied = await copyNoteToClipboard(title, editor.innerHTML, container);
      if (typeof copied === "string") result = copied;
      else fallback = copied.text;
    } catch {
      result = "error";
    }
    if (!copyPending.current) return;
    copyPending.current = false;
    setCopyStatus(result);
    setManualCopy(fallback);
    copyTimeout.current = window.setTimeout(() => setCopyStatus(null), 1800);
  };
  const copied = copyStatus === "copied" || copyStatus === "text";
  const copyMessage = copyStatus === "copied" ? t("Copied!") : copyStatus === "text" ? t("Copied as text")
    : copyStatus === "error" ? t("Could not copy note.") : null;

  const manage = Boolean(note?.canManage && !sessionBlocked && !workspaceReadOnly);
  const showSettings = manage || (workspaceReadOnly && note?.canManage);
  const readOnly = note && !manage;
  const readOnlyVisibility = note?.visibility === "custom" ? t("Custom") : note ? visibilityLabel(note.visibility) : "";
  const showSave = !workspaceReadOnly && (!note || note.canManage || (note.visibility === "public" && note.canEdit));
  const visibility = access ?? { visibility: note?.visibility ?? "private", sharedWithManagerIds: note?.sharedWithManagerIds ?? [] };
  const saveState = sessionBlocked ? "ready" : isSaving ? "saving" : saveFailed ? "error" : note && !dirty ? "saved" : "ready";
  const saveLabels = { ready: t("Save"), saving: t("Saving..."), saved: t("Saved"), error: t("Retry") };

  return (
    <div ref={layout} className={styles.editorLayout} onBlur={focusEditing ? event => {
      const next = event.relatedTarget;
      if (next instanceof Element && (event.currentTarget.contains(next) ||
        popover.current?.contains(next))) return;
      onLeaveWritingArea?.();
    } : undefined}>
      <section key="note-document" className={styles.editorView} aria-label={t("Note editor")}>
        <header className={styles.editorHeader}>
          <div className={styles.editorNavigation}>
            <button type="button" className={styles.editorBack} aria-label={t("Notes")} title={t("Notes")} onClick={onBack}>
              <NotepadIcon name="back" size={18} />
              <span className={`${styles.backLabel} ${views.compactLabel}`}>{t("Notes")}</span>
            </button>
            {!workspaceReadOnly && <button type="button" className={`${styles.iconButton} ${styles.historyButton}`}
              aria-label={t("Undo")} title={t("Undo")} disabled={!canEdit || !canUndo}
              onPointerDown={event => { event.preventDefault(); rememberSelection(); }}
              onClick={() => runCommand("undo")}>
              <NotepadIcon name="undo" size={18} />
            </button>}
            <div className={styles.copyControl}>
              <button type="button" className={`${styles.iconButton} ${styles.historyButton} ${styles.copyNoteButton}`}
                aria-label={t("Copy note")} title={copyMessage ?? t("Copy note")} data-copied={copied}
                onPointerDown={event => event.preventDefault()} onClick={() => void copyNote()}>
                {copied ? <CheckIcon size={18} /> : <CopyLinkIcon size={17} />}
              </button>
              <span className={styles.copyFeedback} role="status" data-state={copyStatus ?? undefined}>{copyMessage}</span>
            </div>
          </div>
          <div className={styles.editorActions}>
            {showSettings && (
              <>
                <button type="button" className={`${styles.iconButton} ${styles.editorMore}`} aria-haspopup="dialog"
                  aria-expanded={expanded && Boolean(actions)} aria-label={t("Note actions")}
                  onClick={event => setActions(actions ? null : event.currentTarget)}>
                  <NotepadIcon name="more" size={19} />
                </button>
                {expanded && note && actions && (
                  <NotepadSettingsMenu anchor={actions} note={note} access={visibility} busy={isSaving} workspaceReadOnly={workspaceReadOnly}
                    onClose={() => setActions(null)} onAccessChange={onAccessChange}
                    onLockChange={onLockChange} onVersions={onVersions} onDelete={onDelete} />
                )}
              </>
            )}
            {expanded && !workspaceReadOnly && (!note || manage) && (
              <NotepadVisibilityControl value={visibility} expanded={expanded} busy={isSaving} onChange={onAccessChange} />
            )}
            {readOnly && <span className={`${views.badge} ${views.compactLabel}`} title={readOnlyVisibility}>{readOnlyVisibility}</span>}
            {showSave && (
              <button type="button" className={`${styles.saveButton} ${styles.editorSave}`} data-save-state={saveState}
                disabled={!canSave || isSaving || saveState === "saved"} onClick={onSave}
                aria-label={saveLabels[saveState]} title={saveState === "error" ? `${saveLabels[saveState]} · ${t("Could not save this note.")}` : saveLabels[saveState]}>
                <span className={styles.saveIcon} aria-hidden="true">
                  {saveState === "saving" ? <span className={styles.savingSpinner} />
                    : saveState === "saved" ? <CheckIcon size={14} />
                    : saveState === "error" ? <NotepadIcon name="alert" size={14} /> : <SaveIcon size={14} />}
                </span>
                <span className={styles.saveLabel} aria-live="polite">
                  <span>{saveLabels[saveState]}</span>
                  {Object.entries(saveLabels).map(([state, label]) => (
                    <span key={state} aria-hidden="true" className={styles.saveLabelMeasure}>{label}</span>
                  ))}
                </span>
              </button>
            )}
          </div>
        </header>
        <input className={styles.titleInput} value={title} maxLength={160} placeholder={t("Note title")}
          aria-label={t("Note title")} autoFocus={!isPhone && !note} readOnly={!canEdit}
          onFocus={canEdit ? onStartFocusEditing : undefined} onChange={event => onTitle(event.target.value)} />
        {contentTooLong && <span className={styles.contentLengthError} role="alert">
          {t("Note content cannot exceed {0} characters.", MAX_NOTE_CONTENT.toLocaleString(getLocale()))}
        </span>}
        {children}
        {manualCopy !== null && !focusEditing && <label className={styles.manualCopy}>
          {t("Select and copy the note manually.")}
          <textarea readOnly rows={3} value={manualCopy} onFocus={event => event.currentTarget.select()} />
        </label>}
        <div
            ref={attachEditor}
            className={[styles.contentEditor, views.checklistContent].join(" ")} contentEditable={canEdit} role="textbox" aria-readonly={!canEdit}
            aria-label={t("Note content")} aria-multiline="true" tabIndex={0}
            data-placeholder={canEdit ? t("Write something...") : undefined} suppressContentEditableWarning
            onInput={canEdit ? handleEditorInput : undefined}
              onPaste={canEdit ? paste : undefined}
            onDragOver={event => { if (canEdit) event.preventDefault(); }}
            onDrop={event => { if (canEdit) event.preventDefault(); }}
            onMouseUp={() => { rememberSelection(); updateFormats(); }}
            onKeyUp={() => { splitChecklistItem.current = null; rememberSelection(); updateFormats(); }}
            onFocus={() => {
              if (canEdit) onStartFocusEditing?.();
              updateFormats();
            }}
            onBlur={rememberSelection}
            onClick={event => {
              if (!canEdit || !(event.target instanceof Element)) return;
              const button = event.target.closest("button[data-checklist-toggle]");
              if (button?.parentElement?.matches('ul[data-checklist="true"] > li')) {
                button.parentElement.dataset.checked = String(button.parentElement.dataset.checked !== "true");
                syncContent();
              }
            }}
          />
      </section>
      {canEdit ? <div className={styles.formatDock}>
        <div className={styles.formatToolbar} role="toolbar" aria-label={t("Text formatting")}>
          <div className={styles.toolGroup}>
            {format("bold", t("Bold"), <strong>B</strong>)}
            {format("italic", t("Italic"), <em>I</em>)}
            {toolbarWidth >= 370 && format("underline", t("Underline"), <span className={styles.underlineGlyph}>U</span>)}
            {toolbarWidth >= 370 && format("strikeThrough", t("Strikethrough"), <span className={styles.strikeGlyph}>S</span>)}
          </div>
          {toolbarWidth >= 444 && <div className={styles.toolGroup}>
            <ToolbarButton label={t("Text size")} menuKind="size" active={menuOpen && toolbarMenu === "size"} popupOpen={menuOpen && toolbarMenu === "size"} rememberSelection={rememberSelection}
              onClick={event => toggleFormattingMenu("size", event)}>Aa</ToolbarButton>
            <ToolbarButton label={t("Text color")} menuKind="color" active={menuOpen && toolbarMenu === "color"} popupOpen={menuOpen && toolbarMenu === "color"} rememberSelection={rememberSelection}
              onClick={event => toggleFormattingMenu("color", event)}>
              <span className={styles.colorGlyph} style={{ "--text-color": textColor ?? "#0f172a" } as React.CSSProperties}>A</span>
            </ToolbarButton>
          </div>}
          <div className={styles.toolGroup}>
            {toolbarWidth >= 298 && format("insertUnorderedList", t("Bulleted list"), "•≡")}
            {toolbarWidth >= 298 && format("insertOrderedList", t("Numbered list"), "1≡")}
            <ToolbarButton label={t("Checklist")} active={active.has("checklist")} rememberSelection={rememberSelection} onClick={toggleChecklist}>☑</ToolbarButton>
            <ToolbarButton label={t("Line spacing")} menuKind="spacing" active={menuOpen && toolbarMenu === "spacing"} popupOpen={menuOpen && toolbarMenu === "spacing"} rememberSelection={rememberSelection}
              onClick={event => toggleFormattingMenu("spacing", event)}><NotepadIcon name="lineSpacing" size={18} /></ToolbarButton>
            <ToolbarButton label={t("More formatting actions")} menuKind="more" active={menuOpen && formattingMenu?.anchor.dataset.menuKind === "more"}
              popupOpen={menuOpen && formattingMenu?.anchor.dataset.menuKind === "more"} rememberSelection={rememberSelection}
              onClick={event => toggleFormattingMenu("more", event)}>
              <NotepadIcon name="more" size={18} />
            </ToolbarButton>
          </div>
        </div>
        {collapseButton && <div className={styles.collapseSlot}>{collapseButton}</div>}
        {menuOpen && menuPresentation && createPortal(
          <div ref={popover} className={styles.toolbarPopover} role="dialog" aria-label={t(menuPresentation.label)}
            style={{ width: menuPresentation.width, ...menuPosition }} onKeyDown={event => {
              if (event.key !== "Tab") return;
              const buttons = event.currentTarget.querySelectorAll<HTMLButtonElement>("button");
              const first = buttons[0];
              const last = buttons[buttons.length - 1];
              if (!first || !last) return;
              if (event.shiftKey && event.target === first) {
                event.preventDefault();
                last.focus();
              } else if (!event.shiftKey && event.target === last) {
                event.preventDefault();
                first.focus();
              }
            }}>
            <div className={styles.popoverContent}>
              <header className={styles.popoverHeading}>
                <NotepadIcon name={menuPresentation.icon} size={17} />
                <strong>{t(menuPresentation.label)}</strong>
              </header>
              {toolbarMenu === "spacing" && (
                <div className={styles.spacingOptions}>
                  {lineSpacingValues.map(value => (
                    <button key={value} type="button" className={styles.popoverOption} aria-pressed={lineSpacing === value}
                      onPointerDown={event => event.preventDefault()} onClick={() => applySpacing(value)}>
                      <strong>{value === "1" || value === "2" ? `${value}.0` : value}</strong>
                      <span>{t(spacingLabels[value])}</span>
                      {lineSpacing === value && <CheckIcon size={15} />}
                    </button>
                  ))}
                </div>
              )}
              {toolbarMenu === "size" && (
                <div className={styles.sizeGrid}>
                  {fontSizes.map((size, index) => <button key={size} type="button" className={styles.popoverOption}
                    aria-pressed={textSize === size} onPointerDown={event => event.preventDefault()}
                    onClick={() => runCommand("fontSize", String(index + 2))}>
                    {size}px{size === textSize && <CheckIcon size={13} />}
                  </button>)}
                </div>
              )}
              {toolbarMenu === "color" && (
                <>
                  {textColor && <div className={styles.currentColor}>
                    <span style={{ backgroundColor: textColor }} />
                    {t(textColors.find(({ value }) => value === textColor)?.label ?? textColor)}
                  </div>}
                  <div className={styles.textColorGrid}>
                    {textColors.map(({ value, label }) => <button type="button" key={value} className={styles.textColorButton}
                      style={{ backgroundColor: value }} aria-pressed={value === textColor} aria-label={t(label)} title={t(label)}
                      onPointerDown={event => event.preventDefault()} onClick={() => runCommand("foreColor", value)} />)}
                  </div>
                </>
              )}
              {toolbarMenu === "more" && (
                <div className={styles.moreOptions}>
                  {formatMenuItems.map(item => (
                    <button key={item.id} type="button" title={item.label} className={styles.popoverOption}
                      onPointerDown={event => event.preventDefault()} onClick={item.onSelect}>
                      {"icon" in item && item.icon}
                      <span><strong>{item.label}</strong>{"description" in item && <small>{item.description}</small>}</span>
                    </button>
                  ))}
                </div>
              )}
            </div>
          </div>, formattingMenu?.anchor.closest("[data-manager-notepad-modal]") ?? document.body,
        )}
      </div> : collapseButton && <div className={styles.collapseOnly}>{collapseButton}</div>}
    </div>
  );
}
