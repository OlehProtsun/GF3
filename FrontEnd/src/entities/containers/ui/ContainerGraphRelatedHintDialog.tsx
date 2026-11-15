import { t } from "@shared/i18n";
import { useEffect, useId, useMemo, useState, type MouseEvent } from "react";
import {
  GRAPH_EMPTY_MARK,
  getGraphCellKey,
  parseGraphCellContent,
  type GraphMatrixCellMap,
  type GraphMatrixColumn,
  type GraphRelatedScheduleHintDayValue,
  type GraphRelatedScheduleHintDetail,
} from "@entities/containers/model/graphWorkspace";
import { formatHoursMinutes } from "@entities/containers/model/statistics";
import { CloseIcon, EmployeeIcon, ScheduleDetailsIcon } from "@shared/ui/icons";
import { ContainerGraphMatrix } from "./ContainerGraphMatrix";
import styles from "./ContainerGraphRelatedHintDialog.module.css";

type ContainerGraphRelatedHintDialogProps = {
  open: boolean;
  graphName: string;
  employeeName: string;
  year: number;
  month: number;
  dayLabel: string;
  currentCellMap: GraphMatrixCellMap;
  detail?: GraphRelatedScheduleHintDetail | null;
  onCancel: () => void;
};

const EMPTY_CELL_MAP: GraphMatrixCellMap = {};
const EMPTY_COLUMNS: GraphMatrixColumn[] = [];
const EMPTY_SELECTED_CELL_KEYS: string[] = [];
const MATRIX_PREVIEW_STYLE = { height: "auto" } as const;
const NOOP = () => {};

function parseTimeLabelMinutes(value: string) {
  const [hoursValue, minutesValue] = value.split(":").map(Number);
  if (!Number.isFinite(hoursValue) || !Number.isFinite(minutesValue)) {
    return null;
  }

  return (hoursValue * 60) + minutesValue;
}

function getDayValueSummary(dayValues: GraphRelatedScheduleHintDayValue[], dayOfMonth: number) {
  const selectedDay = dayValues.find(dayValue => dayValue.dayOfMonth === dayOfMonth);
  if (!selectedDay) {
    return t("Selected day: no shift");
  }

  return selectedDay.value.trim() === GRAPH_EMPTY_MARK
    ? t("Selected day: no shift")
    : t("Selected day: {0}", selectedDay.value);
}

function buildPreviewCellMap(employeeId: number, dayValues: GraphRelatedScheduleHintDayValue[]) {
  return dayValues.reduce<GraphMatrixCellMap>((accumulator, dayValue) => {
    accumulator[getGraphCellKey(employeeId, dayValue.dayOfMonth)] = dayValue.value;
    return accumulator;
  }, {});
}

function getDayValuesTotalMinutes(dayValues: GraphRelatedScheduleHintDayValue[]) {
  return dayValues.reduce((totalMinutes, dayValue) => {
    const parsedValue = parseGraphCellContent(dayValue.value);
    if (parsedValue.kind !== "intervals") {
      return totalMinutes;
    }

    return totalMinutes + parsedValue.value.reduce((intervalMinutes, interval) => {
      const fromMinutes = parseTimeLabelMinutes(interval.from);
      const toMinutes = parseTimeLabelMinutes(interval.to);
      if (fromMinutes === null || toMinutes === null) {
        return intervalMinutes;
      }

      const normalizedToMinutes = toMinutes <= fromMinutes ? toMinutes + (24 * 60) : toMinutes;
      return intervalMinutes + (normalizedToMinutes - fromMinutes);
    }, 0);
  }, 0);
}

function buildPreviewColumn(employeeId: number, employeeName: string, dayValues: GraphRelatedScheduleHintDayValue[]) {
  const totalMinutes = getDayValuesTotalMinutes(dayValues);

  return {
    employeeId,
    kind: "employee",
    manualColumnId: null,
    graphEmployeeId: null,
    label: employeeName,
    minHoursMonth: null,
    totalMinutes,
    totalText: totalMinutes > 0 ? formatHoursMinutes(totalMinutes) : "",
  } satisfies GraphMatrixColumn;
}

function buildCurrentGraphDayValues(
  employeeId: number,
  currentCellMap: GraphMatrixCellMap,
  daysInMonth: number,
) {
  return Array.from({ length: daysInMonth }, (_, index) => {
    const dayOfMonth = index + 1;

    return {
      dayOfMonth,
      weekdayLabel: "",
      value: currentCellMap[getGraphCellKey(employeeId, dayOfMonth)] ?? GRAPH_EMPTY_MARK,
      isWorked: false,
      isWeekend: false,
    } satisfies GraphRelatedScheduleHintDayValue;
  });
}

function getDaysInMonth(year: number, month: number) {
  return new Date(Date.UTC(year, month, 0)).getUTCDate();
}

export function ContainerGraphRelatedHintDialog({
  open,
  graphName,
  employeeName,
  year,
  month,
  dayLabel,
  currentCellMap,
  detail,
  onCancel,
}: ContainerGraphRelatedHintDialogProps) {
  const titleId = useId();
  const descriptionId = useId();
  const [preferredRelatedGraphId, setPreferredRelatedGraphId] = useState<number | null>(null);

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

  const selectedCellKeys = useMemo(() => {
    if (!detail) {
      return EMPTY_SELECTED_CELL_KEYS;
    }

    return [getGraphCellKey(detail.employeeId, detail.dayOfMonth)];
  }, [detail]);

  const currentDayValues = useMemo(() => {
    if (!detail) {
      return [];
    }

    return buildCurrentGraphDayValues(
      detail.employeeId,
      currentCellMap,
      getDaysInMonth(year, month),
    );
  }, [currentCellMap, detail, month, year]);

  const currentMatrixColumns = useMemo(() => {
    if (!detail) {
      return EMPTY_COLUMNS;
    }

    return [buildPreviewColumn(detail.employeeId, employeeName, currentDayValues)];
  }, [currentDayValues, detail, employeeName]);

  const activeRelatedGraphId = useMemo(() => {
    if (!open || !detail) {
      return null;
    }

    if (
      preferredRelatedGraphId !== null &&
      detail.relatedGraphs.some(graph => graph.graphId === preferredRelatedGraphId)
    ) {
      return preferredRelatedGraphId;
    }

    return detail.relatedGraphs[0]?.graphId ?? null;
  }, [detail, open, preferredRelatedGraphId]);

  const activeRelatedGraph = useMemo(() => {
    if (!detail) {
      return null;
    }

    return detail.relatedGraphs.find(graph => graph.graphId === activeRelatedGraphId) ?? detail.relatedGraphs[0] ?? null;
  }, [activeRelatedGraphId, detail]);

  const relatedMatrixColumns = useMemo(() => {
    if (!detail || !activeRelatedGraph) {
      return EMPTY_COLUMNS;
    }

    return [buildPreviewColumn(detail.employeeId, employeeName, activeRelatedGraph.dayValues)];
  }, [activeRelatedGraph, detail, employeeName]);

  const relatedCellMap = useMemo(() => {
    if (!detail || !activeRelatedGraph) {
      return EMPTY_CELL_MAP;
    }

    return buildPreviewCellMap(detail.employeeId, activeRelatedGraph.dayValues);
  }, [activeRelatedGraph, detail]);

  if (!open || !detail) {
    return null;
  }

  const handleOverlayMouseDown = (event: MouseEvent<HTMLDivElement>) => {
    if (event.target !== event.currentTarget) {
      return;
    }

    onCancel();
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
        <div className={styles.header}>
          <div className={styles.titleBlock}>
            <div className={styles.eyebrow}>
              <ScheduleDetailsIcon size={16} />
              <span>{t("Related schedule")}</span>
            </div>

            <h3 id={titleId} className={styles.title}>{employeeName}</h3>
            <p id={descriptionId} className={styles.description}>
              {dayLabel}
              {" | "}
              {t("Compare the current and related schedule in the same matrix style as the profile view.")}</p>
          </div>

          <div className={styles.headerActions}>
            <div className={styles.summaryChip}>
              <EmployeeIcon size={16} />
              <span>{detail.visualHint}</span>
            </div>

            <button
              type="button"
              className={styles.closeButton}
              aria-label={t("Close related schedule")}
              onClick={onCancel}
            >
              <CloseIcon size={18} />
            </button>
          </div>
        </div>

        <div className={styles.content}>
          <div className={styles.matrixGrid}>
            <div className={styles.matrixColumn}>
              <ContainerGraphMatrix
                className={styles.matrixCard}
                style={MATRIX_PREVIEW_STYLE}
                graph={{ year, month }}
                columns={currentMatrixColumns}
                cellMap={currentCellMap}
                title={t("Current")}
                helperText={`${graphName} | ${getDayValueSummary(currentDayValues, detail.dayOfMonth)}`}
                readOnly
                compactSize
                selectedCellKeys={selectedCellKeys}
                enableSelectionWhenReadOnly
                onSelectedCellKeysChange={NOOP}
                emptyMessage={t("No schedule data for this employee in the current graph.")}
                headerRightSlot={(
                  <span className={styles.graphPill}>
                    {graphName}
                  </span>
                )}
              />
            </div>

            <div className={styles.matrixColumn}>
              <ContainerGraphMatrix
                className={styles.matrixCard}
                style={MATRIX_PREVIEW_STYLE}
                graph={{ year, month }}
                columns={relatedMatrixColumns}
                cellMap={relatedCellMap}
                title={t("Related")}
                helperText={
                  activeRelatedGraph
                    ? `${activeRelatedGraph.graphName} | ${getDayValueSummary(activeRelatedGraph.dayValues, detail.dayOfMonth)}`
                    : t("No related schedule data available.")
                }
                readOnly
                compactSize
                selectedCellKeys={selectedCellKeys}
                enableSelectionWhenReadOnly
                onSelectedCellKeysChange={NOOP}
                emptyMessage={t("No related schedule data for this employee.")}
                headerRightSlot={(
                  detail.relatedGraphs.length > 1 ? (
                    <div className={styles.graphSelector} role="tablist" aria-label={t("Related schedules")}>
                      {detail.relatedGraphs.map(relatedGraph => {
                        const isActive = relatedGraph.graphId === activeRelatedGraph?.graphId;

                        return (
                          <button
                            key={relatedGraph.graphId}
                            type="button"
                            role="tab"
                            aria-selected={isActive}
                            className={`${styles.graphSelectorButton} ${isActive ? styles.graphSelectorButtonActive : ""}`}
                            onClick={() => setPreferredRelatedGraphId(relatedGraph.graphId)}
                          >
                            {relatedGraph.graphName}
                          </button>
                        );
                      })}
                    </div>
                  ) : (
                    <span className={styles.graphPill}>
                      {activeRelatedGraph?.graphName ?? t("Related")}
                    </span>
                  )
                )}
              />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
