import { useEffect, useMemo, useRef, useState } from "react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import {
  buildManagerEditLockMessage,
  managerEditResourceTypes,
  useManagerEditLocks,
} from "@app/providers/PresenceProvider";
import { useQueryClient, type QueryClient } from "@tanstack/react-query";
import {
  buildActiveAvailabilityBindMap,
  normalizeBindKey,
  type AvailabilityBind,
  type SaveAvailabilityBindInput,
  useAvailabilityBindsListQuery,
  useCreateAvailabilityBindMutation,
  useDeleteAvailabilityBindMutation,
  useUpdateAvailabilityBindMutation,
} from "@entities/availability-binds";
import {
  buildAvailabilityCellMap,
  buildAvailabilityColumns,
  type AvailabilityMatrixCellMap,
  type AvailabilityMatrixColumn,
  useAvailabilityGroupMembersQuery,
  useAvailabilityGroupSlotsQuery,
  useAvailabilityGroupsListQuery,
} from "@entities/availability-groups";
import { AVAILABILITY_NONE_MARK } from "@entities/availability-groups/model/matrix";
import {
  ContainerGraphSessionTabs,
  ContainerGraphEditor,
  GRAPH_EMPTY_MARK,
  applyGraphApiErrors,
  buildGraphSessionSearch,
  buildGraphNoteContent,
  buildGraphCellMap,
  buildGraphConflictDayMap,
  buildGraphDraftSlots,
  buildGraphFormErrors,
  buildGraphMatrixColumns,
  buildGraphRelatedScheduleHintData,
  buildGraphStyleMap,
  buildGraphTotals,
  containersApi,
  createGraphFormFromGraph,
  createInitialGraphForm,
  getGraphSessionIds,
  GRAPH_DAY_STYLE_EMPLOYEE_ID,
  getGraphCellKey,
  getGraphDaysInMonth,
  parseGraphNoteContent,
  parseIntegerField,
  rehydrateGraphNoteCellStyles,
  rehydrateGraphNoteTextCells,
  rgbHexToArgb,
  resolveGraphSession,
  sanitizeGraphCellMap,
  serializeGraphNoteCellStyles,
  serializeGraphNoteTextCells,
  sortGraphItemsByDisplayOrder,
  useContainerByIdQuery,
  useContainerGraphsQuery,
  useCreateSchedulePresetMutation,
  useGenerateGraphPreviewMutation,
  useGraphByIdQuery,
  useGraphCellStylesQuery,
  useGraphEmployeesQuery,
  useGraphSlotsBatchQuery,
  useGraphSlotsQuery,
  useSaveGraphWorkspaceMutation,
  useSchedulePresetsQuery,
  type ContainerGraphFormErrors,
  type ContainerGraphFormState,
  type EditableGraphManualColumn,
  type GraphAutoAvailabilityStyleSuppressionMap,
  type GraphCellStyle,
  type GraphMatrixColumn,
  type ManualColumnShiftPublicationInput,
  type PendingManualColumnShiftPublication,
  type SaveSchedulePresetDto,
} from "@entities/containers";
import { useEmployeesListQuery } from "@entities/employees/api/queries";
import { getEmployeeFullName } from "@entities/employees/model/presentation";
import {
  useCancelManagerManualShiftSwapMutation,
  useCreateManagerManualShiftSwapMutation,
  useGraphShiftSwapLogQuery,
} from "@entities/shift-swaps";
import { useShopsListQuery } from "@entities/shops/api/queries";
import { queryKeys } from "@shared/api/queryKeys";
import { ApiError } from "@shared/api/httpClient";
import { usePageScrollbarHidden } from "@shared/lib/usePageScrollbarHidden";
import { stableSerialize } from "@shared/lib/stableSerialize";
import { useUnsavedChangesPrompt } from "@shared/lib/useUnsavedChangesPrompt";
import { ConfirmDialog } from "@shared/ui/ConfirmDialog";
import { ManagerEditLockDialog } from "@shared/ui/ManagerEditLockDialog";
import { SavingOverlay } from "@shared/ui/SavingOverlay";
import { IosButton } from "@shared/ui/components/IosButton";
import { SaveIcon } from "@shared/ui/icons";
import { PageHeader } from "@shared/ui/PageHeader";
import styles from "./ContainerGraphEditPage.module.css";

type EditableAvailabilityBind = {
  clientId: string;
  id: number | null;
  key: string;
  value: string;
  isActive: boolean;
  persistedKey: string;
  persistedValue: string;
  persistedIsActive: boolean;
};

type EditableScheduleManualColumn = EditableGraphManualColumn;
type GraphEditorSessionDraft = {
  graphId: number;
  form: ContainerGraphFormState;
  graphEmployeeRows: Array<{ id: number | null; employeeId: number; minHoursMonth: string }>;
  selectedSchedulePresetId: number | null;
  selectedEmployeeId: number | null;
  cellMap: Record<string, string>;
  manualColumns: EditableScheduleManualColumn[];
  pendingManualShiftPublishes: PendingManualColumnShiftPublication[];
  scheduleColumnOrder: number[];
  selectedCellKeys: string[];
  styleRecords: GraphCellStyle[];
  autoAvailabilityStyleSuppressions: GraphAutoAvailabilityStyleSuppressionMap;
  fillColor: string;
  textColor: string;
  syncedAvailabilityGroupId: number | null;
  previewAvailabilitySelection: string;
};

const FOLLOW_SCHEDULE_DETAILS_PREVIEW = "__schedule-details__";
const AUTO_UNAVAILABLE_AVAILABILITY_BACKGROUND_ARGB = rgbHexToArgb("#f8b4b4") ?? -478600;

function runMutation<TData, TVariables>(
  mutate: (
    variables: TVariables,
    callbacks?: { onSuccess?: (data: TData) => void; onError?: (error: unknown) => void },
  ) => void,
  variables: TVariables,
) {
  return new Promise<TData>((resolve, reject) => {
    mutate(variables, {
      onSuccess: resolve,
      onError: reject,
    });
  });
}

function toErrorMessage(error: unknown) {
  if (error instanceof Error) {
    return error.message;
  }

  return "Something went wrong while saving bind information.";
}

function toManualShiftPublishError(error: unknown) {
  if (error instanceof ApiError || error instanceof Error) {
    return error.message;
  }

  return "Could not publish this manual shift.";
}

function toEditableAvailabilityBind(bind: AvailabilityBind): EditableAvailabilityBind {
  return {
    clientId: `bind-${bind.id}`,
    id: bind.id,
    key: bind.key,
    value: bind.value,
    isActive: bind.isActive,
    persistedKey: bind.key,
    persistedValue: bind.value,
    persistedIsActive: bind.isActive,
  };
}

function createDraftAvailabilityBind(): EditableAvailabilityBind {
  return {
    clientId: `draft-${crypto.randomUUID()}`,
    id: null,
    key: "",
    value: "",
    isActive: true,
    persistedKey: "",
    persistedValue: "",
    persistedIsActive: true,
  };
}

function toEditableScheduleManualColumn(column: {
  id: number;
  label: string;
  cells: Record<string, string>;
}): EditableScheduleManualColumn {
  return {
    columnId: column.id,
    label: column.label,
    cells: column.cells,
  };
}

function getNextManualColumnId(columns: EditableScheduleManualColumn[]) {
  return columns.reduce((maxColumnId, column) => Math.max(maxColumnId, column.columnId), 0) + 1;
}

function createDraftScheduleManualColumn(columns: EditableScheduleManualColumn[]): EditableScheduleManualColumn {
  return {
    columnId: getNextManualColumnId(columns),
    label: "",
    cells: {},
  };
}

function buildManualColumnEmployeeId(columnId: number) {
  return -Math.abs(columnId);
}

function getManualColumnIdFromEmployeeId(employeeId: number) {
  return employeeId < 0 ? Math.abs(employeeId) : null;
}

function normalizeManualColumnCellValue(value: string) {
  const trimmedValue = value.trim();
  if (!trimmedValue || trimmedValue === GRAPH_EMPTY_MARK) {
    return null;
  }

  return value;
}

function sanitizeManualColumnsForNote(
  manualColumns: EditableScheduleManualColumn[],
  year: number,
  month: number,
) {
  const daysInMonth = getGraphDaysInMonth(year, month);

  return manualColumns.map(column => ({
    id: column.columnId,
    label: column.label,
    cells: Object.entries(column.cells).reduce<Record<string, string>>((accumulator, [dayKey, value]) => {
      const dayOfMonth = Number(dayKey);
      const normalizedValue = normalizeManualColumnCellValue(value);
      if (!Number.isInteger(dayOfMonth) || dayOfMonth < 1 || dayOfMonth > daysInMonth || normalizedValue === null) {
        return accumulator;
      }

      accumulator[String(dayOfMonth)] = normalizedValue;
      return accumulator;
    }, {}),
  }));
}

function buildDefaultScheduleColumnOrder(
  employeeRows: Array<{ employeeId: number }>,
  manualColumns: EditableScheduleManualColumn[],
) {
  return [
    ...employeeRows.map(row => row.employeeId),
    ...manualColumns.map(column => buildManualColumnEmployeeId(column.columnId)),
  ];
}

function sanitizeScheduleColumnOrder(
  columnOrder: number[],
  employeeRows: Array<{ employeeId: number }>,
  manualColumns: EditableScheduleManualColumn[],
) {
  const fallbackOrder = buildDefaultScheduleColumnOrder(employeeRows, manualColumns);
  const validIds = new Set(fallbackOrder);
  const nextOrder: number[] = [];

  columnOrder.forEach(columnId => {
    if (!validIds.has(columnId) || nextOrder.includes(columnId)) {
      return;
    }

    nextOrder.push(columnId);
  });

  fallbackOrder.forEach(columnId => {
    if (!nextOrder.includes(columnId)) {
      nextOrder.push(columnId);
    }
  });

  return nextOrder;
}

function moveScheduleColumnOrder(columnOrder: number[], sourceColumnId: number, targetColumnId: number) {
  const sourceIndex = columnOrder.indexOf(sourceColumnId);
  const targetIndex = columnOrder.indexOf(targetColumnId);

  if (sourceIndex < 0 || targetIndex < 0 || sourceIndex === targetIndex) {
    return columnOrder;
  }

  const nextOrder = [...columnOrder];
  const [movedColumnId] = nextOrder.splice(sourceIndex, 1);

  if (movedColumnId === undefined) {
    return columnOrder;
  }

  nextOrder.splice(targetIndex, 0, movedColumnId);
  return nextOrder;
}

function sortGraphEmployeeRowsByColumnOrder(
  rows: Array<{ id: number | null; employeeId: number; minHoursMonth: string }>,
  columnOrder: number[],
) {
  const indexByEmployeeId = new Map(
    columnOrder
      .filter(columnId => columnId > 0)
      .map((employeeId, index) => [employeeId, index] as const),
  );

  return [...rows].sort((left, right) => {
    const leftIndex = indexByEmployeeId.get(left.employeeId) ?? Number.MAX_SAFE_INTEGER;
    const rightIndex = indexByEmployeeId.get(right.employeeId) ?? Number.MAX_SAFE_INTEGER;

    if (leftIndex !== rightIndex) {
      return leftIndex - rightIndex;
    }

    return left.employeeId - right.employeeId;
  });
}

function sortManualColumnsByColumnOrder(
  columns: EditableScheduleManualColumn[],
  columnOrder: number[],
) {
  const indexByManualEmployeeId = new Map(
    columnOrder
      .filter(columnId => columnId < 0)
      .map((manualEmployeeId, index) => [manualEmployeeId, index] as const),
  );

  return [...columns].sort((left, right) => {
    const leftIndex = indexByManualEmployeeId.get(buildManualColumnEmployeeId(left.columnId)) ?? Number.MAX_SAFE_INTEGER;
    const rightIndex = indexByManualEmployeeId.get(buildManualColumnEmployeeId(right.columnId)) ?? Number.MAX_SAFE_INTEGER;

    if (leftIndex !== rightIndex) {
      return leftIndex - rightIndex;
    }

    return left.columnId - right.columnId;
  });
}

function getPersistedScheduleColumnOrder(
  employeeRows: Array<{ employeeId: number }>,
  manualColumns: EditableScheduleManualColumn[],
  columnOrder: number[],
) {
  const sanitizedColumnOrder = sanitizeScheduleColumnOrder(columnOrder, employeeRows, manualColumns);
  const defaultColumnOrder = buildDefaultScheduleColumnOrder(employeeRows, manualColumns);

  return arraysEqual(sanitizedColumnOrder, defaultColumnOrder) ? [] : sanitizedColumnOrder;
}

function cloneGraphEditorSessionDraft(draft: GraphEditorSessionDraft): GraphEditorSessionDraft {
  return {
    ...draft,
    form: { ...draft.form },
    graphEmployeeRows: draft.graphEmployeeRows.map(row => ({ ...row })),
    cellMap: { ...draft.cellMap },
    autoAvailabilityStyleSuppressions: cloneAutoAvailabilityStyleSuppressions(draft.autoAvailabilityStyleSuppressions ?? {}),
    manualColumns: draft.manualColumns.map(column => ({
      ...column,
      cells: { ...column.cells },
    })),
    pendingManualShiftPublishes: draft.pendingManualShiftPublishes.map(shift => ({ ...shift })),
    scheduleColumnOrder: [...draft.scheduleColumnOrder],
    selectedCellKeys: [...draft.selectedCellKeys],
    styleRecords: draft.styleRecords.map(style => ({ ...style })),
  };
}

function toPayload(
  form: ContainerGraphFormState,
  employeeRows: Array<{ employeeId: number }>,
  manualColumns: EditableScheduleManualColumn[],
  columnOrder: number[],
  styleRecords: GraphCellStyle[],
  cellMap: Record<string, string>,
  autoAvailabilityStyleSuppressions: GraphAutoAvailabilityStyleSuppressionMap,
) {
  const year = Number(form.year) || new Date().getFullYear();
  const month = Number(form.month) || 1;
  const persistedColumnOrder = getPersistedScheduleColumnOrder(employeeRows, manualColumns, columnOrder);
  const employeeIds = employeeRows.map(row => row.employeeId);

  return {
    shopId: Number(form.shopId),
    name: form.name.trim(),
    year,
    month,
    publicationStatus: form.publicationStatus,
    peoplePerShift: Number(form.peoplePerShift),
    shift1Time: form.shift1Time.trim(),
    shift2Time: form.shift2Time.trim(),
    maxHoursPerEmpMonth: Number(form.maxHoursPerEmpMonth),
    maxConsecutiveDays: Number(form.maxConsecutiveDays),
    maxConsecutiveFull: Number(form.maxConsecutiveFull),
    maxFullPerMonth: Number(form.maxFullPerMonth),
    note: buildGraphNoteContent(
      form.note,
      sanitizeManualColumnsForNote(manualColumns, year, month),
      persistedColumnOrder,
      serializeGraphNoteCellStyles(styleRecords),
      serializeGraphNoteTextCells(cellMap, employeeIds, year, month),
      autoAvailabilityStyleSuppressions,
    ) || undefined,
    availabilityGroupId: form.availabilityGroupId ? Number(form.availabilityGroupId) : null,
  };
}

function buildGraphWorkspaceSnapshot(params: {
  form: ContainerGraphFormState;
  graphEmployeeRows: Array<{ id: number | null; employeeId: number; minHoursMonth: string }>;
  manualColumns: EditableScheduleManualColumn[];
  pendingManualShiftPublishes?: PendingManualColumnShiftPublication[];
  scheduleColumnOrder: number[];
  styleRecords: GraphCellStyle[];
  cellMap: Record<string, string>;
  autoAvailabilityStyleSuppressions: GraphAutoAvailabilityStyleSuppressionMap;
}) {
  const orderedEmployeeRows = sortGraphEmployeeRowsByColumnOrder(params.graphEmployeeRows, params.scheduleColumnOrder);
  const year = Number(params.form.year) || new Date().getFullYear();
  const month = Number(params.form.month) || 1;
  const employeeIds = orderedEmployeeRows.map(row => row.employeeId);
  const sanitizedCellMap = sanitizeGraphCellMap(params.cellMap, employeeIds, year, month);

  return stableSerialize({
    payload: toPayload(
      params.form,
      orderedEmployeeRows,
      params.manualColumns,
      params.scheduleColumnOrder,
      params.styleRecords,
      params.cellMap,
      params.autoAvailabilityStyleSuppressions,
    ),
    employeeAssignments: orderedEmployeeRows.map(row => ({
      employeeId: row.employeeId,
      minHoursMonth: row.minHoursMonth.trim(),
    })),
    pendingManualShiftPublishes: (params.pendingManualShiftPublishes ?? [])
      .map(shift => ({
        manualColumnId: shift.manualColumnId,
        dayOfMonth: shift.dayOfMonth,
        fromTime: shift.fromTime,
        toTime: shift.toTime,
        targetEmployeeId: shift.targetEmployeeId ?? null,
      }))
      .sort((left, right) =>
        left.manualColumnId - right.manualColumnId ||
        left.dayOfMonth - right.dayOfMonth ||
        left.fromTime.localeCompare(right.fromTime) ||
        left.toTime.localeCompare(right.toTime) ||
        (left.targetEmployeeId ?? 0) - (right.targetEmployeeId ?? 0)),
    cellMap: Object.entries(sanitizedCellMap).sort(([leftKey], [rightKey]) => leftKey.localeCompare(rightKey)),
  });
}

function buildGraphDraftSnapshot(draft: GraphEditorSessionDraft) {
  return buildGraphWorkspaceSnapshot({
    form: draft.form,
    graphEmployeeRows: draft.graphEmployeeRows,
    manualColumns: draft.manualColumns,
    pendingManualShiftPublishes: draft.pendingManualShiftPublishes,
    scheduleColumnOrder: draft.scheduleColumnOrder,
    styleRecords: draft.styleRecords,
    cellMap: draft.cellMap,
    autoAvailabilityStyleSuppressions: draft.autoAvailabilityStyleSuppressions,
  });
}

function createEditableEmployeeRows(
  graphEmployees: Array<{ id: number; employeeId: number; minHoursMonth?: number | null; displayOrder?: number | null }>,
  employeesById: Map<number, { firstName: string; lastName: string }>,
) {
  return sortGraphItemsByDisplayOrder(
    [...graphEmployees].map(graphEmployee => ({
      id: graphEmployee.id,
      employeeId: graphEmployee.employeeId,
      displayOrder: graphEmployee.displayOrder,
      minHoursMonth: graphEmployee.minHoursMonth != null ? String(graphEmployee.minHoursMonth) : "",
      label: getEmployeeFullName(employeesById.get(graphEmployee.employeeId), `Employee ${graphEmployee.employeeId}`),
    })),
  ).map(item => ({
      id: item.id,
      employeeId: item.employeeId,
      minHoursMonth: item.minHoursMonth,
    }));
}

function sortAvailabilityMembersByDisplayOrder(
  members: Array<{ id: number; employeeId: number; displayOrder: number }>,
) {
  return [...members].sort((left, right) => {
    if (left.displayOrder !== right.displayOrder) {
      return left.displayOrder - right.displayOrder;
    }

    return left.employeeId - right.employeeId;
  });
}

function buildEmployeeRowsFromAvailabilityMembers(
  currentRows: Array<{ id: number | null; employeeId: number; minHoursMonth: string }>,
  members: Array<{ id: number; employeeId: number; displayOrder: number }>,
) {
  const currentRowByEmployeeId = new Map(currentRows.map(row => [row.employeeId, row] as const));

  return sortAvailabilityMembersByDisplayOrder(members).map(member => {
    const currentRow = currentRowByEmployeeId.get(member.employeeId);

    return {
      id: currentRow?.id ?? null,
      employeeId: member.employeeId,
      minHoursMonth: currentRow?.minHoursMonth ?? "",
    };
  });
}

function arraysEqual(left: number[], right: number[]) {
  return left.length === right.length && left.every((value, index) => value === right[index]);
}

function hasPendingBindDraftChanges(bindRows: EditableAvailabilityBind[]) {
  return bindRows.some(bind => {
    if (bind.id === null) {
      return bind.key.trim().length > 0 || bind.value.trim().length > 0 || bind.isActive !== true;
    }

    return (
      bind.key !== bind.persistedKey ||
      bind.value !== bind.persistedValue ||
      bind.isActive !== bind.persistedIsActive
    );
  });
}

function graphEmployeeRowsEqual(
  left: Array<{ id: number | null; employeeId: number; minHoursMonth: string }>,
  right: Array<{ id: number | null; employeeId: number; minHoursMonth: string }>,
) {
  return left.length === right.length && left.every((row, index) => {
    const otherRow = right[index];

    return Boolean(
      otherRow &&
      row.id === otherRow.id &&
      row.employeeId === otherRow.employeeId &&
      row.minHoursMonth === otherRow.minHoursMonth,
    );
  });
}

type StyleProperty = "fill" | "text";
type CompactSizeHeaderToggleProps = {
  checked: boolean;
  onToggle: () => void;
};

function joinClassNames(...values: Array<string | false | undefined>) {
  return values.filter(Boolean).join(" ");
}

function formatAvailabilityGroupPeriod(year: number, month: number) {
  return new Intl.DateTimeFormat("en-GB", {
    month: "long",
    year: "numeric",
    timeZone: "UTC",
  }).format(new Date(Date.UTC(year, month - 1, 1)));
}

function CompactSizeHeaderToggle({ checked, onToggle }: CompactSizeHeaderToggleProps) {
  return (
    <button
      type="button"
      className={joinClassNames(styles.compactToggle, checked && styles.compactToggleActive)}
      aria-pressed={checked}
      onClick={onToggle}
    >
      <span className={styles.compactToggleTitle}>Compact Size</span>

      <span className={styles.compactToggleTrack} aria-hidden="true">
        <span className={styles.compactToggleThumb} />
      </span>
    </button>
  );
}

function parseSelectedCellKey(cellKey: string) {
  const [employeeIdValue, dayValue] = cellKey.split(":");

  return {
    employeeId: Number(employeeIdValue),
    dayOfMonth: Number(dayValue),
  };
}

function filterStyleRecordsByKeys(styles: GraphCellStyle[], keysToRemove: Set<string>) {
  return styles.filter(style => !keysToRemove.has(getGraphCellKey(style.employeeId, style.dayOfMonth)));
}

function cloneAutoAvailabilityStyleSuppressions(
  suppressions: GraphAutoAvailabilityStyleSuppressionMap,
): GraphAutoAvailabilityStyleSuppressionMap {
  return Object.fromEntries(
    Object.entries(suppressions).map(([groupId, cellKeys]) => [groupId, [...cellKeys]]),
  );
}

function buildStyleRecordByKey(styles: GraphCellStyle[]) {
  return new Map(styles.map(style => [getGraphCellKey(style.employeeId, style.dayOfMonth), style]));
}

function getAutoAvailabilitySuppressionCellKeys(
  suppressions: GraphAutoAvailabilityStyleSuppressionMap,
  availabilityGroupId: number | null,
) {
  if (!availabilityGroupId) {
    return [] as string[];
  }

  return suppressions[String(availabilityGroupId)] ?? [];
}

function addAutoAvailabilityStyleSuppressions(
  current: GraphAutoAvailabilityStyleSuppressionMap,
  availabilityGroupId: number | null,
  cellKeys: Iterable<string>,
) {
  if (!availabilityGroupId) {
    return current;
  }

  const groupKey = String(availabilityGroupId);
  const currentCellKeys = current[groupKey] ?? [];
  const seenCellKeys = new Set(currentCellKeys);
  const nextCellKeys = [...currentCellKeys];
  let hasChanges = false;

  for (const cellKey of cellKeys) {
    if (seenCellKeys.has(cellKey)) {
      continue;
    }

    seenCellKeys.add(cellKey);
    nextCellKeys.push(cellKey);
    hasChanges = true;
  }

  if (!hasChanges) {
    return current;
  }

  return {
    ...current,
    [groupKey]: nextCellKeys,
  };
}

function buildAutoAvailabilityUnavailableStyles(params: {
  scheduleId: number;
  autoCellKeys: string[];
  suppressedCellKeys: Set<string>;
  existingStyles: GraphCellStyle[];
  getNextStyleId: () => number;
}) {
  const { scheduleId, autoCellKeys, suppressedCellKeys, existingStyles, getNextStyleId } = params;
  const nextStyleByKey = buildStyleRecordByKey(existingStyles);
  let hasChanges = false;

  autoCellKeys.forEach(cellKey => {
    if (suppressedCellKeys.has(cellKey) || nextStyleByKey.has(cellKey)) {
      return;
    }

    const { employeeId, dayOfMonth } = parseSelectedCellKey(cellKey);
    nextStyleByKey.set(cellKey, {
      id: getNextStyleId(),
      scheduleId,
      employeeId,
      dayOfMonth,
      backgroundColorArgb: AUTO_UNAVAILABLE_AVAILABILITY_BACKGROUND_ARGB,
      textColorArgb: null,
    });
    hasChanges = true;
  });

  return hasChanges ? [...nextStyleByKey.values()] : null;
}

function normalizeApiStyleRecords(styles: GraphCellStyle[], scheduleId: number) {
  const styleByKey = new Map<string, GraphCellStyle>();

  styles.forEach(style => {
    if (!Number.isInteger(style.employeeId) || style.employeeId <= 0) {
      return;
    }

    const cellKey = getGraphCellKey(style.employeeId, style.dayOfMonth);
    const backgroundColorArgb = style.backgroundColorArgb ?? null;
    const textColorArgb = style.textColorArgb ?? null;

    if (backgroundColorArgb === null && textColorArgb === null) {
      styleByKey.delete(cellKey);
      return;
    }

    styleByKey.set(cellKey, {
      ...style,
      scheduleId,
      backgroundColorArgb,
      textColorArgb,
    });
  });

  return [...styleByKey.values()];
}

function mergeNormalizedStyleRecords(apiStyles: GraphCellStyle[], localStyles: GraphCellStyle[]) {
  return [...apiStyles, ...localStyles];
}

async function persistGraphStyleDraft(params: {
  containerId: number;
  graphId: number;
  styleRecords: GraphCellStyle[];
  queryClient: QueryClient;
}) {
  const { containerId, graphId, styleRecords, queryClient } = params;
  const draftApiStyles = normalizeApiStyleRecords(styleRecords, graphId);
  const persistedApiStyles = normalizeApiStyleRecords(
    await containersApi.listGraphCellStyles(containerId, graphId),
    graphId,
  );
  const draftStyleByKey = buildStyleRecordByKey(draftApiStyles);
  const persistedStyleByKey = buildStyleRecordByKey(persistedApiStyles);
  const stylesToDelete = persistedApiStyles.filter(style => !draftStyleByKey.has(getGraphCellKey(style.employeeId, style.dayOfMonth)));
  const stylesToUpsert = draftApiStyles.filter(style => {
    const currentPersistedStyle = persistedStyleByKey.get(getGraphCellKey(style.employeeId, style.dayOfMonth));
    return !currentPersistedStyle || !haveSameStyleValues(currentPersistedStyle, style);
  });

  if (stylesToDelete.length > 0) {
    await Promise.all(
      stylesToDelete
        .filter(style => style.id > 0)
        .map(style => containersApi.removeGraphCellStyle(containerId, graphId, style.id)),
    );
  }

  if (stylesToUpsert.length > 0) {
    await Promise.all(
      stylesToUpsert.map(style =>
        containersApi.upsertGraphCellStyle(containerId, graphId, {
          employeeId: style.employeeId,
          dayOfMonth: style.dayOfMonth,
          backgroundColorArgb: style.backgroundColorArgb ?? null,
          textColorArgb: style.textColorArgb ?? null,
        }),
      ),
    );
  }

  const savedStyles =
    stylesToDelete.length === 0 && stylesToUpsert.length === 0
      ? persistedApiStyles
      : await containersApi.listGraphCellStyles(containerId, graphId);

  queryClient.setQueryData(queryKeys.containers.graphCellStyles(containerId, graphId), savedStyles);
}

function getFullySelectedDayOfMonths(selectedCellKeys: string[], employeeIds: number[]) {
  if (selectedCellKeys.length === 0 || employeeIds.length === 0) {
    return [] as number[];
  }

  const validEmployeeIdSet = new Set(employeeIds);
  const selectedEmployeeIdsByDay = new Map<number, Set<number>>();

  selectedCellKeys.forEach(cellKey => {
    const { employeeId, dayOfMonth } = parseSelectedCellKey(cellKey);
    if (!validEmployeeIdSet.has(employeeId)) {
      return;
    }

    const employeeIdsForDay = selectedEmployeeIdsByDay.get(dayOfMonth) ?? new Set<number>();
    employeeIdsForDay.add(employeeId);
    selectedEmployeeIdsByDay.set(dayOfMonth, employeeIdsForDay);
  });

  return [...selectedEmployeeIdsByDay.entries()]
    .filter(([, selectedEmployeeIds]) => selectedEmployeeIds.size === employeeIds.length)
    .map(([dayOfMonth]) => dayOfMonth);
}

function haveSameStyleValues(left: GraphCellStyle | undefined, right: GraphCellStyle | undefined) {
  return (left?.backgroundColorArgb ?? null) === (right?.backgroundColorArgb ?? null)
    && (left?.textColorArgb ?? null) === (right?.textColorArgb ?? null);
}

const PRESET_MANAGED_FIELDS = new Set<keyof ContainerGraphFormState>([
  "name",
  "shopId",
  "year",
  "month",
  "peoplePerShift",
  "shift1Time",
  "shift2Time",
  "maxHoursPerEmpMonth",
  "maxConsecutiveDays",
  "maxConsecutiveFull",
  "maxFullPerMonth",
  "availabilityGroupId",
]);

const GENERATE_AVAILABILITY_GROUP_REQUIRED_MESSAGE = "Select an availability group before generating a schedule.";

export function ContainerGraphEditPage() {
  usePageScrollbarHidden();

  const location = useLocation();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { containerId: containerIdParam, graphId: graphIdParam } = useParams<{ containerId: string; graphId?: string }>();
  const parsedContainerId = containerIdParam ? Number(containerIdParam) : null;
  const parsedGraphId = graphIdParam ? Number(graphIdParam) : null;
  const containerId = Number.isFinite(parsedContainerId) ? parsedContainerId : null;
  const graphId = Number.isFinite(parsedGraphId) ? parsedGraphId : null;
  const isCreate = graphId === null;

  const containerQuery = useContainerByIdQuery(containerId);
  const containerGraphsQuery = useContainerGraphsQuery(containerId, containerId !== null);
  const graphQuery = useGraphByIdQuery(containerId, graphId);
  const graphEmployeesQuery = useGraphEmployeesQuery(containerId, graphId);
  const slotsQuery = useGraphSlotsQuery(containerId, graphId);
  const cellStylesQuery = useGraphCellStylesQuery(containerId, graphId);
  const schedulePresetsQuery = useSchedulePresetsQuery(containerId);
  const shiftSwapLogQuery = useGraphShiftSwapLogQuery(containerId, graphId, !isCreate);
  const availabilityGroupsQuery = useAvailabilityGroupsListQuery(location.key);
  const bindsQuery = useAvailabilityBindsListQuery();
  const employeesQuery = useEmployeesListQuery({ refreshKey: location.key });
  const shopsQuery = useShopsListQuery({ refreshKey: location.key });
  const saveWorkspaceMutation = useSaveGraphWorkspaceMutation();
  const createSchedulePresetMutation = useCreateSchedulePresetMutation();
  const generateMutation = useGenerateGraphPreviewMutation();
  const createBindMutation = useCreateAvailabilityBindMutation();
  const updateBindMutation = useUpdateAvailabilityBindMutation();
  const deleteBindMutation = useDeleteAvailabilityBindMutation();
  const createManualShiftSwapMutation = useCreateManagerManualShiftSwapMutation();
  const cancelManualShiftSwapMutation = useCancelManagerManualShiftSwapMutation();

  const [form, setForm] = useState<ContainerGraphFormState>(() => createInitialGraphForm());
  const [formErrors, setFormErrors] = useState<ContainerGraphFormErrors>({});
  const [graphEmployeeRows, setGraphEmployeeRows] = useState<Array<{ id: number | null; employeeId: number; minHoursMonth: string }>>([]);
  const [selectedSchedulePresetId, setSelectedSchedulePresetId] = useState<number | null>(null);
  const [selectedEmployeeId, setSelectedEmployeeId] = useState<number | null>(null);
  const [isCompactMatrix, setIsCompactMatrix] = useState(false);
  const [cellMap, setCellMap] = useState<Record<string, string>>({});
  const [cellErrors, setCellErrors] = useState<Record<string, string>>({});
  const [submitError, setSubmitError] = useState<string | undefined>();
  const [manualShiftPublishError, setManualShiftPublishError] = useState<string | null>(null);
  const [hydratedKey, setHydratedKey] = useState<string | null>(null);
  const [isHeaderCollapsed, setIsHeaderCollapsed] = useState(false);
  const [selectedCellKeys, setSelectedCellKeys] = useState<string[]>([]);
  const [previewSelectedCellKeys, setPreviewSelectedCellKeys] = useState<string[]>([]);
  const [styleRecords, setStyleRecords] = useState<GraphCellStyle[]>([]);
  const [autoAvailabilityStyleSuppressions, setAutoAvailabilityStyleSuppressions] =
    useState<GraphAutoAvailabilityStyleSuppressionMap>({});
  const [fillColor, setFillColor] = useState("#dbeafe");
  const [textColor, setTextColor] = useState("#0f172a");
  const [previewAvailabilitySelection, setPreviewAvailabilitySelection] = useState(FOLLOW_SCHEDULE_DETAILS_PREVIEW);
  const [bindError, setBindError] = useState<string | undefined>();
  const [bindRows, setBindRows] = useState<EditableAvailabilityBind[]>([]);
  const [manualColumns, setManualColumns] = useState<EditableScheduleManualColumn[]>([]);
  const [pendingManualShiftPublishes, setPendingManualShiftPublishes] = useState<PendingManualColumnShiftPublication[]>([]);
  const [scheduleColumnOrder, setScheduleColumnOrder] = useState<number[]>([]);
  const [selectedBindClientId, setSelectedBindClientId] = useState<string | null>(null);
  const [hasLocalBindChanges, setHasLocalBindChanges] = useState(false);
  const [bindDeleteTarget, setBindDeleteTarget] = useState<EditableAvailabilityBind | null>(null);
  const [isSaveConfirmOpen, setIsSaveConfirmOpen] = useState(false);
  const [isSessionSaving, setIsSessionSaving] = useState(false);
  const optimisticStyleIdRef = useRef(-1);
  const styleRecordsRef = useRef<GraphCellStyle[]>([]);
  const syncedAvailabilityGroupIdRef = useRef<number | null>(null);
  const sessionDraftsRef = useRef<Record<number, GraphEditorSessionDraft>>({});
  const savedGraphSnapshotByIdRef = useRef<Record<number, string>>({});

  const employeesById = useMemo(
    () => new Map((employeesQuery.data ?? []).map(employee => [employee.id, employee])),
    [employeesQuery.data],
  );
  const employeeNameById = useMemo(
    () => new Map((employeesQuery.data ?? []).map(employee => [employee.id, getEmployeeFullName(employee)])),
    [employeesQuery.data],
  );

  useEffect(() => {
    if (isCreate) {
      const createKey = `create:${containerId ?? "none"}`;
      if (hydratedKey === createKey) {
        return;
      }

      setForm(createInitialGraphForm(shopsQuery.data?.[0]?.id ?? null));
      setFormErrors({});
      setGraphEmployeeRows([]);
      setSelectedSchedulePresetId(null);
      setSelectedEmployeeId(null);
      setCellMap({});
      setCellErrors({});
      setSubmitError(undefined);
      setManualShiftPublishError(null);
      setSelectedCellKeys([]);
      setManualColumns([]);
      setPendingManualShiftPublishes([]);
      setScheduleColumnOrder([]);
      setStyleRecords([]);
      setAutoAvailabilityStyleSuppressions({});
      styleRecordsRef.current = [];
      syncedAvailabilityGroupIdRef.current = null;
      setFillColor("#dbeafe");
      setTextColor("#0f172a");
      setPreviewAvailabilitySelection(FOLLOW_SCHEDULE_DETAILS_PREVIEW);
      setHydratedKey(createKey);
      return;
    }

    if (graphId !== null) {
      const sessionDraft = sessionDraftsRef.current[graphId];
      if (sessionDraft) {
        const restoredDraft = cloneGraphEditorSessionDraft(sessionDraft);

        setForm(restoredDraft.form);
        setFormErrors({});
        setGraphEmployeeRows(restoredDraft.graphEmployeeRows);
        setSelectedSchedulePresetId(restoredDraft.selectedSchedulePresetId);
        setSelectedEmployeeId(restoredDraft.selectedEmployeeId);
        setCellMap(restoredDraft.cellMap);
        setCellErrors({});
        setSubmitError(undefined);
        setManualShiftPublishError(null);
        setSelectedCellKeys(restoredDraft.selectedCellKeys);
        setManualColumns(restoredDraft.manualColumns);
        setPendingManualShiftPublishes(restoredDraft.pendingManualShiftPublishes);
        setScheduleColumnOrder(restoredDraft.scheduleColumnOrder);
        setStyleRecords(restoredDraft.styleRecords);
        setAutoAvailabilityStyleSuppressions(restoredDraft.autoAvailabilityStyleSuppressions);
        styleRecordsRef.current = restoredDraft.styleRecords;
        syncedAvailabilityGroupIdRef.current = restoredDraft.syncedAvailabilityGroupId;
        setFillColor(restoredDraft.fillColor);
        setTextColor(restoredDraft.textColor);
        setPreviewAvailabilitySelection(restoredDraft.previewAvailabilitySelection ?? FOLLOW_SCHEDULE_DETAILS_PREVIEW);
        setHydratedKey(String(graphId));
        return;
      }
    }

    if (!graphQuery.data || !graphEmployeesQuery.data || !slotsQuery.data || !cellStylesQuery.data) {
      return;
    }

    const nextHydratedKey = String(graphQuery.data.id);
    if (hydratedKey === nextHydratedKey) {
      return;
    }

    const parsedGraphNote = parseGraphNoteContent(graphQuery.data.note);
    const editableEmployeeRows = createEditableEmployeeRows(graphEmployeesQuery.data, employeesById);
    const editableManualColumns = parsedGraphNote.manualColumns.map(toEditableScheduleManualColumn);
    const nextScheduleColumnOrder = sanitizeScheduleColumnOrder(
      parsedGraphNote.columnOrder,
      editableEmployeeRows,
      editableManualColumns,
    );

    setForm({
      ...createGraphFormFromGraph(graphQuery.data),
      note: parsedGraphNote.note,
    });
    setFormErrors({});
    setGraphEmployeeRows(editableEmployeeRows);
    setSelectedSchedulePresetId(null);
    setSelectedEmployeeId(editableEmployeeRows[0]?.employeeId ?? null);
    setCellMap({
      ...buildGraphCellMap(slotsQuery.data),
      ...rehydrateGraphNoteTextCells(parsedGraphNote.textCells),
    });
    setCellErrors({});
    setSubmitError(undefined);
    setManualShiftPublishError(null);
    setSelectedCellKeys([]);
    setManualColumns(sortManualColumnsByColumnOrder(editableManualColumns, nextScheduleColumnOrder));
    setPendingManualShiftPublishes([]);
    setScheduleColumnOrder(nextScheduleColumnOrder);
    setAutoAvailabilityStyleSuppressions(parsedGraphNote.autoAvailabilityStyleSuppressions);
    const hydratedStyles = mergeNormalizedStyleRecords(
      cellStylesQuery.data,
      rehydrateGraphNoteCellStyles(parsedGraphNote.cellStyles, graphQuery.data.id),
    );
    setStyleRecords(hydratedStyles);
    styleRecordsRef.current = hydratedStyles;
    syncedAvailabilityGroupIdRef.current = graphQuery.data.availabilityGroupId ?? null;
    setFillColor("#dbeafe");
    setTextColor("#0f172a");
    setPreviewAvailabilitySelection(FOLLOW_SCHEDULE_DETAILS_PREVIEW);
    setHydratedKey(nextHydratedKey);
  }, [
    cellStylesQuery.data,
    containerId,
    employeesById,
    graphEmployeesQuery.data,
    graphId,
    graphQuery.data,
    hydratedKey,
    isCreate,
    shopsQuery.data,
    slotsQuery.data,
  ]);

  const monthValue = parseIntegerField(form.month);
  const yearValue = parseIntegerField(form.year);
  const matchingAvailabilityGroups = useMemo(() => {
    if (monthValue === null || yearValue === null) {
      return [];
    }

    return (availabilityGroupsQuery.data ?? []).filter(group => group.month === monthValue && group.year === yearValue);
  }, [availabilityGroupsQuery.data, monthValue, yearValue]);

  const availabilityGroupIdSet = useMemo(
    () => new Set(matchingAvailabilityGroups.map(group => group.id)),
    [matchingAvailabilityGroups],
  );
  const shopIdSet = useMemo(
    () => new Set((shopsQuery.data ?? []).map(shop => shop.id)),
    [shopsQuery.data],
  );

  useEffect(() => {
    if (!form.availabilityGroupId) {
      return;
    }

    const availabilityGroupId = Number(form.availabilityGroupId);
    if (availabilityGroupIdSet.has(availabilityGroupId)) {
      return;
    }

    setForm(current => ({ ...current, availabilityGroupId: "" }));
  }, [availabilityGroupIdSet, form.availabilityGroupId]);

  useEffect(() => {
    if (!bindsQuery.data || hasLocalBindChanges) {
      return;
    }

    const nextBindRows = bindsQuery.data.map(toEditableAvailabilityBind);
    setBindRows(nextBindRows);
    setSelectedBindClientId(currentSelection => {
      if (currentSelection && nextBindRows.some(bind => bind.clientId === currentSelection)) {
        return currentSelection;
      }

      return nextBindRows[0]?.clientId ?? null;
    });
  }, [bindsQuery.data, hasLocalBindChanges]);

  useEffect(() => {
    const employeeIds = graphEmployeeRows.map(row => row.employeeId);
    const sanitizedCellMap = sanitizeGraphCellMap(
      cellMap,
      employeeIds,
      Number(form.year) || new Date().getFullYear(),
      Number(form.month) || 1,
    );

    if (JSON.stringify(sanitizedCellMap) !== JSON.stringify(cellMap)) {
      setCellMap(sanitizedCellMap);
    }
  }, [cellMap, form.month, form.year, graphEmployeeRows]);

  useEffect(() => {
    if (hydratedKey === null) {
      return;
    }

    const daysInMonth = getGraphDaysInMonth(Number(form.year) || new Date().getFullYear(), Number(form.month) || 1);
    const validEmployeeIds = new Set([
      GRAPH_DAY_STYLE_EMPLOYEE_ID,
      ...graphEmployeeRows.map(row => row.employeeId),
      ...manualColumns.map(column => buildManualColumnEmployeeId(column.columnId)),
    ]);
    const nextStyleRecords = styleRecordsRef.current.filter(style =>
      validEmployeeIds.has(style.employeeId) &&
      style.dayOfMonth >= 1 &&
      style.dayOfMonth <= daysInMonth,
    );

    if (nextStyleRecords.length === styleRecordsRef.current.length) {
      return;
    }

    syncStyleRecords(nextStyleRecords);
  }, [form.month, form.year, graphEmployeeRows, hydratedKey, manualColumns]);

  useEffect(() => {
    const nextColumnOrder = sanitizeScheduleColumnOrder(scheduleColumnOrder, graphEmployeeRows, manualColumns);
    if (!arraysEqual(nextColumnOrder, scheduleColumnOrder)) {
      setScheduleColumnOrder(nextColumnOrder);
    }
  }, [graphEmployeeRows, manualColumns, scheduleColumnOrder]);

  useEffect(() => {
    styleRecordsRef.current = styleRecords;
  }, [styleRecords]);

  const orderedGraphEmployeeRows = useMemo(
    () => sortGraphEmployeeRowsByColumnOrder(graphEmployeeRows, scheduleColumnOrder),
    [graphEmployeeRows, scheduleColumnOrder],
  );
  const draftGraphEmployees = useMemo(
    () =>
      orderedGraphEmployeeRows.map((row, index) => ({
        id: row.id ?? 0,
        scheduleId: graphId ?? 0,
        employeeId: row.employeeId,
        minHoursMonth: row.minHoursMonth ? Number(row.minHoursMonth) : null,
        displayOrder: index,
      })),
    [graphId, orderedGraphEmployeeRows],
  );

  const draftSlots = useMemo(() => {
    if (monthValue === null || yearValue === null) {
      return { slots: slotsQuery.data ?? [], errors: {} };
    }

    return buildGraphDraftSlots({
      scheduleId: graphId ?? 0,
      existingSlots: slotsQuery.data ?? [],
      employeeIds: orderedGraphEmployeeRows.map(row => row.employeeId),
      year: yearValue,
      month: monthValue,
      cellMap,
    });
  }, [cellMap, graphId, monthValue, orderedGraphEmployeeRows, slotsQuery.data, yearValue]);

  const effectiveSlots = useMemo(
    () => (Object.keys(draftSlots.errors).length === 0 ? draftSlots.slots : slotsQuery.data ?? []),
    [draftSlots.errors, draftSlots.slots, slotsQuery.data],
  );
  const baseScheduleColumns = useMemo(
    () => buildGraphMatrixColumns(draftGraphEmployees, employeesById, effectiveSlots),
    [draftGraphEmployees, effectiveSlots, employeesById],
  );
  const manualScheduleColumns = useMemo<GraphMatrixColumn[]>(
    () =>
      manualColumns.map(column => ({
        employeeId: buildManualColumnEmployeeId(column.columnId),
        kind: "manual",
        manualColumnId: column.columnId,
        graphEmployeeId: null,
        label: column.label,
        minHoursMonth: null,
        totalMinutes: 0,
        totalText: "",
      })),
    [manualColumns],
  );
  const daysInDisplayedMonth = useMemo(
    () => getGraphDaysInMonth(Number(form.year) || new Date().getFullYear(), Number(form.month) || 1),
    [form.month, form.year],
  );
  const manualCellMap = useMemo(() => {
    return manualColumns.reduce<Record<string, string>>((accumulator, column) => {
      const manualEmployeeId = buildManualColumnEmployeeId(column.columnId);

      for (let dayOfMonth = 1; dayOfMonth <= daysInDisplayedMonth; dayOfMonth += 1) {
        const value = normalizeManualColumnCellValue(column.cells[String(dayOfMonth)] ?? "");
        if (value === null) {
          continue;
        }

        accumulator[getGraphCellKey(manualEmployeeId, dayOfMonth)] = value;
      }

      return accumulator;
    }, {});
  }, [daysInDisplayedMonth, manualColumns]);
  const scheduleColumns = useMemo(
    () => {
      const columnByEmployeeId = new Map(
        [...baseScheduleColumns, ...manualScheduleColumns].map(column => [column.employeeId, column] as const),
      );

      return sanitizeScheduleColumnOrder(scheduleColumnOrder, graphEmployeeRows, manualColumns)
        .map(columnId => columnByEmployeeId.get(columnId))
        .filter((column): column is GraphMatrixColumn => Boolean(column));
    },
    [baseScheduleColumns, graphEmployeeRows, manualColumns, manualScheduleColumns, scheduleColumnOrder],
  );
  const matrixCellMap = useMemo(
    () => ({ ...cellMap, ...manualCellMap }),
    [cellMap, manualCellMap],
  );
  const orderedEmployeeIds = useMemo(
    () => scheduleColumns.map(column => column.employeeId),
    [scheduleColumns],
  );
  const styleMap = useMemo(
    () => buildGraphStyleMap(styleRecords),
    [styleRecords],
  );
  const selectedBindRow = useMemo(
    () => bindRows.find(bind => bind.clientId === selectedBindClientId) ?? null,
    [bindRows, selectedBindClientId],
  );
  const activeBindValueByKey = useMemo(() => buildActiveAvailabilityBindMap(bindRows), [bindRows]);
  const bindDeleteLabel = bindDeleteTarget?.key.trim() || "this bind";

  const effectiveGraph = useMemo(
    () => ({
      ...(graphQuery.data ?? {
        id: graphId ?? 0,
        containerId: containerId ?? 0,
        shopId: Number(form.shopId) || 0,
        name: form.name,
        year: Number(form.year) || new Date().getFullYear(),
        month: Number(form.month) || 1,
        publicationStatus: form.publicationStatus,
        peoplePerShift: Number(form.peoplePerShift) || 1,
        shift1Time: form.shift1Time,
        shift2Time: form.shift2Time,
        maxHoursPerEmpMonth: Number(form.maxHoursPerEmpMonth) || 0,
        maxConsecutiveDays: Number(form.maxConsecutiveDays) || 0,
        maxConsecutiveFull: Number(form.maxConsecutiveFull) || 0,
        maxFullPerMonth: Number(form.maxFullPerMonth) || 0,
        note: form.note,
        availabilityGroupId: form.availabilityGroupId ? Number(form.availabilityGroupId) : null,
      }),
      shopId: Number(form.shopId) || 0,
      name: form.name,
      year: Number(form.year) || new Date().getFullYear(),
      month: Number(form.month) || 1,
      publicationStatus: form.publicationStatus,
      peoplePerShift: Number(form.peoplePerShift) || 1,
      shift1Time: form.shift1Time,
      shift2Time: form.shift2Time,
      maxHoursPerEmpMonth: Number(form.maxHoursPerEmpMonth) || 0,
      maxConsecutiveDays: Number(form.maxConsecutiveDays) || 0,
      maxConsecutiveFull: Number(form.maxConsecutiveFull) || 0,
      maxFullPerMonth: Number(form.maxFullPerMonth) || 0,
      note: form.note,
      availabilityGroupId: form.availabilityGroupId ? Number(form.availabilityGroupId) : null,
    }),
    [containerId, form, graphId, graphQuery.data],
  );
  const persistedGraphSnapshot = useMemo(() => {
    if (!graphQuery.data || !graphEmployeesQuery.data || !slotsQuery.data || !cellStylesQuery.data) {
      return null;
    }

    const parsedGraphNote = parseGraphNoteContent(graphQuery.data.note);
    const editableEmployeeRows = createEditableEmployeeRows(graphEmployeesQuery.data, employeesById);
    const editableManualColumns = parsedGraphNote.manualColumns.map(toEditableScheduleManualColumn);
    const nextScheduleColumnOrder = sanitizeScheduleColumnOrder(
      parsedGraphNote.columnOrder,
      editableEmployeeRows,
      editableManualColumns,
    );
    const hydratedStyles = mergeNormalizedStyleRecords(
      cellStylesQuery.data,
      rehydrateGraphNoteCellStyles(parsedGraphNote.cellStyles, graphQuery.data.id),
    );

    return buildGraphWorkspaceSnapshot({
      form: {
        ...createGraphFormFromGraph(graphQuery.data),
        note: parsedGraphNote.note,
      },
      graphEmployeeRows: editableEmployeeRows,
      manualColumns: sortManualColumnsByColumnOrder(editableManualColumns, nextScheduleColumnOrder),
      pendingManualShiftPublishes: [],
      scheduleColumnOrder: nextScheduleColumnOrder,
      styleRecords: hydratedStyles,
      cellMap: {
        ...buildGraphCellMap(slotsQuery.data),
        ...rehydrateGraphNoteTextCells(parsedGraphNote.textCells),
      },
      autoAvailabilityStyleSuppressions: parsedGraphNote.autoAvailabilityStyleSuppressions,
    });
  }, [
    cellStylesQuery.data,
    employeesById,
    graphEmployeesQuery.data,
    graphQuery.data,
    slotsQuery.data,
  ]);
  const createModeSnapshot = useMemo(
    () =>
      buildGraphWorkspaceSnapshot({
        form: createInitialGraphForm(shopsQuery.data?.[0]?.id ?? null),
        graphEmployeeRows: [],
        manualColumns: [],
        pendingManualShiftPublishes: [],
        scheduleColumnOrder: [],
        styleRecords: [],
        cellMap: {},
        autoAvailabilityStyleSuppressions: {},
      }),
    [shopsQuery.data],
  );
  const currentGraphSnapshot = useMemo(
    () =>
      buildGraphWorkspaceSnapshot({
        form,
        graphEmployeeRows,
        manualColumns,
        pendingManualShiftPublishes,
        scheduleColumnOrder,
        styleRecords,
        cellMap,
        autoAvailabilityStyleSuppressions,
      }),
    [
      autoAvailabilityStyleSuppressions,
      cellMap,
      form,
      graphEmployeeRows,
      manualColumns,
      pendingManualShiftPublishes,
      scheduleColumnOrder,
      styleRecords,
    ],
  );
  const currentGraphBaselineSnapshot = isCreate
    ? createModeSnapshot
    : (graphId !== null ? (savedGraphSnapshotByIdRef.current[graphId] ?? persistedGraphSnapshot) : null);
  const hasDirtySessionDrafts =
    Object.values(sessionDraftsRef.current).some(draft => {
      if (draft.graphId === graphId) {
        return false;
      }

      const savedSnapshot = savedGraphSnapshotByIdRef.current[draft.graphId];
      return savedSnapshot ? buildGraphDraftSnapshot(draft) !== savedSnapshot : false;
    });
  const hasUnsavedScheduleChanges = hydratedKey !== null && Boolean(currentGraphBaselineSnapshot) && (
    currentGraphSnapshot !== currentGraphBaselineSnapshot ||
    hasDirtySessionDrafts
  );
  const hasUnsavedChanges = hasUnsavedScheduleChanges || (
    hydratedKey !== null &&
    Boolean(currentGraphBaselineSnapshot) &&
    hasPendingBindDraftChanges(bindRows)
  );

  useEffect(() => {
    if (graphId === null || !persistedGraphSnapshot) {
      return;
    }

    const savedSnapshot = savedGraphSnapshotByIdRef.current[graphId];
    if (!savedSnapshot || currentGraphSnapshot === persistedGraphSnapshot) {
      savedGraphSnapshotByIdRef.current[graphId] = persistedGraphSnapshot;
    }
  }, [currentGraphSnapshot, graphId, persistedGraphSnapshot]);

  useEffect(() => {
    if (
      isCreate ||
      graphId === null ||
      !persistedGraphSnapshot ||
      !currentGraphBaselineSnapshot ||
      currentGraphSnapshot !== currentGraphBaselineSnapshot ||
      persistedGraphSnapshot === currentGraphBaselineSnapshot ||
      !graphQuery.data ||
      !graphEmployeesQuery.data ||
      !slotsQuery.data ||
      !cellStylesQuery.data
    ) {
      return;
    }

    const parsedGraphNote = parseGraphNoteContent(graphQuery.data.note);
    const editableEmployeeRows = createEditableEmployeeRows(graphEmployeesQuery.data, employeesById);
    const editableManualColumns = parsedGraphNote.manualColumns.map(toEditableScheduleManualColumn);
    const nextScheduleColumnOrder = sanitizeScheduleColumnOrder(
      parsedGraphNote.columnOrder,
      editableEmployeeRows,
      editableManualColumns,
    );
    const hydratedStyles = mergeNormalizedStyleRecords(
      cellStylesQuery.data,
      rehydrateGraphNoteCellStyles(parsedGraphNote.cellStyles, graphQuery.data.id),
    );

    setForm({
      ...createGraphFormFromGraph(graphQuery.data),
      note: parsedGraphNote.note,
    });
    setFormErrors({});
    setGraphEmployeeRows(editableEmployeeRows);
    setSelectedSchedulePresetId(null);
    setSelectedEmployeeId(editableEmployeeRows[0]?.employeeId ?? null);
    setCellMap({
      ...buildGraphCellMap(slotsQuery.data),
      ...rehydrateGraphNoteTextCells(parsedGraphNote.textCells),
    });
    setCellErrors({});
    setSubmitError(undefined);
    setManualShiftPublishError(null);
    setSelectedCellKeys([]);
    setManualColumns(sortManualColumnsByColumnOrder(editableManualColumns, nextScheduleColumnOrder));
    setPendingManualShiftPublishes([]);
    setScheduleColumnOrder(nextScheduleColumnOrder);
    setStyleRecords(hydratedStyles);
    setAutoAvailabilityStyleSuppressions(parsedGraphNote.autoAvailabilityStyleSuppressions);
    styleRecordsRef.current = hydratedStyles;
    syncedAvailabilityGroupIdRef.current = graphQuery.data.availabilityGroupId ?? null;
    setFillColor("#dbeafe");
    setTextColor("#0f172a");
    setPreviewAvailabilitySelection(FOLLOW_SCHEDULE_DETAILS_PREVIEW);
    delete sessionDraftsRef.current[graphId];
    savedGraphSnapshotByIdRef.current[graphId] = persistedGraphSnapshot;
  }, [
    cellStylesQuery.data,
    currentGraphBaselineSnapshot,
    currentGraphSnapshot,
    employeesById,
    graphEmployeesQuery.data,
    graphId,
    graphQuery.data,
    isCreate,
    persistedGraphSnapshot,
    slotsQuery.data,
  ]);

  const relatedGraphs = useMemo(
    () => (containerGraphsQuery.data ?? []).filter(item =>
      item.id !== graphId &&
      item.year === effectiveGraph.year &&
      item.month === effectiveGraph.month,
    ),
    [containerGraphsQuery.data, effectiveGraph.month, effectiveGraph.year, graphId],
  );
  const relatedGraphSlotsQuery = useGraphSlotsBatchQuery(
    containerId,
    relatedGraphs.map(item => item.id),
    containerId !== null,
  );
  const relatedScheduleHintData = useMemo(
    () => buildGraphRelatedScheduleHintData({
      currentGraph: effectiveGraph,
      columns: scheduleColumns,
      cellMap: matrixCellMap,
      relatedGraphs: relatedGraphs.map(relatedGraph => ({
        graph: relatedGraph,
        slots: relatedGraphSlotsQuery.data?.[relatedGraph.id] ?? [],
      })),
    }),
    [effectiveGraph, matrixCellMap, relatedGraphSlotsQuery.data, relatedGraphs, scheduleColumns],
  );
  const visualHintMap = relatedScheduleHintData.visualHintMap;
  const visualHintDetailMap = relatedScheduleHintData.detailMap;
  const hasExplicitSession = useMemo(
    () => new URLSearchParams(location.search).has("openGraphIds"),
    [location.search],
  );
  const openGraphIds = useMemo(() => {
    if (isCreate) {
      return [];
    }

    const requestedGraphIds = getGraphSessionIds(location.search, graphId);
    const availableGraphIdSet = new Set((containerGraphsQuery.data ?? []).map(item => item.id));

    if (availableGraphIdSet.size === 0) {
      return requestedGraphIds;
    }

    const filteredGraphIds = requestedGraphIds.filter(item => availableGraphIdSet.has(item));
    if (graphId !== null && availableGraphIdSet.has(graphId) && !filteredGraphIds.includes(graphId)) {
      filteredGraphIds.unshift(graphId);
    }

    return filteredGraphIds.length > 0 ? filteredGraphIds : (graphId !== null ? [graphId] : []);
  }, [containerGraphsQuery.data, graphId, isCreate, location.search]);
  const sessionGraphs = useMemo(
    () => resolveGraphSession(openGraphIds, containerGraphsQuery.data ?? [], graphQuery.data ?? null),
    [containerGraphsQuery.data, graphQuery.data, openGraphIds],
  );
  const sessionSearch = useMemo(
    () => (!isCreate && (openGraphIds.length > 1 || hasExplicitSession) ? buildGraphSessionSearch(openGraphIds) : ""),
    [hasExplicitSession, isCreate, openGraphIds],
  );
  const sessionGraphNames = useMemo(() => {
    const names = sessionGraphs.map(item => item.name.trim()).filter(Boolean);
    if (names.length > 0) {
      return names;
    }

    const fallbackName = effectiveGraph.name.trim();
    return fallbackName ? [fallbackName] : ["New schedule"];
  }, [effectiveGraph.name, sessionGraphs]);
  const showSessionTabs = !isCreate && (openGraphIds.length > 1 || hasExplicitSession);
  const visibleSessionTabCount = Math.min(Math.max(sessionGraphs.length, 1), 3);
  const pageHeaderMaxWidth = showSessionTabs
    ? `${860 + (visibleSessionTabCount * 122)}px`
    : "980px";
  const editLockTargets = useMemo(
    () => (
      !isCreate && containerId
        ? openGraphIds.map(openGraphId => ({
          resourceType: managerEditResourceTypes.schedule,
          resourceId: `${containerId}:${openGraphId}`,
          containerId,
          graphId: openGraphId,
        }))
        : []
    ),
    [containerId, isCreate, openGraphIds],
  );
  const { lockedByOtherState, isCheckingLocks } = useManagerEditLocks(editLockTargets);
  const editLockMessage = lockedByOtherState
    ? buildManagerEditLockMessage(lockedByOtherState, "This schedule")
    : null;
  const canEdit = !editLockMessage && !isCheckingLocks;

  const dayConflictMap = useMemo(
    () => buildGraphConflictDayMap(effectiveGraph, effectiveSlots),
    [effectiveGraph, effectiveSlots],
  );
  const totals = useMemo(
    () => buildGraphTotals(draftGraphEmployees, effectiveSlots, employeesById),
    [draftGraphEmployees, effectiveSlots, employeesById],
  );

  const scheduleAvailabilityGroupId = form.availabilityGroupId ? Number(form.availabilityGroupId) : null;
  const scheduleAvailabilityMembersQuery = useAvailabilityGroupMembersQuery(scheduleAvailabilityGroupId);
  const scheduleAvailabilitySlotsQuery = useAvailabilityGroupSlotsQuery(scheduleAvailabilityGroupId);
  const previewAvailabilityGroupId =
    previewAvailabilitySelection === FOLLOW_SCHEDULE_DETAILS_PREVIEW
      ? scheduleAvailabilityGroupId
      : Number(previewAvailabilitySelection);
  const previewMembersQuery = useAvailabilityGroupMembersQuery(previewAvailabilityGroupId);
  const previewSlotsQuery = useAvailabilityGroupSlotsQuery(previewAvailabilityGroupId);
  const selectedScheduleAvailabilityGroup = useMemo(
    () => matchingAvailabilityGroups.find(group => group.id === scheduleAvailabilityGroupId) ?? null,
    [matchingAvailabilityGroups, scheduleAvailabilityGroupId],
  );
  const previewAvailabilityOptions = useMemo(() => {
    const linkedHint =
      selectedScheduleAvailabilityGroup
        ? `${formatAvailabilityGroupPeriod(selectedScheduleAvailabilityGroup.year, selectedScheduleAvailabilityGroup.month)} - synced from Schedule Details`
        : "Use the availability selected in Schedule Details.";

    return [
      {
        value: FOLLOW_SCHEDULE_DETAILS_PREVIEW,
        label: selectedScheduleAvailabilityGroup ? `Auto: ${selectedScheduleAvailabilityGroup.name}` : "Auto: Schedule Details",
        hint: linkedHint,
        keywords: [
          "auto",
          "schedule details",
          "linked",
          "synced",
          selectedScheduleAvailabilityGroup?.name ?? "",
          selectedScheduleAvailabilityGroup ? formatAvailabilityGroupPeriod(selectedScheduleAvailabilityGroup.year, selectedScheduleAvailabilityGroup.month) : "",
        ].join(" "),
      },
      ...matchingAvailabilityGroups.map(group => ({
        value: String(group.id),
        label: group.name,
        hint: group.id === scheduleAvailabilityGroupId
          ? `${formatAvailabilityGroupPeriod(group.year, group.month)} - selected in Schedule Details`
          : formatAvailabilityGroupPeriod(group.year, group.month),
        keywords: `${group.id} ${group.name} ${group.month} ${group.year} ${formatAvailabilityGroupPeriod(group.year, group.month)}`,
      })),
    ];
  }, [matchingAvailabilityGroups, scheduleAvailabilityGroupId, selectedScheduleAvailabilityGroup]);
  const previewColumns = useMemo<AvailabilityMatrixColumn[]>(
    () => buildAvailabilityColumns(previewMembersQuery.data ?? [], employeeNameById),
    [employeeNameById, previewMembersQuery.data],
  );
  const autoAvailabilityUnavailableCellKeys = useMemo(() => {
    if (!scheduleAvailabilityGroupId || !scheduleAvailabilityMembersQuery.data || !scheduleAvailabilitySlotsQuery.data) {
      return [] as string[];
    }

    const eligibleEmployeeIds = new Set(graphEmployeeRows.map(row => row.employeeId));
    if (eligibleEmployeeIds.size === 0) {
      return [] as string[];
    }

    return Object.entries(
      buildAvailabilityCellMap(scheduleAvailabilityMembersQuery.data, scheduleAvailabilitySlotsQuery.data),
    )
      .filter(([cellKey, value]) => {
        if (value !== AVAILABILITY_NONE_MARK) {
          return false;
        }

        const { employeeId } = parseSelectedCellKey(cellKey);
        return eligibleEmployeeIds.has(employeeId);
      })
      .map(([cellKey]) => cellKey)
      .sort((left, right) => {
        const leftCell = parseSelectedCellKey(left);
        const rightCell = parseSelectedCellKey(right);

        if (leftCell.employeeId !== rightCell.employeeId) {
          return leftCell.employeeId - rightCell.employeeId;
        }

        return leftCell.dayOfMonth - rightCell.dayOfMonth;
      });
  }, [
    graphEmployeeRows,
    scheduleAvailabilityGroupId,
    scheduleAvailabilityMembersQuery.data,
    scheduleAvailabilitySlotsQuery.data,
  ]);
  const currentAutoAvailabilitySuppressionCellKeys = useMemo(
    () => getAutoAvailabilitySuppressionCellKeys(autoAvailabilityStyleSuppressions, scheduleAvailabilityGroupId),
    [autoAvailabilityStyleSuppressions, scheduleAvailabilityGroupId],
  );
  const currentAutoAvailabilitySuppressionCellKeySet = useMemo(
    () => new Set(currentAutoAvailabilitySuppressionCellKeys),
    [currentAutoAvailabilitySuppressionCellKeys],
  );
  const autoAvailabilityUnavailableCellKeySet = useMemo(
    () => new Set(autoAvailabilityUnavailableCellKeys),
    [autoAvailabilityUnavailableCellKeys],
  );
  const previewCellMap = useMemo<AvailabilityMatrixCellMap>(
    () => buildAvailabilityCellMap(previewMembersQuery.data ?? [], previewSlotsQuery.data ?? []),
    [previewMembersQuery.data, previewSlotsQuery.data],
  );
  const selectedPreviewAvailabilityGroup = useMemo(
    () => matchingAvailabilityGroups.find(group => group.id === previewAvailabilityGroupId) ?? null,
    [matchingAvailabilityGroups, previewAvailabilityGroupId],
  );
  const previewYear = selectedPreviewAvailabilityGroup?.year ?? yearValue ?? new Date().getFullYear();
  const previewMonth = selectedPreviewAvailabilityGroup?.month ?? monthValue ?? 1;
  const isSaving = saveWorkspaceMutation.isPending || isSessionSaving;
  const {
    dialog: unsavedChangesDialog,
    runWithoutPrompt,
  } = useUnsavedChangesPrompt({
    when: hasUnsavedChanges && !isSaving,
  });

  const handleEditLockDialogClose = () => {
    runWithoutPrompt(() => {
      if (containerId && graphId !== null) {
        navigate(`/container/${containerId}/graphs/${graphId}${sessionSearch}`);
        return;
      }

      if (containerId) {
        navigate(`/container?openContainerId=${containerId}`);
        return;
      }

      navigate("/container");
    });
  };

  useEffect(() => {
    if (previewAvailabilitySelection === FOLLOW_SCHEDULE_DETAILS_PREVIEW) {
      return;
    }

    const manualPreviewAvailabilityGroupId = Number(previewAvailabilitySelection);
    if (availabilityGroupIdSet.has(manualPreviewAvailabilityGroupId)) {
      return;
    }

    setPreviewAvailabilitySelection(FOLLOW_SCHEDULE_DETAILS_PREVIEW);
  }, [availabilityGroupIdSet, previewAvailabilitySelection]);

  useEffect(() => {
    if (!form.availabilityGroupId) {
      syncedAvailabilityGroupIdRef.current = null;
      return;
    }

    if (!scheduleAvailabilityMembersQuery.data) {
      return;
    }

    const availabilityGroupId = Number(form.availabilityGroupId);
    if (!Number.isInteger(availabilityGroupId) || syncedAvailabilityGroupIdRef.current === availabilityGroupId) {
      return;
    }

    const previewMembers = scheduleAvailabilityMembersQuery.data;
    setGraphEmployeeRows(currentRows => {
      const nextRows = buildEmployeeRowsFromAvailabilityMembers(currentRows, previewMembers);
      return graphEmployeeRowsEqual(currentRows, nextRows) ? currentRows : nextRows;
    });
    setSelectedSchedulePresetId(null);
    setSubmitError(undefined);
    syncedAvailabilityGroupIdRef.current = availabilityGroupId;
  }, [form.availabilityGroupId, scheduleAvailabilityMembersQuery.data]);

  useEffect(() => {
    if (autoAvailabilityUnavailableCellKeys.length === 0) {
      return;
    }

    const nextStyles = buildAutoAvailabilityUnavailableStyles({
      scheduleId: graphId ?? 0,
      autoCellKeys: autoAvailabilityUnavailableCellKeys,
      suppressedCellKeys: currentAutoAvailabilitySuppressionCellKeySet,
      existingStyles: styleRecordsRef.current,
      getNextStyleId: () => optimisticStyleIdRef.current--,
    });

    if (!nextStyles) {
      return;
    }

    syncStyleRecords(nextStyles);
  }, [
    autoAvailabilityUnavailableCellKeys,
    currentAutoAvailabilitySuppressionCellKeySet,
    graphId,
  ]);

  const isLoading =
    containerId !== null &&
    (
      containerQuery.isLoading ||
      shopsQuery.isLoading ||
      employeesQuery.isLoading ||
      availabilityGroupsQuery.isLoading ||
      (!isCreate && (graphQuery.isLoading || graphEmployeesQuery.isLoading || slotsQuery.isLoading || cellStylesQuery.isLoading))
    );
  const hasLoadError =
    containerId === null ||
    containerQuery.isError ||
    shopsQuery.isError ||
    employeesQuery.isError ||
    availabilityGroupsQuery.isError ||
    (!isCreate &&
      (
        graphQuery.isError ||
        graphEmployeesQuery.isError ||
        slotsQuery.isError ||
        cellStylesQuery.isError ||
        !graphQuery.data
      ));

  useEffect(() => {
    const employeeIdSet = new Set(orderedEmployeeIds);
    const daysInMonth = getGraphDaysInMonth(Number(form.year) || new Date().getFullYear(), Number(form.month) || 1);

    setSelectedCellKeys(currentSelection => {
      const nextSelection = currentSelection.filter(cellKey => {
        const { employeeId, dayOfMonth } = parseSelectedCellKey(cellKey);
        return employeeIdSet.has(employeeId) && dayOfMonth >= 1 && dayOfMonth <= daysInMonth;
      });

      return currentSelection.length === nextSelection.length &&
        currentSelection.every((cellKey, index) => cellKey === nextSelection[index])
        ? currentSelection
        : nextSelection;
    });
  }, [form.month, form.year, orderedEmployeeIds]);

  useEffect(() => {
    setPreviewSelectedCellKeys([]);
  }, [previewAvailabilityGroupId]);

  useEffect(() => {
    const previewEmployeeIdSet = new Set(previewColumns.map(column => column.employeeId));
    const daysInMonth = getGraphDaysInMonth(previewYear, previewMonth);

    setPreviewSelectedCellKeys(currentSelection => {
      const nextSelection = currentSelection.filter(cellKey => {
        const { employeeId, dayOfMonth } = parseSelectedCellKey(cellKey);
        return previewEmployeeIdSet.has(employeeId) && dayOfMonth >= 1 && dayOfMonth <= daysInMonth;
      });

      return currentSelection.length === nextSelection.length &&
        currentSelection.every((cellKey, index) => cellKey === nextSelection[index])
        ? currentSelection
        : nextSelection;
    });
  }, [previewColumns, previewMonth, previewYear]);

  const syncStyleRecords = (nextStyles: GraphCellStyle[]) => {
    styleRecordsRef.current = nextStyles;
    setStyleRecords(nextStyles);
  };
  const createCurrentGraphDraft = () => {
    if (graphId === null || hydratedKey !== String(graphId)) {
      return null;
    }

    return cloneGraphEditorSessionDraft({
      graphId,
      form,
      graphEmployeeRows,
      selectedSchedulePresetId,
      selectedEmployeeId,
      cellMap,
      manualColumns,
      pendingManualShiftPublishes,
      scheduleColumnOrder,
      selectedCellKeys,
      styleRecords: styleRecordsRef.current,
      autoAvailabilityStyleSuppressions,
      fillColor,
      textColor,
      syncedAvailabilityGroupId: syncedAvailabilityGroupIdRef.current,
      previewAvailabilitySelection,
    });
  };
  const storeCurrentGraphDraft = () => {
    const nextDraft = createCurrentGraphDraft();
    if (!nextDraft) {
      return null;
    }

    sessionDraftsRef.current[nextDraft.graphId] = nextDraft;
    return nextDraft;
  };
  const updateBindRows = (nextRows: EditableAvailabilityBind[]) => {
    setHasLocalBindChanges(true);
    setBindRows(nextRows);
  };

  const replaceBindRow = (clientId: string, updater: (row: EditableAvailabilityBind) => EditableAvailabilityBind) => {
    setHasLocalBindChanges(true);
    setBindRows(currentRows => currentRows.map(row => (row.clientId === clientId ? updater(row) : row)));
  };

  const removeBindRowLocally = (clientId: string) => {
    const currentIndex = bindRows.findIndex(bind => bind.clientId === clientId);
    const nextRows = bindRows.filter(bind => bind.clientId !== clientId);

    updateBindRows(nextRows);

    if (selectedBindClientId !== clientId) {
      return;
    }

    const nextSelectedBind = nextRows[currentIndex] ?? nextRows[currentIndex - 1] ?? nextRows[0] ?? null;
    setSelectedBindClientId(nextSelectedBind?.clientId ?? null);
  };

  const setFieldValue = (field: keyof ContainerGraphFormState) => (value: string) => {
    setForm(current => ({ ...current, [field]: value }));
    setSubmitError(undefined);

    if (PRESET_MANAGED_FIELDS.has(field)) {
      setSelectedSchedulePresetId(null);
    }

    if (field === "availabilityGroupId") {
      syncedAvailabilityGroupIdRef.current = null;
      setPreviewAvailabilitySelection(FOLLOW_SCHEDULE_DETAILS_PREVIEW);
    }

    setFormErrors(currentErrors => {
      if (!currentErrors[field]) {
        return currentErrors;
      }

      const nextErrors = { ...currentErrors };
      delete nextErrors[field];
      return nextErrors;
    });
  };

  const handleApplySchedulePreset = (presetId: number) => {
    const preset = schedulePresetsQuery.data?.find(item => item.id === presetId);
    if (!preset) {
      setSubmitError("Could not find the selected preset.");
      return;
    }

    setForm(current => ({
      ...current,
      name: preset.scheduleName,
      shopId: String(preset.shopId),
      year: String(preset.year),
      month: String(preset.month),
      peoplePerShift: String(preset.peoplePerShift),
      shift1Time: preset.shift1Time,
      shift2Time: preset.shift2Time,
      maxHoursPerEmpMonth: String(preset.maxHoursPerEmpMonth),
      maxConsecutiveDays: String(preset.maxConsecutiveDays),
      maxConsecutiveFull: String(preset.maxConsecutiveFull),
      maxFullPerMonth: String(preset.maxFullPerMonth),
    }));
    setFormErrors({});
    setCellErrors({});
    setSubmitError(undefined);
    setSelectedSchedulePresetId(preset.id);
  };

  const handleCreateSchedulePreset = async (payload: SaveSchedulePresetDto) => {
    if (!containerId) {
      throw new Error("Container is missing.");
    }

    const createdPreset = await runMutation(createSchedulePresetMutation.mutate, {
      containerId,
      payload,
    });

    void queryClient.invalidateQueries({
      queryKey: queryKeys.containers.schedulePresets(containerId),
    });
    setSelectedSchedulePresetId(createdPreset.id);
  };

  const handleAddEmployee = () => {
    setSubmitError(undefined);

    if (!selectedEmployeeId) {
      setSubmitError("Select an employee first.");
      return;
    }

    if (graphEmployeeRows.some(row => row.employeeId === selectedEmployeeId)) {
      setSubmitError("This employee is already added.");
      return;
    }

    setGraphEmployeeRows(current => [...current, { id: null, employeeId: selectedEmployeeId, minHoursMonth: "" }]);
    setScheduleColumnOrder(current => [...current, selectedEmployeeId]);
    setSelectedEmployeeId(null);
  };

  const handleRemoveEmployee = (employeeId: number) => {
    setGraphEmployeeRows(current => current.filter(row => row.employeeId !== employeeId));
    setScheduleColumnOrder(current => current.filter(columnId => columnId !== employeeId));
    setCellMap(current => {
      const next = { ...current };
      Object.keys(next).forEach(key => {
        if (key.startsWith(`${employeeId}:`)) {
          delete next[key];
        }
      });
      return next;
    });
    setSelectedCellKeys(currentSelection => currentSelection.filter(cellKey => !cellKey.startsWith(`${employeeId}:`)));
  };

  const handleColumnMove = (columnId: number, targetColumnId: number) => {
    const currentColumnOrder = sanitizeScheduleColumnOrder(scheduleColumnOrder, graphEmployeeRows, manualColumns);
    const nextColumnOrder = moveScheduleColumnOrder(currentColumnOrder, columnId, targetColumnId);

    if (arraysEqual(currentColumnOrder, nextColumnOrder)) {
      return;
    }

    setScheduleColumnOrder(nextColumnOrder);
    setGraphEmployeeRows(currentRows => sortGraphEmployeeRowsByColumnOrder(currentRows, nextColumnOrder));
    setManualColumns(currentColumns => sortManualColumnsByColumnOrder(currentColumns, nextColumnOrder));
    setSubmitError(undefined);
  };

  const handleValidate = ({ requireAvailabilityGroup = false }: { requireAvailabilityGroup?: boolean } = {}) => {
    const nextFormErrors = buildGraphFormErrors(form, shopIdSet, availabilityGroupIdSet);
    const nextCellErrors = draftSlots.errors;

    if (requireAvailabilityGroup && !form.availabilityGroupId) {
      nextFormErrors.availabilityGroupId = GENERATE_AVAILABILITY_GROUP_REQUIRED_MESSAGE;
    }

    setFormErrors(nextFormErrors);
    setCellErrors(nextCellErrors);

    if (graphEmployeeRows.length === 0) {
      setSubmitError("Add at least one employee to this schedule.");
      return false;
    }

    if (Object.keys(nextFormErrors).length > 0 || Object.keys(nextCellErrors).length > 0) {
      const shouldShowAvailabilityMessage =
        requireAvailabilityGroup &&
        nextFormErrors.availabilityGroupId === GENERATE_AVAILABILITY_GROUP_REQUIRED_MESSAGE &&
        Object.keys(nextFormErrors).length === 1 &&
        Object.keys(nextCellErrors).length === 0;

      setSubmitError(
        shouldShowAvailabilityMessage
          ? GENERATE_AVAILABILITY_GROUP_REQUIRED_MESSAGE
          : "Check highlighted fields before continuing.",
      );
      return false;
    }

    return true;
  };

  const saveGraphDraft = async (draft: GraphEditorSessionDraft) => {
    if (!containerId) {
      throw new Error("Container is missing.");
    }

    const orderedDraftRows = sortGraphEmployeeRowsByColumnOrder(draft.graphEmployeeRows, draft.scheduleColumnOrder);
    const isCurrentGraph = draft.graphId === graphId;
    const existingEmployees = isCurrentGraph
      ? (graphEmployeesQuery.data ?? [])
      : await containersApi.listGraphEmployees(containerId, draft.graphId);
    const existingSlots = isCurrentGraph
      ? (slotsQuery.data ?? [])
      : await containersApi.listGraphSlots(containerId, draft.graphId);

    const result = await runMutation(saveWorkspaceMutation.mutate, {
      containerId,
      graphId: draft.graphId,
      payload: toPayload(
        draft.form,
        draft.graphEmployeeRows,
        draft.manualColumns,
        draft.scheduleColumnOrder,
        draft.styleRecords,
        draft.cellMap,
        draft.autoAvailabilityStyleSuppressions,
      ),
      employeeAssignments: orderedDraftRows.map(row => ({
        id: row.id,
        employeeId: row.employeeId,
        minHoursMonth: row.minHoursMonth ? Number(row.minHoursMonth) : null,
      })),
      cellMap: draft.cellMap,
      existingEmployees,
      existingSlots,
    });

    await persistGraphStyleDraft({
      containerId,
      graphId: result.graphId,
      styleRecords: draft.styleRecords,
      queryClient,
    });

    for (const pendingShift of draft.pendingManualShiftPublishes) {
      await runMutation(createManualShiftSwapMutation.mutate, {
        containerId,
        graphId: result.graphId,
        manualColumnId: pendingShift.manualColumnId,
        dayOfMonth: pendingShift.dayOfMonth,
        fromTime: pendingShift.fromTime,
        toTime: pendingShift.toTime,
        targetEmployeeId: pendingShift.targetEmployeeId ?? null,
      });
    }

    savedGraphSnapshotByIdRef.current[result.graphId] = buildGraphDraftSnapshot({
      ...draft,
      graphId: result.graphId,
      pendingManualShiftPublishes: [],
    });

    return result.graphId;
  };

  const handleGenerateValidate = (
    generationRows: Array<{ id: number | null; employeeId: number; minHoursMonth: string }>,
  ) => {
    const nextFormErrors = buildGraphFormErrors(form, shopIdSet, availabilityGroupIdSet);

    if (!form.availabilityGroupId) {
      nextFormErrors.availabilityGroupId = GENERATE_AVAILABILITY_GROUP_REQUIRED_MESSAGE;
    }

    setFormErrors(nextFormErrors);
    setCellErrors({});

    if (generationRows.length === 0) {
      setSubmitError("No employees found for selected availability group.");
      return false;
    }

    if (Object.keys(nextFormErrors).length > 0) {
      const shouldShowAvailabilityMessage =
        nextFormErrors.availabilityGroupId === GENERATE_AVAILABILITY_GROUP_REQUIRED_MESSAGE &&
        Object.keys(nextFormErrors).length === 1;

      setSubmitError(
        shouldShowAvailabilityMessage
          ? GENERATE_AVAILABILITY_GROUP_REQUIRED_MESSAGE
          : "Check highlighted fields before continuing.",
      );
      return false;
    }

    return true;
  };

  const handleSave = async () => {
    setIsSaveConfirmOpen(false);

    if (editLockMessage) {
      setSubmitError(editLockMessage);
      return;
    }

    if (isCheckingLocks) {
      setSubmitError("Checking edit access. Please wait a moment.");
      return;
    }

    if (!containerId || !handleValidate()) {
      return;
    }

    setSubmitError(undefined);

    try {
      setIsSessionSaving(true);

      if (isCreate) {
        const result = await runMutation(saveWorkspaceMutation.mutate, {
          containerId,
          graphId,
          payload: toPayload(
            form,
            graphEmployeeRows,
            manualColumns,
            scheduleColumnOrder,
            styleRecordsRef.current,
            cellMap,
            autoAvailabilityStyleSuppressions,
          ),
          employeeAssignments: orderedGraphEmployeeRows.map(row => ({
            id: row.id,
            employeeId: row.employeeId,
            minHoursMonth: row.minHoursMonth ? Number(row.minHoursMonth) : null,
          })),
          cellMap,
          existingEmployees: graphEmployeesQuery.data ?? [],
          existingSlots: slotsQuery.data ?? [],
        });

        await persistGraphStyleDraft({
          containerId,
          graphId: result.graphId,
          styleRecords: styleRecordsRef.current,
          queryClient,
        });
        runWithoutPrompt(() => navigate(`/container/${containerId}/graphs/${result.graphId}`));
        return;
      }

      const currentDraft = createCurrentGraphDraft();
      const draftsToSave = new Map<number, GraphEditorSessionDraft>();

      Object.values(sessionDraftsRef.current).forEach(draft => {
        draftsToSave.set(draft.graphId, cloneGraphEditorSessionDraft(draft));
      });

      if (currentDraft) {
        draftsToSave.set(currentDraft.graphId, currentDraft);
      }

      if (graphId !== null && !draftsToSave.has(graphId)) {
        throw new Error("Current schedule draft is not ready yet.");
      }

      const graphIdsToSave = showSessionTabs
        ? openGraphIds.filter(openGraphId => draftsToSave.has(openGraphId))
        : (currentDraft ? [currentDraft.graphId] : []);

      if (graphIdsToSave.length === 0) {
        throw new Error("Nothing is ready to save yet.");
      }

      for (const openGraphId of graphIdsToSave) {
        const draft = draftsToSave.get(openGraphId);
        if (!draft) {
          continue;
        }

        await saveGraphDraft(draft);
      }

      if (graphId !== null) {
        runWithoutPrompt(() => navigate(`/container/${containerId}/graphs/${graphId}${sessionSearch}`));
        return;
      }

      runWithoutPrompt(() => navigate(`/container?openContainerId=${containerId}`));
    } catch (error) {
      if (error instanceof ApiError) {
        setFormErrors(currentErrors => ({ ...currentErrors, ...applyGraphApiErrors(error.validationErrors) }));
        setSubmitError(error.message);
        return;
      }

      setSubmitError(error instanceof Error ? error.message : "Could not save this schedule.");
    } finally {
      setIsSessionSaving(false);
    }
  };

  const handleGenerate = async () => {
    if (!containerId) {
      return;
    }

    const generationRows = form.availabilityGroupId && scheduleAvailabilityMembersQuery.data
      ? buildEmployeeRowsFromAvailabilityMembers(graphEmployeeRows, scheduleAvailabilityMembersQuery.data ?? [])
      : [];

    if (!handleGenerateValidate(generationRows)) {
      return;
    }

    if (!graphEmployeeRowsEqual(graphEmployeeRows, generationRows)) {
      setGraphEmployeeRows(generationRows);
    }

    setSubmitError(undefined);

    try {
      const generated = await runMutation(generateMutation.mutate, {
        containerId,
        payload: {
          graphId,
          graph: toPayload(
            form,
            graphEmployeeRows,
            manualColumns,
            scheduleColumnOrder,
            styleRecordsRef.current,
            cellMap,
            autoAvailabilityStyleSuppressions,
          ),
          employees: generationRows.map((row, index) => ({
            employeeId: row.employeeId,
            minHoursMonth: row.minHoursMonth ? Number(row.minHoursMonth) : null,
            displayOrder: index,
          })),
        },
      });

      if (generated.slots) {
        setCellMap({
          ...buildGraphCellMap(generated.slots),
          ...rehydrateGraphNoteTextCells(
            serializeGraphNoteTextCells(
              cellMap,
              graphEmployeeRows.map(row => row.employeeId),
              Number(form.year) || new Date().getFullYear(),
              Number(form.month) || 1,
            ),
          ),
        });
        setCellErrors({});
      }
    } catch (error) {
      if (error instanceof ApiError) {
        setFormErrors(currentErrors => ({ ...currentErrors, ...applyGraphApiErrors(error.validationErrors) }));
        setSubmitError(error.message);
        return;
      }

      setSubmitError(error instanceof Error ? error.message : "Could not generate this schedule.");
    }
  };

  const handleBindFieldChange = (
    clientId: string,
    patch: Partial<Pick<EditableAvailabilityBind, "key" | "value" | "isActive">>,
  ) => {
    setBindError(undefined);
    replaceBindRow(clientId, row => ({ ...row, ...patch }));
  };

  const handleBindCommit = (clientId: string) => {
    const bind = bindRows.find(item => item.clientId === clientId);
    if (!bind) {
      return;
    }

    const trimmedKey = bind.key.trim();
    const trimmedValue = bind.value.trim();

    if (!trimmedKey && !trimmedValue) {
      return;
    }

    if (!trimmedKey || !trimmedValue) {
      setBindError("Bind key and value are required before the bind can be saved.");
      return;
    }

    const normalizedKey = normalizeBindKey(trimmedKey);
    if (!normalizedKey) {
      setBindError("Invalid bind key format.");
      return;
    }

    const duplicateBindExists = bindRows.some(item => item.clientId !== clientId && normalizeBindKey(item.key) === normalizedKey);
    if (duplicateBindExists) {
      setBindError(`Bind '${normalizedKey}' already exists.`);
      return;
    }

    const payload: SaveAvailabilityBindInput = {
      key: normalizedKey,
      value: trimmedValue,
      isActive: bind.isActive,
    };

    const isUnchanged =
      bind.id !== null &&
      bind.persistedKey === payload.key &&
      bind.persistedValue === payload.value &&
      bind.persistedIsActive === payload.isActive;

    if (isUnchanged) {
      replaceBindRow(clientId, row => ({
        ...row,
        key: payload.key,
        value: payload.value,
      }));
      return;
    }

    setBindError(undefined);

    if (bind.id === null) {
      void runMutation(createBindMutation.mutate, payload)
        .then(createdBind => {
          setHasLocalBindChanges(true);
          const nextBind = toEditableAvailabilityBind(createdBind);
          setBindRows(currentRows => currentRows.map(row => (row.clientId === clientId ? nextBind : row)));
          setSelectedBindClientId(currentSelection => (currentSelection === clientId ? nextBind.clientId : currentSelection));
        })
        .catch(error => {
          setBindError(toErrorMessage(error));
        });
      return;
    }

    void runMutation(updateBindMutation.mutate, { id: bind.id, payload })
      .then(() => {
        replaceBindRow(clientId, row => ({
          ...row,
          key: payload.key,
          value: payload.value,
          isActive: payload.isActive,
          persistedKey: payload.key,
          persistedValue: payload.value,
          persistedIsActive: payload.isActive,
        }));
      })
      .catch(error => {
        setBindError(toErrorMessage(error));
      });
  };

  const handleAddBind = () => {
    const nextBind = createDraftAvailabilityBind();
    setBindError(undefined);
    updateBindRows([...bindRows, nextBind]);
    setSelectedBindClientId(nextBind.clientId);
  };

  const handleDeleteBind = () => {
    setBindError(undefined);

    if (!selectedBindRow) {
      setBindError("Select bind first.");
      return;
    }

    setBindDeleteTarget(selectedBindRow);
  };

  const handleDeleteBindConfirm = () => {
    if (!bindDeleteTarget) {
      return;
    }

    setBindError(undefined);

    if (bindDeleteTarget.id === null) {
      removeBindRowLocally(bindDeleteTarget.clientId);
      setBindDeleteTarget(null);
      return;
    }

    void runMutation(deleteBindMutation.mutate, bindDeleteTarget.id)
      .then(() => {
        removeBindRowLocally(bindDeleteTarget.clientId);
        setBindDeleteTarget(null);
      })
      .catch(error => {
        setBindError(toErrorMessage(error));
      });
  };

  const handleApplyStyleProperty = (property: StyleProperty) => {
    if (selectedCellKeys.length === 0) {
      return;
    }

    const nextColorArgb = rgbHexToArgb(property === "fill" ? fillColor : textColor);
    if (nextColorArgb === null) {
      return;
    }

    const previousStyles = [...styleRecordsRef.current];
    const nextStyleByKey = buildStyleRecordByKey(previousStyles);

    selectedCellKeys
      .map(cellKey => ({ cellKey, ...parseSelectedCellKey(cellKey) }))
      .forEach(({ cellKey, employeeId, dayOfMonth }) => {
        const currentStyle = nextStyleByKey.get(cellKey);

        nextStyleByKey.set(cellKey, {
          id: currentStyle?.id ?? optimisticStyleIdRef.current--,
          scheduleId: graphId ?? 0,
          employeeId,
          dayOfMonth,
          backgroundColorArgb: property === "fill" ? nextColorArgb : currentStyle?.backgroundColorArgb ?? null,
          textColorArgb: property === "text" ? nextColorArgb : currentStyle?.textColorArgb ?? null,
        });
      });

    getFullySelectedDayOfMonths(selectedCellKeys, orderedEmployeeIds).forEach(dayOfMonth => {
      const dayCellKey = getGraphCellKey(GRAPH_DAY_STYLE_EMPLOYEE_ID, dayOfMonth);
      const currentStyle = nextStyleByKey.get(dayCellKey);

      nextStyleByKey.set(dayCellKey, {
        id: currentStyle?.id ?? optimisticStyleIdRef.current--,
        scheduleId: graphId ?? 0,
        employeeId: GRAPH_DAY_STYLE_EMPLOYEE_ID,
        dayOfMonth,
        backgroundColorArgb: property === "fill" ? nextColorArgb : currentStyle?.backgroundColorArgb ?? null,
        textColorArgb: property === "text" ? nextColorArgb : currentStyle?.textColorArgb ?? null,
      });
    });

    setSubmitError(undefined);
    syncStyleRecords([...nextStyleByKey.values()]);
  };

  const handleClearCellStyle = () => {
    if (selectedCellKeys.length === 0) {
      return;
    }

    const previousStyles = [...styleRecordsRef.current];
    const selectedKeySet = new Set(selectedCellKeys);
    getFullySelectedDayOfMonths(selectedCellKeys, orderedEmployeeIds).forEach(dayOfMonth => {
      selectedKeySet.add(getGraphCellKey(GRAPH_DAY_STYLE_EMPLOYEE_ID, dayOfMonth));
    });
    const stylesToDelete = previousStyles.filter(style => selectedKeySet.has(getGraphCellKey(style.employeeId, style.dayOfMonth)));
    const autoCellKeysToSuppress = [...selectedKeySet].filter(cellKey => autoAvailabilityUnavailableCellKeySet.has(cellKey));

    if (stylesToDelete.length === 0 && autoCellKeysToSuppress.length === 0) {
      return;
    }

    setSubmitError(undefined);
    if (autoCellKeysToSuppress.length > 0) {
      setAutoAvailabilityStyleSuppressions(current =>
        addAutoAvailabilityStyleSuppressions(current, scheduleAvailabilityGroupId, autoCellKeysToSuppress),
      );
    }

    if (stylesToDelete.length > 0) {
      syncStyleRecords(filterStyleRecordsByKeys(previousStyles, selectedKeySet));
    }
  };

  const handleClearAllCellStyles = () => {
    if (styleRecordsRef.current.length === 0 && autoAvailabilityUnavailableCellKeys.length === 0) {
      return;
    }

    setSubmitError(undefined);
    if (autoAvailabilityUnavailableCellKeys.length > 0) {
      setAutoAvailabilityStyleSuppressions(current =>
        addAutoAvailabilityStyleSuppressions(current, scheduleAvailabilityGroupId, autoAvailabilityUnavailableCellKeys),
      );
    }

    if (styleRecordsRef.current.length > 0) {
      syncStyleRecords([]);
    }
  };

  const handleManualColumnLabelChange = (columnId: number, value: string) => {
    setManualColumns(currentColumns => currentColumns.map(column => (
      column.columnId === columnId ? { ...column, label: value } : column
    )));
    setSubmitError(undefined);
    setManualShiftPublishError(null);
  };

  const handleAddManualColumn = () => {
    const nextManualColumn = createDraftScheduleManualColumn(manualColumns);

    setManualColumns(currentColumns => [...currentColumns, nextManualColumn]);
    setScheduleColumnOrder(current => [...current, buildManualColumnEmployeeId(nextManualColumn.columnId)]);
    setSubmitError(undefined);
    setManualShiftPublishError(null);
  };

  const handleDeleteManualColumn = (columnId: number) => {
    const manualEmployeeId = buildManualColumnEmployeeId(columnId);

    setManualColumns(currentColumns => currentColumns.filter(column => column.columnId !== columnId));
    setPendingManualShiftPublishes(current => current.filter(shift => shift.manualColumnId !== columnId));
    setScheduleColumnOrder(current => current.filter(columnEmployeeId => columnEmployeeId !== manualEmployeeId));
    setSelectedCellKeys(currentSelection =>
      currentSelection.filter(cellKey => parseSelectedCellKey(cellKey).employeeId !== manualEmployeeId),
    );
    setSubmitError(undefined);
    setManualShiftPublishError(null);
  };

  const handlePublishManualShift = (input: ManualColumnShiftPublicationInput) => {
    setManualShiftPublishError(null);

    if (!containerId || !graphId) {
      setManualShiftPublishError("Save this schedule before adding manual shifts to swap.");
      return;
    }

    if (!handleValidate()) {
      setManualShiftPublishError("Check highlighted schedule fields before publishing this shift.");
      return;
    }

    const currentDraft = createCurrentGraphDraft();
    if (!currentDraft) {
      setManualShiftPublishError("Current schedule draft is not ready yet.");
      return;
    }

    const hasExistingOpenOffer = (shiftSwapLogQuery.data ?? []).some(shift =>
      shift.status === "open" &&
      shift.isManagerCreated &&
      shift.manualColumnId === input.manualColumnId &&
      shift.dayOfMonth === input.dayOfMonth);
    const hasPendingOffer = pendingManualShiftPublishes.some(shift =>
      shift.manualColumnId === input.manualColumnId &&
      shift.dayOfMonth === input.dayOfMonth);

    if (hasExistingOpenOffer || hasPendingOffer) {
      setManualShiftPublishError("This manual shift already has an open or pending swap offer.");
      return;
    }

    setPendingManualShiftPublishes(current => [
      ...current,
      {
        ...input,
        clientId: `pending-manual-shift-${crypto.randomUUID()}`,
      },
    ]);
    setSubmitError(undefined);
  };

  const handleCancelPendingManualShift = (clientId: string) => {
    setPendingManualShiftPublishes(current => current.filter(shift => shift.clientId !== clientId));
    setManualShiftPublishError(null);
    setSubmitError(undefined);
  };

  const handleCancelManualShift = (shiftId: number) => {
    setManualShiftPublishError(null);

    if (!containerId || !graphId) {
      setManualShiftPublishError("Schedule is not ready yet.");
      return;
    }

    cancelManualShiftSwapMutation.mutate(
      {
        containerId,
        graphId,
        id: shiftId,
      },
      {
        onError: error => {
          setManualShiftPublishError(toManualShiftPublishError(error));
        },
      },
    );
  };

  return (
    <div className={styles.page}>
      <PageHeader
        title={isCreate ? "Add Schedule" : "Schedule Edit"}
        subtitle={isCreate ? "Create a new saved schedule for this container" : "Update schedule details, employees, matrix content and styling"}
        onBack={() => navigate(
          isCreate
            ? `/container?openContainerId=${containerId ?? ""}`
            : `/container/${containerId}/graphs/${graphId}${sessionSearch}`,
        )}
        onCollapseChange={setIsHeaderCollapsed}
        maxWidth={pageHeaderMaxWidth}
        rightSlot={
          showSessionTabs ? (
            <div className={styles.headerActionCluster}>
              <CompactSizeHeaderToggle
                checked={isCompactMatrix}
                onToggle={() => setIsCompactMatrix(current => !current)}
              />

              <ContainerGraphSessionTabs
                className={styles.sessionSection}
                items={sessionGraphs.map(item => ({
                  graphId: item.id,
                  label: item.name,
                  active: item.id === graphId,
                }))}
                onSelect={(nextGraphId) => {
                  if (!containerId || nextGraphId === graphId) {
                    return;
                  }

                  runWithoutPrompt(() => {
                    storeCurrentGraphDraft();
                    navigate(`/container/${containerId}/graphs/${nextGraphId}/edit${buildGraphSessionSearch(openGraphIds)}`);
                  });
                }}
                  actionSlot={
                    <IosButton
                      label="Save"
                      icon={<SaveIcon size={18} />}
                      disabled={isSaving || Boolean(editLockMessage) || isCheckingLocks}
                      onClick={() => setIsSaveConfirmOpen(true)}
                    />
                }
              />
            </div>
          ) : (
            <div className={styles.headerActions}>
              <CompactSizeHeaderToggle
                checked={isCompactMatrix}
                onToggle={() => setIsCompactMatrix(current => !current)}
              />
            </div>
          )
        }
      />

      {isCheckingLocks ? (
        <ManagerEditLockDialog
          open
          title="Checking edit access"
          message="Please wait while we check whether this schedule can be edited."
        />
      ) : null}

      {editLockMessage ? (
        <ManagerEditLockDialog
          open
          message={editLockMessage}
          actionText="Back to schedule"
          onClose={handleEditLockDialogClose}
        />
      ) : null}

      {canEdit ? (
        <ContainerGraphEditor
        graph={effectiveGraph}
        isHeaderCollapsed={isHeaderCollapsed}
        compactSize={isCompactMatrix}
        form={form}
        formErrors={formErrors}
        shops={shopsQuery.data ?? []}
        availabilityGroups={matchingAvailabilityGroups}
        employees={employeesQuery.data ?? []}
        schedulePresets={schedulePresetsQuery.data ?? []}
        shiftSwapLog={shiftSwapLogQuery.data ?? []}
        isShiftSwapLogLoading={shiftSwapLogQuery.isLoading}
        selectedSchedulePresetId={selectedSchedulePresetId}
        graphEmployeeRows={graphEmployeeRows}
        manualColumns={manualColumns}
        pendingManualShiftPublishes={pendingManualShiftPublishes}
        selectedEmployeeId={selectedEmployeeId}
        scheduleColumns={scheduleColumns}
        cellMap={matrixCellMap}
        visualHintMap={visualHintMap}
        visualHintDetailMap={visualHintDetailMap}
        cellErrors={cellErrors}
        styleMap={styleMap}
        dayConflictMap={dayConflictMap}
        totals={totals}
        previewColumns={previewColumns}
        previewCellMap={previewCellMap}
        previewYear={previewYear}
        previewMonth={previewMonth}
        previewAvailabilitySelection={previewAvailabilitySelection}
        previewAvailabilityOptions={previewAvailabilityOptions}
        binds={bindRows}
        selectedBindClientId={selectedBindClientId}
        bindValueByKey={activeBindValueByKey}
        selectedCellKeys={selectedCellKeys}
        previewSelectedCellKeys={previewSelectedCellKeys}
        fillColor={fillColor}
        textColor={textColor}
        isLoading={isLoading}
        hasLoadError={hasLoadError}
        isSaving={isSaving}
        isGenerating={generateMutation.isPending}
        isStylingBusy={false}
        showMatrixSaveAction={!showSessionTabs}
        isBindsLoading={bindsQuery.isLoading && bindRows.length === 0}
        isBindBusy={createBindMutation.isPending || updateBindMutation.isPending || deleteBindMutation.isPending}
        isSchedulePresetsLoading={schedulePresetsQuery.isLoading}
        isPublishingManualShift={createManualShiftSwapMutation.isPending || isSaving}
        isCancellingManualShift={cancelManualShiftSwapMutation.isPending}
        submitError={submitError}
        bindErrorMessage={bindError ?? (bindsQuery.isError && bindRows.length === 0 ? "Could not load bind information." : undefined)}
        manualShiftPublishError={manualShiftPublishError}
        onFieldChange={setFieldValue}
        onSelectedEmployeeIdChange={setSelectedEmployeeId}
        onPreviewAvailabilitySelectionChange={setPreviewAvailabilitySelection}
        onApplySchedulePreset={handleApplySchedulePreset}
        onCreateSchedulePreset={handleCreateSchedulePreset}
        onSelectedBindChange={clientId => {
          setSelectedBindClientId(clientId);
          setBindError(undefined);
        }}
        onBindFieldChange={handleBindFieldChange}
        onBindCommit={handleBindCommit}
        onAddEmployee={handleAddEmployee}
        onRemoveEmployee={handleRemoveEmployee}
        onAddBind={handleAddBind}
        onDeleteBind={handleDeleteBind}
        onManualColumnLabelChange={handleManualColumnLabelChange}
        onAddManualColumn={handleAddManualColumn}
        onDeleteManualColumn={handleDeleteManualColumn}
        onPublishManualShift={handlePublishManualShift}
        onCancelManualShift={handleCancelManualShift}
        onCancelPendingManualShift={handleCancelPendingManualShift}
        onEmployeeMinHoursChange={(employeeId, value) => {
          setGraphEmployeeRows(current => current.map(row => (row.employeeId === employeeId ? { ...row, minHoursMonth: value } : row)));
          setSelectedSchedulePresetId(null);
          setSubmitError(undefined);
        }}
        onColumnMove={handleColumnMove}
        onCellChange={(employeeId, dayOfMonth, value) => {
          const manualColumnId = getManualColumnIdFromEmployeeId(employeeId);
          if (manualColumnId !== null) {
            setManualColumns(currentColumns => currentColumns.map(column => {
              if (column.columnId !== manualColumnId) {
                return column;
              }

              const normalizedValue = normalizeManualColumnCellValue(value);
              const nextCells = { ...column.cells };

              if (normalizedValue === null) {
                delete nextCells[String(dayOfMonth)];
              } else {
                nextCells[String(dayOfMonth)] = normalizedValue;
              }

              return { ...column, cells: nextCells };
            }));
            setPendingManualShiftPublishes(current => current.filter(shift =>
              shift.manualColumnId !== manualColumnId || shift.dayOfMonth !== dayOfMonth));
            setSubmitError(undefined);
            setManualShiftPublishError(null);
            return;
          }

          const key = getGraphCellKey(employeeId, dayOfMonth);
          setCellMap(current => ({ ...current, [key]: value }));
          setCellErrors(current => {
            if (!current[key]) {
              return current;
            }

            const nextErrors = { ...current };
            delete nextErrors[key];
            return nextErrors;
          });
        }}
        onSelectedCellKeysChange={setSelectedCellKeys}
        onPreviewSelectedCellKeysChange={setPreviewSelectedCellKeys}
        onFillColorChange={setFillColor}
        onTextColorChange={setTextColor}
        onApplyFillColor={() => void handleApplyStyleProperty("fill")}
        onApplyTextColor={() => void handleApplyStyleProperty("text")}
        onClearCellStyle={() => void handleClearCellStyle()}
        onClearAllCellStyles={() => void handleClearAllCellStyles()}
        onSave={() => setIsSaveConfirmOpen(true)}
        onGenerate={() => void handleGenerate()}
        />
      ) : null}

      <ConfirmDialog
        open={bindDeleteTarget !== null}
        title="Delete bind"
        message={`Are you sure you want to delete '${bindDeleteLabel}' from the bind list?`}
        onCancel={() => setBindDeleteTarget(null)}
        onConfirm={handleDeleteBindConfirm}
        confirmText={deleteBindMutation.isPending ? "Deleting..." : "Delete"}
        confirmDisabled={deleteBindMutation.isPending}
        cancelDisabled={deleteBindMutation.isPending}
      />

      <ConfirmDialog
        open={isSaveConfirmOpen}
        title={sessionGraphNames.length > 1 ? "Save schedule session" : "Save schedule"}
        message={
          sessionGraphNames.length > 1
            ? "Save changes for the schedules in this editing session?"
            : `Save changes for '${sessionGraphNames[0]}'?`
        }
        footerSlot={
          sessionGraphNames.length > 0 ? (
            <div className={styles.dialogList}>
              <span className={styles.dialogListTitle}>
                {sessionGraphNames.length > 1 ? "Open schedules" : "Selected schedule"}
              </span>
              <span className={styles.dialogListValue}>{sessionGraphNames.join(", ")}</span>
            </div>
          ) : null
        }
        onCancel={() => setIsSaveConfirmOpen(false)}
        onConfirm={() => void handleSave()}
        confirmText={isSaving ? "Saving..." : "Save"}
        confirmDisabled={isSaving}
        cancelDisabled={isSaving}
        variant="confirm"
      />

      <SavingOverlay active={isSaving} />
      {unsavedChangesDialog}
    </div>
  );
}
