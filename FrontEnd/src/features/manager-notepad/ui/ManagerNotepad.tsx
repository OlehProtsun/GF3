import { t } from "@shared/i18n";
import { useCallback, useEffect, useEffectEvent, useLayoutEffect, useRef, useState, type PointerEvent as ReactPointerEvent } from "react";
import {
  managerNotepadApi,
  useDeleteManagerNoteMutation,
  useManagerNotepadQuery,
  useSaveManagerNotepadStateMutation,
  useUpdateManagerNoteMetadataMutation,
  type ManagerNote,
  type ManagerNotepadState,
  type SaveManagerNoteInput,
  type UpdateManagerNoteMetadataInput,
} from "@entities/manager-notepad";
import { useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import { ApiError, getAuthAccessToken, getAuthGeneration, getErrorMessage } from "@shared/api/httpClient";
import { useNavigationBlocker } from "@shared/lib/react-router-dom";
import { ConfirmDialog } from "@shared/ui/ConfirmDialog";
import { IosButton } from "@shared/ui/components/IosButton";
import { NotepadIcon } from "./NotepadIcon";
import { NotepadEditorView } from "./NotepadEditorView";
import { NotepadListView, type NotepadListFilters } from "./NotepadListView";
import { NotepadVersionHistoryView } from "./NotepadVersionHistoryView";
import { contentToEditorHtml, richTextToPlainText } from "./notepadRichText";
import { MAX_NOTE_CONTENT, useNotepadAutosave, type NoteAccess, type NotepadSaveError } from "./useNotepadAutosave";
import { usePublicNoteSession } from "./usePublicNoteSession";
import styles from "./ManagerNotepad.module.css";
import views from "./NotepadViews.module.css";

type View = "list" | "editor" | "versions";
type Draft = SaveManagerNoteInput & { saved: ManagerNote | null; savedHtml: string; access?: NoteAccess };

type RecoveredDraft = { draft: Draft; error: NotepadSaveError | null };
type PendingNavigation = { proceed: () => void; cancel?: () => void };
const recoveryKey = queryKeys.managerNotepad.draftRecovery();

const MIN_WIDTH = 486;
const MAX_WIDTH = 760;
const MIN_HEIGHT = 520;
const MAX_HEIGHT = 900;
const DEFAULT_DRAFT: Draft = { title: "", content: "", color: "slate", saved: null, savedHtml: "" };

function clampHeight(value: number) {
  return Math.max(MIN_HEIGHT, Math.min(value, MAX_HEIGHT));
}

function createDraft(note: ManagerNote | null): Draft {
  if (!note) return DEFAULT_DRAFT;
  const html = contentToEditorHtml(note.content);
  return { title: note.title, content: html, color: note.color, saved: note, savedHtml: html };
}

function hasDraftChanges(draft: Draft | null) {
  return draft !== null && (draft.saved
    ? draft.title !== draft.saved.title || draft.content !== draft.savedHtml || draft.color !== draft.saved.color
    : Boolean(draft.title.trim() || richTextToPlainText(draft.content).trim()));
}

function PhoneNotepadNavigationGuard({ shouldBlock, onBlock }: {
  shouldBlock: () => boolean;
  onBlock: (navigation: PendingNavigation) => void;
}) {
  const navigation = useRef<PendingNavigation | null>(null);
  const handleBlocked = useCallback(() => {
    if (navigation.current) onBlock(navigation.current);
  }, [onBlock]);
  const { proceedBlockedNavigation, cancelBlockedNavigation } = useNavigationBlocker(true, shouldBlock, handleBlocked);
  useLayoutEffect(() => {
    navigation.current = { proceed: proceedBlockedNavigation, cancel: cancelBlockedNavigation };
  }, [proceedBlockedNavigation, cancelBlockedNavigation]);
  return null;
}

export function ManagerNotepad({ onClosePhone }: { onClosePhone?: () => void } = {}) {
  const isPhone = Boolean(onClosePhone);
  const [isExpanded, setIsExpanded] = useState(isPhone);
  const [isPinned, setIsPinned] = useState(false);
  const [width, setWidth] = useState(MIN_WIDTH);
  const [height, setHeight] = useState(MIN_HEIGHT);
  const [view, setView] = useState<View>("list");
  const [focusRequested, setFocusRequested] = useState(false);
  const [tab, setTab] = useState<"private" | "public">("private");
  const [filters, setFilters] = useState<NotepadListFilters>({ search: "", sort: "updated" });
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [draft, setDraft] = useState<Draft | null>(null);
  const [editorGeneration, setEditorGeneration] = useState(0);
  const [deleteNoteId, setDeleteNoteId] = useState<number | null>(null);
  const [pendingNavigation, setPendingNavigation] = useState<PendingNavigation | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [showDraftCopy, setShowDraftCopy] = useState(false);
  const [opening, setOpening] = useState(false);
  const queryClient = useQueryClient();
  const [recovering, setRecovering] = useState(() => Boolean(queryClient.getQueryState(recoveryKey).data));
  const acknowledgedNote = useRef<ManagerNote | null>(null);
  const unmountDraft = useRef<Draft | null>(null);
  const savingDraft = useRef(false);
  const shouldBlockPhoneNavigation = useCallback(() => unmountDraft.current !== null || savingDraft.current, []);
  const panelRef = useRef<HTMLElement>(null);
  const phoneDialogRef = useRef<HTMLDialogElement>(null);
  const endFocusEditing = useCallback(() => {
    setFocusRequested(false);
    panelRef.current?.focus({ preventScroll: true });
  }, []);
  const launcherRef = useRef<HTMLButtonElement>(null);
  const initialFocus = useRef(isPhone);
  const hydrated = useRef(false);
  const navigating = useRef(false);
  const resizeCleanup = useRef<(() => void) | null>(null);
  const windowState = useRef<ManagerNotepadState>({ isExpanded: false, isPinned: false, height: MIN_HEIGHT });

  const publicNote = draft?.saved?.visibility === "public" ? draft.saved : null;
  const notepad = useManagerNotepadQuery(true, isExpanded && (isPhone || tab === "public" || publicNote !== null));
  const deleteMutation = useDeleteManagerNoteMutation();
  const stateMutation = useSaveManagerNotepadStateMutation();
  const metadataMutation = useUpdateManagerNoteMetadataMutation();
  const notes = notepad.data?.notes ?? [];
  const note = notes.find(item => item.id === selectedId);
  const dirty = hasDraftChanges(draft);
  const publicSession = usePublicNoteSession(
    isExpanded && view !== "list" && publicNote && (publicNote.canEdit || publicNote.canManage) ? publicNote.id : null,
    fresh => {
      if (dirty || draft?.access) {
        if (draft?.saved?.revision !== fresh.revision) {
          autosave.restoreError({ kind: "conflict", cause: new Error(t("This note changed elsewhere. Your draft is still here.")) });
        } else if (autosave.readError()?.kind !== "conflict") {
          autosave.resetError();
        }
      } else if (draft?.saved?.revision !== fresh.revision) {
        resetEditor(createDraft(fresh));
      }
    },
    () => void notepad.refresh(),
  );
  const sessionRequired = publicNote !== null;
  const sessionBlocked = sessionRequired && !publicSession.token;
  const canEdit = draft !== null && !sessionBlocked &&
    (!draft.saved || (sessionRequired ? draft.saved.canEdit : note?.canEdit ?? draft.saved.canEdit));
  const focusEditing = isPhone && view === "editor" && canEdit && focusRequested;
  const shouldBlockPhoneRoute = useCallback(() => focusEditing || shouldBlockPhoneNavigation(), [focusEditing, shouldBlockPhoneNavigation]);
  const handleBlockedPhoneRoute = useCallback((navigation: PendingNavigation) => {
    if (focusEditing) {
      navigation.cancel?.();
      endFocusEditing();
    } else {
      setPendingNavigation(navigation);
    }
  }, [focusEditing, endFocusEditing]);
  const access = draft?.access;
  const contentToSave = draft?.saved && draft.content === draft.savedHtml ? draft.saved.content : draft?.content ?? "";
  const contentTooLong = contentToSave.length > MAX_NOTE_CONTENT;
  const autosave = useNotepadAutosave({
    noteId: draft?.saved?.id ?? null,
    revision: draft?.saved?.revision ?? null,
    title: draft?.title ?? "",
    content: contentToSave,
    color: draft?.color ?? DEFAULT_DRAFT.color,
    dirty,
    hasPendingAccess: Boolean(access),
    explicitSave: isPhone,
    sessionRequired,
    sessionToken: publicSession.token,
    enabled: canEdit && view === "editor" && !contentTooLong,
    onSaved: (saved, submitted) => {
      acknowledgedNote.current = saved;
      setSelectedId(saved.id);
      const acknowledge = (current: Draft | null) => current && {
        ...current,
        title: current.title === submitted.title ? saved.title : current.title,
        access: submitted.visibility !== undefined ? undefined : current.access,
        saved,
        savedHtml: contentToEditorHtml(saved.content),
      };
      const remaining = acknowledge(unmountDraft.current);
      unmountDraft.current = hasDraftChanges(remaining) || remaining?.access ? remaining : null;
      setDraft(acknowledge);
      setShowDraftCopy(false);
    },
    onError: error => {
      setActionError(null);
      if (error instanceof ApiError && (error.details as { code?: string } | null)?.code === "notepad_edit_session_conflict") {
        publicSession.invalidate();
      }
    },
  });

  useLayoutEffect(() => {
    unmountDraft.current = dirty || access || (isPhone && autosave.isSaving) ? draft : null;
    savingDraft.current = autosave.isSaving;
  }, [dirty, draft, access, autosave.isSaving, isPhone]);

  const { flush, waitForSave, readError, restoreError } = autosave;
  useEffect(() => {
    const generation = getAuthGeneration();
    let active = true;
    const pending = queryClient.getQueryState<Promise<RecoveredDraft | null>>(recoveryKey).data;
    // Account-scoped memory preserves a draft across workspace-mode changes.
    void pending?.then(recovered => {
      if (!active || generation !== getAuthGeneration()) return;
      queryClient.setQueryData(recoveryKey, undefined);
      setRecovering(false);
      if (!recovered) return;
      setSelectedId(recovered.draft.saved?.id ?? null);
      acknowledgedNote.current = recovered.draft.saved;
      setDraft(recovered.draft);
      setEditorGeneration(value => value + 1);
      setView("editor");
      restoreError(recovered.error);
    });
    return () => {
      active = false;
      if (!unmountDraft.current || !getAuthAccessToken()) return;
      const save = generation !== getAuthGeneration() ? Promise.resolve(false) : isPhone ? waitForSave() : flush();
      queryClient.setQueryData(recoveryKey, save.then(success => {
        const current = unmountDraft.current;
        return !current || (!isPhone && success && !current.access) ? null : {
          draft: current,
          error: success ? null : readError() ?? (isPhone ? null : { kind: "error", cause: new Error(t("Could not save this note.")) }),
        };
      }));
    };
  }, [flush, isPhone, queryClient, readError, restoreError, waitForSave]);

  const saveWindowState = (patch: Partial<ManagerNotepadState>) => {
    if (isPhone) return;
    const next = { ...windowState.current, ...patch };
    windowState.current = next;
    stateMutation.mutate(next, {
      onError: error => setActionError(getErrorMessage(error, t("Could not save notepad position."))),
    });
  };

  const showPanel = () => {
    window.dispatchEvent(new CustomEvent("gf3:manager-overlay-open", { detail: "notepad" }));
    initialFocus.current = true;
    setIsExpanded(true);
  };

  const hydratePanel = useEffectEvent((state: ManagerNotepadState) => {
    if (hydrated.current) return;
    hydrated.current = true;
    windowState.current = { ...state, height: clampHeight(state.height) };
    setHeight(windowState.current.height);
    setIsPinned(state.isPinned);
    if (state.isExpanded) {
      window.dispatchEvent(new CustomEvent("gf3:manager-overlay-open", { detail: "notepad" }));
      setIsExpanded(true);
    }
  });

  useEffect(() => {
    if (!isPhone && notepad.data) hydratePanel(notepad.data.state);
  }, [notepad.data, isPhone]);

  useEffect(() => {
    if (isExpanded && initialFocus.current) {
      initialFocus.current = false;
      panelRef.current?.focus();
    }
  }, [isExpanded]);

  useLayoutEffect(() => {
    const dialog = phoneDialogRef.current;
    if (!dialog) return;
    const scroll = { left: window.scrollX, top: window.scrollY };
    const lockPosition = getComputedStyle(document.body).position !== "fixed";
    const bodyStyle = document.body.style;
    if (lockPosition) {
      bodyStyle.setProperty("--notepad-page-left", `${-scroll.left}px`);
      bodyStyle.setProperty("--notepad-page-top", `${-scroll.top}px`);
      bodyStyle.setProperty("--notepad-scrollbar-gap", `${window.innerWidth - document.documentElement.clientWidth}px`);
      dialog.dataset.lockPagePosition = "";
    }
    dialog.showModal();
    return () => {
      dialog.close();
      if (!lockPosition) return;
      delete dialog.dataset.lockPagePosition;
      bodyStyle.removeProperty("--notepad-page-left");
      bodyStyle.removeProperty("--notepad-page-top");
      bodyStyle.removeProperty("--notepad-scrollbar-gap");
      window.scrollTo({ ...scroll, behavior: "instant" });
    };
  }, [isPhone]);

  const handlePhoneKeyboard = useEffectEvent((wasVisible: boolean, obscuredHeight: number) => {
    if (!focusEditing) return false;
    if (wasVisible && obscuredHeight < 60) {
      endFocusEditing();
      return false;
    }
    return wasVisible || obscuredHeight > 100;
  });

  useLayoutEffect(() => {
    const dialog = phoneDialogRef.current;
    const viewport = window.visualViewport;
    if (!dialog || !viewport) return;
    let keyboardVisible = false;
    let layoutWidth = window.innerWidth;
    let restingHeight = window.innerHeight;
    let frame = 0;
    const update = () => {
      frame = 0;
      dialog.style.setProperty("--notepad-phone-height", `${viewport.height}px`);
      dialog.style.setProperty("--notepad-phone-top", `${viewport.offsetTop}px`);
      dialog.style.setProperty("--notepad-phone-width", `${viewport.width}px`);
      dialog.style.setProperty("--notepad-phone-left", `${viewport.offsetLeft}px`);
      if (Math.abs(viewport.scale - 1) < 0.01) {
        if (layoutWidth !== window.innerWidth) {
          layoutWidth = window.innerWidth;
          restingHeight = window.innerHeight;
        } else {
          restingHeight = Math.max(restingHeight, window.innerHeight);
        }
        keyboardVisible = handlePhoneKeyboard(keyboardVisible, restingHeight - viewport.height);
        dialog.toggleAttribute("data-keyboard-visible", keyboardVisible);
      }
    };
    const scheduleUpdate = () => { if (!frame) frame = requestAnimationFrame(update); };
    update();
    viewport.addEventListener("resize", scheduleUpdate);
    viewport.addEventListener("scroll", scheduleUpdate);
    window.addEventListener("resize", scheduleUpdate);
    return () => {
      viewport.removeEventListener("resize", scheduleUpdate);
      viewport.removeEventListener("scroll", scheduleUpdate);
      window.removeEventListener("resize", scheduleUpdate);
      cancelAnimationFrame(frame);
    };
  }, [isPhone]);

  const phoneConfirmationOpen = isPhone && (pendingNavigation !== null || deleteNoteId !== null);
  useLayoutEffect(() => {
    if (!phoneConfirmationOpen) return;
    phoneDialogRef.current?.querySelector<HTMLElement>('div[aria-modal="true"] button:not(:disabled)')?.focus({ preventScroll: true });
  }, [phoneConfirmationOpen]);

  useEffect(() => {
    if (isPhone && focusRequested && !focusEditing) endFocusEditing();
  }, [isPhone, focusRequested, focusEditing, endFocusEditing]);

  const closePanel = (returnFocus: boolean) => {
    if (onClosePhone) {
      onClosePhone();
      return;
    }
    setIsExpanded(false);
    saveWindowState({ isExpanded: false });
    if (returnFocus) requestAnimationFrame(() => launcherRef.current?.focus());
  };

  const navigate = async (proceed: () => void) => {
    if (isPhone) {
      if (shouldBlockPhoneNavigation()) setPendingNavigation({ proceed });
      else proceed();
      return;
    }
    if (navigating.current) return;
    navigating.current = true;
    try {
      if (await autosave.flush() && !unmountDraft.current?.access) proceed();
      else setPendingNavigation({ proceed });
    } finally {
      navigating.current = false;
    }
  };

  const collapseOutside = useEffectEvent((event: PointerEvent) => {
    const target = event.target;
    if (!(target instanceof Element) || panelRef.current?.contains(target) ||
      launcherRef.current?.contains(target) || target.closest('[role="alertdialog"], [role="dialog"], [role="menu"]')) return;
    void navigate(() => closePanel(false));
  });

  const openNews = useEffectEvent((event: Event) => {
    if ((event as CustomEvent<string>).detail !== "news" || !isExpanded) return;
    // News takes the overlay immediately. Keep the editor draft mounted if its final save fails.
    closePanel(false);
    void autosave.flush();
  });

  useLayoutEffect(() => {
    if (isPhone || !isExpanded || isPinned || deleteNoteId !== null || pendingNavigation) return;
    const handleOutside = (event: PointerEvent) => collapseOutside(event);
    window.addEventListener("pointerdown", handleOutside);
    return () => window.removeEventListener("pointerdown", handleOutside);
  }, [isExpanded, isPinned, deleteNoteId, pendingNavigation, isPhone]);

  useEffect(() => {
    if (isPhone) return;
    const handleOverlay = (event: Event) => openNews(event);
    window.addEventListener("gf3:manager-overlay-open", handleOverlay);
    return () => {
      window.removeEventListener("gf3:manager-overlay-open", handleOverlay);
      resizeCleanup.current?.();
    };
  }, [isPhone]);

  const resetEditor = (next: Draft | null) => {
    if (isPhone) setFocusRequested(false);
    acknowledgedNote.current = next?.saved ?? null;
    autosave.resetError();
    setShowDraftCopy(false);
    setDraft(next);
    setEditorGeneration(value => value + 1);
  };

  const openNote = async (selected: ManagerNote) => {
    if (opening) return;
    setOpening(true);
    setActionError(null);
    try {
      // Recheck visibility and permissions before deciding whether the editor needs a lease.
      const current = await managerNotepadApi.getNote(selected.id);
      if (isPhone) {
        const key = queryKeys.managerNotepad.current();
        const { data: cached } = queryClient.getQueryState<NonNullable<typeof notepad.data>>(key);
        if (cached) {
          queryClient.setQueryData(key, {
            ...cached,
            notes: cached.notes.map(item => item.id === current.id && item.revision <= current.revision ? current : item),
          });
        }
      }
      setSelectedId(current.id);
      resetEditor(current.canManage || current.visibility === "public" ? createDraft(current) : null);
      setView("editor");
    } catch (error) {
      setActionError(getErrorMessage(error, t("This note is no longer available.")));
      void notepad.refresh();
    } finally {
      setOpening(false);
    }
  };

  const goToList = () => {
    resetEditor(null);
    setSelectedId(null);
    setView("list");
  };

  const restoreServerCopy = async () => {
    if (!selectedId) return;
    setActionError(null);
    try {
      const current = await managerNotepadApi.getNote(selectedId);
      resetEditor(createDraft(current));
      void notepad.refresh();
    } catch (error) {
      setActionError(getErrorMessage(error, t("Could not load notes.")));
    }
  };

  const copyDraft = async () => {
    try {
      await navigator.clipboard.writeText([draft?.title, richTextToPlainText(draft?.content ?? "")].join("\n\n"));
      setShowDraftCopy(false);
    } catch {
      setShowDraftCopy(true);
    }
  };

  const togglePanel = () => {
    if (isExpanded) {
      void navigate(() => closePanel(true));
      return;
    }
    hydrated.current = true;
    showPanel();
    saveWindowState({ isExpanded: true });
    if (notepad.data) notepad.refresh();
  };

  const resizeLimits = (panel: HTMLElement) => {
    const style = getComputedStyle(panel);
    const viewport = document.documentElement;
    const maxWidth = Math.min(MAX_WIDTH, Number.parseFloat(style.maxWidth) || viewport.clientWidth);
    const maxHeight = Math.min(MAX_HEIGHT, Number.parseFloat(style.maxHeight) || viewport.clientHeight - panel.offsetTop - 14);
    return {
      width: (value: number) => Math.min(maxWidth, Math.max(MIN_WIDTH, value)),
      height: (value: number) => Math.min(maxHeight, clampHeight(value)),
    };
  };

  const startResize = (event: ReactPointerEvent<HTMLDivElement>, edge: "bottom" | "right" | "left" | "corner") => {
    const panel = panelRef.current;
    if (!panel || !event.isPrimary || event.button !== 0) return;
    event.preventDefault();
    resizeCleanup.current?.();
    const handle = event.currentTarget;
    const { pointerId, clientX, clientY } = event;
    const size = panel.getBoundingClientRect();
    const limits = resizeLimits(panel);
    let nextHeight: number | undefined;

    const move = (pointer: PointerEvent) => {
      if (pointer.pointerId !== pointerId) return;
      if (edge !== "bottom") {
        const delta = (pointer.clientX - clientX) * (edge === "left" ? -1 : 1);
        setWidth(limits.width(Math.round(size.width + delta)));
      }
      if (edge === "bottom" || edge === "corner") {
        nextHeight = limits.height(Math.round(size.height + pointer.clientY - clientY));
        setHeight(nextHeight);
      }
    };
    const end = (pointer: PointerEvent) => {
      if (pointer.pointerId !== pointerId) return;
      resizeCleanup.current?.();
      if (nextHeight !== undefined) saveWindowState({ height: clampHeight(nextHeight) });
    };
    resizeCleanup.current = () => {
      handle.removeEventListener("pointermove", move);
      handle.removeEventListener("pointerup", end);
      handle.removeEventListener("pointercancel", end);
      handle.removeEventListener("lostpointercapture", end);
      if (handle.hasPointerCapture(pointerId)) handle.releasePointerCapture(pointerId);
      resizeCleanup.current = null;
    };
    handle.setPointerCapture(pointerId);
    handle.addEventListener("pointermove", move);
    handle.addEventListener("pointerup", end);
    handle.addEventListener("pointercancel", end);
    handle.addEventListener("lostpointercapture", end);
  };

  const updateMetadata = async (noteId: number, payload: UpdateManagerNoteMetadataInput) => {
    setActionError(null);
    try {
      return await metadataMutation.mutateAsync({ noteId, payload, token: selectedId === noteId ? publicSession.token : undefined });
    } catch (error) {
      if (selectedId === noteId && error instanceof ApiError &&
        (error.details as { code?: string } | null)?.code === "notepad_edit_session_conflict") {
        publicSession.invalidate();
      }
      if (error instanceof ApiError && error.status === 409) {
        setActionError(t("This note changed elsewhere. Reopen it to load the latest copy."));
        void notepad.refresh();
      } else {
        setActionError(getErrorMessage(error, t("Could not update note properties.")));
      }
      return null;
    }
  };

  const saveDraft = () => {
    void autosave.save(true, access);
  };
  const canSaveDraft = Boolean(draft && !sessionBlocked && (dirty ? canEdit : access && draft.saved?.canManage) && !contentTooLong);
  const selectedNote = publicNote ?? note ?? draft?.saved ?? null;
  const publicAccessLost = publicNote !== null && (publicSession.status === "unavailable" ||
    (notepad.isSuccess && !note && !publicNote.canManage));
  const publicEditor = note?.activeEditor;
  useEffect(() => {
    if (!publicNote || !note || !sessionBlocked || dirty || access || note.revision <= publicNote.revision) return;
    resetEditor(createDraft(note));
  }, [publicNote, note, sessionBlocked, dirty, access]);
  const saveError = autosave.error;
  const cancelNavigation = () => {
    pendingNavigation?.cancel?.();
    setPendingNavigation(null);
  };
  const saveAndNavigate = async () => {
    const next = pendingNavigation;
    if (!next || autosave.isSaving) return;
    const saved = await autosave.save(true, access);
    if (saved && !unmountDraft.current) {
      setPendingNavigation(null);
      next.proceed();
    } else {
      cancelNavigation();
    }
  };
  const queryError = notepad.error;
  const showEditor = view === "editor" && Boolean(draft || note);
  const collapseButton = !isPhone && (
    <button type="button" className={styles.collapseButton} aria-label={t("Collapse notepad")}
      title={t("Collapse notepad")} onClick={togglePanel}><NotepadIcon name="chevronUp" size={18} /></button>
  );

  const content = (
    <>
      {isPhone && <PhoneNotepadNavigationGuard shouldBlock={shouldBlockPhoneRoute} onBlock={handleBlockedPhoneRoute} />}
      {!isPhone && <button
        type="button"
        ref={launcherRef}
        className={[styles.openTab, isExpanded ? styles.openTabHidden : ""].join(" ")}
        aria-hidden={isExpanded}
        tabIndex={isExpanded ? -1 : 0}
        aria-expanded={isExpanded}
        aria-controls="manager-notepad-panel"
        aria-label={t("Open notepad")}
        title={t("Open notepad")}
        onClick={togglePanel}
      >
        <NotepadIcon name="note" size={19} />
      </button>}

      <aside
        id="manager-notepad-panel"
        data-notepad-docked={!isPhone && isExpanded && isPinned ? "" : undefined}
        ref={panelRef}
        className={[styles.notepad, isExpanded ? styles.notepadExpanded : styles.notepadCollapsed, isPhone ? styles.phoneNotepad : ""].join(" ")}
        data-focus-editing={focusEditing || undefined}
        style={isPhone ? undefined : {
          height,
          width,
          minWidth: `min(${MIN_WIDTH}px, var(--notepad-available-width))`,
          minHeight: `min(${MIN_HEIGHT}px, var(--notepad-available-height))`,
        }}
        role={isPhone ? undefined : "dialog"}
        aria-label={isPhone ? undefined : t("Manager notepad")}
        tabIndex={-1}
        inert={!isExpanded || phoneConfirmationOpen}
        aria-hidden={!isExpanded}
        onKeyDown={event => {
          if (event.key !== "Escape" || event.defaultPrevented ||
            (event.target as HTMLElement).closest('[role="dialog"], dialog') !== (isPhone ? phoneDialogRef.current : event.currentTarget)) return;
          event.preventDefault();
          event.stopPropagation();
          if (focusEditing) endFocusEditing();
          else void navigate(() => closePanel(true));
        }}
      >
        <header className={styles.header}>
          {isPhone && (
            <button type="button" className={styles.phoneBack} onClick={togglePanel}>
              <NotepadIcon name="back" size={16} />{t("Back")}
            </button>
          )}
          <div className={styles.heading}>
            <span className={styles.headingIcon}><NotepadIcon name="note" size={22} /></span>
            <div><strong className={views.compactLabel} title={t("Notepad")}>{t("Notepad")}</strong><span className={views.compactLabel} title={t("Manager notes")}>{t("Manager notes")}</span></div>
          </div>
          <div className={views.headerActions}>
            {!isPhone && <button type="button" className={isPinned ? `${styles.iconButton} ${styles.headerButton} ${styles.iconButtonActive}` : `${styles.iconButton} ${styles.headerButton}`}
              aria-label={isPinned ? t("Unpin notepad") : t("Pin notepad")} aria-pressed={isPinned}
              onClick={() => { setIsPinned(!isPinned); saveWindowState({ isPinned: !isPinned }); }}>
              <NotepadIcon name="pin" size={17} />
            </button>}
            <button type="button" className={`${styles.iconButton} ${styles.headerButton}`} aria-label={t("Close notepad")} onClick={togglePanel}>
              <NotepadIcon name="close" size={17} />
            </button>
          </div>
        </header>

        <div className={styles.body}>
          {actionError && <div className={styles.errorMessage} role="alert">{actionError}</div>}
          {view === "list" && (
            <NotepadListView
              tab={tab} onTabChange={next => { setTab(next); if (next === "public") notepad.refresh(); }}
              filters={filters} onFiltersChange={setFilters} notes={notes}
              loading={notepad.isLoading || recovering} error={queryError} onRetry={() => void notepad.refresh()} opening={opening}
              onOpen={selected => void openNote(selected)}
              onNew={() => { setTab("private"); setSelectedId(null); resetEditor(createDraft(null)); setView("editor"); }}
              onTogglePin={item => void updateMetadata(item.id, { isPinned: !item.isPinned, expectedRevision: item.revision })}
              pinPending={metadataMutation.isPending}
            />
          )}
          {view === "editor" && (!publicAccessLost && (draft || note) ? (
            <NotepadEditorView
              key={draft ? editorGeneration : `reader-${note?.id}-${note?.revision}`}
              note={selectedNote} title={draft?.title ?? note?.title ?? ""}
              content={draft?.content ?? contentToEditorHtml(note?.content ?? "")}
              isPhone={isPhone} sessionBlocked={sessionBlocked} canEdit={canEdit} dirty={dirty || Boolean(access)} isSaving={autosave.isSaving || metadataMutation.isPending} saveFailed={Boolean(saveError)}
              focusEditing={focusEditing}
              onStartFocusEditing={isPhone ? () => setFocusRequested(true) : undefined}
              onLeaveWritingArea={isPhone ? () => setFocusRequested(false) : undefined}
              onTitle={title => setDraft(current => current && { ...current, title })}
              onContent={content => setDraft(current => current && { ...current, content })}
              onBack={() => void navigate(goToList)}
              onSave={saveDraft}
              collapseButton={collapseButton}
              access={access} expanded={isExpanded}
              onAccessChange={next => setDraft(current => {
                if (!current) return current;
                const savedVisibility = current.saved?.visibility ?? "private";
                const savedRecipients = current.saved?.sharedWithManagerIds ?? [];
                const unchanged = next.visibility === savedVisibility &&
                  next.sharedWithManagerIds.length === savedRecipients.length &&
                  next.sharedWithManagerIds.every(id => savedRecipients.includes(id));
                return { ...current, access: unchanged ? undefined : next };
              })}
              onDelete={() => selectedNote && setDeleteNoteId(selectedNote.id)}
              onVersions={() => void navigate(() => setView("versions"))}
              onLockChange={async isLocked => {
                if (!selectedNote?.canManage) return;
                const applyLock = async () => {
                  const base = acknowledgedNote.current;
                  if (base?.id !== selectedNote.id) return;
                  const saved = await updateMetadata(base.id, { isLocked, expectedRevision: base.revision });
                  if (!saved || acknowledgedNote.current?.id !== saved.id) return;
                  acknowledgedNote.current = saved;
                  setDraft(item => item?.saved?.id === saved.id ? { ...item, saved } : item);
                };
                if (isPhone && isLocked) {
                  await navigate(() => void applyLock());
                } else if (!isLocked || await autosave.flush()) {
                  await applyLock();
                }
              }}
              contentTooLong={contentTooLong} canSave={saveError?.kind !== "conflict" && canSaveDraft}
            >
              {sessionBlocked && (publicSession.status || publicEditor) && (
                <div className={views.sessionBanner} role="status">
                  <NotepadIcon name="lock" size={17} />
                  <div>
                    <strong>{publicSession.status === "connecting" ? t("Checking edit access")
                      : publicEditor ? t("Currently being edited") : t("Public note is read-only")}</strong>
                    {publicEditor && <p>{t("{0} is editing this note.", publicEditor.displayName)}</p>}
                    {publicSession.status === "lost" && <p>{t("The edit session ended. Your draft is still here.")}</p>}
                  </div>
                  {publicSession.status && publicSession.status !== "connecting" && (
                    <button type="button" onClick={publicSession.retry}>{t("Try again")}</button>
                  )}
                </div>
              )}
              {saveError && (
                <div className={styles.errorMessage} role="alert">
                  {saveError.kind === "conflict" ? t("This note changed elsewhere. Your draft is still here.")
                    : getErrorMessage(saveError.cause, t("Could not save this note."))}
                  {saveError.kind === "conflict" && (
                    <div className={views.headerActions}>
                      <button type="button" onClick={() => void restoreServerCopy()}>{t("Reload server copy")}</button>
                      <button type="button" onClick={() => void copyDraft()}>{t("Copy my draft")}</button>
                    </div>
                  )}
                  {showDraftCopy && <textarea readOnly aria-label={t("Copy my draft")}
                    value={[draft?.title, richTextToPlainText(draft?.content ?? "")].join("\n\n")}
                    onFocus={event => event.target.select()} />}
                </div>
              )}
            </NotepadEditorView>
          ) : (
            <div className={styles.emptyState} role="status">
              <strong>{t("This note is no longer available.")}</strong>
              {dirty && <button type="button" onClick={() => void copyDraft()}>{t("Copy my draft")}</button>}
              {showDraftCopy && <textarea readOnly aria-label={t("Copy my draft")}
                value={[draft?.title, richTextToPlainText(draft?.content ?? "")].join("\n\n")} />}
              <button type="button" onClick={() => void navigate(goToList)}>{t("Back to notes")}</button>
            </div>
          ))}
          {view === "versions" && selectedNote?.canManage && <NotepadVersionHistoryView note={selectedNote}
            sessionBlocked={sessionBlocked} sessionToken={publicSession.token}
            onBack={() => setView("editor")} onRestored={saved => { resetEditor(createDraft(saved)); setView("editor"); }} />}
          {!isPhone && !showEditor && <div className={styles.collapseOnly}>{collapseButton}</div>}
        </div>

        {!isPhone && <>
          <div className={`${styles.resizeEdge} ${styles.resizeBottom}`} role="separator" aria-orientation="horizontal"
            aria-label={t("Resize notepad")} aria-valuemin={MIN_HEIGHT} aria-valuemax={MAX_HEIGHT} aria-valuenow={height}
            tabIndex={0} onPointerDown={event => startResize(event, "bottom")} onKeyDown={event => {
              if (event.target !== event.currentTarget || !["ArrowUp", "ArrowDown"].includes(event.key)) return;
              event.preventDefault();
              const panel = panelRef.current;
              if (!panel) return;
              const nextHeight = resizeLimits(panel).height(panel.getBoundingClientRect().height + (event.key === "ArrowDown" ? 20 : -20));
              setHeight(nextHeight);
              saveWindowState({ height: clampHeight(nextHeight) });
            }} />
          <div className={`${styles.resizeEdge} ${styles.resizeRight}`} role="separator" aria-orientation="vertical"
            aria-label={t("Resize notepad width")} aria-valuemin={MIN_WIDTH} aria-valuemax={MAX_WIDTH} aria-valuenow={Math.round(width)}
            tabIndex={0} onPointerDown={event => startResize(event, "right")} onKeyDown={event => {
              const panel = panelRef.current;
              if (!panel || !["ArrowLeft", "ArrowRight"].includes(event.key)) return;
              event.preventDefault();
              setWidth(resizeLimits(panel).width(panel.getBoundingClientRect().width + (event.key === "ArrowRight" ? 20 : -20)));
            }} />
          <div className={`${styles.resizeEdge} ${styles.resizeLeft}`} aria-hidden="true"
            onPointerDown={event => startResize(event, "left")} />
          <div className={`${styles.resizeEdge} ${styles.resizeCorner}`} aria-hidden="true"
            onPointerDown={event => startResize(event, "corner")} />
        </>}
      </aside>

      <ConfirmDialog open={deleteNoteId !== null && isExpanded} title={t("Delete note?")}
        message={t("This note will be permanently deleted.")}
        confirmText={deleteMutation.isPending ? t("Deleting...") : t("Delete")}
        confirmDisabled={sessionBlocked || deleteMutation.isPending || autosave.isSaving}
        cancelDisabled={deleteMutation.isPending} onCancel={() => setDeleteNoteId(null)}
        onConfirm={() => {
          if (deleteNoteId === null || autosave.isSaving || sessionBlocked) return;
          deleteMutation.mutate({ noteId: deleteNoteId, token: publicSession.token }, {
            onSuccess: () => { setDeleteNoteId(null); setActionError(null); goToList(); },
            onError: error => { setDeleteNoteId(null); setActionError(getErrorMessage(error, t("Could not delete this note."))); },
          });
        }} />
      <ConfirmDialog open={pendingNavigation !== null && isExpanded} title={t("Discard unsaved changes?")}
        message={isPhone ? t("You have unsaved changes on this page. If you leave now, those changes will be lost.")
          : t("The note could not be saved. Keep editing to preserve your draft.")}
        confirmText={t("Discard changes")} cancelText={t("Keep editing")}
        footerSlot={isPhone ? <IosButton label={autosave.isSaving ? t("Saving...") : t("Save changes")}
          disabled={autosave.isSaving || metadataMutation.isPending || (Boolean(dirty || access) && (!canSaveDraft || saveError?.kind === "conflict"))}
          onClick={() => void saveAndNavigate()} /> : undefined}
        confirmDisabled={isPhone && autosave.isSaving}
        cancelDisabled={isPhone && autosave.isSaving}
        onCancel={cancelNavigation}
        onConfirm={() => {
          const next = pendingNavigation;
          setPendingNavigation(null);
          if (isPhone) unmountDraft.current = null;
          resetEditor(draft ? createDraft(note?.canManage ? note : draft.saved) : null);
          next?.proceed();
        }} />
    </>
  );

  return isPhone ? (
    <dialog ref={phoneDialogRef} className={styles.phoneModal} data-manager-notepad-modal=""
      aria-label={t("Manager notepad")}
      onCancel={event => {
        event.preventDefault();
        if (focusEditing) endFocusEditing();
        else if (!phoneConfirmationOpen) void navigate(() => closePanel(true));
      }}>
      {content}
    </dialog>
  ) : content;
}
