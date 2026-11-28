import { useManagerNoteShareTargetsQuery } from "@entities/manager-notepad";
import { getErrorMessage } from "@shared/api/httpClient";
import { t } from "@shared/i18n";
import styles from "./ManagerNotepad.module.css";
import views from "./NotepadViews.module.css";

function toggleId(ids: number[], id: number) {
  return ids.includes(id) ? ids.filter(item => item !== id) : [...ids, id];
}

export function NotepadShareRecipients({ recipients, busy, onChange }: {
  recipients: number[];
  busy: boolean;
  onChange: (ids: number[]) => void;
}) {
  const shareTargets = useManagerNoteShareTargetsQuery(true);
  return (
    <fieldset disabled={busy}>
      <legend>{t("Share with managers")}</legend>
      {shareTargets.isLoading && <p role="status">{t("Loading managers...")}</p>}
      {shareTargets.error && <div className={styles.errorMessage} role="alert">
        {getErrorMessage(shareTargets.error, t("Could not load managers."))}
        <button type="button" onClick={() => void shareTargets.refresh()}>{t("Retry")}</button>
      </div>}
      {!shareTargets.isLoading && !shareTargets.error && !shareTargets.data?.length && <p>{t("No managers available.")}</p>}
      <div className={views.checkList}>
        {shareTargets.data?.map(manager => (
          <label key={manager.id}><input type="checkbox" checked={recipients.includes(manager.id)}
            disabled={recipients.length >= 50 && !recipients.includes(manager.id)}
            onChange={() => onChange(toggleId(recipients, manager.id))} />{manager.displayName}</label>
        ))}
      </div>
    </fieldset>
  );
}

