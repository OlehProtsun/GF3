import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useQueryClient } from "@tanstack/react-query";
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
import {
  ContainerGraphEditor,
  GRAPH_EMPTY_MARK,
  applyGraphApiErrors,
  buildGraphNoteContent,
  buildGraphCellMap,
  buildGraphConflictDayMap,
  buildGraphDraftSlots,
  buildGraphFormErrors,
  buildGraphMatrixColumns,
  buildGraphStyleMap,
  buildGraphTotals,
  containersApi,
  createGraphFormFromGraph,
  createInitialGraphForm,
  getGraphCellKey,
  getGraphDaysInMonth,
  parseGraphNoteContent,
  parseIntegerField,
  rgbHexToArgb,
  sanitizeGraphCellMap,
  sortGraphItemsByDisplayOrder,
  useContainerByIdQuery,
  useCreateSchedulePresetMutation,
  useGenerateGraphMutation,
  useGraphByIdQuery,
  useGraphCellStylesQuery,
  useGraphEmployeesQuery,
  useGraphSlotsQuery,
  useSaveGraphWorkspaceMutation,
  useSchedulePresetsQuery,
  type ContainerGraphFormErrors,
  type ContainerGraphFormState,
  type EditableGraphManualColumn,
  type GraphCellStyle,
  type GraphMatrixColumn,
  type SaveSchedulePresetDto,
} from "@entities/containers";
import { useEmployeesListQuery } from "@entities/employees/api/queries";
import { getEmployeeFullName } from "@entities/employees/model/presentation";
import { useShopsListQuery } from "@entities/shops/api/queries";
import { queryKeys } from "@shared/api/queryKeys";
import { ApiError } from "@shared/api/httpClient";
import { ConfirmDialog } from "@shared/ui/ConfirmDialog";
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

function toPayload(
  form: ContainerGraphFormState,
  manualColumns: EditableScheduleManualColumn[],
  columnOrder: number[],
) {
  const year = Number(form.year) || new Date().getFullYear();
  const month = Number(form.month) || 1;

  return {
    shopId: Number(form.shopId),
    name: form.name.trim(),
    year,
    month,
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
      columnOrder,
    ) || undefined,
    availabilityGroupId: form.availabilityGroupId ? Number(form.availabilityGroupId) : null,
  };
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

function arraysEqual(left: number[], right: number[]) {
  return left.length === right.length && left.every((value, index) => value === right[index]);
}

type StyleProperty = "fill" | "text";
type CompactSizeHeaderToggleProps = {
  checked: boolean;
  onToggle: () => void;
};

function joinClassNames(...values: Array<string | false | undefined>) {
  return values.filter(Boolean).join(" ");
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

function buildStyleRecordByKey(styles: GraphCellStyle[]) {
  return new Map(styles.map(style => [getGraphCellKey(style.employeeId, style.dayOfMonth), style]));
}

function normalizeStyleRecords(styles: GraphCellStyle[], scheduleId: number) {
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

const REGENERATION_FIELDS = new Set<keyof ContainerGraphFormState>([
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

export function ContainerGraphEditPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { containerId: containerIdParam, graphId: graphIdParam } = useParams<{ containerId: string; graphId?: string }>();
  const parsedContainerId = containerIdParam ? Number(containerIdParam) : null;
  const parsedGraphId = graphIdParam ? Number(graphIdParam) : null;
  const containerId = Number.isFinite(parsedContainerId) ? parsedContainerId : null;
  const graphId = Number.isFinite(parsedGraphId) ? parsedGraphId : null;
  const isCreate = graphId === null;

  const containerQuery = useContainerByIdQuery(containerId);
  const graphQuery = useGraphByIdQuery(containerId, graphId);
  const graphEmployeesQuery = useGraphEmployeesQuery(containerId, graphId);
  const slotsQuery = useGraphSlotsQuery(containerId, graphId);
  const cellStylesQuery = useGraphCellStylesQuery(containerId, graphId);
  const schedulePresetsQuery = useSchedulePresetsQuery(containerId);
  const availabilityGroupsQuery = useAvailabilityGroupsListQuery();
  const bindsQuery = useAvailabilityBindsListQuery();
  const employeesQuery = useEmployeesListQuery();
  const shopsQuery = useShopsListQuery();
  const saveWorkspaceMutation = useSaveGraphWorkspaceMutation();
  const createSchedulePresetMutation = useCreateSchedulePresetMutation();
  const generateMutation = useGenerateGraphMutation();
  const createBindMutation = useCreateAvailabilityBindMutation();
  const updateBindMutation = useUpdateAvailabilityBindMutation();
  const deleteBindMutation = useDeleteAvailabilityBindMutation();

  const [form, setForm] = useState<ContainerGraphFormState>(() => createInitialGraphForm());
  const [formErrors, setFormErrors] = useState<ContainerGraphFormErrors>({});
  const [graphEmployeeRows, setGraphEmployeeRows] = useState<Array<{ id: number | null; employeeId: number; minHoursMonth: string }>>([]);
  const [selectedSchedulePresetId, setSelectedSchedulePresetId] = useState<number | null>(null);
  const [selectedEmployeeId, setSelectedEmployeeId] = useState<number | null>(null);
  const [isCompactMatrix, setIsCompactMatrix] = useState(false);
  const [employeeSearchText, setEmployeeSearchText] = useState("");
  const [cellMap, setCellMap] = useState<Record<string, string>>({});
  const [cellErrors, setCellErrors] = useState<Record<string, string>>({});
  const [submitError, setSubmitError] = useState<string | undefined>();
  const [hydratedKey, setHydratedKey] = useState<string | null>(null);
  const [needsRegeneration, setNeedsRegeneration] = useState(false);
  const [isHeaderCollapsed, setIsHeaderCollapsed] = useState(false);
  const [selectedCellKeys, setSelectedCellKeys] = useState<string[]>([]);
  const [styleRecords, setStyleRecords] = useState<GraphCellStyle[]>([]);
  const [isPersistingStyleDraft, setIsPersistingStyleDraft] = useState(false);
  const [fillColor, setFillColor] = useState("#dbeafe");
  const [textColor, setTextColor] = useState("#0f172a");
  const [bindError, setBindError] = useState<string | undefined>();
  const [bindRows, setBindRows] = useState<EditableAvailabilityBind[]>([]);
  const [manualColumns, setManualColumns] = useState<EditableScheduleManualColumn[]>([]);
  const [scheduleColumnOrder, setScheduleColumnOrder] = useState<number[]>([]);
  const [selectedBindClientId, setSelectedBindClientId] = useState<string | null>(null);
  const [hasLocalBindChanges, setHasLocalBindChanges] = useState(false);
  const [bindDeleteTarget, setBindDeleteTarget] = useState<EditableAvailabilityBind | null>(null);
  const optimisticStyleIdRef = useRef(-1);
  const styleRecordsRef = useRef<GraphCellStyle[]>([]);
  const persistedStyleRecordsRef = useRef<GraphCellStyle[]>([]);

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
      setEmployeeSearchText("");
      setCellMap({});
      setCellErrors({});
      setSubmitError(undefined);
      setNeedsRegeneration(false);
      setSelectedCellKeys([]);
      setManualColumns([]);
      setScheduleColumnOrder([]);
      setStyleRecords([]);
      styleRecordsRef.current = [];
      persistedStyleRecordsRef.current = [];
      setHydratedKey(createKey);
      return;
    }

    if (!graphQuery.data || !graphEmployeesQuery.data || !slotsQuery.data) {
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
    setEmployeeSearchText("");
    setCellMap(buildGraphCellMap(slotsQuery.data));
    setCellErrors({});
    setSubmitError(undefined);
    setNeedsRegeneration(false);
    setSelectedCellKeys([]);
    setManualColumns(sortManualColumnsByColumnOrder(editableManualColumns, nextScheduleColumnOrder));
    setScheduleColumnOrder(nextScheduleColumnOrder);
    const hydratedStyles = cellStylesQuery.data ?? [];
    setStyleRecords(hydratedStyles);
    styleRecordsRef.current = hydratedStyles;
    persistedStyleRecordsRef.current = hydratedStyles;
    setHydratedKey(nextHydratedKey);
  }, [
    cellStylesQuery.data,
    containerId,
    employeesById,
    graphEmployeesQuery.data,
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
    const nextColumnOrder = sanitizeScheduleColumnOrder(scheduleColumnOrder, graphEmployeeRows, manualColumns);
    if (!arraysEqual(nextColumnOrder, scheduleColumnOrder)) {
      setScheduleColumnOrder(nextColumnOrder);
    }
  }, [graphEmployeeRows, manualColumns, scheduleColumnOrder]);

  useEffect(() => {
    if (isCreate) {
      persistedStyleRecordsRef.current = [];
      return;
    }

    persistedStyleRecordsRef.current = cellStylesQuery.data ?? [];
  }, [cellStylesQuery.data, isCreate]);

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

  const dayConflictMap = useMemo(
    () => buildGraphConflictDayMap(effectiveGraph, effectiveSlots),
    [effectiveGraph, effectiveSlots],
  );
  const totals = useMemo(
    () => buildGraphTotals(draftGraphEmployees, effectiveSlots, employeesById),
    [draftGraphEmployees, effectiveSlots, employeesById],
  );

  const previewAvailabilityGroupId = form.availabilityGroupId ? Number(form.availabilityGroupId) : null;
  const previewMembersQuery = useAvailabilityGroupMembersQuery(previewAvailabilityGroupId);
  const previewSlotsQuery = useAvailabilityGroupSlotsQuery(previewAvailabilityGroupId);
  const previewColumns = useMemo<AvailabilityMatrixColumn[]>(
    () => buildAvailabilityColumns(previewMembersQuery.data ?? [], employeeNameById),
    [employeeNameById, previewMembersQuery.data],
  );
  const previewCellMap = useMemo<AvailabilityMatrixCellMap>(
    () => buildAvailabilityCellMap(previewMembersQuery.data ?? [], previewSlotsQuery.data ?? []),
    [previewMembersQuery.data, previewSlotsQuery.data],
  );

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

  const syncStyleRecords = (nextStyles: GraphCellStyle[]) => {
    styleRecordsRef.current = nextStyles;
    setStyleRecords(nextStyles);
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
  const syncPersistedStyles = (resolvedGraphId: number, nextStyles: GraphCellStyle[]) => {
    if (!containerId) {
      return;
    }

    const normalizedStyles = normalizeStyleRecords(nextStyles, resolvedGraphId);
    persistedStyleRecordsRef.current = normalizedStyles;

    (
      queryClient as typeof queryClient & {
        setQueryData: (queryKey: unknown, data: GraphCellStyle[]) => void;
      }
    ).setQueryData(queryKeys.containers.graphCellStyles(containerId, resolvedGraphId), normalizedStyles);
  };

  const persistStyleDraft = async (resolvedGraphId: number) => {
    if (!containerId) {
      return;
    }

    const draftStyles = normalizeStyleRecords(styleRecordsRef.current, resolvedGraphId);
    const persistedStyles = normalizeStyleRecords(persistedStyleRecordsRef.current, resolvedGraphId);
    const draftStyleByKey = buildStyleRecordByKey(draftStyles);
    const persistedStyleByKey = buildStyleRecordByKey(persistedStyles);

    const stylesToDelete = persistedStyles.filter(style => !draftStyleByKey.has(getGraphCellKey(style.employeeId, style.dayOfMonth)));
    const stylesToUpsert = draftStyles.filter(style => {
      const currentPersistedStyle = persistedStyleByKey.get(getGraphCellKey(style.employeeId, style.dayOfMonth));
      return !currentPersistedStyle || !haveSameStyleValues(currentPersistedStyle, style);
    });

    if (stylesToDelete.length === 0 && stylesToUpsert.length === 0) {
      syncStyleRecords(draftStyles);
      syncPersistedStyles(resolvedGraphId, draftStyles);
      return;
    }

    setIsPersistingStyleDraft(true);

    try {
      await Promise.all(
        stylesToDelete
          .filter(style => style.id > 0)
          .map(style => containersApi.removeGraphCellStyle(containerId, resolvedGraphId, style.id)),
      );

      await Promise.all(
        stylesToUpsert.map(style =>
          containersApi.upsertGraphCellStyle(containerId, resolvedGraphId, {
            employeeId: style.employeeId,
            dayOfMonth: style.dayOfMonth,
            backgroundColorArgb: style.backgroundColorArgb ?? null,
            textColorArgb: style.textColorArgb ?? null,
          }),
        ),
      );

      const savedStyles = await containersApi.listGraphCellStyles(containerId, resolvedGraphId);
      syncStyleRecords(savedStyles);
      syncPersistedStyles(resolvedGraphId, savedStyles);
    } catch (error) {
      try {
        const currentServerStyles = await containersApi.listGraphCellStyles(containerId, resolvedGraphId);
        syncPersistedStyles(resolvedGraphId, currentServerStyles);
      } catch {
        // Keep the local draft intact and let the original save error surface.
      }

      throw error;
    } finally {
      setIsPersistingStyleDraft(false);
    }
  };

  const setFieldValue = (field: keyof ContainerGraphFormState) => (value: string) => {
    setForm(current => ({ ...current, [field]: value }));
    setSubmitError(undefined);

    if (PRESET_MANAGED_FIELDS.has(field)) {
      setSelectedSchedulePresetId(null);
    }

    setFormErrors(currentErrors => {
      if (!currentErrors[field]) {
        return currentErrors;
      }

      const nextErrors = { ...currentErrors };
      delete nextErrors[field];
      return nextErrors;
    });

    if (REGENERATION_FIELDS.has(field)) {
      setNeedsRegeneration(true);
    }
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
    setNeedsRegeneration(true);
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
    setNeedsRegeneration(true);
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
    setNeedsRegeneration(true);
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

  const handleValidate = () => {
    const nextFormErrors = buildGraphFormErrors(form, shopIdSet, availabilityGroupIdSet);
    const nextCellErrors = draftSlots.errors;

    setFormErrors(nextFormErrors);
    setCellErrors(nextCellErrors);

    if (graphEmployeeRows.length === 0) {
      setSubmitError("Add at least one employee to this schedule.");
      return false;
    }

    if (Object.keys(nextFormErrors).length > 0 || Object.keys(nextCellErrors).length > 0) {
      setSubmitError("Check highlighted fields before continuing.");
      return false;
    }

    return true;
  };

  const handleSave = async () => {
    if (!containerId || !handleValidate()) {
      return;
    }

    setSubmitError(undefined);

    try {
      const result = await runMutation(saveWorkspaceMutation.mutate, {
        containerId,
        graphId,
        payload: toPayload(form, manualColumns, scheduleColumnOrder),
        employeeAssignments: orderedGraphEmployeeRows.map(row => ({
          id: row.id,
          employeeId: row.employeeId,
          minHoursMonth: row.minHoursMonth ? Number(row.minHoursMonth) : null,
        })),
        cellMap,
        existingEmployees: graphEmployeesQuery.data ?? [],
        existingSlots: slotsQuery.data ?? [],
      });

      await persistStyleDraft(result.graphId);

      navigate(`/container/${containerId}/graphs/${result.graphId}`);
    } catch (error) {
      if (error instanceof ApiError) {
        setFormErrors(currentErrors => ({ ...currentErrors, ...applyGraphApiErrors(error.validationErrors) }));
        setSubmitError(error.message);
        return;
      }

      setSubmitError(error instanceof Error ? error.message : "Could not save this schedule.");
    }
  };

  const handleGenerate = async () => {
    if (!containerId || !handleValidate()) {
      return;
    }

    if (!form.availabilityGroupId) {
      setSubmitError("Select an availability group before generating a schedule.");
      return;
    }

    setSubmitError(undefined);

    try {
      const saved = await runMutation(saveWorkspaceMutation.mutate, {
        containerId,
        graphId,
        payload: toPayload(form, manualColumns, scheduleColumnOrder),
        employeeAssignments: orderedGraphEmployeeRows.map(row => ({
          id: row.id,
          employeeId: row.employeeId,
          minHoursMonth: row.minHoursMonth ? Number(row.minHoursMonth) : null,
        })),
        cellMap,
        existingEmployees: graphEmployeesQuery.data ?? [],
        existingSlots: slotsQuery.data ?? [],
      });

      await runMutation(generateMutation.mutate, {
        containerId,
        graphId: saved.graphId,
        payload: { overwrite: true },
      });

      await Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.containers.graphById(containerId, saved.graphId) }),
        queryClient.invalidateQueries({ queryKey: queryKeys.containers.graphEmployees(containerId, saved.graphId) }),
        queryClient.invalidateQueries({ queryKey: queryKeys.containers.graphSlots(containerId, saved.graphId) }),
      ]);

      setNeedsRegeneration(false);

      if (isCreate) {
        navigate(`/container/${containerId}/graphs/${saved.graphId}/edit`);
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
    if (!graphId || selectedCellKeys.length === 0) {
      setSubmitError("Save the schedule first before styling matrix cells.");
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
      .filter(({ employeeId }) => employeeId > 0)
      .forEach(({ cellKey, employeeId, dayOfMonth }) => {
        const currentStyle = nextStyleByKey.get(cellKey);

        nextStyleByKey.set(cellKey, {
          id: currentStyle?.id ?? optimisticStyleIdRef.current--,
          scheduleId: graphId,
          employeeId,
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
    const stylesToDelete = previousStyles.filter(style => selectedKeySet.has(getGraphCellKey(style.employeeId, style.dayOfMonth)));
    if (stylesToDelete.length === 0) {
      return;
    }

    setSubmitError(undefined);
    syncStyleRecords(filterStyleRecordsByKeys(previousStyles, selectedKeySet));
  };

  const handleClearAllCellStyles = () => {
    if (styleRecordsRef.current.length === 0) {
      return;
    }

    setSubmitError(undefined);
    syncStyleRecords([]);
  };

  const handleManualColumnLabelChange = (columnId: number, value: string) => {
    setManualColumns(currentColumns => currentColumns.map(column => (
      column.columnId === columnId ? { ...column, label: value } : column
    )));
    setSubmitError(undefined);
  };

  const handleAddManualColumn = () => {
    const nextManualColumn = createDraftScheduleManualColumn(manualColumns);

    setManualColumns(currentColumns => [...currentColumns, nextManualColumn]);
    setScheduleColumnOrder(current => [...current, buildManualColumnEmployeeId(nextManualColumn.columnId)]);
    setSubmitError(undefined);
  };

  const handleDeleteManualColumn = (columnId: number) => {
    const manualEmployeeId = buildManualColumnEmployeeId(columnId);

    setManualColumns(currentColumns => currentColumns.filter(column => column.columnId !== columnId));
    setScheduleColumnOrder(current => current.filter(columnEmployeeId => columnEmployeeId !== manualEmployeeId));
    setSelectedCellKeys(currentSelection =>
      currentSelection.filter(cellKey => parseSelectedCellKey(cellKey).employeeId !== manualEmployeeId),
    );
    setSubmitError(undefined);
  };

  return (
    <div className={styles.page}>
      <PageHeader
        title={isCreate ? "Add Schedule" : "Schedule Edit"}
        subtitle={isCreate ? "Create a new saved schedule for this container" : "Update schedule details, employees, matrix content and styling"}
        onBack={() => navigate(isCreate ? `/container?openContainerId=${containerId ?? ""}` : `/container/${containerId}/graphs/${graphId}`)}
        onCollapseChange={setIsHeaderCollapsed}
        rightSlot={
          <CompactSizeHeaderToggle
            checked={isCompactMatrix}
            onToggle={() => setIsCompactMatrix(current => !current)}
          />
        }
      />

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
        selectedSchedulePresetId={selectedSchedulePresetId}
        graphEmployeeRows={graphEmployeeRows}
        manualColumns={manualColumns}
        selectedEmployeeId={selectedEmployeeId}
        employeeSearchText={employeeSearchText}
        scheduleColumns={scheduleColumns}
        cellMap={matrixCellMap}
        cellErrors={cellErrors}
        styleMap={styleMap}
        dayConflictMap={dayConflictMap}
        totals={totals}
        previewColumns={previewColumns}
        previewCellMap={previewCellMap}
        previewYear={previewAvailabilityGroupId
          ? (matchingAvailabilityGroups.find(group => group.id === previewAvailabilityGroupId)?.year ?? yearValue ?? new Date().getFullYear())
          : (yearValue ?? new Date().getFullYear())}
        previewMonth={previewAvailabilityGroupId
          ? (matchingAvailabilityGroups.find(group => group.id === previewAvailabilityGroupId)?.month ?? monthValue ?? 1)
          : (monthValue ?? 1)}
        binds={bindRows}
        selectedBindClientId={selectedBindClientId}
        bindValueByKey={activeBindValueByKey}
        selectedCellKeys={selectedCellKeys}
        fillColor={fillColor}
        textColor={textColor}
        isLoading={isLoading}
        hasLoadError={hasLoadError}
        isSaving={saveWorkspaceMutation.isPending || isPersistingStyleDraft}
        isGenerating={generateMutation.isPending}
        isStylingBusy={false}
        isBindsLoading={bindsQuery.isLoading && bindRows.length === 0}
        isBindBusy={createBindMutation.isPending || updateBindMutation.isPending || deleteBindMutation.isPending}
        isSchedulePresetsLoading={schedulePresetsQuery.isLoading}
        submitError={submitError}
        bindErrorMessage={bindError ?? (bindsQuery.isError && bindRows.length === 0 ? "Could not load bind information." : undefined)}
        needsRegeneration={needsRegeneration}
        onFieldChange={setFieldValue}
        onEmployeeSearchTextChange={setEmployeeSearchText}
        onSelectedEmployeeIdChange={setSelectedEmployeeId}
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
        onEmployeeMinHoursChange={(employeeId, value) => {
          setGraphEmployeeRows(current => current.map(row => (row.employeeId === employeeId ? { ...row, minHoursMonth: value } : row)));
          setSelectedSchedulePresetId(null);
          setSubmitError(undefined);
          setNeedsRegeneration(true);
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
            setSubmitError(undefined);
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
        onFillColorChange={setFillColor}
        onTextColorChange={setTextColor}
        onApplyFillColor={() => void handleApplyStyleProperty("fill")}
        onApplyTextColor={() => void handleApplyStyleProperty("text")}
        onClearCellStyle={() => void handleClearCellStyle()}
        onClearAllCellStyles={() => void handleClearAllCellStyles()}
        onSave={() => void handleSave()}
        onGenerate={() => void handleGenerate()}
      />

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
    </div>
  );
}
