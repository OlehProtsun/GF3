import { dateTimeFormat, t } from "@shared/i18n";
import type { CommunicationDraftBlock } from "../api/dto";
import { CommunicationGif, CommunicationPhoto } from "./CommunicationGif";
import { CloseIcon, NoteIcon } from "@shared/ui/icons";
import styles from "./EmployeeCommunicationDialog.module.css";

const deadlineFormatter = dateTimeFormat(undefined, {
  month: "short",
  day: "2-digit",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
});

function formatDeadline(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return t("until the deadline");
  }

  return t("until {0}", deadlineFormatter.format(date));
}

export function CommunicationMessageContent({ message, blocks, onClose, busy = false, index = 0, count = 1 }: { message: {id?: number; title: string; deadlineAtUtc: string; createdByManagerName: string}; blocks: readonly CommunicationDraftBlock[]; onClose: () => void; busy?: boolean; index?: number; count?: number }) {
  return <>
          <div className={styles.header}>
            <span className={styles.icon} aria-hidden="true">
              <NoteIcon size={18} />
            </span>
            <div>
              <span className={styles.eyebrow}>{t("Communication")}</span>
              <h2 id="employee-communication-title">{message.title}</h2>
            </div>
            <button
              type="button"
              className={styles.iconButton}
              onClick={onClose}
              aria-label={t("Close communication")}
              disabled={busy}
            >
              <CloseIcon size={16} />
            </button>
          </div>

          {blocks.map(block => {
            if (block.type === "text") {
              return block.text?.trim() ? <p key={block.id} className={styles.body}>{block.text.trim()}</p> : null;
            }
            const media = { id: message.id, attachmentKey: block.media?.key, file: block.file };
            return block.type === "photo"
              ? <CommunicationPhoto key={block.id} {...media} />
              : <CommunicationGif key={block.id} {...media} employee />;
          })}

          <div className={styles.metaLine}>
            <span>{formatDeadline(message.deadlineAtUtc)}</span>
            <span>{t("From")} {message.createdByManagerName}</span>
            {count > 1 ? (
              <span>{index + 1}  {t("of")} {count}</span>
            ) : null}
          </div>
  </>;
}

export function CommunicationMessageFooter({ doNotShowAgain, onChange, onClose, busy = false }: { doNotShowAgain: boolean; onChange: (value: boolean) => void; onClose: () => void; busy?: boolean }) {
  return (
        <div className={styles.footer}>
          <label className={styles.checkboxRow}>
            <input
              type="checkbox"
              checked={doNotShowAgain}
              onChange={event => onChange(event.target.checked)}
              disabled={busy}
            />
            <span>{t("Don't show this again")}</span>
          </label>

          <button
            type="button"
            className={styles.primaryButton}
            onClick={onClose}
            disabled={busy}
          >
            {busy ? t("Saving...") : t("Close")}
          </button>
        </div>
  );
}
