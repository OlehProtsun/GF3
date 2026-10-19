import { dateTimeFormat, t } from "@shared/i18n";
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
  return dateTimeFormat("en-GB", {
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
  const returnBefore = useMemo(() => buildMatrix(swap?.returnBeforeSnapshot), [swap?.returnBeforeSnapshot]);
  const returnAfter = useMemo(() => buildMatrix(swap?.returnAfterSnapshot), [swap?.returnAfterSnapshot]);
  const selectedBeforeKeys = useMemo(
    () => beforeMatrix.columns.flatMap(column => [getGraphCellKey(column.employeeId, swap?.dayOfMonth ?? 0),
      ...(swap?.requestKind === "exchange" && swap.returnScheduleId === swap.scheduleId && swap.returnDayOfMonth
        ? [getGraphCellKey(column.employeeId, swap.returnDayOfMonth)] : [])]),
    [beforeMatrix.columns, swap],
  );
  const selectedAfterKeys = useMemo(
    () => afterMatrix.columns.flatMap(column => [getGraphCellKey(column.employeeId, swap?.dayOfMonth ?? 0),
      ...(swap?.requestKind === "exchange" && swap.returnScheduleId === swap.scheduleId && swap.returnDayOfMonth
        ? [getGraphCellKey(column.employeeId, swap.returnDayOfMonth)] : [])]),
    [afterMatrix.columns, swap],
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
              <span>{t("Swap comparison")}</span>
            </div>
            <h3 id={titleId} className={styles.title}>
              {swap.fromEmployeeName}  {t("to")} {swap.acceptedByEmployeeName ?? t("Employee")}
            </h3>
            <p id={descriptionId} className={styles.description}>
              {formatSwapDate(swap)} | {swap.fromTime.slice(0, 5)}-{swap.toTime.slice(0, 5)} | {swap.scheduleName}
            </p>
            {swap.requestKind === "exchange" && <p className={styles.description}>{t("Swap shifts")} · {t("In return")}: {swap.returnScheduleName} · {swap.returnDayOfMonth}.{swap.returnMonth}.{swap.returnYear} · {swap.returnFromTime}–{swap.returnToTime}</p>}
          </div>

          <button type="button" className={styles.closeButton} aria-label={t("Close swap comparison")} onClick={onCancel}>
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
              title={t("Before")}
              helperText={t("Schedule immediately before the swap was accepted.")}
              readOnly
              compactSize
              selectedCellKeys={selectedBeforeKeys}
              enableSelectionWhenReadOnly
              onSelectedCellKeysChange={NOOP}
              emptyMessage={t("No before-swap schedule data is available.")}
            />
            <ContainerGraphMatrix
              className={styles.matrixCard}
              style={MATRIX_STYLE}
              graph={{ year: swap.year, month: swap.month }}
              columns={afterMatrix.columns}
              cellMap={afterMatrix.cellMap}
              title={t("After")}
              helperText={t("Schedule immediately after the swap was accepted.")}
              readOnly
              compactSize
              selectedCellKeys={selectedAfterKeys}
              enableSelectionWhenReadOnly
              onSelectedCellKeysChange={NOOP}
              emptyMessage={t("No after-swap schedule data is available.")}
            />
          </div>
        ) : (
          <div className={styles.emptyComparison}>
            {t("No one has accepted this swap yet. The comparison will appear after it is accepted.")}</div>
        )}
        {swap.requestKind === "exchange" && swap.returnScheduleId !== swap.scheduleId && swap.returnYear && swap.returnMonth && returnBefore.columns.length > 0 && <>
          <h3>{swap.returnScheduleName}</h3>
          <div className={styles.matrixGrid}>
            {([{ matrix: returnBefore, title: t("Before") }, { matrix: returnAfter, title: t("After") }]).map(({ matrix, title }) =>
              <ContainerGraphMatrix key={title} className={styles.matrixCard} style={MATRIX_STYLE}
                graph={{ year: swap.returnYear!, month: swap.returnMonth! }} columns={matrix.columns} cellMap={matrix.cellMap}
                title={title} readOnly compactSize enableSelectionWhenReadOnly
                selectedCellKeys={matrix.columns.map(column => getGraphCellKey(column.employeeId, swap.returnDayOfMonth ?? 0))}
                onSelectedCellKeysChange={NOOP} />)}
          </div>
        </>}
      </div>
    </div>
  );
}
