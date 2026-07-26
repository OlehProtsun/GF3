import { memo, useEffect, useMemo, useRef, useState } from "react";
import type { CSSProperties, ClipboardEvent, DragEvent, FocusEvent, KeyboardEvent, MouseEvent, ReactNode } from "react";
import {
  GRAPH_EMPTY_MARK,
  buildGraphDayShiftStaffingCounts,
  getGraphCellKey,
  getGraphDaysInMonth,
  getGraphWeekdayLabel,
  isGraphWeekend,
  type GraphMatrixCellMap,
  type GraphMatrixColumn,
  type GraphRelatedScheduleHintDetail,
  type GraphRelatedScheduleHintDetailMap,
  type GraphMatrixStyleMap,
} from "@entities/containers/model/graphWorkspace";
import {
  formatBindKeyFromKeyboardEvent,
  isBindNavigationKey,
  isCommonEditorShortcut,
} from "@entities/availability-binds";
import type { Graph } from "@entities/containers/model/types";
import {
  buildMatrixAutoColumnWidths,
  MATRIX_COLUMN_MIN_WIDTH_PX,
} from "@entities/containers/model/matrixLayout";
import { ScheduleIcon } from "@shared/ui/icons";
import { CardSection } from "@shared/ui/sections/CardSection";
import styles from "./ContainerGraphMatrix.module.css";

export type GraphMatrixSelectionMode = "replace" | "toggle" | "range";
export type ContainerGraphMatrixEditMode = "deferred" | "inline";
export type ContainerGraphMatrixEmptyCellVariant = "neutral" | "danger";

type ContainerGraphMatrixProps = {
  graph: Pick<Graph, "year" | "month"> & Partial<Pick<Graph, "shift1Time" | "shift2Time">>;
  columns: GraphMatrixColumn[];
  cellMap: GraphMatrixCellMap;
  visualHintMap?: GraphMatrixCellMap;
  mutedSuffixMap?: GraphMatrixCellMap;
  visualHintDetailMap?: GraphRelatedScheduleHintDetailMap;
  lockVisualHintCells?: boolean;
  styleMap?: GraphMatrixStyleMap;
  dayConflictMap?: Record<number, boolean>;
  title?: ReactNode;
  helperText?: string;
  readOnly?: boolean;
  highlightReadOnlyEmpty?: boolean;
  emptyMessage?: string;
  className?: string;
  style?: CSSProperties;
  icon?: ReactNode;
  compactSize?: boolean;
  neutralStyle?: boolean;
  showColumnTotals?: boolean;
  showShiftStaffingCounts?: boolean;
  allowColumnResize?: boolean;
  stretchColumns?: boolean;
  compactHeader?: boolean;
  preserveShellHeightOnCompact?: boolean;
  editMode?: ContainerGraphMatrixEditMode;
  emptyCellVariant?: ContainerGraphMatrixEmptyCellVariant;
  cellErrors?: Record<string, string>;
  toolbar?: ReactNode;
  headerCenterSlot?: ReactNode;
  headerRightSlot?: ReactNode;
  bindValueByKey?: ReadonlyMap<string, string>;
  selectedCellKeys?: string[];
  enableSelectionWhenReadOnly?: boolean;
  normalizeCellValue?: (employeeId: number, value: string) => string;
  onSelectedCellKeysChange?: (keys: string[]) => void;
  onColumnHeaderClick?: (column: GraphMatrixColumn) => void;
  onColumnMove?: (employeeId: number, targetEmployeeId: number) => void;
  onColumnLabelChange?: (columnId: number, value: string) => void;
  onCellChange?: (employeeId: number, dayOfMonth: number, value: string) => void;
  onVisualHintClick?: (detail: GraphRelatedScheduleHintDetail) => void;
  onVisualHintCellClick?: (employeeId: number, dayOfMonth: number) => void;
};

type MatrixDay = {
  dayOfMonth: number;
  label: string;
  isWeekend: boolean;
  hasConflict: boolean;
};

type MatrixSelectionTarget =
  | {
    type: "cell";
    employeeId: number;
    dayOfMonth: number;
    cellKey: string;
    selectionKey: string;
  }
  | {
    type: "day";
    dayOfMonth: number;
    selectionKey: string;
  };

type MatrixDayStyleMeta = {
  inlineStyle: CSSProperties;
};

type MatrixColumnResizeSession = {
  employeeId: number;
  startClientX: number;
  startWidth: number;
};

type MatrixEditorSelectionBehavior = "select-all" | "caret-end";

type MatrixValueCellProps = {
  cellKey: string;
  employeeId: number;
  dayOfMonth: number;
  columnLabel: string;
  value: string;
  visualHint?: string;
  mutedSuffix?: string;
  lockVisualHint?: boolean;
  error?: string;
  isEmpty: boolean;
  isSelected: boolean;
  isEditing: boolean;
  readOnly: boolean;
  isWeekend: boolean;
  editMode: ContainerGraphMatrixEditMode;
  emptyCellVariant: ContainerGraphMatrixEmptyCellVariant;
  highlightReadOnlyEmpty: boolean;
  interactionEnabled: boolean;
  selectionEnabled: boolean;
  backgroundColor?: string | null;
  textColor?: string | null;
  editorValue?: string;
  editorSelectionBehavior?: MatrixEditorSelectionBehavior;
  onFocusTargetRef?: (element: HTMLButtonElement | HTMLInputElement | null) => void;
  onEditorValueChange: (value: string) => void;
  onEditorBlur: () => void;
  onInlineValueChange?: (value: string) => void;
  onInlineValueCommit?: (value: string) => void;
  onVisualHintClick?: () => void;
};

type MatrixDayCellProps = {
  day: MatrixDay;
  shiftStaffingCounts?: number[];
  selectionEnabled: boolean;
  isSelected: boolean;
  inlineStyle: CSSProperties;
};

const EMPTY_STYLE = {} as CSSProperties;
const NOOP = () => {};
function joinClassNames(...values: Array<string | undefined | false>) {
  return values.filter(Boolean).join(" ");
}

function arraysEqual(left: string[], right: string[]) {
  return left.length === right.length && left.every((value, index) => value === right[index]);
}

function parseSelectedCellKey(cellKey: string) {
  const [employeeIdValue, dayValue] = cellKey.split(":");

  return {
    employeeId: Number(employeeIdValue),
    dayOfMonth: Number(dayValue),
  };
}

function buildRangeCellKeys(anchorKey: string, targetKey: string, employeeIds: number[]) {
  const anchor = parseSelectedCellKey(anchorKey);
  const target = parseSelectedCellKey(targetKey);
  const anchorEmployeeIndex = employeeIds.indexOf(anchor.employeeId);
  const targetEmployeeIndex = employeeIds.indexOf(target.employeeId);

  if (anchorEmployeeIndex < 0 || targetEmployeeIndex < 0) {
    return [targetKey];
  }

  const startEmployeeIndex = Math.min(anchorEmployeeIndex, targetEmployeeIndex);
  const endEmployeeIndex = Math.max(anchorEmployeeIndex, targetEmployeeIndex);
  const startDay = Math.min(anchor.dayOfMonth, target.dayOfMonth);
  const endDay = Math.max(anchor.dayOfMonth, target.dayOfMonth);
  const cellKeys: string[] = [];

  for (let dayOfMonth = startDay; dayOfMonth <= endDay; dayOfMonth += 1) {
    for (let employeeIndex = startEmployeeIndex; employeeIndex <= endEmployeeIndex; employeeIndex += 1) {
      cellKeys.push(getGraphCellKey(employeeIds[employeeIndex], dayOfMonth));
    }
  }

  return cellKeys;
}

function buildRowSelectionKeys(dayOfMonth: number, employeeIds: number[]) {
  return employeeIds.map(employeeId => getGraphCellKey(employeeId, dayOfMonth));
}

function buildDayRangeSelectionKeys(anchorKey: string, targetDayOfMonth: number, employeeIds: number[]) {
  const anchor = parseSelectedCellKey(anchorKey);
  const startDay = Math.min(anchor.dayOfMonth, targetDayOfMonth);
  const endDay = Math.max(anchor.dayOfMonth, targetDayOfMonth);
  const cellKeys: string[] = [];

  for (let dayOfMonth = startDay; dayOfMonth <= endDay; dayOfMonth += 1) {
    cellKeys.push(...buildRowSelectionKeys(dayOfMonth, employeeIds));
  }

  return cellKeys;
}

function getNextColumnCellKey(employeeId: number, dayOfMonth: number, lastDayOfMonth: number) {
  const nextDayOfMonth = dayOfMonth < lastDayOfMonth ? dayOfMonth + 1 : dayOfMonth;
  return getGraphCellKey(employeeId, nextDayOfMonth);
}

function isDirectCellInputKey(event: KeyboardEvent<HTMLTableElement>) {
  return !event.ctrlKey && !event.metaKey && !event.altKey && event.key.length === 1 && event.key !== " ";
}

function normalizePastedCellValue(value: string) {
  return value.replace(/\r\n|\r|\n/g, " ");
}

function resolveCellNavigationDirection(key: string) {
  if (key === "ArrowUp") {
    return "up" as const;
  }

  if (key === "ArrowDown") {
    return "down" as const;
  }

  if (key === "ArrowLeft") {
    return "left" as const;
  }

  if (key === "ArrowRight") {
    return "right" as const;
  }

  return null;
}

function toggleSelection(currentSelection: string[], keys: string[]) {
  const nextSelection = new Set(currentSelection);
  const shouldRemove = keys.every(key => nextSelection.has(key));

  keys.forEach(key => {
    if (shouldRemove) {
      nextSelection.delete(key);
      return;
    }

    nextSelection.add(key);
  });

  return [...nextSelection];
}

function getUniformStyleValue(values: Array<string | null | undefined>) {
  const firstDefinedValue = values.find(value => value !== undefined);
  if (firstDefinedValue === undefined || firstDefinedValue === null) {
    return null;
  }

  return values.every(value => (value ?? null) === firstDefinedValue) ? firstDefinedValue : null;
}

function getSelectionTarget(target: EventTarget | null) {
  if (!(target instanceof HTMLElement)) {
    return null;
  }

  const targetElement = target.closest<HTMLElement>("[data-matrix-target]");
  if (!targetElement) {
    return null;
  }

  const selectionType = targetElement.dataset.matrixTarget;

  if (selectionType === "cell") {
    const employeeId = Number(targetElement.dataset.employeeId);
    const dayOfMonth = Number(targetElement.dataset.dayOfMonth);
    const cellKey = targetElement.dataset.cellKey;

    if (!Number.isFinite(employeeId) || !Number.isFinite(dayOfMonth) || !cellKey) {
      return null;
    }

    return {
      type: "cell",
      employeeId,
      dayOfMonth,
      cellKey,
      selectionKey: cellKey,
    } satisfies MatrixSelectionTarget;
  }

  if (selectionType === "day") {
    const dayOfMonth = Number(targetElement.dataset.dayOfMonth);

    if (!Number.isFinite(dayOfMonth)) {
      return null;
    }

    return {
      type: "day",
      dayOfMonth,
      selectionKey: `day:${dayOfMonth}`,
    } satisfies MatrixSelectionTarget;
  }

  return null;
}

const MatrixDayCell = memo(function MatrixDayCell({
  day,
  shiftStaffingCounts,
  selectionEnabled,
  isSelected,
  inlineStyle,
}: MatrixDayCellProps) {
  const staffingLabel = shiftStaffingCounts?.join(",") ?? "";
  const staffingDescription = shiftStaffingCounts
    ?.map((count, index) => `Shift ${index + 1}: ${count} ${count === 1 ? "employee" : "employees"}`)
    .join("; ");

  return (
    <th
      className={joinClassNames(
        styles.dayCell,
        day.hasConflict && styles.dayCellConflict,
        isSelected && styles.dayCellSelected,
      )}
      style={inlineStyle}
      data-matrix-target={selectionEnabled ? "day" : undefined}
      data-day-of-month={selectionEnabled ? day.dayOfMonth : undefined}
    >
      <span
        className={joinClassNames(
          styles.dayButton,
          shiftStaffingCounts && styles.dayButtonWithStaffing,
          isSelected && styles.dayButtonSelected,
        )}
      >
        {shiftStaffingCounts ? (
          <span className={styles.shiftStaffingBadge} aria-label={staffingDescription} title={staffingDescription}>
            {staffingLabel}
          </span>
        ) : null}
        <span className={styles.dayLabel}>{day.label}</span>
      </span>
    </th>
  );
});

const MatrixValueCell = memo(function MatrixValueCell({
  cellKey,
  employeeId,
  dayOfMonth,
  columnLabel,
  value,
  visualHint,
  mutedSuffix,
  lockVisualHint,
  error,
  isEmpty,
  isSelected,
  isEditing,
  readOnly,
  isWeekend,
  editMode,
  emptyCellVariant,
  highlightReadOnlyEmpty,
  interactionEnabled,
  selectionEnabled,
  backgroundColor,
  textColor,
  editorValue,
  editorSelectionBehavior = "select-all",
  onFocusTargetRef,
  onEditorValueChange,
  onEditorBlur,
  onInlineValueChange,
  onInlineValueCommit,
  onVisualHintClick,
}: MatrixValueCellProps) {
  const visualHintClickTimeoutRef = useRef<number | null>(null);
  const inlineStyle = {
    ...(backgroundColor ? { backgroundColor } : {}),
    ...(textColor ? { color: textColor } : {}),
  } satisfies CSSProperties;
  const trimmedVisualHint = visualHint?.trim() ?? "";
  const hasVisualHint = Boolean(trimmedVisualHint);
  const isLockedVisualHint = Boolean(lockVisualHint && isEmpty && hasVisualHint);
  const renderedValue = isEmpty && hasVisualHint ? trimmedVisualHint : value;
  const trimmedMutedSuffix = mutedSuffix?.trim() ?? "";
  const renderedPrefix = trimmedMutedSuffix && renderedValue.endsWith(trimmedMutedSuffix)
    ? renderedValue.slice(0, -trimmedMutedSuffix.length).replace(/,\s*$/, "")
    : renderedValue;
  const renderedContent = trimmedMutedSuffix ? (
    <>
      {renderedPrefix ? renderedPrefix + ",\u00a0" : null}
      <span className={styles.mutedSuffix}>{trimmedMutedSuffix}</span>
    </>
  ) : renderedValue;
  const cellTitle = error ?? (hasVisualHint ? `Also works in: ${trimmedVisualHint}` : value);
  const interactiveCellTitle =
    hasVisualHint && onVisualHintClick
      ? `${cellTitle}. Click for details.`
      : cellTitle;
  const isInlineEditing = !readOnly && editMode === "inline";
  const isSelectionOnlyReadOnlyCell = readOnly && selectionEnabled;
  const isDangerEmpty = isEmpty && emptyCellVariant === "danger";
  const isInteractiveVisualHint = hasVisualHint && Boolean(onVisualHintClick);
  const clearVisualHintClickTimeout = () => {
    if (visualHintClickTimeoutRef.current !== null) {
      window.clearTimeout(visualHintClickTimeoutRef.current);
      visualHintClickTimeoutRef.current = null;
    }
  };

  useEffect(() => () => {
    clearVisualHintClickTimeout();
  }, []);

  const handleVisualHintButtonClick = (event: MouseEvent<HTMLButtonElement>) => {
    if (!onVisualHintClick) {
      return;
    }

    if (event.shiftKey || event.ctrlKey || event.metaKey || event.altKey) {
      return;
    }

    event.preventDefault();
    event.stopPropagation();
    clearVisualHintClickTimeout();

    if (event.detail > 1) {
      return;
    }

    visualHintClickTimeoutRef.current = window.setTimeout(() => {
      visualHintClickTimeoutRef.current = null;
      onVisualHintClick();
    }, 180);
  };

  const handleVisualHintButtonDoubleClick = () => {
    clearVisualHintClickTimeout();
  };

  const renderVisualHintContent = () => (
    <>
      {!isEmpty ? <span className={styles.visualHintCurrentValue}>{renderedContent},{"\u00a0"}</span> : null}
      <button
        type="button"
        ref={onFocusTargetRef}
        className={joinClassNames(
          styles.visualHintButton,
          !isEmpty && styles.visualHintValue,
        )}
        aria-label={`${columnLabel} day ${dayOfMonth}`}
        aria-invalid={Boolean(error)}
        title={interactiveCellTitle}
        onClick={handleVisualHintButtonClick}
        onDoubleClick={handleVisualHintButtonDoubleClick}
      >
        {isEmpty ? renderedContent : trimmedVisualHint}
      </button>
    </>
  );

  const handleEditorFocus = (event: FocusEvent<HTMLInputElement>) => {
    if (editorSelectionBehavior === "caret-end") {
      const { value: currentValue } = event.currentTarget;
      event.currentTarget.setSelectionRange(currentValue.length, currentValue.length);
      return;
    }

    event.currentTarget.select();
  };

  return (
    <td
      className={joinClassNames(
        styles.matrixCell,
        isEmpty && styles.emptyCell,
        isDangerEmpty && styles.emptyCellDanger,
        isDangerEmpty && isWeekend && styles.emptyCellDangerWeekend,
        readOnly && isEmpty && highlightReadOnlyEmpty && styles.readonlyEmptyCell,
        readOnly && isDangerEmpty && styles.readonlyEmptyCellDanger,
        readOnly && isDangerEmpty && isWeekend && styles.readonlyEmptyCellDangerWeekend,
        error && styles.errorCell,
        isSelected && styles.selectedCell,
        isEditing && styles.editingCell,
      )}
      style={inlineStyle}
      data-matrix-target={interactionEnabled ? "cell" : undefined}
      data-employee-id={interactionEnabled ? employeeId : undefined}
      data-day-of-month={interactionEnabled ? dayOfMonth : undefined}
      data-cell-key={interactionEnabled ? cellKey : undefined}
    >
      <div className={styles.cellSurface}>
        {readOnly && !isSelectionOnlyReadOnlyCell ? (
          isInteractiveVisualHint ? (
            <div
              className={joinClassNames(
                styles.visualHintShell,
                styles.readonlyValue,
                isEmpty && styles.cellValueEmpty,
                isDangerEmpty && styles.dangerEmptyValue,
                isEmpty && hasVisualHint && styles.visualHintValue,
              )}
              title={interactiveCellTitle}
            >
              {renderVisualHintContent()}
            </div>
          ) : (
            <span
              className={joinClassNames(
                styles.readonlyValue,
                isEmpty && styles.cellValueEmpty,
                isDangerEmpty && styles.dangerEmptyValue,
                isEmpty && hasVisualHint && styles.visualHintValue,
              )}
              title={interactiveCellTitle}
            >
              {renderedContent}
            </span>
          )
        ) : isInlineEditing && !isLockedVisualHint ? (
          <input
            ref={onFocusTargetRef}
            className={joinClassNames(
              styles.cellEditor,
              isEmpty && styles.cellValueEmpty,
              isDangerEmpty && styles.dangerEmptyValue,
            )}
            value={value}
            onChange={event => onInlineValueChange?.(event.target.value)}
            onBlur={event => onInlineValueCommit?.(event.target.value)}
            onFocus={handleEditorFocus}
            aria-label={`${columnLabel} day ${dayOfMonth}`}
            aria-invalid={Boolean(error)}
            title={interactiveCellTitle}
            data-matrix-editor="true"
          />
        ) : isEditing && !isLockedVisualHint ? (
          <input
            autoFocus
            ref={onFocusTargetRef}
            className={joinClassNames(
              styles.cellEditor,
              isEmpty && styles.cellValueEmpty,
              isDangerEmpty && styles.dangerEmptyValue,
            )}
            value={editorValue ?? value}
            onChange={event => onEditorValueChange(event.target.value)}
            onBlur={onEditorBlur}
            onFocus={handleEditorFocus}
            aria-label={`${columnLabel} day ${dayOfMonth}`}
            aria-invalid={Boolean(error)}
            title={interactiveCellTitle}
            data-matrix-editor="true"
          />
        ) : isInteractiveVisualHint ? (
          <div
            className={joinClassNames(
              styles.visualHintShell,
              isEmpty && styles.cellValueEmpty,
              isDangerEmpty && styles.dangerEmptyValue,
              isEmpty && hasVisualHint && styles.visualHintValue,
            )}
            title={interactiveCellTitle}
          >
            {renderVisualHintContent()}
          </div>
        ) : (
          <button
            type="button"
            ref={onFocusTargetRef}
            className={joinClassNames(
              styles.cellButton,
              readOnly && styles.readonlyValue,
              isEmpty && styles.cellValueEmpty,
              isDangerEmpty && styles.dangerEmptyValue,
              isEmpty && hasVisualHint && styles.visualHintValue,
            )}
            aria-label={`${columnLabel} day ${dayOfMonth}`}
            aria-invalid={Boolean(error)}
            title={interactiveCellTitle}
          >
            {renderedContent}
          </button>
        )}
      </div>
    </td>
  );
});

export function ContainerGraphMatrix({
  graph,
  columns,
  cellMap,
  visualHintMap = {},
  mutedSuffixMap = {},
  visualHintDetailMap = {},
  lockVisualHintCells = false,
  styleMap = {},
  dayConflictMap = {},
  title = "Schedule Matrix",
  helperText,
  readOnly = false,
  highlightReadOnlyEmpty = false,
  emptyMessage,
  className,
  style,
  icon = <ScheduleIcon size={18} />,
  compactSize = false,
  neutralStyle = false,
  showColumnTotals = true,
  showShiftStaffingCounts = false,
  allowColumnResize = true,
  stretchColumns = true,
  compactHeader = false,
  preserveShellHeightOnCompact = false,
  editMode = "deferred",
  emptyCellVariant = "neutral",
  cellErrors = {},
  toolbar,
  headerCenterSlot,
  headerRightSlot,
  bindValueByKey,
  selectedCellKeys = [],
  enableSelectionWhenReadOnly = false,
  normalizeCellValue,
  onSelectedCellKeysChange,
  onColumnHeaderClick,
  onColumnMove,
  onColumnLabelChange,
  onCellChange,
  onVisualHintClick,
  onVisualHintCellClick,
}: ContainerGraphMatrixProps) {
  const [editingCellKey, setEditingCellKey] = useState<string | null>(null);
  const [editingValue, setEditingValue] = useState(GRAPH_EMPTY_MARK);
  const [editingSelectionBehavior, setEditingSelectionBehavior] = useState<MatrixEditorSelectionBehavior>("select-all");
  const [draftSelectedCellKeys, setDraftSelectedCellKeys] = useState<string[] | null>(null);
  const [draggedEmployeeId, setDraggedEmployeeId] = useState<number | null>(null);
  const [dropTargetEmployeeId, setDropTargetEmployeeId] = useState<number | null>(null);
  const [columnWidthOverrides, setColumnWidthOverrides] = useState<Record<number, number>>({});
  const [resizingEmployeeId, setResizingEmployeeId] = useState<number | null>(null);
  const isPointerSelectingRef = useRef(false);
  const lastDraggedSelectionKeyRef = useRef<string | null>(null);
  const selectionAnchorKeyRef = useRef<string | null>(selectedCellKeys[0] ?? null);
  const draftSelectedCellKeysRef = useRef<string[] | null>(null);
  const dragSelectionTypeRef = useRef<"cell" | "day" | null>(null);
  const selectedCellKeysRef = useRef(selectedCellKeys);
  const onSelectedCellKeysChangeRef = useRef(onSelectedCellKeysChange);
  const cellMapRef = useRef(cellMap);
  const onCellChangeRef = useRef(onCellChange);
  const normalizeCellValueRef = useRef(normalizeCellValue);
  const cellFocusTargetRefs = useRef<Record<string, HTMLButtonElement | HTMLInputElement | null>>({});
  const editingCellKeyRef = useRef<string | null>(null);
  const editingValueRef = useRef(editingValue);
  const activeResizeSessionRef = useRef<MatrixColumnResizeSession | null>(null);

  const days = useMemo<MatrixDay[]>(
    () =>
      Array.from({ length: getGraphDaysInMonth(graph.year, graph.month) }, (_, index) => {
        const dayOfMonth = index + 1;
        return {
          dayOfMonth,
          label: `${getGraphWeekdayLabel(graph.year, graph.month, dayOfMonth)}/${String(dayOfMonth).padStart(2, "0")}`,
          isWeekend: isGraphWeekend(graph.year, graph.month, dayOfMonth),
          hasConflict: Boolean(dayConflictMap[dayOfMonth]),
        };
      }),
    [dayConflictMap, graph.month, graph.year],
  );
  const shiftStaffingCountsByDay = useMemo(
    () => showShiftStaffingCounts
      ? buildGraphDayShiftStaffingCounts({ graph, columns, cellMap })
      : {},
    [cellMap, columns, graph.month, graph.shift1Time, graph.shift2Time, graph.year, showShiftStaffingCounts],
  );
  const useCompactShell = compactSize && !preserveShellHeightOnCompact;
  const effectiveEditMode: ContainerGraphMatrixEditMode = readOnly ? "deferred" : editMode;
  const selectionEnabled = Boolean(onSelectedCellKeysChange) && (!readOnly || enableSelectionWhenReadOnly);
  const cellInteractionEnabled = !readOnly || selectionEnabled;
  const cardClassName = [
    styles.card,
    showShiftStaffingCounts ? styles.cardWithStaffing : "",
    useCompactShell ? styles.cardCompact : "",
    className ?? "",
  ].filter(Boolean).join(" ");
  const cardStyle = useMemo(
    () =>
      ({
        ...(style ?? {}),
      }) as CSSProperties,
    [style],
  );
  const hasToolbar = Boolean(toolbar);
  const isEmpty = columns.length === 0;
  const layoutClassName = joinClassNames(
    styles.layout,
    hasToolbar && styles.layoutWithToolbar,
    isEmpty && styles.layoutEmpty,
    useCompactShell && styles.layoutCompact,
  );
  const orderedEmployeeIds = useMemo(() => columns.map(column => column.employeeId), [columns]);
  const reorderableColumnIds = useMemo(
    () => new Set(columns.map(column => column.employeeId)),
    [columns],
  );
  const automaticColumnWidths = useMemo(
    () => buildMatrixAutoColumnWidths({ columns, cellMap, visualHintMap }),
    [cellMap, columns, visualHintMap],
  );
  const fixedWidthSum = useMemo(
    () => columns.reduce((totalWidth, column) => totalWidth + (columnWidthOverrides[column.employeeId] ?? 0), 0),
    [columnWidthOverrides, columns],
  );
  const automaticWidthSum = useMemo(
    () => columns.reduce((totalWidth, column) => (
      columnWidthOverrides[column.employeeId] === undefined
        ? totalWidth + (automaticColumnWidths[column.employeeId] ?? MATRIX_COLUMN_MIN_WIDTH_PX)
        : totalWidth
    ), 0),
    [automaticColumnWidths, columnWidthOverrides, columns],
  );
  const autoWidthColumnCount = useMemo(
    () => columns.reduce((count, column) => count + (columnWidthOverrides[column.employeeId] === undefined ? 1 : 0), 0),
    [columnWidthOverrides, columns],
  );
  const tableStyle = useMemo(
    () =>
      ({
        "--matrix-column-count": String(columns.length),
        "--matrix-auto-extra-width":
          stretchColumns && autoWidthColumnCount > 0
            ? `max(0px, calc((100% - var(--matrix-day-column-width) - ${fixedWidthSum}px - ${automaticWidthSum}px) / ${autoWidthColumnCount}))`
            : "0px",
        "--matrix-table-min-width": `calc(var(--matrix-day-column-width) + ${fixedWidthSum + automaticWidthSum}px)`,
      }) as CSSProperties,
    [autoWidthColumnCount, automaticWidthSum, columns.length, fixedWidthSum, stretchColumns],
  );
  const effectiveSelectedCellKeys = draftSelectedCellKeys ?? selectedCellKeys;
  const selectedCellKeySet = useMemo(() => new Set(effectiveSelectedCellKeys), [effectiveSelectedCellKeys]);
  const dayStyleMetaByDay = useMemo(() => {
    return days.reduce<Record<number, MatrixDayStyleMeta>>((accumulator, day) => {
      const explicitDayStyle = styleMap[getGraphCellKey(0, day.dayOfMonth)];
      const rowCellKeys = columns.map(column => getGraphCellKey(column.employeeId, day.dayOfMonth));
      const rowStyles = rowCellKeys.map(cellKey => styleMap[cellKey]);
      const backgroundColor =
        explicitDayStyle?.backgroundColor ?? getUniformStyleValue(rowStyles.map(styleRecord => styleRecord?.backgroundColor));
      const textColor =
        explicitDayStyle?.textColor ?? getUniformStyleValue(rowStyles.map(styleRecord => styleRecord?.textColor));

      accumulator[day.dayOfMonth] = {
        inlineStyle: {
          ...(backgroundColor ? { backgroundColor } : {}),
          ...(textColor ? { color: textColor } : {}),
        },
      };

      return accumulator;
    }, {});
  }, [columns, days, styleMap]);
  const selectedDaySet = useMemo(() => {
    const nextSet = new Set<number>();

    days.forEach(day => {
      const rowCellKeys = columns.map(column => getGraphCellKey(column.employeeId, day.dayOfMonth));
      if (rowCellKeys.length > 0 && rowCellKeys.every(cellKey => selectedCellKeySet.has(cellKey))) {
        nextSet.add(day.dayOfMonth);
      }
    });

    return nextSet;
  }, [columns, days, selectedCellKeySet]);
  const resolvedEmptyMessage =
    emptyMessage ??
    (readOnly
      ? "No employees are assigned to this schedule yet."
      : "Add employees to the schedule to start filling the matrix.");
  const canReorderColumns = !readOnly && typeof onColumnMove === "function";

  useEffect(() => {
    selectedCellKeysRef.current = selectedCellKeys;
  }, [selectedCellKeys]);

  useEffect(() => {
    onSelectedCellKeysChangeRef.current = onSelectedCellKeysChange;
  }, [onSelectedCellKeysChange]);

  useEffect(() => {
    cellMapRef.current = cellMap;
  }, [cellMap]);

  useEffect(() => {
    onCellChangeRef.current = onCellChange;
  }, [onCellChange]);

  useEffect(() => {
    normalizeCellValueRef.current = normalizeCellValue;
  }, [normalizeCellValue]);

  useEffect(() => {
    editingCellKeyRef.current = editingCellKey;
  }, [editingCellKey]);

  useEffect(() => {
    editingValueRef.current = editingValue;
  }, [editingValue]);

  useEffect(() => {
    if (selectedCellKeys.length === 0) {
      selectionAnchorKeyRef.current = null;
      return;
    }

    if (selectionAnchorKeyRef.current && selectedCellKeys.includes(selectionAnchorKeyRef.current)) {
      return;
    }

    selectionAnchorKeyRef.current = selectedCellKeys[0];
  }, [selectedCellKeys]);

  useEffect(() => {
    if (draggedEmployeeId === null && dropTargetEmployeeId === null) {
      return;
    }

    if (
      (draggedEmployeeId !== null && !reorderableColumnIds.has(draggedEmployeeId)) ||
      (dropTargetEmployeeId !== null && !reorderableColumnIds.has(dropTargetEmployeeId))
    ) {
      setDraggedEmployeeId(null);
      setDropTargetEmployeeId(null);
    }
  }, [draggedEmployeeId, dropTargetEmployeeId, reorderableColumnIds]);

  useEffect(() => {
    const activeEmployeeIdSet = new Set(columns.map(column => column.employeeId));

    setColumnWidthOverrides(currentOverrides => {
      let changed = false;
      const nextOverrides = Object.entries(currentOverrides).reduce<Record<number, number>>((accumulator, [employeeIdKey, width]) => {
        const employeeId = Number(employeeIdKey);
        if (!activeEmployeeIdSet.has(employeeId)) {
          changed = true;
          return accumulator;
        }

        accumulator[employeeId] = width;
        return accumulator;
      }, {});

      return changed ? nextOverrides : currentOverrides;
    });

    if (resizingEmployeeId !== null && !activeEmployeeIdSet.has(resizingEmployeeId)) {
      activeResizeSessionRef.current = null;
      setResizingEmployeeId(null);
    }
  }, [columns, resizingEmployeeId]);

  useEffect(() => {
    if (!allowColumnResize || resizingEmployeeId === null) {
      return;
    }

    const handleResizeMove = (event: globalThis.MouseEvent) => {
      const activeResizeSession = activeResizeSessionRef.current;
      if (!activeResizeSession) {
        return;
      }

      const nextWidth = Math.max(
        MATRIX_COLUMN_MIN_WIDTH_PX,
        Math.round(activeResizeSession.startWidth + (event.clientX - activeResizeSession.startClientX)),
      );

      setColumnWidthOverrides(currentOverrides => (
        currentOverrides[activeResizeSession.employeeId] === nextWidth
          ? currentOverrides
          : {
            ...currentOverrides,
            [activeResizeSession.employeeId]: nextWidth,
          }
      ));
    };

    const stopResize = () => {
      activeResizeSessionRef.current = null;
      setResizingEmployeeId(null);
    };

    const previousCursor = document.body.style.cursor;
    const previousUserSelect = document.body.style.userSelect;

    document.body.style.cursor = "col-resize";
    document.body.style.userSelect = "none";

    window.addEventListener("mousemove", handleResizeMove);
    window.addEventListener("mouseup", stopResize);
    window.addEventListener("blur", stopResize);

    return () => {
      document.body.style.cursor = previousCursor;
      document.body.style.userSelect = previousUserSelect;
      window.removeEventListener("mousemove", handleResizeMove);
      window.removeEventListener("mouseup", stopResize);
      window.removeEventListener("blur", stopResize);
    };
  }, [allowColumnResize, resizingEmployeeId]);

  const resolveSelectionMode = (event: { shiftKey: boolean; ctrlKey: boolean; metaKey: boolean }): GraphMatrixSelectionMode => {
    if (event.shiftKey) {
      return "range";
    }

    if (event.ctrlKey || event.metaKey) {
      return "toggle";
    }

    return "replace";
  };

  const previewSelection = (nextSelection: string[]) => {
    draftSelectedCellKeysRef.current = nextSelection;
    setDraftSelectedCellKeys(currentSelection => (
      currentSelection !== null && arraysEqual(currentSelection, nextSelection)
        ? currentSelection
        : nextSelection
    ));
  };

  const commitSelection = (nextSelection: string[], nextAnchorKey: string | null) => {
    draftSelectedCellKeysRef.current = null;
    setDraftSelectedCellKeys(null);
    selectionAnchorKeyRef.current = nextAnchorKey;

    if (!arraysEqual(selectedCellKeysRef.current, nextSelection)) {
      onSelectedCellKeysChangeRef.current?.(nextSelection);
    }
  };

  const focusCell = (cellKey: string | null) => {
    if (!cellKey) {
      return;
    }

    requestAnimationFrame(() => {
      const nextFocusTarget = cellFocusTargetRefs.current[cellKey];
      if (!nextFocusTarget) {
        return;
      }

      nextFocusTarget.focus();
      if (nextFocusTarget instanceof HTMLInputElement) {
        nextFocusTarget.select();
      }
    });
  };

  const moveFocusToCellBelow = (employeeId: number, dayOfMonth: number) => {
    const nextCellKey = getNextColumnCellKey(employeeId, dayOfMonth, days.length);
    commitSelection([nextCellKey], nextCellKey);
    focusCell(nextCellKey);
  };

  const moveFocusToCell = (employeeId: number, dayOfMonth: number) => {
    const nextCellKey = getGraphCellKey(employeeId, dayOfMonth);
    commitSelection([nextCellKey], nextCellKey);
    focusCell(nextCellKey);
  };

  const moveFocusByDirection = (
    employeeId: number,
    dayOfMonth: number,
    direction: "up" | "down" | "left" | "right",
  ) => {
    if (orderedEmployeeIds.length === 0) {
      return;
    }

    const employeeIndex = orderedEmployeeIds.indexOf(employeeId);
    if (employeeIndex < 0) {
      return;
    }

    if (direction === "up") {
      moveFocusToCell(employeeId, Math.max(1, dayOfMonth - 1));
      return;
    }

    if (direction === "down") {
      moveFocusToCell(employeeId, Math.min(days.length, dayOfMonth + 1));
      return;
    }

    if (direction === "left") {
      moveFocusToCell(orderedEmployeeIds[Math.max(0, employeeIndex - 1)], dayOfMonth);
      return;
    }

    moveFocusToCell(orderedEmployeeIds[Math.min(orderedEmployeeIds.length - 1, employeeIndex + 1)], dayOfMonth);
  };

  const startDirectCellEditing = (cellKey: string, initialValue: string) => {
    commitSelection([cellKey], cellKey);
    beginEditingCell(cellKey, {
      initialValue,
      selectionBehavior: "caret-end",
    });
  };

  const commitCellValue = (employeeId: number, dayOfMonth: number, value: string) => {
    const nextValue = normalizeCellValueRef.current?.(employeeId, value) ?? value;
    const cellKey = getGraphCellKey(employeeId, dayOfMonth);
    const currentValue = cellMapRef.current[cellKey] ?? GRAPH_EMPTY_MARK;

    if (nextValue !== currentValue) {
      onCellChangeRef.current?.(employeeId, dayOfMonth, nextValue);
    }
  };

  const clearCellValues = (cellKeys: string[]) => {
    const uniqueCellKeys = [...new Set(cellKeys)];
    uniqueCellKeys.forEach(cellKey => {
      const { employeeId, dayOfMonth } = parseSelectedCellKey(cellKey);
      commitCellValue(employeeId, dayOfMonth, "");
    });
  };

  const flushEditingCell = () => {
    const currentEditingCellKey = editingCellKeyRef.current;
    if (!currentEditingCellKey) {
      return;
    }

    const { employeeId, dayOfMonth } = parseSelectedCellKey(currentEditingCellKey);

    editingCellKeyRef.current = null;
    setEditingCellKey(null);
    setEditingSelectionBehavior("select-all");
    commitCellValue(employeeId, dayOfMonth, editingValueRef.current);
  };

  const cancelEditingCell = () => {
    editingCellKeyRef.current = null;
    setEditingCellKey(null);
    setEditingSelectionBehavior("select-all");
    setEditingValue(GRAPH_EMPTY_MARK);
  };

  const beginEditingCell = (
    cellKey: string,
    options?: {
      initialValue?: string;
      selectionBehavior?: MatrixEditorSelectionBehavior;
    },
  ) => {
    flushEditingCell();
    editingCellKeyRef.current = cellKey;
    const currentValue = cellMapRef.current[cellKey] ?? GRAPH_EMPTY_MARK;
    setEditingCellKey(cellKey);
    setEditingSelectionBehavior(options?.selectionBehavior ?? "select-all");
    setEditingValue(options?.initialValue ?? currentValue);
  };

  const applyBindShortcut = (
    event: KeyboardEvent<HTMLTableElement>,
    target: Extract<MatrixSelectionTarget, { type: "cell" }>,
    isEditorTarget: boolean,
  ) => {
    if (readOnly || !onCellChangeRef.current || !bindValueByKey || bindValueByKey.size === 0) {
      return false;
    }

    if (isBindNavigationKey(event.key) || isCommonEditorShortcut(event)) {
      return false;
    }

    const bindToken = formatBindKeyFromKeyboardEvent(event);
    if (!bindToken) {
      return false;
    }

    const bindValue = bindValueByKey.get(bindToken);
    if (bindValue === undefined) {
      return false;
    }

    event.preventDefault();

    if (isEditorTarget) {
      editingCellKeyRef.current = null;
      setEditingCellKey(null);
      setEditingValue(GRAPH_EMPTY_MARK);
    }

    commitCellValue(target.employeeId, target.dayOfMonth, bindValue);

    moveFocusToCellBelow(target.employeeId, target.dayOfMonth);
    return true;
  };

  useEffect(() => {
    const handlePointerRelease = () => {
      if (isPointerSelectingRef.current && draftSelectedCellKeysRef.current) {
        commitSelection(draftSelectedCellKeysRef.current, selectionAnchorKeyRef.current);
      }

      isPointerSelectingRef.current = false;
      lastDraggedSelectionKeyRef.current = null;
      dragSelectionTypeRef.current = null;
    };

    window.addEventListener("mouseup", handlePointerRelease);
    return () => {
      window.removeEventListener("mouseup", handlePointerRelease);
    };
  }, []);

  const startSelection = (
    target: MatrixSelectionTarget,
    mode: GraphMatrixSelectionMode,
  ) => {
    if (orderedEmployeeIds.length === 0) {
      return;
    }

    flushEditingCell();
    isPointerSelectingRef.current = true;
    dragSelectionTypeRef.current = target.type;
    lastDraggedSelectionKeyRef.current = target.selectionKey;

    if (target.type === "cell") {
      const cellKey = target.cellKey;
      const anchorKey = selectionAnchorKeyRef.current;
      let nextSelection: string[];

      if (mode === "toggle") {
        nextSelection = toggleSelection(selectedCellKeysRef.current, [cellKey]);
        selectionAnchorKeyRef.current = nextSelection.length > 0 ? cellKey : null;
      } else if (mode === "range" && anchorKey) {
        nextSelection = buildRangeCellKeys(anchorKey, cellKey, orderedEmployeeIds);
      } else {
        nextSelection = [cellKey];
        selectionAnchorKeyRef.current = cellKey;
      }

      previewSelection(nextSelection);
      return;
    }

    const rowKeys = buildRowSelectionKeys(target.dayOfMonth, orderedEmployeeIds);
    const rowAnchorKey = rowKeys[0] ?? null;
    const anchorKey = selectionAnchorKeyRef.current;
    let nextSelection: string[];

    if (mode === "toggle") {
      nextSelection = toggleSelection(selectedCellKeysRef.current, rowKeys);
      selectionAnchorKeyRef.current = nextSelection.length > 0 ? rowAnchorKey : null;
    } else if (mode === "range" && anchorKey) {
      nextSelection = buildDayRangeSelectionKeys(anchorKey, target.dayOfMonth, orderedEmployeeIds);
    } else {
      nextSelection = rowKeys;
      selectionAnchorKeyRef.current = rowAnchorKey;
    }

    previewSelection(nextSelection);
  };

  const extendSelection = (target: MatrixSelectionTarget) => {
    if (!isPointerSelectingRef.current || orderedEmployeeIds.length === 0) {
      return;
    }

    if (lastDraggedSelectionKeyRef.current === target.selectionKey) {
      return;
    }

    lastDraggedSelectionKeyRef.current = target.selectionKey;

    if (dragSelectionTypeRef.current === "cell" && target.type === "cell") {
      const anchorKey = selectionAnchorKeyRef.current ?? target.cellKey;
      previewSelection(buildRangeCellKeys(anchorKey, target.cellKey, orderedEmployeeIds));
      return;
    }

    if (dragSelectionTypeRef.current === "day" && target.type === "day") {
      const anchorKey = selectionAnchorKeyRef.current ?? getGraphCellKey(orderedEmployeeIds[0], target.dayOfMonth);
      previewSelection(buildDayRangeSelectionKeys(anchorKey, target.dayOfMonth, orderedEmployeeIds));
    }
  };

  const handleTableMouseDown = (event: MouseEvent<HTMLTableElement>) => {
    if (!selectionEnabled || event.button !== 0) {
      return;
    }

    const isInlineEditorTarget =
      event.target instanceof HTMLElement &&
      Boolean(event.target.closest("[data-matrix-editor='true']"));

    if (isInlineEditorTarget && effectiveEditMode !== "inline") {
      return;
    }

    const target = getSelectionTarget(event.target);
    if (!target) {
      return;
    }

    if (!isInlineEditorTarget) {
      event.preventDefault();
    }

    startSelection(target, resolveSelectionMode(event));

    if (!isInlineEditorTarget && target.type === "cell") {
      focusCell(target.cellKey);
    }
  };

  const handleTableMouseMove = (event: MouseEvent<HTMLTableElement>) => {
    if (!selectionEnabled || !isPointerSelectingRef.current) {
      return;
    }

    const target = getSelectionTarget(event.target);
    if (!target) {
      return;
    }

    extendSelection(target);
  };

  const handleTableDoubleClick = (event: MouseEvent<HTMLTableElement>) => {
    if (readOnly || effectiveEditMode === "inline") {
      return;
    }

    if (event.target instanceof HTMLInputElement && event.target.dataset.matrixEditor === "true") {
      return;
    }

    const target = getSelectionTarget(event.target);
    if (!target || target.type !== "cell") {
      return;
    }

    event.preventDefault();
    beginEditingCell(target.cellKey);
  };

  const handleTableKeyDown = (event: KeyboardEvent<HTMLTableElement>) => {
    if (!cellInteractionEnabled) {
      return;
    }

    const target = getSelectionTarget(event.target);
    if (!target || target.type !== "cell") {
      return;
    }

    const isEditorTarget = event.target instanceof HTMLInputElement && event.target.dataset.matrixEditor === "true";

    if (applyBindShortcut(event, target, isEditorTarget)) {
      return;
    }

    if (isEditorTarget) {
      const navigationDirection = resolveCellNavigationDirection(event.key);
      if (effectiveEditMode === "inline" && navigationDirection) {
        event.preventDefault();
        moveFocusByDirection(target.employeeId, target.dayOfMonth, navigationDirection);
        return;
      }

      if (event.key === "Escape") {
        if (effectiveEditMode === "inline") {
          return;
        }

        event.preventDefault();
        cancelEditingCell();
        return;
      }

      if (event.key === "Enter") {
        event.preventDefault();
        if (effectiveEditMode !== "inline") {
          flushEditingCell();
        }

        moveFocusToCellBelow(target.employeeId, target.dayOfMonth);
      }

      return;
    }

    const navigationDirection = resolveCellNavigationDirection(event.key);
    if (navigationDirection) {
      event.preventDefault();
      moveFocusByDirection(target.employeeId, target.dayOfMonth, navigationDirection);
      return;
    }

    if (!readOnly && (event.key === "Delete" || event.key === "Backspace")) {
      event.preventDefault();
      const cellKeysToClear =
        selectedCellKeysRef.current.length > 0 && selectedCellKeysRef.current.includes(target.cellKey)
          ? selectedCellKeysRef.current
          : [target.cellKey];
      clearCellValues(cellKeysToClear);
      return;
    }

    if (event.key === "Enter") {
      event.preventDefault();
      moveFocusToCellBelow(target.employeeId, target.dayOfMonth);
      return;
    }

    if (!readOnly && event.key === "F2") {
      event.preventDefault();
      beginEditingCell(target.cellKey);
      return;
    }

    if (!readOnly && effectiveEditMode !== "inline" && isDirectCellInputKey(event)) {
      event.preventDefault();
      startDirectCellEditing(target.cellKey, event.key);
      return;
    }

    if (event.key === " " || event.key === "Spacebar") {
      event.preventDefault();

      const selectionMode = resolveSelectionMode(event);
      const anchorKey = selectionAnchorKeyRef.current;

      if (selectionMode === "toggle") {
        const nextSelection = toggleSelection(selectedCellKeysRef.current, [target.cellKey]);
        commitSelection(nextSelection, nextSelection.length > 0 ? target.cellKey : null);
        return;
      }

      if (selectionMode === "range" && anchorKey) {
        commitSelection(buildRangeCellKeys(anchorKey, target.cellKey, orderedEmployeeIds), anchorKey);
        return;
      }

      commitSelection([target.cellKey], target.cellKey);
    }
  };

  const handleTablePaste = (event: ClipboardEvent<HTMLTableElement>) => {
    if (readOnly || effectiveEditMode === "inline") {
      return;
    }

    if (event.target instanceof HTMLInputElement && event.target.dataset.matrixEditor === "true") {
      return;
    }

    const target = getSelectionTarget(event.target);
    if (!target || target.type !== "cell") {
      return;
    }

    const pastedValue = normalizePastedCellValue(event.clipboardData.getData("text"));
    if (!pastedValue) {
      return;
    }

    event.preventDefault();
    startDirectCellEditing(target.cellKey, pastedValue);
  };

  const handleHeaderDragStart = (event: DragEvent<HTMLTableCellElement>, employeeId: number) => {
    if (!canReorderColumns || activeResizeSessionRef.current !== null || !reorderableColumnIds.has(employeeId)) {
      return;
    }

    setDraggedEmployeeId(employeeId);
    setDropTargetEmployeeId(employeeId);
    event.dataTransfer.effectAllowed = "move";
    event.dataTransfer.setData("text/plain", String(employeeId));
  };

  const handleHeaderDragEnter = (employeeId: number) => {
    if (
      !canReorderColumns ||
      draggedEmployeeId === null ||
      draggedEmployeeId === employeeId ||
      !reorderableColumnIds.has(employeeId)
    ) {
      return;
    }

    setDropTargetEmployeeId(employeeId);
  };

  const handleHeaderDragOver = (event: DragEvent<HTMLTableCellElement>, employeeId: number) => {
    if (
      !canReorderColumns ||
      draggedEmployeeId === null ||
      draggedEmployeeId === employeeId ||
      !reorderableColumnIds.has(employeeId)
    ) {
      return;
    }

    event.preventDefault();
    event.dataTransfer.dropEffect = "move";
    setDropTargetEmployeeId(employeeId);
  };

  const handleHeaderDrop = (event: DragEvent<HTMLTableCellElement>, targetEmployeeId: number) => {
    if (!canReorderColumns || draggedEmployeeId === null || !reorderableColumnIds.has(targetEmployeeId)) {
      return;
    }

    event.preventDefault();

    const sourceEmployeeId = draggedEmployeeId;
    setDraggedEmployeeId(null);
    setDropTargetEmployeeId(null);

    if (sourceEmployeeId === targetEmployeeId) {
      return;
    }

    onColumnMove?.(sourceEmployeeId, targetEmployeeId);
  };

  const handleHeaderDragEnd = () => {
    setDraggedEmployeeId(null);
    setDropTargetEmployeeId(null);
  };

  const handleColumnResizeStart = (event: MouseEvent<HTMLSpanElement>, employeeId: number) => {
    if (!allowColumnResize) {
      return;
    }

    event.preventDefault();
    event.stopPropagation();

    const currentWidth = columnWidthOverrides[employeeId];
    const fallbackWidth = Math.max(
      MATRIX_COLUMN_MIN_WIDTH_PX,
      Math.round((event.currentTarget.closest("th")?.getBoundingClientRect().width ?? MATRIX_COLUMN_MIN_WIDTH_PX)),
    );

    activeResizeSessionRef.current = {
      employeeId,
      startClientX: event.clientX,
      startWidth: currentWidth ?? fallbackWidth,
    };
    setResizingEmployeeId(employeeId);
  };

  const getColumnStyle = (employeeId: number) => {
    const manualWidth = columnWidthOverrides[employeeId];
    const automaticWidth = automaticColumnWidths[employeeId] ?? MATRIX_COLUMN_MIN_WIDTH_PX;

    return ({
      "--matrix-column-width": manualWidth !== undefined
        ? `${manualWidth}px`
        : `calc(${automaticWidth}px + var(--matrix-auto-extra-width))`,
    }) as CSSProperties;
  };

  return (
    <CardSection
      className={cardClassName}
      style={cardStyle}
      title={title}
      icon={icon}
      headerClassName={compactHeader ? styles.compactHeader : undefined}
      titleClassName={compactHeader ? styles.compactTitle : undefined}
      headerRightClassName={compactHeader ? styles.compactHeaderRight : undefined}
      headerCenterSlot={headerCenterSlot}
      headerRightSlot={headerRightSlot}
    >
      <div className={layoutClassName}>
        {helperText ? <p className={styles.helperText}>{helperText}</p> : null}

        {hasToolbar ? <div className={styles.toolbar}>{toolbar}</div> : null}

        {isEmpty ? (
          <div className={styles.emptyState}>{resolvedEmptyMessage}</div>
        ) : (
          <div className={joinClassNames(styles.tableShell, useCompactShell && styles.tableShellCompact)}>
            <div className={joinClassNames(styles.tableScroll, useCompactShell && styles.tableScrollCompact)}>
              <table
                className={joinClassNames(
                  styles.table,
                  neutralStyle && styles.tableNeutral,
                  !stretchColumns && styles.tableFixedColumns,
                )}
                style={tableStyle}
                onMouseDown={handleTableMouseDown}
                onMouseMove={handleTableMouseMove}
                onDoubleClick={handleTableDoubleClick}
                onKeyDown={handleTableKeyDown}
                onPaste={handleTablePaste}
              >
                <colgroup>
                  <col className={styles.dayColumn} />
                  {columns.map(column => (
                    <col
                      key={`col-${column.employeeId}`}
                      className={styles.valueColumn}
                      style={getColumnStyle(column.employeeId)}
                    />
                  ))}
                </colgroup>
                <thead>
                  <tr>
                    <th className={styles.dayHeader}>Day</th>
                    {columns.map(column => {
                      const isEditableManualColumn =
                        !readOnly &&
                        column.kind === "manual" &&
                        column.manualColumnId !== null &&
                        typeof onColumnLabelChange === "function";
                      const isReorderableColumn = canReorderColumns;
                      const isDragSource = draggedEmployeeId === column.employeeId;
                      const isDropTarget =
                        dropTargetEmployeeId === column.employeeId &&
                        draggedEmployeeId !== null &&
                        draggedEmployeeId !== column.employeeId;

                      return (
                        <th
                          key={column.employeeId}
                          className={joinClassNames(
                            styles.valueHeader,
                            isReorderableColumn && styles.draggableHeader,
                            isDragSource && styles.draggingHeader,
                            isDropTarget && styles.dropTargetHeader,
                            resizingEmployeeId === column.employeeId && styles.resizingHeader,
                          )}
                          style={getColumnStyle(column.employeeId)}
                          draggable={isReorderableColumn}
                          onDragStart={isReorderableColumn ? event => handleHeaderDragStart(event, column.employeeId) : undefined}
                          onDragEnter={isReorderableColumn ? () => handleHeaderDragEnter(column.employeeId) : undefined}
                          onDragOver={isReorderableColumn ? event => handleHeaderDragOver(event, column.employeeId) : undefined}
                          onDrop={isReorderableColumn ? event => handleHeaderDrop(event, column.employeeId) : undefined}
                          onDragEnd={isReorderableColumn ? handleHeaderDragEnd : undefined}
                          title={isReorderableColumn ? column.label : undefined}
                        >
                          {isEditableManualColumn ? (
                            <div className={joinClassNames(styles.headerCell, styles.manualHeaderCell)}>
                              <input
                                className={styles.manualHeaderInput}
                                value={column.label}
                                placeholder="Manual column"
                                aria-label={`Manual column ${column.manualColumnId} header`}
                                draggable={false}
                                onChange={event => onColumnLabelChange(column.manualColumnId as number, event.target.value)}
                              />
                            </div>
                          ) : (
                            <div className={styles.headerCell}>
                              {onColumnHeaderClick && column.kind === "employee" ? (
                                <button
                                  type="button"
                                  className={styles.headerLabelButton}
                                  aria-label={`Customize columns from ${column.label}`}
                                  onClick={() => onColumnHeaderClick(column)}
                                >
                                  {column.label}
                                </button>
                              ) : (
                                <span className={styles.headerLabel}>{column.label}</span>
                              )}
                              {showColumnTotals && column.totalText ? <span className={styles.headerMeta}>{column.totalText}</span> : null}
                            </div>
                          )}

                          {allowColumnResize ? (
                            <span
                              className={joinClassNames(
                                styles.columnResizeHandle,
                                resizingEmployeeId === column.employeeId && styles.columnResizeHandleActive,
                              )}
                              role="presentation"
                              draggable={false}
                              onMouseDown={event => handleColumnResizeStart(event, column.employeeId)}
                            />
                          ) : null}
                        </th>
                      );
                    })}
                  </tr>
                </thead>

                <tbody>
                  {days.map(day => (
                    <tr
                      key={day.dayOfMonth}
                      className={joinClassNames(
                        day.isWeekend && styles.weekendRow,
                        day.hasConflict && styles.conflictRow,
                      )}
                    >
                      <MatrixDayCell
                        day={day}
                        shiftStaffingCounts={shiftStaffingCountsByDay[day.dayOfMonth]}
                        selectionEnabled={selectionEnabled}
                        isSelected={selectedDaySet.has(day.dayOfMonth)}
                        inlineStyle={dayStyleMetaByDay[day.dayOfMonth]?.inlineStyle ?? EMPTY_STYLE}
                      />

                      {columns.map(column => {
                        const cellKey = getGraphCellKey(column.employeeId, day.dayOfMonth);
                        const cellValue = cellMap[cellKey] ?? GRAPH_EMPTY_MARK;
                        const visualHint = visualHintMap[cellKey];
                        const visualHintDetail = visualHintDetailMap[cellKey];
                        const error = cellErrors[cellKey];
                        const isEmpty = cellValue.trim() === GRAPH_EMPTY_MARK;
                        const isSelected = selectedCellKeySet.has(cellKey);
                        const isEditing = editingCellKey === cellKey;
                        const cellStyle = styleMap[cellKey];

                        return (
                          <MatrixValueCell
                            key={cellKey}
                            cellKey={cellKey}
                            employeeId={column.employeeId}
                            dayOfMonth={day.dayOfMonth}
                            columnLabel={column.label}
                            value={cellValue}
                            visualHint={visualHint}
                            mutedSuffix={mutedSuffixMap[cellKey]}
                            lockVisualHint={lockVisualHintCells}
                            error={error}
                            isEmpty={isEmpty}
                            isSelected={isSelected}
                            isEditing={isEditing}
                            readOnly={readOnly}
                            isWeekend={day.isWeekend}
                            editMode={effectiveEditMode}
                            emptyCellVariant={emptyCellVariant}
                            highlightReadOnlyEmpty={highlightReadOnlyEmpty}
                            interactionEnabled={cellInteractionEnabled}
                            selectionEnabled={selectionEnabled}
                            backgroundColor={cellStyle?.backgroundColor}
                            textColor={cellStyle?.textColor}
                            editorValue={isEditing ? editingValue : undefined}
                            editorSelectionBehavior={isEditing ? editingSelectionBehavior : undefined}
                            onFocusTargetRef={element => {
                              cellFocusTargetRefs.current[cellKey] = element;
                            }}
                            onEditorValueChange={setEditingValue}
                            onEditorBlur={isEditing ? flushEditingCell : NOOP}
                            onInlineValueChange={nextValue => onCellChange?.(column.employeeId, day.dayOfMonth, nextValue)}
                            onInlineValueCommit={nextValue => commitCellValue(column.employeeId, day.dayOfMonth, nextValue)}
                            onVisualHintClick={onVisualHintCellClick
                              ? () => onVisualHintCellClick(column.employeeId, day.dayOfMonth)
                              : visualHintDetail && onVisualHintClick
                                ? () => onVisualHintClick(visualHintDetail)
                                : undefined}
                          />
                        );
                      })}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}
      </div>
    </CardSection>
  );
}
