import type { Ref } from "react";
import { t } from "@shared/i18n";
import { EyeIcon, NewsIcon } from "@shared/ui/icons";
import styles from "./EmployeeSchedulePage.module.css";

export function EmployeeScheduleHeaderActions({
  daily, toggleRef, onToggle, onExportPdf, pdfLoading, pdfReady,
}: {
  daily: boolean;
  toggleRef: Ref<HTMLButtonElement>;
  onToggle: () => void;
  onExportPdf: () => void;
  pdfLoading: boolean;
  pdfReady: boolean;
}) {
  const viewLabel = t(daily ? "Show schedule matrix view" : "Show daily schedule view");
  const pdfLabel = t(pdfLoading ? "Preparing PDF..." : pdfReady ? "Share PDF" : "Export schedule to PDF");

  return <div className={styles.openScheduleActions}>
    <button
      ref={toggleRef}
      type="button"
      className={daily ? `${styles.scheduleViewToggle} ${styles.scheduleViewToggleActive}` : styles.scheduleViewToggle}
      aria-label={viewLabel}
      aria-pressed={daily}
      title={viewLabel}
      onClick={onToggle}
    ><EyeIcon size={20} /></button>
    <button type="button" className={styles.openSchedulePdfButton} onClick={onExportPdf}
      disabled={pdfLoading} aria-busy={pdfLoading} title={pdfLabel} aria-label={pdfLabel}>
      <NewsIcon size={14} />PDF
    </button>
  </div>;
}
