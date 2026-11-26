import { type ManagerNote } from "@entities/manager-notepad";
import { getLocale, t } from "@shared/i18n";
import { PencilIcon, ShareIcon } from "@shared/ui/icons";
import { NotepadIcon } from "./NotepadIcon";
import { notePreview, visibilityLabel } from "./notepadRichText";
import styles from "./ManagerNotepad.module.css";
import views from "./NotepadViews.module.css";

type Props = {
  note: ManagerNote;
  disabled: boolean;
  pinPending: boolean;
  onOpen: () => void;
  onTogglePin?: () => void;
};

export function NotepadNoteCard({ note, disabled, pinPending, onOpen, onTogglePin }: Props) {
  const editor = note.visibility === "public" ? note.activeEditor : null;
  const editingLabel = editor ? t("Editing by {0}", editor.displayName) : null;
  return (
    <article className={views.card} data-pinned={note.isPinned || undefined}>
      <button type="button" className={styles.noteCard} disabled={disabled} onClick={onOpen}>
        <span className={styles.cardDocument} aria-hidden="true"><NotepadIcon name="note" size={22} /></span>
        <span className={styles.cardContent}>
          <strong title={note.title}>{note.title}</strong>
          <span className={styles.cardPreview}>{notePreview(note)}</span>
          <span className={views.cardDetails}>
            <time dateTime={note.updatedAtUtc}>
              <NotepadIcon name="calendar" size={13} />{new Date(note.updatedAtUtc).toLocaleDateString(getLocale())}
            </time>
            <span className={views.badge} data-visibility={note.visibility}>
              {note.visibility === "custom" ? <ShareIcon size={12} />
                : <NotepadIcon name={note.visibility === "private" ? "lock" : "users"} size={12} />}
              {note.visibility === "custom" ? t("Custom") : visibilityLabel(note.visibility)}
            </span>
          </span>
          {editingLabel && <span className={views.cardEditing} title={editingLabel}>
            <PencilIcon size={12} /><span>{editingLabel}</span>
          </span>}
        </span>
      </button>
      {note.canManage && onTogglePin ? (
        <button type="button" className={`${styles.iconButton} ${views.cardPin}`}
          aria-label={note.isPinned ? t("Unpin note") : t("Pin note")} aria-pressed={note.isPinned}
          disabled={pinPending} onClick={onTogglePin}>
          <NotepadIcon name="pin" size={18} />
        </button>
      ) : note.isPinned && <span className={views.cardPin} aria-label={t("Pinned")}><NotepadIcon name="pin" size={18} /></span>}
    </article>
  );
}
