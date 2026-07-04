import {
  buildGraphCellMap,
  buildGraphConflictDayMap,
  buildGraphMatrixColumns,
  buildGraphRelatedScheduleHintMap,
  buildGraphStyleMap,
  buildGraphTotals,
  containersApi,
  formatHoursMinutes,
  formatGraphMonthYear,
  getGraphCellKey,
  getSlotDurationMinutes,
  parseGraphNoteContent,
  rehydrateGraphNoteCellStyles,
  rehydrateGraphNoteTextCells,
  type Container,
  type Graph,
  type GraphCellStyle,
  type GraphEmployee,
  type GraphMatrixCellMap,
  type GraphMatrixColumn,
  type GraphMatrixStyleMap,
  type GraphSlot,
  type GraphTotals,
} from "@entities/containers";
import { employeesApi } from "@entities/employees/api/employeesApi";
import { getEmployeeFullName } from "@entities/employees/model/presentation";
import type { Employee } from "@entities/employees/model/types";
import { shopsApi } from "@entities/shops/api/shopsApi";
import type { Shop } from "@entities/shops/model/types";

export type HomeWhoWorksTodayRow = {
  id: string;
  dateLabel: string;
  employee: string;
  shift: string;
  shop: string;
  route: string;
};

export type HomeSchedulePreview = {
  graph: Graph;
  container: Container | null;
  shop: Shop | null;
  monthLabel: string;
  route: string;
  columns: GraphMatrixColumn[];
  cellMap: GraphMatrixCellMap;
  visualHintMap: GraphMatrixCellMap;
  styleMap: GraphMatrixStyleMap;
  dayConflictMap: Record<number, boolean>;
  totals: GraphTotals;
};

export type HomeDashboardData = {
  monthSchedulesCount: number;
  totalContainersCount: number;
  todayAssignmentsCount: number;
  activeShopsCount: number;
  currentMonthContainerName: string;
  currentMonthLabel: string;
  currentMonthScheduleNames: string[];
  currentMonthShopNames: string[];
  currentMonthEmployeeNames: string[];
  currentMonthTotalEmployees: number;
  currentMonthTotalSchedules: number;
  currentMonthTotalHoursText: string;
  currentMonthTotalShops: number;
  overallTotalEmployees: number;
  overallTotalContainers: number;
  overallTotalShops: number;
  statusText: string;
  todayRows: HomeWhoWorksTodayRow[];
  activeSchedules: HomeSchedulePreview[];
};

type LoadedMonthGraph = {
  container: Container;
  graph: Graph;
  graphEmployees: GraphEmployee[];
  slots: GraphSlot[];
  cellStyles: GraphCellStyle[];
  shop: Shop | null;
};

function createAbortError() {
  const error = new Error("Request was canceled.");
  error.name = "AbortError";
  return error;
}

function throwIfAborted(signal?: AbortSignal) {
  if (signal?.aborted) {
    throw createAbortError();
  }
}

function formatShortDate(year: number, month: number, dayOfMonth: number) {
  return `${String(dayOfMonth).padStart(2, "0")}.${String(month).padStart(2, "0")}.${year}`;
}

function buildManualColumnEmployeeId(columnId: number) {
  return -Math.abs(columnId);
}

function sanitizeScheduleColumnOrder(
  columnOrder: number[],
  baseColumns: GraphMatrixColumn[],
  manualColumns: GraphMatrixColumn[],
) {
  const columnByEmployeeId = new Map(
    [...baseColumns, ...manualColumns].map(column => [column.employeeId, column] as const),
  );
  const fallbackOrder = [...baseColumns.map(column => column.employeeId), ...manualColumns.map(column => column.employeeId)];
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

  return nextOrder
    .map(columnId => columnByEmployeeId.get(columnId))
    .filter((column): column is GraphMatrixColumn => Boolean(column));
}

function buildHomeSchedulePreview(
  params: {
    container: Container;
    graph: Graph;
    graphEmployees: GraphEmployee[];
    slots: GraphSlot[];
    cellStyles: GraphCellStyle[];
    shop: Shop | null;
    employeesById: Map<number, Employee>;
    relatedGraphs: LoadedMonthGraph[];
  },
) {
  const { container, graph, graphEmployees, slots, cellStyles, shop, employeesById, relatedGraphs } = params;
  const parsedGraphNote = parseGraphNoteContent(graph.note);
  const baseColumns = buildGraphMatrixColumns(graphEmployees, employeesById, slots);
  const manualColumns = parsedGraphNote.manualColumns.map(column => ({
    employeeId: buildManualColumnEmployeeId(column.id),
    kind: "manual" as const,
    manualColumnId: column.id,
    graphEmployeeId: null,
    label: column.label || `Custom ${column.id}`,
    minHoursMonth: null,
    totalMinutes: 0,
    totalText: "",
  }));
  const columns = sanitizeScheduleColumnOrder(parsedGraphNote.columnOrder, baseColumns, manualColumns);
  const manualCellMap = parsedGraphNote.manualColumns.reduce<GraphMatrixCellMap>((accumulator, column) => {
    Object.entries(column.cells).forEach(([dayOfMonth, value]) => {
      if (!value.trim()) {
        return;
      }

      accumulator[getGraphCellKey(buildManualColumnEmployeeId(column.id), Number(dayOfMonth))] = value;
    });

    return accumulator;
  }, {});
  const cellMap = {
    ...buildGraphCellMap(slots),
    ...rehydrateGraphNoteTextCells(parsedGraphNote.textCells),
    ...manualCellMap,
  };

  return {
    graph,
    container,
    shop,
    monthLabel: formatGraphMonthYear(graph.year, graph.month),
    route: `/container/${container.id}/graphs/${graph.id}`,
    columns,
    cellMap,
    visualHintMap: buildGraphRelatedScheduleHintMap({
      currentGraph: graph,
      columns,
      cellMap,
      relatedGraphs: relatedGraphs.map(relatedGraph => ({
        graph: relatedGraph.graph,
        slots: relatedGraph.slots,
      })),
    }),
    styleMap: buildGraphStyleMap([
      ...cellStyles,
      ...rehydrateGraphNoteCellStyles(parsedGraphNote.cellStyles, graph.id),
    ]),
    dayConflictMap: buildGraphConflictDayMap(graph, slots),
    totals: buildGraphTotals(graphEmployees, slots, employeesById),
  } satisfies HomeSchedulePreview;
}

function compareGraphs(left: LoadedMonthGraph, right: LoadedMonthGraph) {
  const leftShop = left.shop?.name?.trim() || `Shop ${left.graph.shopId}`;
  const rightShop = right.shop?.name?.trim() || `Shop ${right.graph.shopId}`;
  const shopComparison = leftShop.localeCompare(rightShop);

  if (shopComparison !== 0) {
    return shopComparison;
  }

  return left.graph.name.localeCompare(right.graph.name);
}

export async function loadHomeDashboard(signal?: AbortSignal): Promise<HomeDashboardData> {
  const now = new Date();
  const currentYear = now.getFullYear();
  const currentMonth = now.getMonth() + 1;
  const currentDay = now.getDate();
  const currentMonthLabel = formatGraphMonthYear(currentYear, currentMonth);
  let hasPartialData = false;

  const [containers, employees, shops] = await Promise.all([
    containersApi.list(signal),
    employeesApi.list(signal),
    shopsApi.list(signal),
  ]);

  throwIfAborted(signal);

  const employeesById = new Map(employees.map(employee => [employee.id, employee] as const));
  const shopsById = new Map(shops.map(shop => [shop.id, shop] as const));
  const containersById = new Map(containers.map(container => [container.id, container] as const));

  const containerGraphResults = await Promise.allSettled(
    containers.map(async container => ({
      containerId: container.id,
      graphs: await containersApi.listGraphs(container.id, signal),
    })),
  );

  throwIfAborted(signal);

  const monthGraphs = containerGraphResults.flatMap(result => {
    if (result.status !== "fulfilled") {
      hasPartialData = true;
      return [];
    }

    return result.value.graphs
      .filter(graph => graph.year === currentYear && graph.month === currentMonth)
      .map(graph => ({
        container: containersById.get(result.value.containerId) ?? null,
        graph,
      }))
      .filter((entry): entry is { container: Container; graph: Graph } => entry.container !== null);
  });

  const monthGraphDetails = await Promise.allSettled(
    monthGraphs.map(async ({ container, graph }) => {
      const [graphEmployees, slots, cellStyles] = await Promise.all([
        containersApi.listGraphEmployees(container.id, graph.id, signal),
        containersApi.listGraphSlots(container.id, graph.id, signal),
        containersApi.listGraphCellStyles(container.id, graph.id, signal),
      ]);

      return {
        container,
        graph,
        graphEmployees,
        slots,
        cellStyles,
        shop: shopsById.get(graph.shopId) ?? null,
      };
    }),
  );

  throwIfAborted(signal);

  const loadedMonthGraphs = monthGraphDetails.flatMap(result => {
    if (result.status !== "fulfilled") {
      hasPartialData = true;
      return [];
    }

    return [result.value];
  }).sort(compareGraphs);

  const monthScheduleNames = new Set<string>();
  const monthShopNames = new Set<string>();
  const monthEmployeeNames = new Set<string>();
  const monthEmployeeIds = new Set<number>();
  const monthShopIds = new Set<number>();
  const activeContainerIds = new Set<number>();
  const todayRowsByKey = new Map<string, HomeWhoWorksTodayRow>();
  let monthTotalMinutes = 0;

  const activeSchedules = loadedMonthGraphs.map(record => {
    monthScheduleNames.add(record.graph.name);
    monthShopIds.add(record.graph.shopId);
    activeContainerIds.add(record.container.id);

    const shopName = record.shop?.name?.trim() || `Shop ${record.graph.shopId}`;
    monthShopNames.add(shopName);

    record.graphEmployees.forEach(graphEmployee => {
      if (graphEmployee.employeeId <= 0) {
        return;
      }

      monthEmployeeIds.add(graphEmployee.employeeId);
      monthEmployeeNames.add(
        getEmployeeFullName(employeesById.get(graphEmployee.employeeId), `Employee ${graphEmployee.employeeId}`),
      );
    });

    record.slots.forEach(slot => {
      if (!slot.employeeId || slot.employeeId <= 0) {
        return;
      }

      monthEmployeeIds.add(slot.employeeId);
      monthEmployeeNames.add(
        getEmployeeFullName(employeesById.get(slot.employeeId), `Employee ${slot.employeeId}`),
      );
      monthTotalMinutes += getSlotDurationMinutes(slot);

      if (slot.dayOfMonth !== currentDay) {
        return;
      }

      const employeeName = getEmployeeFullName(employeesById.get(slot.employeeId), `Employee ${slot.employeeId}`);
      const shift = `${slot.fromTime} - ${slot.toTime}`;
      const rowKey = `${slot.dayOfMonth}:${employeeName}:${shift}:${shopName}`;
      if (todayRowsByKey.has(rowKey)) {
        return;
      }

      todayRowsByKey.set(rowKey, {
        id: rowKey,
        dateLabel: formatShortDate(currentYear, currentMonth, slot.dayOfMonth),
        employee: employeeName,
        shift,
        shop: shopName,
        route: `/container/${record.container.id}/graphs/${record.graph.id}`,
      });
    });

    return buildHomeSchedulePreview({
      container: record.container,
      graph: record.graph,
      graphEmployees: record.graphEmployees,
      slots: record.slots,
      cellStyles: record.cellStyles,
      shop: record.shop,
      employeesById,
      relatedGraphs: loadedMonthGraphs,
    });
  });

  const activeContainerNames = [...activeContainerIds]
    .map(containerId => containersById.get(containerId)?.name?.trim())
    .filter((name): name is string => Boolean(name));

  const currentMonthContainerName =
    activeContainerNames.length === 0
      ? "No active containers"
      : activeContainerNames.length === 1
        ? activeContainerNames[0]
        : `${activeContainerNames.length} active containers`;

  const todayRows = [...todayRowsByKey.values()].sort((left, right) => {
    const shiftComparison = left.shift.localeCompare(right.shift);

    if (shiftComparison !== 0) {
      return shiftComparison;
    }

    const employeeComparison = left.employee.localeCompare(right.employee);
    if (employeeComparison !== 0) {
      return employeeComparison;
    }

    return left.shop.localeCompare(right.shop);
  });

  return {
    monthSchedulesCount: monthGraphs.length,
    totalContainersCount: containers.length,
    todayAssignmentsCount: todayRows.length,
    activeShopsCount: new Set(monthGraphs.map(item => item.graph.shopId)).size,
    currentMonthContainerName,
    currentMonthLabel,
    currentMonthScheduleNames: [...monthScheduleNames].sort((left, right) => left.localeCompare(right)),
    currentMonthShopNames: [...monthShopNames].sort((left, right) => left.localeCompare(right)),
    currentMonthEmployeeNames: [...monthEmployeeNames].sort((left, right) => left.localeCompare(right)),
    currentMonthTotalEmployees: monthEmployeeIds.size,
    currentMonthTotalSchedules: monthGraphs.length,
    currentMonthTotalHoursText: formatHoursMinutes(monthTotalMinutes),
    currentMonthTotalShops: monthShopIds.size,
    overallTotalEmployees: employees.length,
    overallTotalContainers: containers.length,
    overallTotalShops: shops.length,
    statusText: hasPartialData
      ? "Home data loaded with a few missing schedule previews."
      : "Home data is up to date.",
    todayRows,
    activeSchedules,
  };
}
