import { t } from "@shared/i18n";
import { useEffect, useId, useMemo, type MouseEvent } from "react";
import type { AvailabilityTransferSource } from "@entities/availability-groups/model/transfer";
import {
  getAvailabilityCellKey,
  getAvailabilityCodeFromKind,
  type AvailabilityMatrixCellMap,
} from "@entities/availability-groups/model/matrix";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { AvailabilityIcon, CloseIcon, EmployeeIcon } from "@shared/ui/icons";
import { AvailabilityScheduleMatrix } from "./AvailabilityScheduleMatrix";
import styles from "./AvailabilityRelatedHintDialog.module.css";

type AvailabilityRelatedHintDialogProps = {
  open: boolean;
  employeeId: number;
  employeeName: string;
  year: number;
  month: number;
  source?: AvailabilityTransferSource | null;
  highlightedDayOfMonths: number[];
  isLoading?: boolean;
  errorMessage?: string;
  onCancel: () => void;
};

export function AvailabilityRelatedHintDialog({
  open,
  employeeId,
  employeeName,
  year,
  month,
  source,
  highlightedDayOfMonths,
  isLoading = false,
  errorMessage,
  onCancel,
}: AvailabilityRelatedHintDialogProps) {
  const titleId = useId();
  const descriptionId = useId();
  const cellMap = useMemo<AvailabilityMatrixCellMap>(() => (
    (source?.days ?? []).reduce<AvailabilityMatrixCellMap>((result, day) => {
      result[getAvailabilityCellKey(employeeId, day.dayOfMonth)] = getAvailabilityCodeFromKind(day.kind, day.intervalStr);
      return result;
    }, {})
  ), [employeeId, source?.days]);
  const selectedCellKeys = useMemo(
    () => [...new Set(highlightedDayOfMonths)]
      .sort((left, right) => left - right)
      .map(dayOfMonth => getAvailabilityCellKey(employeeId, dayOfMonth)),
    [employeeId, highlightedDayOfMonths],
  );

  useEffect(() => {
    if (!open) {
      return;
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        onCancel();
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [onCancel, open]);

  if (!open) {
    return null;
  }

  const handleOverlayMouseDown = (event: MouseEvent<HTMLDivElement>) => {
    if (event.target === event.currentTarget) {
      onCancel();
    }
  };

  return (
    <div
      className={styles.overlay}
      role="dialog"
      aria-modal="true"
      aria-labelledby={titleId}
      aria-describedby={descriptionId}
      onMouseDown={handleOverlayMouseDown}
    >
      <div className={styles.dialog}>
        <header className={styles.header}>
          <div className={styles.titleBlock}>
            <div className={styles.eyebrow}>
              <AvailabilityIcon size={16} />
              <span>{t("Related availability")}</span>
            </div>
            <h3 id={titleId} className={styles.title}>{employeeName}</h3>
            <p id={descriptionId} className={styles.description}>
              {t("Blue selection marks the days referenced by the blue text in the current availability.")}</p>
          </div>

          <div className={styles.headerActions}>
            <div className={styles.summaryChip}>
              <EmployeeIcon size={16} />
              <span>{source?.groupName ?? t("Availability")}</span>
              <strong>{selectedCellKeys.length}  {t("days")}</strong>
            </div>
            <button type="button" className={styles.closeButton} aria-label={t("Close related availability")} onClick={onCancel}>
              <CloseIcon size={18} />
            </button>
          </div>
        </header>

        {errorMessage ? <ErrorBanner className={styles.error}>{errorMessage}</ErrorBanner> : null}

        <div className={styles.content}>
          {isLoading ? (
            <div className={styles.state}>{t("Loading related availability...")}</div>
          ) : source ? (
            <AvailabilityScheduleMatrix
              className={styles.matrix}
              style={{ height: "auto" }}
              year={year}
              month={month}
              columns={[{
                employeeId,
                memberId: source.memberId,
                label: employeeName,
              }]}
              cellMap={cellMap}
              readOnly
              compactSize
              selectedCellKeys={selectedCellKeys}
              title={source.groupName}
              helperText={t("Highlighted days are the values currently shown in blue in the other availability.")}
            />
          ) : (
            <div className={styles.state}>{t("This related availability is no longer available.")}</div>
          )}
        </div>
      </div>
    </div>
  );
}
