import { dateTimeFormat, t } from "@shared/i18n";
import type { RegulationAcceptance } from "@entities/regulations";
import { regulationsApi } from "@entities/regulations";
import { AccountSettingsIcon } from "@shared/ui/icons";
import styles from "./RegulationHistoryCard.module.css";

const dateFormatter = dateTimeFormat(undefined, { dateStyle: "medium", timeStyle: "short" });

export function RegulationHistoryCard({
  acceptances,
  isLoading,
  title = t("Regulation history"),
  className,
  appearance = "default",
}: {
  acceptances: RegulationAcceptance[];
  isLoading: boolean;
  title?: string;
  className?: string;
  appearance?: "default" | "profile" | "manager-profile";
}) {
  const download = async (acceptance: RegulationAcceptance) => {
    const file = await regulationsApi.downloadPdf(acceptance.regulationDocumentId);
    const url = URL.createObjectURL(file.blob);
    window.open(url, "_blank", "noopener,noreferrer");
    window.setTimeout(() => URL.revokeObjectURL(url), 60_000);
  };

  return (
    <section className={[styles.card, appearance !== "default" ? styles.profile : "", appearance === "manager-profile" ? styles.managerProfile : "", className].filter(Boolean).join(" ")} aria-label={title}>
      <div className={styles.header}>
        {appearance !== "default" ? <span className={styles.icon}><AccountSettingsIcon name="document" /></span> : null}
        <div>
          <h2>{title}</h2>
          <p>{t("Recorded versions and acceptance dates.")}</p>
        </div>
        <span className={styles.count}>{acceptances.length}</span>
      </div>
      {isLoading ? <div className={styles.empty}>{t("Loading history...")}</div> : acceptances.length === 0 ? (
        <div className={styles.empty}>{t("No regulation has been accepted yet.")}</div>
      ) : (
        <div className={styles.list}>
          {acceptances.map(acceptance => (
            <button key={acceptance.id} type="button" className={styles.item} onClick={() => void download(acceptance)}>
              {appearance === "manager-profile" ? <span className={styles.documentIcon}><AccountSettingsIcon name="document" /></span> : null}
              <span className={styles.metadata}>
                <strong>{acceptance.regulationTitle}</strong>
                <small>{t("Version")} {acceptance.regulationVersion} · PDF {acceptance.pdfSha256.slice(0, 12)}…</small>
              </span>
              <time dateTime={acceptance.acceptedAtUtc}>{dateFormatter.format(new Date(acceptance.acceptedAtUtc))}</time>
              {appearance === "manager-profile" ? <span className={styles.downloadIcon}><AccountSettingsIcon name="download" /></span> : null}
            </button>
          ))}
        </div>
      )}
    </section>
  );
}
