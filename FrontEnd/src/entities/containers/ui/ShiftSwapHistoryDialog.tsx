import { t } from "@shared/i18n";
import { useEffect, useId, useLayoutEffect, useMemo, useRef, useState, type MouseEvent } from "react";
import type { ShiftSwap, ShiftSwapScheduleSnapshot } from "@entities/shift-swaps";
import {
  getGraphCellKey,
  type GraphMatrixCellMap,
  type GraphMatrixColumn,
} from "@entities/containers/model/graphWorkspace";
import { CloseIcon, EyeIcon } from "@shared/ui/icons";
import { ContainerGraphMatrix } from "./ContainerGraphMatrix";
import {
  formatHistoryDate,
  formatHistoryPeriod,
  getHistoryMonth,
  getSwapHistoryShifts,
  isCrossScheduleExchange,
} from "./shiftSwapHistoryPresentation";
import activity from "./ContainerGraphActivity.module.css";
import styles from "./ShiftSwapHistoryDialog.module.css";

type ShiftSwapHistoryDialogProps = {
  open: boolean;
  swap: ShiftSwap | null;
  onCancel: () => void;
};

const NOOP = () => {};

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

function getSelectedKeys(
  matrix: ReturnType<typeof buildMatrix>,
  assignments: { employeeId?: number | null; employeeName?: string | null; day?: number | null }[],
) {
  const keys = new Set<string>();
  for (const { employeeId, employeeName, day } of assignments) {
    if (!day) continue;
    const matches = matrix.columns.filter(column =>
      employeeId != null ? column.employeeId === employeeId : employeeName && column.label === employeeName,
    );
    if (matches.length === 1) keys.add(getGraphCellKey(matches[0].employeeId, day));
  }
  return [...keys];
}

function OpenShiftSwapHistoryDialog({ swap, onCancel }: { swap: ShiftSwap; onCancel: () => void }) {
  const titleId = useId();
  const descriptionId = useId();
  const closeRef = useRef<HTMLButtonElement>(null);
  const [side, setSide] = useState<"source" | "returned">("source");
  const shifts = getSwapHistoryShifts(swap);
  const exchange = swap.requestKind === "exchange";
  const crossSchedule = isCrossScheduleExchange(swap);
  const returnSelected = crossSchedule && side === "returned";
  const displayedSide = returnSelected ? "returned" : "source";
  const graph = getHistoryMonth(shifts[displayedSide]);
  const beforeSnapshot = returnSelected ? swap.returnBeforeSnapshot : swap.beforeSnapshot;
  const afterSnapshot = returnSelected ? swap.returnAfterSnapshot : swap.afterSnapshot;
  const before = useMemo(() => buildMatrix(beforeSnapshot), [beforeSnapshot]);
  const after = useMemo(() => buildMatrix(afterSnapshot), [afterSnapshot]);

  const giver = {
    employeeId: swap.isManagerCreated ? -(swap.manualColumnId || 1) : swap.fromEmployeeId,
    employeeName: shifts.source.employeeName,
  };
  const recipient = {
    employeeId: swap.acceptedByEmployeeId ?? (exchange ? swap.targetEmployeeId : null),
    employeeName: shifts.returned.employeeName,
  };
  const sourceBefore = { ...giver, day: shifts.source.day };
  const sourceAfter = { ...recipient, day: shifts.source.day };
  const returnBefore = { ...recipient, day: shifts.returned.day };
  const returnAfter = { ...giver, day: shifts.returned.day };
  const sameSchedule = exchange && swap.returnScheduleId === swap.scheduleId;

  const handleOverlayMouseDown = (event: MouseEvent<HTMLDivElement>) => {
    if (event.target === event.currentTarget) onCancel();
  };
  useLayoutEffect(() => {
    const trigger = document.activeElement;
    closeRef.current?.focus();
    return () => {
      if (trigger instanceof HTMLElement && trigger.isConnected) trigger.focus();
    };
  }, []);
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") onCancel();
    };
    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [onCancel]);

  return (
    <div className={styles.overlay} role="dialog" aria-modal="true" aria-labelledby={titleId}
      aria-describedby={descriptionId} onMouseDown={handleOverlayMouseDown}>
      <div className={styles.dialog} data-paired-matrices={Boolean(graph && before.columns.length && after.columns.length) || undefined}>
        <header className={styles.header}>
          <div className={styles.headerRow}>
            <div className={styles.titleBlock}>
              <div className={styles.eyebrow}>
                <EyeIcon size={16} />
                <span>{t("Swap comparison")}</span>
              </div>
              <h3 id={titleId} className={styles.title}>
                {swap.fromEmployeeName} {t("to")} {shifts.returned.employeeName || t("Employee")}
              </h3>
              {crossSchedule ? (
                <p id={descriptionId} className={`${activity.badge} ${activity.info} ${styles.exchangeBadge}`}>
                  {t("Exchange between different schedules")}
                </p>
              ) : (
                <p id={descriptionId} className={styles.description}>
                  {formatHistoryDate(shifts.source)} · {formatHistoryPeriod(shifts.source)} · {shifts.source.scheduleName || t("Not available")}
                </p>
              )}
              {exchange && !crossSchedule && (
                <p className={styles.description}>
                  {t("In return")} · {formatHistoryDate(shifts.returned)} · {formatHistoryPeriod(shifts.returned)}
                </p>
              )}
            </div>
            <button ref={closeRef} type="button" className={styles.closeButton} aria-label={t("Close swap comparison")} onClick={onCancel}>
              <CloseIcon size={18} />
            </button>
          </div>
          {crossSchedule && (
            <div className={styles.scheduleSwitcher} role="group" aria-label={t("Comparison schedule")}>
              {(["source", "returned"] as const).map(key => {
                const shift = shifts[key];
                return (
                  <button key={key} type="button" aria-pressed={displayedSide === key} onClick={() => setSide(key)}>
                    <span className={styles.scheduleLabel}>
                      <span className={styles.scheduleKind}>{t(key === "source" ? "Source schedule" : "Return schedule")}</span>
                      <strong className={styles.scheduleIdentifier}>{shift.scheduleName || t("Not available")}</strong>
                    </span>
                    <span className={styles.scheduleMeta}>
                      <strong>{shift.employeeName || t("Unknown employee")}</strong>
                      <span>{formatHistoryDate(shift)}</span>
                      <span className={styles.scheduleTime}>{formatHistoryPeriod(shift)}</span>
                    </span>
                  </button>
                );
              })}
            </div>
          )}
        </header>
        <div key={displayedSide} className={styles.comparisonBody} role="region" aria-label={t("Swap comparison")} tabIndex={0}>
          {graph && (before.columns.length > 0 || after.columns.length > 0) ? (
            <div className={styles.matrixGrid}>
              {[
                {
                  matrix: before,
                  title: t("Before"),
                  description: t("Schedule immediately before the swap was accepted."),
                  empty: t("No before-swap schedule data is available."),
                  assignments: returnSelected ? [returnBefore] : sameSchedule ? [sourceBefore, returnBefore] : [sourceBefore],
                },
                {
                  matrix: after,
                  title: t("After"),
                  description: t("Schedule immediately after the swap was accepted."),
                  empty: t("No after-swap schedule data is available."),
                  assignments: returnSelected ? [returnAfter] : sameSchedule ? [sourceAfter, returnAfter] : [sourceAfter],
                },
              ].map(({ matrix, title, description, empty, assignments }) => (
                <ContainerGraphMatrix key={title} className={styles.matrixCard}
                  graph={graph} columns={matrix.columns} cellMap={matrix.cellMap}
                  title={title} helperText={description} emptyMessage={empty}
                  readOnly compactSize enableSelectionWhenReadOnly
                  selectedCellKeys={getSelectedKeys(matrix, assignments)} onSelectedCellKeysChange={NOOP} />
              ))}
            </div>
          ) : (
            <div className={styles.emptyComparison}>
              {swap.status === "accepted" ? t("No comparison data is available for this schedule.")
                : t("No one has accepted this swap yet. The comparison will appear after it is accepted.")}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

export function ShiftSwapHistoryDialog({ open, swap, onCancel }: ShiftSwapHistoryDialogProps) {
  return open && swap ? <OpenShiftSwapHistoryDialog key={swap.id} swap={swap} onCancel={onCancel} /> : null;
}
