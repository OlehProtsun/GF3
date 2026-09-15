import { dateTimeFormat } from "@shared/i18n";
import { t } from "@shared/i18n";
import type { RegulationAcceptance } from "@entities/regulations";
import { regulationsApi } from "@entities/regulations";
import styles from "./RegulationHistoryCard.module.css";

const dateFormatter = dateTimeFormat(undefined, { dateStyle: "medium", timeStyle: "short" });

export function RegulationHistoryCard({
  acceptances,
  isLoading,
  title = t("Regulation history"),
  className,
}: {
  acceptances: RegulationAcceptance[];
  isLoading: boolean;
  title?: string;
  className?: string;
}) {
  const download = async (acceptance: RegulationAcceptance) => {
    const file = await regulationsApi.downloadPdf(acceptance.regulationDocumentId);
    const url = URL.createObjectURL(file.blob);
    window.open(url, "_blank", "noopener,noreferrer");
    window.setTimeout(() => URL.revokeObjectURL(url), 60_000);
  };

  return (
    <section className={[styles.card, className].filter(Boolean).join(" ")} aria-label={title}>
      <div className={styles.header}>
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
              <span>
                <strong>{acceptance.regulationTitle}</strong>
                <small>{t("Version")} {acceptance.regulationVersion} · PDF {acceptance.pdfSha256.slice(0, 12)}…</small>
              </span>
              <time dateTime={acceptance.acceptedAtUtc}>{dateFormatter.format(new Date(acceptance.acceptedAtUtc))}</time>
            </button>
          ))}
        </div>
      )}
    </section>
  );
}
