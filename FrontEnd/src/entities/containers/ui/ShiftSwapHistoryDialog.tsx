import { useEffect, useId, useMemo, type MouseEvent } from "react";
import type { ShiftSwap, ShiftSwapScheduleSnapshot } from "@entities/shift-swaps";
import {
  getGraphCellKey,
  type GraphMatrixCellMap,
  type GraphMatrixColumn,
} from "@entities/containers/model/graphWorkspace";
import { CloseIcon, EyeIcon } from "@shared/ui/icons";
import { ContainerGraphMatrix } from "./ContainerGraphMatrix";
import styles from "./ShiftSwapHistoryDialog.module.css";

type ShiftSwapHistoryDialogProps = {
  open: boolean;
  swap: ShiftSwap | null;
  onCancel: () => void;
};

const NOOP = () => {};
const MATRIX_STYLE = { height: "auto" } as const;

function buildMatrix(snapshot?: ShiftSwapScheduleSnapshot | null) {
  const columns: GraphMatrixColumn[] = (snapshot?.rows ?? []).map(row => ({
    employeeId: row.employeeId,
    kind: row.kind,
    manualColumnId: row.kind === "manual" ? Math.abs(row.employeeId) : null,
    graphEmployeeId: null,
    label: row.employeeName,
    minHoursMonth: null,
    totalMinutes: 0,
    totalText: "",
  }));
  const cellMap = (snapshot?.rows ?? []).reduce<GraphMatrixCellMap>((result, row) => {
    Object.entries(row.dayValues).forEach(([day, value]) => {
      result[getGraphCellKey(row.employeeId, Number(day))] = value;
    });
    return result;
  }, {});

  return { columns, cellMap };
}

function formatSwapDate(swap: ShiftSwap) {
  return new Intl.DateTimeFormat("en-GB", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    timeZone: "UTC",
  }).format(new Date(Date.UTC(swap.year, swap.month - 1, swap.dayOfMonth)));
}

export function ShiftSwapHistoryDialog({ open, swap, onCancel }: ShiftSwapHistoryDialogProps) {
  const titleId = useId();
  const descriptionId = useId();
  const beforeMatrix = useMemo(() => buildMatrix(swap?.beforeSnapshot), [swap?.beforeSnapshot]);
  const afterMatrix = useMemo(() => buildMatrix(swap?.afterSnapshot), [swap?.afterSnapshot]);
  const selectedBeforeKeys = useMemo(
    () => beforeMatrix.columns.map(column => getGraphCellKey(column.employeeId, swap?.dayOfMonth ?? 0)),
    [beforeMatrix.columns, swap?.dayOfMonth],
  );
  const selectedAfterKeys = useMemo(
    () => afterMatrix.columns.map(column => getGraphCellKey(column.employeeId, swap?.dayOfMonth ?? 0)),
    [afterMatrix.columns, swap?.dayOfMonth],
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

  if (!open || !swap) {
    return null;
  }

  const hasComparison = beforeMatrix.columns.length > 0 && afterMatrix.columns.length > 0;
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
              <EyeIcon size={16} />
              <span>Swap comparison</span>
            </div>
            <h3 id={titleId} className={styles.title}>
              {swap.fromEmployeeName} to {swap.acceptedByEmployeeName ?? "Employee"}
            </h3>
            <p id={descriptionId} className={styles.description}>
              {formatSwapDate(swap)} | {swap.fromTime.slice(0, 5)}-{swap.toTime.slice(0, 5)} | {swap.scheduleName}
            </p>
          </div>

          <button type="button" className={styles.closeButton} aria-label="Close swap comparison" onClick={onCancel}>
            <CloseIcon size={18} />
          </button>
        </header>

        {hasComparison ? (
          <div className={styles.matrixGrid}>
            <ContainerGraphMatrix
              className={styles.matrixCard}
              style={MATRIX_STYLE}
              graph={{ year: swap.year, month: swap.month }}
              columns={beforeMatrix.columns}
              cellMap={beforeMatrix.cellMap}
              title="Before"
              helperText="Schedule immediately before the swap was accepted."
              readOnly
              compactSize
              selectedCellKeys={selectedBeforeKeys}
              enableSelectionWhenReadOnly
              onSelectedCellKeysChange={NOOP}
              emptyMessage="No before-swap schedule data is available."
            />
            <ContainerGraphMatrix
              className={styles.matrixCard}
              style={MATRIX_STYLE}
              graph={{ year: swap.year, month: swap.month }}
              columns={afterMatrix.columns}
              cellMap={afterMatrix.cellMap}
              title="After"
              helperText="Schedule immediately after the swap was accepted."
              readOnly
              compactSize
              selectedCellKeys={selectedAfterKeys}
              enableSelectionWhenReadOnly
              onSelectedCellKeysChange={NOOP}
              emptyMessage="No after-swap schedule data is available."
            />
          </div>
        ) : (
          <div className={styles.emptyComparison}>
            No one has accepted this swap yet. The comparison will appear after it is accepted.
          </div>
        )}
      </div>
    </div>
  );
}
