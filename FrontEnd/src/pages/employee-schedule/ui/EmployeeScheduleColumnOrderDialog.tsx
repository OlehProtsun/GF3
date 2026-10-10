import { t } from "@shared/i18n";
import { useEffect, useId, useState, type MouseEvent } from "react";
import type { GraphMatrixColumn } from "@entities/containers/model/graphWorkspace";
import { IosButton } from "@shared/ui/components/IosButton";
import {
  CheckIcon,
  ChevronLeftIcon,
  ChevronRightIcon,
  CloseIcon,
  EmployeeIcon,
} from "@shared/ui/icons";
import styles from "./EmployeeScheduleColumnOrderDialog.module.css";

type EmployeeScheduleColumnOrderDialogProps = {
  open: boolean;
  columns: GraphMatrixColumn[];
  defaultColumns: GraphMatrixColumn[];
  activeEmployeeId: number | null;
  onCancel: () => void;
  onSave: (columnOrder: number[]) => void;
};

function columnIds(columns: GraphMatrixColumn[]) {
  return columns.map(column => column.employeeId);
}

function arraysEqual(left: number[], right: number[]) {
  return left.length === right.length && left.every((value, index) => value === right[index]);
}

export function EmployeeScheduleColumnOrderDialog({
  open,
  columns,
  defaultColumns,
  activeEmployeeId,
  onCancel,
  onSave,
}: EmployeeScheduleColumnOrderDialogProps) {
  const titleId = useId();
  const descriptionId = useId();
  const [draftColumns, setDraftColumns] = useState(columns);

  useEffect(() => {
    if (open) {
      setDraftColumns(columns);
    }
  }, [columns, open]);

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

  const moveColumn = (employeeId: number, offset: -1 | 1) => {
    setDraftColumns(current => {
      const sourceIndex = current.findIndex(column => column.employeeId === employeeId);
      const targetIndex = sourceIndex + offset;
      if (sourceIndex < 0 || targetIndex < 0 || targetIndex >= current.length) {
        return current;
      }

      const next = [...current];
      [next[sourceIndex], next[targetIndex]] = [next[targetIndex], next[sourceIndex]];
      return next;
    });
  };

  const currentOrder = columnIds(columns);
  const draftOrder = columnIds(draftColumns);
  const defaultOrder = columnIds(defaultColumns);
  const hasChanges = !arraysEqual(currentOrder, draftOrder);
  const isDefaultOrder = arraysEqual(defaultOrder, draftOrder);

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
            <span className={styles.eyebrow}>
              <EmployeeIcon size={16} />
              {t("Column order")}</span>
            <h2 id={titleId}>{t("Customize schedule")}</h2>
            <p id={descriptionId}>{t("Move employees left or right to arrange the schedule for yourself.")}</p>
          </div>

          <button type="button" className={styles.closeButton} aria-label={t("Close column order dialog")} onClick={onCancel}>
            <CloseIcon size={18} />
          </button>
        </header>

        <div className={styles.list} role="list" aria-label={t("Schedule columns")}>
          {draftColumns.map((column, index) => (
            <div
              key={column.employeeId}
              className={[styles.row, column.employeeId === activeEmployeeId ? styles.activeRow : ""].filter(Boolean).join(" ")}
              role="listitem"
            >
              <span className={styles.position}>{index + 1}</span>
              <span className={styles.employeeName}>{column.label}</span>
              <span className={styles.controls}>
                <button
                  type="button"
                  aria-label={t("Move ") + column.label + t(" left")}
                  disabled={index === 0}
                  onClick={() => moveColumn(column.employeeId, -1)}
                >
                  <ChevronLeftIcon size={17} />
                </button>
                <button
                  type="button"
                  aria-label={t("Move ") + column.label + t(" right")}
                  disabled={index === draftColumns.length - 1}
                  onClick={() => moveColumn(column.employeeId, 1)}
                >
                  <ChevronRightIcon size={17} />
                </button>
              </span>
            </div>
          ))}
        </div>

        <footer className={styles.footer}>
          <button
            type="button"
            className={styles.resetButton}
            disabled={isDefaultOrder}
            onClick={() => setDraftColumns(defaultColumns)}
          >
            {t("Reset default")}</button>
          <span className={styles.footerActions}>
            <IosButton label={t("Cancel")} variant="secondary" size="compact" onClick={onCancel} />
            <IosButton
              label={t("Apply")}
              size="compact"
              icon={<CheckIcon size={15} />}
              disabled={!hasChanges}
              onClick={() => onSave(draftOrder)}
            />
          </span>
        </footer>
      </div>
    </div>
  );
}
