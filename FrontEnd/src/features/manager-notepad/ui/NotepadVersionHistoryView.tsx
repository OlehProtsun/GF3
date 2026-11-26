import { useMemo, useState } from "react";
import { useAuth } from "@app/providers/AuthProvider";
import {
  useManagerNoteShareTargetsQuery, useManagerNoteVersionsQuery, useRestoreManagerNoteVersionMutation,
  type ManagerNote, type ManagerNoteVersion,
} from "@entities/manager-notepad";
import { ApiError, getErrorMessage } from "@shared/api/httpClient";
import { getLocale, t } from "@shared/i18n";
import { ConfirmDialog } from "@shared/ui/ConfirmDialog";
import { createIcon } from "@shared/ui/icons/createIcon";
import { NotepadIcon } from "./NotepadIcon";
import { contentToEditorHtml, richTextToPlainText } from "./notepadRichText";
import styles from "./ManagerNotepad.module.css";
import views from "./NotepadViews.module.css";

const CalendarIcon = createIcon("M8 3v4m8-4v4M4 10h16M6 5h12a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V7a2 2 0 0 1 2-2Z");
const timeZone = "Europe/Warsaw";
const calendarDay = new Intl.DateTimeFormat("en-GB", { timeZone, year: "numeric", month: "2-digit", day: "2-digit" });
const groups = ["Today", "Yesterday", "Older"] as const;

type Revision = Pick<ManagerNoteVersion, "revision" | "title" | "content" | "color"> & {
  timestamp: string;
  snapshot?: ManagerNoteVersion;
};

const changeDetails = {
  "Title changed": "The note title was edited.",
  "Text updated": "The note body was edited.",
  "Formatting changed": "The body formatting changed.",
  "Checklist updated": "Checklist items or their completion states changed.",
  "Note color changed": "The note color was changed.",
} as const;
type Change = keyof typeof changeDetails;

function describeContent(content: string) {
  const template = document.createElement("template");
  template.innerHTML = contentToEditorHtml(content);
  const checklists = Array.from(template.content.querySelectorAll('ul[data-checklist="true"]'), list =>
    Array.from(list.children).filter(item => item.tagName === "LI").map(item => [
      item.textContent, item.getAttribute("data-checked") === "true",
    ]),
  );
  template.content.querySelectorAll("[data-checklist], [data-checked]").forEach(element => {
    element.removeAttribute("data-checklist");
    element.removeAttribute("data-checked");
  });
  return {
    text: richTextToPlainText(content),
    formatting: template.innerHTML,
    checklists: JSON.stringify(checklists),
  };
}

function buildTimeline(note: ManagerNote, snapshots: ManagerNoteVersion[]) {
  const revisions: Revision[] = [
    { revision: note.revision, title: note.title, content: note.content, color: note.color, timestamp: note.updatedAtUtc },
    ...snapshots.filter(snapshot => snapshot.revision < note.revision)
      .sort((left, right) => right.revision - left.revision)
      .map(snapshot => ({ ...snapshot, snapshot, timestamp: snapshot.savedAtUtc })),
  ];
  const contentByRevision = new Map<number, ReturnType<typeof describeContent>>();
  const describe = (revision: Revision) => {
    const cached = contentByRevision.get(revision.revision);
    if (cached) return cached;
    const description = describeContent(revision.content);
    contentByRevision.set(revision.revision, description);
    return description;
  };

  return revisions.map((revision, index) => {
    const previous = revisions[index + 1];
    const changes: Change[] = [];
    // Metadata-only updates can advance the revision without leaving a snapshot.
    if (previous?.revision === revision.revision - 1) {
      if (previous.title !== revision.title) changes.push("Title changed");
      if (previous.color !== revision.color) changes.push("Note color changed");
      if (previous.content !== revision.content) {
        const before = describe(previous);
        const after = describe(revision);
        if (before.text !== after.text) changes.push("Text updated");
        else if (before.formatting !== after.formatting) changes.push("Formatting changed");
        if (before.checklists !== after.checklists) changes.push("Checklist updated");
      }
    }
    return { revision: revision.revision, timestamp: revision.timestamp, snapshot: revision.snapshot, changes };
  });
}

export function NotepadVersionHistoryView({ note, onBack, onRestored, workspaceReadOnly, sessionBlocked, sessionToken }: {
  note: ManagerNote; workspaceReadOnly?: boolean; sessionBlocked?: boolean; sessionToken?: string; onBack: () => void; onRestored: (note: ManagerNote) => void;
}) {
  const { session } = useAuth();
  const versions = useManagerNoteVersionsQuery(note.id);
  const restore = useRestoreManagerNoteVersionMutation();
  const [baseRevision] = useState(note.revision);
  const [selected, setSelected] = useState<ManagerNoteVersion | null>(null);
  const [error, setError] = useState<string | null>(null);
  const needsAuthorNames = versions.data?.some(version =>
    version.savedByManagerId > 0 && version.savedByManagerId !== session?.managerId) ?? false;
  const authorNames = useManagerNoteShareTargetsQuery(needsAuthorNames);
  const timeline = useMemo(() => buildTimeline(note, versions.data ?? []), [note, versions.data]);
  const now = new Date();
  const today = calendarDay.format(now);
  const [day, month, year] = today.split("/").map(Number);
  const yesterday = calendarDay.format(Date.UTC(year, month - 1, day - 1, 12));
  const dated = timeline.map(item => {
    const date = new Date(item.timestamp);
    const day = calendarDay.format(date);
    const group = day === today ? "Today" : day === yesterday ? "Yesterday" : "Older";
    return { ...item, date, group };
  });
  const dateFormat = new Intl.DateTimeFormat(getLocale(), { timeZone, dateStyle: "medium" });
  const timeFormat = new Intl.DateTimeFormat(getLocale(), { timeZone, timeStyle: "medium" });
  const plural = new Intl.PluralRules(getLocale());
  const countFormat = new Intl.NumberFormat(getLocale());

  return (
    <section className={`${views.subview} ${views.timelineView}`} aria-label={t("Version history")}>
      <header className={`${views.viewHeader} ${views.timelineHeader}`}>
        <button type="button" className={views.timelineBack} onClick={onBack}><NotepadIcon name="back" size={16} />{t("Note")}</button>
        <h2><NotepadIcon name="clock" size={18} /><span className={views.compactLabel} title={t("Version history")}>{t("Version history")}</span></h2>
      </header>
      <div className={views.timelineScroll}>
        {!workspaceReadOnly && note.isLocked && <p>{t("Unlock editing before restoring a version.")}</p>}
        {versions.isLoading && <p role="status" className={styles.emptyState}>{t("Loading versions...")}</p>}
        {versions.error && <div className={styles.errorMessage} role="alert">
          {getErrorMessage(versions.error, t("Could not load versions."))}
          <button type="button" onClick={() => void versions.refresh()}>{t("Retry")}</button>
        </div>}
        {!versions.isLoading && !versions.error && !versions.data?.length && <p className={views.timelineHint}>{t("No previous versions yet.")}</p>}
        {groups.map(group => {
          const entries = dated.filter(item => item.group === group);
          if (!entries.length) return null;
          return (
            <section className={views.timelineGroup} key={group}>
              <h3>{t(group)}</h3>
              <ol className={views.versionList}>
                {entries.map(({ revision, timestamp, snapshot, changes, date }) => {
                  const editorId = snapshot?.savedByManagerId;
                  const editorName = editorId && editorId === session?.managerId
                    ? t("Edited by you")
                    : authorNames.data?.find(manager => manager.id === editorId)?.displayName;
                  const author = editorName && editorId !== session?.managerId ? t("Edited by {0}", editorName) : editorName;
                  const changeCount = changes.length;
                  const category = plural.select(changeCount);
                  const countLabel = t(category === "one" ? "{0} change" : category === "few" ? "{0} changes (few)" : "{0} changes", countFormat.format(changeCount));
                  const dateLabel = dateFormat.format(date);
                  const timeLabel = timeFormat.format(date);
                  const timestampLabel = `${dateLabel} · ${timeLabel}`;
                  const archiveLabel = snapshot ? t("Archived {0}", timestampLabel) : undefined;
                  return (
                    <li key={revision} className={views.revisionCard} data-latest={!snapshot}>
                      <div className={views.revisionHeading}>
                        <div className={views.revisionName}>
                          <NotepadIcon name="note" size={16} />
                          <strong className={views.compactLabel} title={t("Revision {0}", revision)}>{t("Revision {0}", revision)}</strong>
                          {!snapshot && <span className={views.latestBadge}>{t("Latest")}</span>}
                        </div>
                        {snapshot && !workspaceReadOnly && (
                          <button type="button" className={views.revisionRestore} onClick={() => setSelected(snapshot)}
                            disabled={sessionBlocked || note.isLocked || restore.isPending} aria-label={t("Restore revision {0}", revision)}>
                            <NotepadIcon name="undo" size={15} />{t("Restore")}
                          </button>
                        )}
                      </div>
                      <time className={views.revisionMeta} dateTime={timestamp} aria-label={archiveLabel} title={archiveLabel}>
                        <span><CalendarIcon size={14} />{dateLabel}</span>
                        <span className={views.revisionTime}><NotepadIcon name="clock" size={14} />{timeLabel}</span>
                      </time>
                      {author && <div className={views.revisionAuthor}><NotepadIcon name="users" size={14} /><span>{author}</span></div>}
                      <div className={views.revisionSummary}>
                        <span>{changeCount ? countLabel : t("Version saved")}</span>
                        {changes.map(change => <span className={views.changeChip} key={change}>{t(change)}</span>)}
                      </div>
                      {changeCount > 0 && (
                        <details className={views.revisionDetails}>
                          <summary>{t("View changes")}<NotepadIcon name="chevronRight" size={14} /></summary>
                          <ul>{changes.map(change => <li key={change}>{t(changeDetails[change])}</li>)}</ul>
                        </details>
                      )}
                    </li>
                  );
                })}
              </ol>
            </section>
          );
        })}
      </div>
      <ConfirmDialog open={!workspaceReadOnly && selected !== null}
        title={selected ? t("Restore revision {0}?", selected.revision) : t("Restore this version?")}
        message={t("The current note will be kept in version history.")}
        confirmText={t("Restore")} confirmDisabled={sessionBlocked || restore.isPending || note.isLocked} cancelDisabled={restore.isPending}
        onCancel={() => setSelected(null)} onConfirm={() => {
          if (!selected || workspaceReadOnly || sessionBlocked || note.isLocked) return;
          setError(null);
          restore.mutate({ noteId: note.id, versionId: selected.id, expectedRevision: baseRevision, token: sessionToken }, {
            onSuccess: onRestored,
            onError: cause => {
              setSelected(null);
              setError(cause instanceof ApiError && cause.status === 409
                ? t("This note changed elsewhere. Reopen it to load the latest copy.")
                : getErrorMessage(cause, t("Could not restore this version.")));
            },
          });
        }} />
    </section>
  );
}
