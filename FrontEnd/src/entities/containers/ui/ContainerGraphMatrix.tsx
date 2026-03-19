import { memo, useEffect, useMemo, useRef, useState } from "react";
import type { CSSProperties, DragEvent, KeyboardEvent, MouseEvent, ReactNode } from "react";
import {
  GRAPH_EMPTY_MARK,
  getGraphCellKey,
  getGraphDaysInMonth,
  getGraphWeekdayLabel,
  isGraphWeekend,
  type GraphMatrixCellMap,
  type GraphMatrixColumn,
  type GraphMatrixStyleMap,
} from "@entities/containers/model/graphWorkspace";
import {
  formatBindKeyFromKeyboardEvent,
  isBindNavigationKey,
  isCommonEditorShortcut,
} from "@entities/availability-binds";
import type { Graph } from "@entities/containers/model/types";
import { ScheduleIcon } from "@shared/ui/icons";
import { CardSection } from "@shared/ui/sections/CardSection";
import styles from "./ContainerGraphMatrix.module.css";

export type GraphMatrixSelectionMode = "replace" | "toggle" | "range";

type ContainerGraphMatrixProps = {
  graph: Pick<Graph, "year" | "month">;
  columns: GraphMatrixColumn[];
  cellMap: GraphMatrixCellMap;
  styleMap?: GraphMatrixStyleMap;
  dayConflictMap?: Record<number, boolean>;
  title?: string;
  helperText?: string;
  readOnly?: boolean;
  highlightReadOnlyEmpty?: boolean;
  emptyMessage?: string;
  className?: string;
  style?: CSSProperties;
  compactSize?: boolean;
  cellErrors?: Record<string, string>;
  toolbar?: ReactNode;
  headerCenterSlot?: ReactNode;
  headerRightSlot?: ReactNode;
  bindValueByKey?: ReadonlyMap<string, string>;
  selectedCellKeys?: string[];
  onSelectedCellKeysChange?: (keys: string[]) => void;
  onColumnMove?: (employeeId: number, targetEmployeeId: number) => void;
  onColumnLabelChange?: (columnId: number, value: string) => void;
  onCellChange?: (employeeId: number, dayOfMonth: number, value: string) => void;
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

type MatrixValueCellProps = {
  cellKey: string;
  employeeId: number;
  dayOfMonth: number;
  columnLabel: string;
  value: string;
  error?: string;
  isEmpty: boolean;
  isSelected: boolean;
  isEditing: boolean;
  readOnly: boolean;
  highlightReadOnlyEmpty: boolean;
  backgroundColor?: string | null;
  textColor?: string | null;
  editorValue?: string;
  onFocusTargetRef?: (element: HTMLButtonElement | HTMLInputElement | null) => void;
  onEditorValueChange: (value: string) => void;
  onEditorBlur: () => void;
};

type MatrixDayCellProps = {
  day: MatrixDay;
  readOnly: boolean;
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

function getCellTitle(error: string | undefined, value: string) {
  return error ?? value;
}

const MatrixDayCell = memo(function MatrixDayCell({
  day,
  readOnly,
  isSelected,
  inlineStyle,
}: MatrixDayCellProps) {
  return (
    <th
      className={joinClassNames(
        styles.dayCell,
        day.hasConflict && styles.dayCellConflict,
        isSelected && styles.dayCellSelected,
      )}
      style={inlineStyle}
      data-matrix-target={readOnly ? undefined : "day"}
      data-day-of-month={readOnly ? undefined : day.dayOfMonth}
    >
      <span
        className={joinClassNames(
          styles.dayButton,
          isSelected && styles.dayButtonSelected,
        )}
      >
        {day.label}
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
  error,
  isEmpty,
  isSelected,
  isEditing,
  readOnly,
  highlightReadOnlyEmpty,
  backgroundColor,
  textColor,
  editorValue,
  onFocusTargetRef,
  onEditorValueChange,
  onEditorBlur,
}: MatrixValueCellProps) {
  const inlineStyle = {
    ...(backgroundColor ? { backgroundColor } : {}),
    ...(textColor ? { color: textColor } : {}),
  } satisfies CSSProperties;
  const cellTitle = getCellTitle(error, value);

  return (
    <td
      className={joinClassNames(
        styles.matrixCell,
        isEmpty && styles.emptyCell,
        readOnly && isEmpty && highlightReadOnlyEmpty && styles.readonlyEmptyCell,
        error && styles.errorCell,
        isSelected && styles.selectedCell,
        isEditing && styles.editingCell,
      )}
      style={inlineStyle}
      data-matrix-target={readOnly ? undefined : "cell"}
      data-employee-id={readOnly ? undefined : employeeId}
      data-day-of-month={readOnly ? undefined : dayOfMonth}
      data-cell-key={readOnly ? undefined : cellKey}
    >
      <div className={styles.cellSurface}>
        {readOnly ? (
          <span
            className={joinClassNames(
              styles.readonlyValue,
              isEmpty && styles.cellValueEmpty,
            )}
            title={cellTitle}
          >
            {value}
          </span>
        ) : isEditing ? (
          <input
            autoFocus
            ref={onFocusTargetRef}
            className={joinClassNames(
              styles.cellEditor,
              isEmpty && styles.cellValueEmpty,
            )}
            value={editorValue ?? value}
            onChange={event => onEditorValueChange(event.target.value)}
            onBlur={onEditorBlur}
            aria-label={`${columnLabel} day ${dayOfMonth}`}
            aria-invalid={Boolean(error)}
            title={cellTitle}
            data-matrix-editor="true"
          />
        ) : (
          <button
            type="button"
            ref={onFocusTargetRef}
            className={joinClassNames(
              styles.cellButton,
              isEmpty && styles.cellValueEmpty,
            )}
            aria-label={`${columnLabel} day ${dayOfMonth}`}
            aria-invalid={Boolean(error)}
            title={cellTitle}
          >
            {value}
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
  styleMap = {},
  dayConflictMap = {},
  title = "Schedule Matrix",
  helperText,
  readOnly = false,
  highlightReadOnlyEmpty = false,
  emptyMessage,
  className,
  style,
  compactSize = false,
  cellErrors = {},
  toolbar,
  headerCenterSlot,
  headerRightSlot,
  bindValueByKey,
  selectedCellKeys = [],
  onSelectedCellKeysChange,
  onColumnMove,
  onColumnLabelChange,
  onCellChange,
}: ContainerGraphMatrixProps) {
  const [editingCellKey, setEditingCellKey] = useState<string | null>(null);
  const [editingValue, setEditingValue] = useState(GRAPH_EMPTY_MARK);
  const [draftSelectedCellKeys, setDraftSelectedCellKeys] = useState<string[] | null>(null);
  const [draggedEmployeeId, setDraggedEmployeeId] = useState<number | null>(null);
  const [dropTargetEmployeeId, setDropTargetEmployeeId] = useState<number | null>(null);
  const isPointerSelectingRef = useRef(false);
  const lastDraggedSelectionKeyRef = useRef<string | null>(null);
  const selectionAnchorKeyRef = useRef<string | null>(selectedCellKeys[0] ?? null);
  const draftSelectedCellKeysRef = useRef<string[] | null>(null);
  const dragSelectionTypeRef = useRef<"cell" | "day" | null>(null);
  const selectedCellKeysRef = useRef(selectedCellKeys);
  const onSelectedCellKeysChangeRef = useRef(onSelectedCellKeysChange);
  const cellMapRef = useRef(cellMap);
  const onCellChangeRef = useRef(onCellChange);
  const cellFocusTargetRefs = useRef<Record<string, HTMLButtonElement | HTMLInputElement | null>>({});
  const editingCellKeyRef = useRef<string | null>(null);
  const editingValueRef = useRef(editingValue);

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
  const cardClassName = [styles.card, compactSize ? styles.cardCompact : "", className ?? ""].filter(Boolean).join(" ");
  const cardStyle = useMemo(
    () =>
      ({
        ...(style ?? {}),
        "--matrix-cell-min-height": compactSize ? "10px" : "30px",
      }) as CSSProperties,
    [compactSize, style],
  );
  const hasToolbar = Boolean(toolbar);
  const layoutClassName = joinClassNames(
    styles.layout,
    hasToolbar && styles.layoutWithToolbar,
    compactSize && styles.layoutCompact,
  );
  const orderedEmployeeIds = useMemo(() => columns.map(column => column.employeeId), [columns]);
  const reorderableColumnIds = useMemo(
    () => new Set(columns.map(column => column.employeeId)),
    [columns],
  );
  const tableStyle = useMemo(
    () =>
      ({
        "--matrix-column-count": String(columns.length),
      }) as CSSProperties,
    [columns.length],
  );
  const effectiveSelectedCellKeys = draftSelectedCellKeys ?? selectedCellKeys;
  const selectedCellKeySet = useMemo(() => new Set(effectiveSelectedCellKeys), [effectiveSelectedCellKeys]);
  const dayStyleMetaByDay = useMemo(() => {
    return days.reduce<Record<number, MatrixDayStyleMeta>>((accumulator, day) => {
      const rowCellKeys = columns.map(column => getGraphCellKey(column.employeeId, day.dayOfMonth));
      const rowStyles = rowCellKeys.map(cellKey => styleMap[cellKey]);
      const backgroundColor = getUniformStyleValue(rowStyles.map(styleRecord => styleRecord?.backgroundColor));
      const textColor = getUniformStyleValue(rowStyles.map(styleRecord => styleRecord?.textColor));

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

  const flushEditingCell = () => {
    const currentEditingCellKey = editingCellKeyRef.current;
    if (!currentEditingCellKey) {
      return;
    }

    const { employeeId, dayOfMonth } = parseSelectedCellKey(currentEditingCellKey);
    const nextValue = editingValueRef.current;
    const currentValue = cellMapRef.current[currentEditingCellKey] ?? GRAPH_EMPTY_MARK;

    editingCellKeyRef.current = null;
    setEditingCellKey(null);

    if (nextValue !== currentValue) {
      onCellChangeRef.current?.(employeeId, dayOfMonth, nextValue);
    }
  };

  const cancelEditingCell = () => {
    editingCellKeyRef.current = null;
    setEditingCellKey(null);
    setEditingValue(GRAPH_EMPTY_MARK);
  };

  const beginEditingCell = (cellKey: string) => {
    flushEditingCell();
    editingCellKeyRef.current = cellKey;
    const currentValue = cellMapRef.current[cellKey] ?? GRAPH_EMPTY_MARK;
    setEditingCellKey(cellKey);
    setEditingValue(currentValue);
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

    onCellChangeRef.current(target.employeeId, target.dayOfMonth, bindValue);

    const nextCellKey =
      target.dayOfMonth < days.length
        ? getGraphCellKey(target.employeeId, target.dayOfMonth + 1)
        : target.cellKey;

    commitSelection([nextCellKey], nextCellKey);
    focusCell(nextCellKey);
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
    if (readOnly || event.button !== 0) {
      return;
    }

    if (event.target instanceof HTMLElement && event.target.closest("[data-matrix-editor='true']")) {
      return;
    }

    const target = getSelectionTarget(event.target);
    if (!target) {
      return;
    }

    event.preventDefault();
    startSelection(target, resolveSelectionMode(event));
  };

  const handleTableMouseMove = (event: MouseEvent<HTMLTableElement>) => {
    if (readOnly || !isPointerSelectingRef.current) {
      return;
    }

    const target = getSelectionTarget(event.target);
    if (!target) {
      return;
    }

    extendSelection(target);
  };

  const handleTableDoubleClick = (event: MouseEvent<HTMLTableElement>) => {
    if (readOnly) {
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
    const target = getSelectionTarget(event.target);
    if (!target || target.type !== "cell") {
      return;
    }

    const isEditorTarget = event.target instanceof HTMLInputElement && event.target.dataset.matrixEditor === "true";

    if (applyBindShortcut(event, target, isEditorTarget)) {
      return;
    }

    if (isEditorTarget) {
      if (event.key === "Escape") {
        event.preventDefault();
        cancelEditingCell();
        return;
      }

      if (event.key === "Enter") {
        event.preventDefault();
        flushEditingCell();
        commitSelection([target.cellKey], target.cellKey);
      }

      return;
    }

    if (event.key === "Enter" || event.key === "F2") {
      event.preventDefault();
      beginEditingCell(target.cellKey);
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

  const handleHeaderDragStart = (event: DragEvent<HTMLTableCellElement>, employeeId: number) => {
    if (!canReorderColumns || !reorderableColumnIds.has(employeeId)) {
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

  return (
    <CardSection
      className={cardClassName}
      style={cardStyle}
      title={title}
      icon={<ScheduleIcon size={18} />}
      headerCenterSlot={headerCenterSlot}
      headerRightSlot={headerRightSlot}
    >
      <div className={layoutClassName}>
        {helperText ? <p className={styles.helperText}>{helperText}</p> : null}

        {hasToolbar ? <div className={styles.toolbar}>{toolbar}</div> : null}

        {columns.length === 0 ? (
          <div className={styles.emptyState}>{resolvedEmptyMessage}</div>
        ) : (
          <div className={joinClassNames(styles.tableShell, compactSize && styles.tableShellCompact)}>
            <div className={joinClassNames(styles.tableScroll, compactSize && styles.tableScrollCompact)}>
              <table
                className={styles.table}
                style={tableStyle}
                onMouseDown={handleTableMouseDown}
                onMouseMove={handleTableMouseMove}
                onDoubleClick={handleTableDoubleClick}
                onKeyDown={handleTableKeyDown}
              >
                <colgroup>
                  <col className={styles.dayColumn} />
                  {columns.map(column => (
                    <col key={`col-${column.employeeId}`} className={styles.valueColumn} />
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
                            isReorderableColumn && styles.draggableHeader,
                            isDragSource && styles.draggingHeader,
                            isDropTarget && styles.dropTargetHeader,
                          )}
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
                              <span className={styles.headerLabel}>{column.label}</span>
                              {column.totalText ? <span className={styles.headerMeta}>{column.totalText}</span> : null}
                            </div>
                          )}
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
                        readOnly={readOnly}
                        isSelected={selectedDaySet.has(day.dayOfMonth)}
                        inlineStyle={dayStyleMetaByDay[day.dayOfMonth]?.inlineStyle ?? EMPTY_STYLE}
                      />

                      {columns.map(column => {
                        const cellKey = getGraphCellKey(column.employeeId, day.dayOfMonth);
                        const cellValue = cellMap[cellKey] ?? GRAPH_EMPTY_MARK;
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
                            error={error}
                            isEmpty={isEmpty}
                            isSelected={isSelected}
                            isEditing={isEditing}
                            readOnly={readOnly}
                            highlightReadOnlyEmpty={highlightReadOnlyEmpty}
                            backgroundColor={cellStyle?.backgroundColor}
                            textColor={cellStyle?.textColor}
                            editorValue={isEditing ? editingValue : undefined}
                            onFocusTargetRef={element => {
                              cellFocusTargetRefs.current[cellKey] = element;
                            }}
                            onEditorValueChange={setEditingValue}
                            onEditorBlur={isEditing ? flushEditingCell : NOOP}
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
