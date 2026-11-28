import { useEffect, useLayoutEffect, useRef } from "react";
import { createPortal } from "react-dom";
import type { ManagerNote } from "@entities/manager-notepad";
import { getLocale, t } from "@shared/i18n";
import { useDropdownPosition } from "@shared/lib/useDropdownPosition";
import { ShareIcon, TrashIcon } from "@shared/ui/icons";
import { NotepadIcon } from "./NotepadIcon";
import { NotepadVisibilityOptions } from "./NotepadVisibilityControl";
import { exportNote } from "./notepadExport";
import type { NoteAccess } from "./useNotepadAutosave";
import styles from "./NotepadViews.module.css";

function SettingsText({ title, subtitle }: { title: string; subtitle: string }) {
  return <span className={styles.settingsText}><strong>{title}</strong><small>{subtitle}</small></span>;
}

export function NotepadSettingsMenu({ anchor, note, access, busy, workspaceReadOnly, onClose, onAccessChange, onLockChange, onVersions, onDelete }: {
  anchor: HTMLButtonElement;
  note: ManagerNote;
  access: NoteAccess;
  busy: boolean;
  workspaceReadOnly?: boolean;
  onClose: () => void;
  onAccessChange: (access: NoteAccess) => void;
  onLockChange: (locked: boolean) => void;
  onVersions: () => void;
  onDelete: () => void;
}) {
  // Align to the action bar so a wide menu stays inside the editor when there is room.
  const triggerElement = anchor.closest("header") ?? anchor;
  const trigger = useRef(triggerElement);
  const menu = useRef<HTMLDivElement>(null);
  const position = useDropdownPosition(true, trigger, menu, "down", Math.min(300, triggerElement.clientWidth), "end");
  useLayoutEffect(() => { menu.current?.focus({ preventScroll: true }); }, []);
  useEffect(() => {
    const dismiss = (event: PointerEvent) => {
      if (event.target instanceof Node && !anchor.contains(event.target) && !menu.current?.contains(event.target)) onClose();
    };
    document.addEventListener("pointerdown", dismiss);
    return () => document.removeEventListener("pointerdown", dismiss);
  }, [anchor, onClose]);

  const act = (action: () => void) => {
    onClose();
    anchor.focus();
    action();
  };

  return createPortal(
    <div ref={menu} className={`${styles.visibilityPopover} ${styles.settingsPopover}`} style={position} role="dialog" tabIndex={-1}
      aria-label={t("Note actions")} onKeyDown={event => {
        if (event.key !== "Escape") return;
        event.preventDefault();
        event.stopPropagation();
        onClose();
        anchor.focus();
      }}>
      <div className={styles.noteSettingsMenu}>
        {!workspaceReadOnly && <>
          <label className={`${styles.settingsRow} ${styles.menuLock}`}>
            <NotepadIcon name="lock" size={18} />
            <SettingsText title={t("Lock editing")} subtitle={t("Prevent changes to this note")} />
            <input type="checkbox" role="switch" aria-label={t("Lock editing")} checked={note.isLocked} disabled={busy}
              onChange={event => { const checked = event.currentTarget.checked; act(() => onLockChange(checked)); }} />
          </label>
          <details className={styles.settingsGroup}>
            <summary className={styles.settingsRow}>
              <ShareIcon size={18} />
              <SettingsText title={t("Visibility")} subtitle={t("Manage who can access this note")} />
              <NotepadIcon name="chevronRight" size={15} />
            </summary>
            <div className={styles.visibilityContents}>
              <NotepadVisibilityOptions value={access} busy={busy} onChange={next => {
                if (next.visibility === "custom") onAccessChange(next);
                else act(() => onAccessChange(next));
              }} />
            </div>
          </details>
        </>}
        <div className={styles.settingsGroup}>
          <button className={styles.settingsRow} type="button" disabled={busy} onClick={() => act(onVersions)}>
            <NotepadIcon name="clock" size={18} />
            <SettingsText title={t("Version history")} subtitle={t(workspaceReadOnly ? "View previous versions" : "View and restore previous versions")} />
          </button>
          <button className={styles.settingsRow} type="button" onClick={() => act(() => exportNote(note, "txt"))}>
            <NotepadIcon name="download" size={18} />
            <SettingsText title={t("Export text (.txt)")} subtitle={t("Download as plain text")} />
          </button>
          <button className={styles.settingsRow} type="button" onClick={() => act(() => exportNote(note, "md"))}>
            <NotepadIcon name="note" size={18} />
            <SettingsText title={t("Export Markdown (.md)")} subtitle={t("Download as Markdown")} />
          </button>
        </div>
        <details className={styles.settingsGroup}>
          <summary className={styles.settingsRow}>
            <NotepadIcon name="info" size={18} />
            <SettingsText title={t("Details")} subtitle={t("View note information")} />
            <NotepadIcon name="chevronRight" size={15} />
          </summary>
          <dl className={styles.properties}>
            <dt>{t("Owner")}</dt><dd>{t("You")}</dd>
            <dt>{t("Created")}</dt><dd><time dateTime={note.createdAtUtc}>{new Date(note.createdAtUtc).toLocaleString(getLocale())}</time></dd>
            <dt>{t("Last updated")}</dt><dd><time dateTime={note.updatedAtUtc}>{new Date(note.updatedAtUtc).toLocaleString(getLocale())}</time></dd>
            <dt>{t("Revision")}</dt><dd>{note.revision}</dd>
          </dl>
        </details>
        {!workspaceReadOnly && <button type="button" className={`${styles.settingsRow} ${styles.dangerAction}`} disabled={busy} onClick={() => act(onDelete)}>
          <TrashIcon size={18} />
          <SettingsText title={t("Delete note")} subtitle={t("Permanently delete this note")} />
        </button>}
      </div>
    </div>, anchor.closest("[data-manager-notepad-modal]") ?? document.body,
  );
}
