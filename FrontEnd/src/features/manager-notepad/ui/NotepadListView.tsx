import { useState } from "react";
import {
  type ManagerNote,
} from "@entities/manager-notepad";
import { getErrorMessage } from "@shared/api/httpClient";
import { getLocale, t } from "@shared/i18n";
import { CheckIcon } from "@shared/ui/icons";
import { ActionMenu } from "@shared/ui/components/ActionMenu";
import { NotepadIcon } from "./NotepadIcon";
import { NotepadNoteCard } from "./NotepadNoteCard";
import { richTextToPlainText } from "./notepadRichText";
import styles from "./ManagerNotepad.module.css";
import views from "./NotepadViews.module.css";

const sortOptions = [
  { id: "updated", label: "Last updated — newest first", icon: "clock" },
  { id: "updated-oldest", label: "Last updated — oldest first", icon: "clock" },
  { id: "created", label: "Date created — newest first", icon: "clock" },
  { id: "created-oldest", label: "Date created — oldest first", icon: "clock" },
  { id: "title", label: "Title — A to Z", icon: "sort" },
  { id: "title-desc", label: "Title — Z to A", icon: "sort" },
] as const;

export type NotepadListFilters = {
  search: string;
  sort: typeof sortOptions[number]["id"];
};

type Props = {
  filters: NotepadListFilters;
  onFiltersChange: (filters: NotepadListFilters) => void;
  tab: "private" | "public";
  onTabChange: (tab: "private" | "public") => void;
  notes: ManagerNote[];
  loading: boolean;
  error: unknown;
  opening: boolean;
  pinPending: boolean;
  onRetry: () => void;
  onOpen: (note: ManagerNote) => void;
  onNew?: () => void;
  onTogglePin?: (note: ManagerNote) => void;
};

export function NotepadListView({
  filters, onFiltersChange, tab, onTabChange, notes, loading, error, opening, pinPending, onRetry, onOpen, onNew, onTogglePin,
}: Props) {
  const { search, sort } = filters;
  const [sortMenu, setSortMenu] = useState<HTMLButtonElement | null>(null);
  const privateNotes = notes.filter(note => note.canManage && note.visibility === "private");
  const publicNotes = notes.filter(note => note.visibility !== "private");
  const query = search.trim().toLocaleLowerCase(getLocale());
  const filtered = (tab === "private" ? privateNotes : publicNotes).filter(note =>
    (!query || `${note.title} ${richTextToPlainText(note.content)}`.toLocaleLowerCase(getLocale()).includes(query)))
    .sort((a, b) => {
      const newest = Date.parse(b.updatedAtUtc) - Date.parse(a.updatedAtUtc) || b.id - a.id;
      switch (sort) {
        case "updated-oldest": return -newest;
        case "created": return Date.parse(b.createdAtUtc) - Date.parse(a.createdAtUtc) || b.id - a.id;
        case "created-oldest": return Date.parse(a.createdAtUtc) - Date.parse(b.createdAtUtc) || a.id - b.id;
        case "title": return a.title.localeCompare(b.title, getLocale(), { sensitivity: "base" }) || newest;
        case "title-desc": return b.title.localeCompare(a.title, getLocale(), { sensitivity: "base" }) || newest;
        default: return newest;
      }
    });
  const pinned = filtered.filter(note => note.isPinned);
  const recent = filtered.filter(note => !note.isPinned);

  const renderCard = (note: ManagerNote) => (
    <NotepadNoteCard key={note.id} note={note} disabled={opening || loading} pinPending={pinPending}
      onOpen={() => onOpen(note)} onTogglePin={onTogglePin && (() => onTogglePin(note))} />
  );

  return (
    <section className={styles.listView} aria-label={t("Notes")}>
      <div className={views.filters}>
        <div className={`${views.segments} ${views.noteTabs}`} role="group" aria-label={t("Note sections")}>
          <button type="button" aria-pressed={tab === "private"} title={t("Private")} onClick={() => onTabChange("private")}>
            <NotepadIcon name="lock" size={18} /> <span className={views.compactLabel}>{t("Private")}</span> <span className={views.tabCount}>{privateNotes.length}</span>
          </button>
          <button type="button" aria-pressed={tab === "public"} title={t("Public")} onClick={() => onTabChange("public")}>
            <NotepadIcon name="users" size={18} /> <span className={views.compactLabel}>{t("Public")}</span> <span className={views.tabCount}>{publicNotes.length}</span>
          </button>
        </div>
        <div className={styles.listToolbar}>
          <label className={styles.searchField}>
            <NotepadIcon name="search" size={18} />
            <input value={search} placeholder={t("Search notes")} aria-label={t("Search notes")} onChange={event => onFiltersChange({ ...filters, search: event.target.value })} />
            {search && <button type="button" aria-label={t("Clear note search")} onClick={() => onFiltersChange({ ...filters, search: "" })}><NotepadIcon name="close" size={13} /></button>}
          </label>
          <button type="button" className={views.filterButton} aria-label={t("Sort notes")}
            title={t("Sort notes")} aria-haspopup="menu" aria-expanded={Boolean(sortMenu)}
            data-active={sort !== "updated" || undefined}
            onClick={event => setSortMenu(sortMenu ? null : event.currentTarget)}>
            <NotepadIcon name="filter" size={18} />
          </button>
          <ActionMenu open={Boolean(sortMenu)} anchor={sortMenu} label={t("Sort notes")}
            heading={{ title: t("Sort notes") }} width={260} alignment="start"
            onClose={() => setSortMenu(null)} items={sortOptions.map(option => ({
              id: option.id,
              label: t(option.label),
              icon: <span data-notepad-menu-icon data-notepad-sort><NotepadIcon name={option.icon} size={15} /></span>,
              trailingIcon: option.id === sort ? <span className={views.sortCheck}><CheckIcon size={14} /></span> : undefined,
              accent: option.id === sort,
              onSelect: () => onFiltersChange({ ...filters, sort: option.id }),
            }))} />
          {onNew && <button type="button" className={`${styles.newButton} ${styles.listNewButton}`} disabled={loading} onClick={onNew}>
            <NotepadIcon name="plus" size={18} />{t("New")}
          </button>}
        </div>

      </div>
      <div className={styles.noteList} aria-busy={loading || opening}>
        {loading && <p className={styles.emptyState} role="status">{t("Loading notes...")}</p>}
        {Boolean(error) && (
          <div className={styles.errorMessage} role="alert">
            {getErrorMessage(error, t("Could not load notes."))}
            <button type="button" onClick={onRetry}>{t("Retry")}</button>
          </div>
        )}
        {pinned.length > 0 && <h3 className={views.groupHeading}><NotepadIcon name="pin" size={17} />{t("Pinned")}</h3>}
        {pinned.map(renderCard)}
        <h3 className={`${views.groupHeading} ${views.recentHeading}`}><NotepadIcon name="layers" size={17} />{t("Recent notes")}</h3>
        {!loading && !error && !filtered.length && (
          <div className={styles.emptyState}>
            <span className={styles.emptyIcon}>{query ? <NotepadIcon name="search" size={18} /> : <NotepadIcon name="note" size={18} />}</span>
            <strong>{query ? t("No matching notes")
              : tab === "private" ? t("No private notes yet.") : t("No public notes yet.")}</strong>
            {query && <span>{t("Try another search.")}</span>}
          </div>
        )}
        {recent.map(renderCard)}
      </div>
    </section>
  );
}
