import {
  useEffect,
  useEffectEvent,
  useLayoutEffect,
  useMemo,
  useRef,
  useState,
  type PointerEvent as ReactPointerEvent,
} from "react";
import {
  useCreateManagerNoteMutation,
  useDeleteManagerNoteMutation,
  useManagerNotepadQuery,
  useSaveManagerNotepadStateMutation,
  useUpdateManagerNoteMutation,
  type ManagerNote,
  type ManagerNoteColor,
  type ManagerNotepadState,
} from "@entities/manager-notepad";
import { getErrorMessage } from "@shared/api/httpClient";
import { ConfirmDialog } from "@shared/ui/ConfirmDialog";
import { ArrowIcon, CloseIcon, NoteIcon, PinIcon, PlusIcon, SaveIcon, SearchIcon } from "@shared/ui/icons";
import styles from "./ManagerNotepad.module.css";

const MIN_HEIGHT = 320;
const MAX_HEIGHT = 900;
const DEFAULT_HEIGHT = 520;
const MAX_CONTENT_LENGTH = 20_000;
const NEW_NOTE_COLOR: ManagerNoteColor = "slate";
const ALLOWED_TAGS = new Set(["B", "STRONG", "I", "EM", "U", "S", "STRIKE", "P", "DIV", "BR", "UL", "OL", "LI", "SPAN", "FONT", "BLOCKQUOTE"]);
const BLOCKED_TAGS = new Set(["SCRIPT", "STYLE", "IFRAME", "OBJECT", "EMBED", "SVG", "MATH", "FORM", "INPUT", "BUTTON"]);
const RICH_TEXT_PATTERN = /<(?:b|strong|i|em|u|s|strike|p|div|br|ul|ol|li|span|font|blockquote)\b/i;
const textColors = ["#0f172a", "#475569", "#2563eb", "#7c3aed", "#db2777", "#dc2626", "#ea580c", "#ca8a04", "#16a34a", "#0891b2", "#ffffff"];
const fontSizes = [
  { label: "12", value: "2" },
  { label: "14", value: "3" },
  { label: "16", value: "4" },
  { label: "20", value: "5" },
  { label: "24", value: "6" },
  { label: "32", value: "7" },
];

type ToolbarMenu = "color" | "size" | null;
type FormatCommand = "bold" | "italic" | "underline" | "strikeThrough" | "insertUnorderedList" | "insertOrderedList";

function clampHeight(value: number) {
  const viewportMaximum = typeof window === "undefined" ? MAX_HEIGHT : Math.max(MIN_HEIGHT, window.innerHeight - 28);
  return Math.max(MIN_HEIGHT, Math.min(value, MAX_HEIGHT, viewportMaximum));
}

function isSafeColor(value: string) {
  return /^(?:#[0-9a-f]{3,8}|rgba?\([\d\s.,%]+\)|[a-z]+)$/i.test(value.trim());
}

function sanitizeRichText(html: string) {
  if (typeof DOMParser === "undefined") return html;
  const documentNode = new DOMParser().parseFromString(`<div>${html}</div>`, "text/html");
  const root = documentNode.body.firstElementChild;
  if (!root) return "";

  [...root.querySelectorAll("*")].forEach(element => {
    if (BLOCKED_TAGS.has(element.tagName)) {
      element.remove();
      return;
    }
    if (!ALLOWED_TAGS.has(element.tagName)) {
      element.replaceWith(...element.childNodes);
      return;
    }

    const htmlElement = element as HTMLElement;
    const color = element.getAttribute("color") ?? htmlElement.style.color;
    const size = element.getAttribute("size");
    const fontSize = htmlElement.style.fontSize;
    const textAlign = htmlElement.style.textAlign;
    [...element.attributes].forEach(attribute => element.removeAttribute(attribute.name));

    if (element.tagName === "FONT") {
      if (color && isSafeColor(color)) element.setAttribute("color", color);
      if (size && /^[1-7]$/.test(size)) element.setAttribute("size", size);
    }
    if (element.tagName !== "FONT" && color && isSafeColor(color)) htmlElement.style.color = color;
    if (fontSize && /^\d+(?:\.\d+)?(?:px|rem|em|%)$/.test(fontSize)) htmlElement.style.fontSize = fontSize;
    if (["left", "center", "right", "justify"].includes(textAlign)) htmlElement.style.textAlign = textAlign;
  });

  return root.innerHTML;
}

function plainTextToHtml(value: string) {
  if (typeof document === "undefined") return value;
  const container = document.createElement("div");
  container.textContent = value;
  return container.innerHTML.replace(/\r?\n/g, "<br>");
}

function contentToEditorHtml(value: string) {
  return RICH_TEXT_PATTERN.test(value) ? sanitizeRichText(value) : plainTextToHtml(value);
}

function richTextToPlainText(value: string) {
  if (!RICH_TEXT_PATTERN.test(value) || typeof DOMParser === "undefined") return value;
  return new DOMParser().parseFromString(sanitizeRichText(value), "text/html").body.textContent ?? "";
}

function notePreview(note: ManagerNote) {
  return richTextToPlainText(note.content).trim().replace(/\s+/g, " ") || "Empty note";
}

function ToolbarButton({
  label,
  children,
  active = false,
  disabled = false,
  onClick,
  onRememberSelection,
}: {
  label: string;
  children: React.ReactNode;
  active?: boolean;
  disabled?: boolean;
  onClick: () => void;
  onRememberSelection: () => void;
}) {
  return (
    <button
      type="button"
      className={[styles.toolbarButton, active ? styles.toolbarButtonActive : ""].filter(Boolean).join(" ")}
      aria-label={label}
      aria-pressed={active}
      title={label}
      disabled={disabled}
      onPointerDown={event => {
        event.preventDefault();
        onRememberSelection();
      }}
      onClick={onClick}
    >
      {children}
    </button>
  );
}

export function ManagerNotepad() {
  const notepadQuery = useManagerNotepadQuery();
  const createMutation = useCreateManagerNoteMutation();
  const updateMutation = useUpdateManagerNoteMutation();
  const deleteMutation = useDeleteManagerNoteMutation();
  const stateMutation = useSaveManagerNotepadStateMutation();
  const panelRef = useRef<HTMLElement | null>(null);
  const editorRef = useRef<HTMLDivElement | null>(null);
  const savedSelectionRef = useRef<Range | null>(null);
  const hydratedRef = useRef(false);
  const [isExpanded, setIsExpanded] = useState(false);
  const [isPinned, setIsPinned] = useState(false);
  const [height, setHeight] = useState(DEFAULT_HEIGHT);
  const [view, setView] = useState<"list" | "editor">("list");
  const [selectedNoteId, setSelectedNoteId] = useState<number | null>(null);
  const [editorVersion, setEditorVersion] = useState(0);
  const [title, setTitle] = useState("");
  const [content, setContent] = useState("");
  const [contentLimit, setContentLimit] = useState({ isTooLong: false, length: 0 });
  const [noteColor, setNoteColor] = useState<ManagerNoteColor>(NEW_NOTE_COLOR);
  const [textColor, setTextColor] = useState("#0f172a");
  const [search, setSearch] = useState("");
  const [isDirty, setIsDirty] = useState(false);
  const [activeFormats, setActiveFormats] = useState<Set<string>>(() => new Set());
  const [toolbarMenu, setToolbarMenu] = useState<ToolbarMenu>(null);
  const [deleteNoteId, setDeleteNoteId] = useState<number | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  const notes = useMemo(() => notepadQuery.data?.notes ?? [], [notepadQuery.data?.notes]);
  const filteredNotes = useMemo(() => {
    const query = search.trim().toLocaleLowerCase();
    if (!query) return notes;
    return notes.filter(note => `${note.title} ${richTextToPlainText(note.content)}`.toLocaleLowerCase().includes(query));
  }, [notes, search]);

  useLayoutEffect(() => {
    if (view !== "editor" || !editorRef.current) return;
    editorRef.current.innerHTML = contentToEditorHtml(content);
    savedSelectionRef.current = null;
  }, [content, editorVersion, view]);

  const updateContentLength = (length: number) => {
    const nextTooLong = length > MAX_CONTENT_LENGTH;
    setContentLimit(current => {
      if (!nextTooLong && !current.isTooLong) return current;
      if (current.isTooLong === nextTooLong && current.length === length) return current;
      return { isTooLong: nextTooLong, length };
    });
  };

  useEffect(() => {
    if (!notepadQuery.data || hydratedRef.current) return;
    hydratedRef.current = true;
    setIsExpanded(notepadQuery.data.state.isExpanded);
    setIsPinned(notepadQuery.data.state.isPinned);
    setHeight(clampHeight(notepadQuery.data.state.height));
  }, [notepadQuery.data]);

  const saveWindowState = (state: ManagerNotepadState) => {
    stateMutation.mutate(state, {
      onError: error => setActionError(getErrorMessage(error, "Could not save notepad position.")),
    });
  };

  const collapseFromOutside = useEffectEvent(() => {
    setIsExpanded(false);
    setToolbarMenu(null);
    saveWindowState({ isExpanded: false, isPinned, height });
  });

  useEffect(() => {
    if (!isExpanded || isPinned) return;
    const handlePointerDown = (event: globalThis.PointerEvent) => {
      if (panelRef.current && !panelRef.current.contains(event.target as Node)) collapseFromOutside();
    };
    window.addEventListener("pointerdown", handlePointerDown);
    return () => window.removeEventListener("pointerdown", handlePointerDown);
  }, [isExpanded, isPinned]);

  useEffect(() => {
    const closeForOtherOverlay = (event: Event) => {
      if ((event as CustomEvent<string>).detail === "news") setIsExpanded(false);
    };
    window.addEventListener("gf3:manager-overlay-open", closeForOtherOverlay);
    return () => window.removeEventListener("gf3:manager-overlay-open", closeForOtherOverlay);
  }, []);

  const openNote = (note: ManagerNote) => {
    setSelectedNoteId(note.id);
    setTitle(note.title);
    setContent(note.content);
    updateContentLength(contentToEditorHtml(note.content).length);
    setNoteColor(note.color);
    setActiveFormats(new Set());
    setEditorVersion(version => version + 1);
    setIsDirty(false);
    setActionError(null);
    setToolbarMenu(null);
    setView("editor");
  };

  const createDraft = () => {
    setSelectedNoteId(null);
    setTitle("");
    setContent("");
    updateContentLength(0);
    setNoteColor(NEW_NOTE_COLOR);
    setActiveFormats(new Set());
    setEditorVersion(version => version + 1);
    setIsDirty(true);
    setActionError(null);
    setToolbarMenu(null);
    setView("editor");
  };

  const syncEditorContent = () => {
    if (!editorRef.current) return "";
    const nextContent = sanitizeRichText(editorRef.current.innerHTML);
    updateContentLength(nextContent === "<br>" ? 0 : nextContent.length);
    setIsDirty(true);
    return nextContent;
  };

  const rememberSelection = () => {
    const selection = window.getSelection();
    if (!selection?.rangeCount || !editorRef.current) return;
    const range = selection.getRangeAt(0);
    if (editorRef.current.contains(range.commonAncestorContainer)) savedSelectionRef.current = range.cloneRange();
  };

  const restoreSelection = () => {
    const selection = window.getSelection();
    const range = savedSelectionRef.current;
    if (!selection || !range || !editorRef.current?.contains(range.commonAncestorContainer)) return;
    selection.removeAllRanges();
    selection.addRange(range);
  };

  const refreshActiveFormats = () => {
    if (typeof document.queryCommandState !== "function") return;
    const commands: FormatCommand[] = ["bold", "italic", "underline", "strikeThrough", "insertUnorderedList", "insertOrderedList"];
    const next = new Set<string>();
    commands.forEach(command => {
      try {
        if (document.queryCommandState(command)) next.add(command);
      } catch {
        // Browsers can reject state checks when selection is outside the editor.
      }
    });
    setActiveFormats(current => {
      const unchanged = current.size === next.size && [...current].every(command => next.has(command));
      return unchanged ? current : next;
    });
  };

  const runEditorCommand = (command: string, value?: string) => {
    editorRef.current?.focus();
    restoreSelection();
    document.execCommand(command, false, value);
    syncEditorContent();
    rememberSelection();
    refreshActiveFormats();
    setToolbarMenu(null);
  };

  const handleSave = () => {
    const editorContent = editorRef.current ? sanitizeRichText(editorRef.current.innerHTML) : contentToEditorHtml(content);
    if (editorContent.length > MAX_CONTENT_LENGTH) {
      setActionError(`Note content cannot exceed ${MAX_CONTENT_LENGTH.toLocaleString()} characters.`);
      return;
    }
    const payload = { title, content: editorContent, color: noteColor };
    const onSuccess = (note: ManagerNote) => {
      setSelectedNoteId(note.id);
      setTitle(note.title);
      setContent(note.content);
      updateContentLength(contentToEditorHtml(note.content).length);
      setNoteColor(note.color);
      setEditorVersion(version => version + 1);
      setIsDirty(false);
      setActionError(null);
    };
    const onError = (error: unknown) => setActionError(getErrorMessage(error, "Could not save this note."));

    if (selectedNoteId === null) {
      createMutation.mutate(payload, { onSuccess, onError });
      return;
    }
    updateMutation.mutate({ noteId: selectedNoteId, payload }, { onSuccess, onError });
  };

  const handleDelete = () => {
    if (deleteNoteId === null) return;
    deleteMutation.mutate(deleteNoteId, {
      onSuccess: () => {
        setDeleteNoteId(null);
        setSelectedNoteId(null);
        setView("list");
        setIsDirty(false);
        setActionError(null);
      },
      onError: error => setActionError(getErrorMessage(error, "Could not delete this note.")),
    });
  };

  const handleToggleExpanded = () => {
    const nextExpanded = !isExpanded;
    if (nextExpanded) window.dispatchEvent(new CustomEvent("gf3:manager-overlay-open", { detail: "notepad" }));
    setIsExpanded(nextExpanded);
    setToolbarMenu(null);
    saveWindowState({ isExpanded: nextExpanded, isPinned, height });
  };

  const handleTogglePinned = () => {
    const nextPinned = !isPinned;
    setIsPinned(nextPinned);
    saveWindowState({ isExpanded: true, isPinned: nextPinned, height });
  };

  const handleResizeStart = (event: ReactPointerEvent<HTMLDivElement>) => {
    if ((event.target as HTMLElement).closest("button")) return;
    event.preventDefault();
    const startY = event.clientY;
    const startHeight = height;
    let nextHeight = height;
    document.body.style.userSelect = "none";
    document.body.style.cursor = "ns-resize";

    const handleMove = (moveEvent: globalThis.PointerEvent) => {
      nextHeight = clampHeight(startHeight + moveEvent.clientY - startY);
      setHeight(nextHeight);
    };
    const handleUp = () => {
      window.removeEventListener("pointermove", handleMove);
      window.removeEventListener("pointerup", handleUp);
      document.body.style.userSelect = "";
      document.body.style.cursor = "";
      saveWindowState({ isExpanded, isPinned, height: nextHeight });
    };
    window.addEventListener("pointermove", handleMove);
    window.addEventListener("pointerup", handleUp, { once: true });
  };

  const isSaving = createMutation.isPending || updateMutation.isPending;
  const queryError = notepadQuery.error;
  const contentTooLong = contentLimit.isTooLong;

  const formatButton = (command: FormatCommand, label: string, glyph: React.ReactNode) => (
    <ToolbarButton
      label={label}
      active={activeFormats.has(command)}
      onRememberSelection={rememberSelection}
      onClick={() => runEditorCommand(command)}
    >
      {glyph}
    </ToolbarButton>
  );

  return (
    <>
      <button
        type="button"
        className={[styles.openTab, isExpanded ? styles.openTabHidden : ""].filter(Boolean).join(" ")}
        aria-label="Open notepad"
        onClick={handleToggleExpanded}
      >
        <NoteIcon size={17} />
        <span>Notes</span>
        <ArrowIcon size={14} />
      </button>

      <aside
        ref={panelRef}
        className={[styles.notepad, isExpanded ? styles.notepadExpanded : styles.notepadCollapsed].join(" ")}
        style={{ height: clampHeight(height) }}
        aria-label="Manager notepad"
        aria-hidden={!isExpanded}
      >
        <header className={styles.header}>
          <div className={styles.heading}>
            <span className={styles.headingIcon}><NoteIcon size={18} /></span>
            <div>
              <strong>Notepad</strong>
              {view === "list" ? (
                <span>{notes.length} notes</span>
              ) : (
                <button type="button" className={styles.headerBackButton} onClick={() => { setView("list"); setToolbarMenu(null); }}>
                  <ArrowIcon size={12} />
                  <span>Notes</span>
                </button>
              )}
            </div>
          </div>
          <button
            type="button"
            className={[styles.iconButton, isPinned ? styles.iconButtonActive : ""].filter(Boolean).join(" ")}
            aria-label={isPinned ? "Unpin notepad" : "Pin notepad"}
            aria-pressed={isPinned}
            title={isPinned ? "Unpin notepad" : "Pin notepad"}
            onClick={handleTogglePinned}
          >
            <PinIcon size={16} />
          </button>
        </header>

        <div className={styles.body}>
          {queryError || actionError ? (
            <div className={styles.errorMessage}>{actionError ?? getErrorMessage(queryError, "Could not load notes.")}</div>
          ) : null}

          {view === "list" ? (
            <div className={styles.listView}>
              <div className={styles.listToolbar}>
                <label className={styles.searchField}>
                  <SearchIcon size={15} />
                  <input value={search} placeholder="Search notes" aria-label="Search notes" onChange={event => setSearch(event.target.value)} />
                  {search ? <button type="button" aria-label="Clear note search" onClick={() => setSearch("")}><CloseIcon size={12} /></button> : null}
                </label>
                <button type="button" className={styles.newButton} onClick={createDraft}>
                  <PlusIcon size={16} />
                  <span>New</span>
                </button>
              </div>

              <div className={styles.noteList}>
                {notepadQuery.isLoading ? <p className={styles.emptyState}>Loading notes...</p> : null}
                {!notepadQuery.isLoading && filteredNotes.length === 0 ? (
                  <div className={styles.emptyState}>
                    <NoteIcon size={26} />
                    <strong>{search ? "No matching notes" : "Your notepad is empty"}</strong>
                    <span>{search ? "Try another search." : "Create a note for something you want to keep close."}</span>
                  </div>
                ) : null}
                {filteredNotes.map(note => (
                  <button type="button" key={note.id} className={styles.noteCard} onClick={() => openNote(note)}>
                    <strong>{note.title}</strong>
                    <span>{notePreview(note)}</span>
                    <time dateTime={note.updatedAtUtc}>{new Date(note.updatedAtUtc).toLocaleDateString()}</time>
                  </button>
                ))}
              </div>
            </div>
          ) : (
            <div className={styles.editorView}>
              <div className={styles.titleRow}>
                <input
                  className={styles.titleInput}
                  value={title}
                  maxLength={160}
                  placeholder="Note title"
                  aria-label="Note title"
                  onChange={event => { setTitle(event.target.value); setIsDirty(true); }}
                />
                {isDirty ? <span className={styles.unsaved}>Unsaved</span> : <span className={styles.saved}>Saved</span>}
              </div>

              <div className={styles.editorWorkspace}>
                <div
                  key={editorVersion}
                  ref={editorRef}
                  className={styles.contentEditor}
                  contentEditable
                  role="textbox"
                  aria-label="Note content"
                  aria-multiline="true"
                  data-placeholder="Write something..."
                  suppressContentEditableWarning
                  onInput={syncEditorContent}
                  onMouseUp={() => { rememberSelection(); refreshActiveFormats(); }}
                  onKeyUp={() => { rememberSelection(); refreshActiveFormats(); }}
                  onBlur={rememberSelection}
                />

                <div className={styles.formatToolbar} role="toolbar" aria-label="Text formatting">
                  {formatButton("bold", "Bold", <strong>B</strong>)}
                  {formatButton("italic", "Italic", <em>I</em>)}
                  {formatButton("underline", "Underline", <span className={styles.underlineGlyph}>U</span>)}
                  {formatButton("strikeThrough", "Strikethrough", <span className={styles.strikeGlyph}>S</span>)}
                  <span className={styles.toolbarSeparator} />
                  <ToolbarButton label="Text size" active={toolbarMenu === "size"} onRememberSelection={rememberSelection} onClick={() => setToolbarMenu(menu => menu === "size" ? null : "size")}>
                    <span className={styles.sizeGlyph}>Aa</span>
                  </ToolbarButton>
                  <ToolbarButton label="Text color" active={toolbarMenu === "color"} onRememberSelection={rememberSelection} onClick={() => setToolbarMenu(menu => menu === "color" ? null : "color")}>
                    <span className={styles.colorGlyph} style={{ "--text-color": textColor } as React.CSSProperties}>A</span>
                  </ToolbarButton>
                  <span className={styles.toolbarSeparator} />
                  {formatButton("insertUnorderedList", "Bulleted list", "•≡")}
                  {formatButton("insertOrderedList", "Numbered list", "1≡")}
                  <span className={styles.toolbarSeparator} />
                  <ToolbarButton label="Undo" onRememberSelection={rememberSelection} onClick={() => runEditorCommand("undo")}>↶</ToolbarButton>
                  <ToolbarButton label="Redo" onRememberSelection={rememberSelection} onClick={() => runEditorCommand("redo")}>↷</ToolbarButton>
                  <ToolbarButton label="Clear formatting" onRememberSelection={rememberSelection} onClick={() => runEditorCommand("removeFormat")}>Tx</ToolbarButton>
                </div>

                {toolbarMenu === "color" ? (
                  <div className={styles.toolbarPopover} role="dialog" aria-label="Choose text color">
                    <strong>Text color</strong>
                    <div className={styles.textColorGrid}>
                      {textColors.map(option => (
                        <button
                          type="button"
                          key={option}
                          className={[styles.textColorButton, textColor === option ? styles.textColorButtonActive : ""].filter(Boolean).join(" ")}
                          style={{ backgroundColor: option }}
                          aria-label={`Text color ${option}`}
                          aria-pressed={textColor === option}
                          onPointerDown={event => { event.preventDefault(); rememberSelection(); }}
                          onClick={() => { setTextColor(option); runEditorCommand("foreColor", option); }}
                        />
                      ))}
                    </div>
                  </div>
                ) : null}

                {toolbarMenu === "size" ? (
                  <div className={styles.toolbarPopover} role="dialog" aria-label="Choose text size">
                    <strong>Text size</strong>
                    <div className={styles.sizeGrid}>
                      {fontSizes.map(option => (
                        <button
                          type="button"
                          key={option.value}
                          onPointerDown={event => { event.preventDefault(); rememberSelection(); }}
                          onClick={() => runEditorCommand("fontSize", option.value)}
                        >
                          {option.label}px
                        </button>
                      ))}
                    </div>
                  </div>
                ) : null}
              </div>

              <div className={styles.editorFooter}>
                <span className={[styles.contentLength, contentTooLong ? styles.contentLengthError : ""].filter(Boolean).join(" ")}>
                  {contentTooLong ? `${contentLimit.length.toLocaleString()} / ${MAX_CONTENT_LENGTH.toLocaleString()}` : ""}
                </span>
                <div className={styles.editorActions}>
                  {selectedNoteId !== null ? (
                    <button type="button" className={styles.deleteButton} onClick={() => setDeleteNoteId(selectedNoteId)}>Delete</button>
                  ) : null}
                  <button type="button" className={styles.saveButton} disabled={isSaving || !isDirty || contentTooLong} onClick={handleSave}>
                    <SaveIcon size={15} />
                    <span>{isSaving ? "Saving..." : "Save"}</span>
                  </button>
                </div>
              </div>
            </div>
          )}
        </div>

        <div className={styles.bottomDock} role="separator" aria-label="Resize notepad" onPointerDown={handleResizeStart}>
          <button type="button" aria-label="Collapse notepad" title="Collapse notepad" onClick={handleToggleExpanded}>
            <ArrowIcon size={15} />
          </button>
        </div>
      </aside>

      <ConfirmDialog
        open={deleteNoteId !== null}
        title="Delete note?"
        message="This note will be permanently deleted."
        confirmText={deleteMutation.isPending ? "Deleting..." : "Delete"}
        confirmDisabled={deleteMutation.isPending}
        cancelDisabled={deleteMutation.isPending}
        onCancel={() => setDeleteNoteId(null)}
        onConfirm={handleDelete}
      />
    </>
  );
}
