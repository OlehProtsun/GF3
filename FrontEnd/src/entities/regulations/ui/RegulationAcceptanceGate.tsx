import { t } from "@shared/i18n";
import { useEffect, useState } from "react";
import { useAcceptRegulationMutation, usePendingRegulationsQuery, regulationsApi } from "@entities/regulations";
import { getErrorMessage } from "@shared/api/httpClient";
import { IosButton } from "@shared/ui/components/IosButton";
import styles from "./RegulationAcceptanceGate.module.css";
import { useAuth } from "@app/providers/AuthProvider";

function saveBlob(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = fileName;
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  URL.revokeObjectURL(url);
}

export function RegulationAcceptanceGate() {
  const { session } = useAuth();
  const accountKey = `${session?.role ?? "unknown"}:${session?.managerId ?? session?.employeeId ?? 0}`;
  const pendingQuery = usePendingRegulationsQuery(accountKey);
  const acceptMutation = useAcceptRegulationMutation();
  const regulation = pendingQuery.data?.[0] ?? null;
  const [confirmed, setConfirmed] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isDownloading, setIsDownloading] = useState(false);

  useEffect(() => {
    setConfirmed(false);
    setError(null);
  }, [regulation?.id]);

  if (!regulation) {
    return null;
  }

  const handleDownload = async () => {
    setIsDownloading(true);
    setError(null);
    try {
      const file = await regulationsApi.downloadPdf(regulation.id);
      saveBlob(file.blob, file.fileName ?? regulation.pdfFileName);
    } catch (downloadError) {
      setError(getErrorMessage(downloadError, t("Could not download the regulation PDF.")));
    } finally {
      setIsDownloading(false);
    }
  };

  const handleAccept = () => {
    setError(null);
    acceptMutation.mutate(regulation.id, {
      onError: acceptError => setError(getErrorMessage(acceptError, t("Could not record your acceptance."))),
    });
  };

  return (
    <div className={styles.overlay} role="dialog" aria-modal="true" aria-labelledby="regulation-dialog-title">
      <section className={styles.dialog}>
        <div className={styles.badge}>{t("Required document")}</div>
        <h2 id="regulation-dialog-title">{regulation.title}</h2>
        <div className={styles.version}>{t("Version")} {regulation.version}</div>
        <p className={styles.message}>{regulation.message}</p>
        <p className={styles.message}>{t("You acknowledge this document personally, not on behalf of your employer. This is not GDPR consent or a company contract.")}</p>
        <a href="/legal/index.html">{t("Legal documents")}</a>
        <button type="button" className={styles.pdfButton} onClick={() => void handleDownload()} disabled={isDownloading}>
          {isDownloading ? t("Downloading...") : t("Download PDF · {0}", regulation.pdfFileName)}
        </button>
        <label className={styles.confirmation}>
          <input type="checkbox" checked={confirmed} onChange={event => setConfirmed(event.target.checked)} />
          <span>{t("I have read this PDF and accept regulation version")} {regulation.version}.</span>
        </label>
        {error ? <div className={styles.error} role="alert">{error}</div> : null}
        <div className={styles.actions}>
          <IosButton
            label={acceptMutation.isPending ? t("Saving acceptance...") : t("Accept and continue")}
            onClick={handleAccept}
            disabled={!confirmed || acceptMutation.isPending}
          />
        </div>
      </section>
    </div>
  );
}
