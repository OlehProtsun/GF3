import { t } from "@shared/i18n";
import { containersApi, formatHoursMinutes, getSlotDurationMinutes } from "@entities/containers";
import { employeesApi } from "@entities/employees/api/employeesApi";
import { getEmployeeFullName } from "@entities/employees/model/presentation";
import { shopsApi } from "@entities/shops/api/shopsApi";
import { formatHomeDate, parseHomeDay } from "./homeActivity";
export type HomeWhoWorksTodayRow = {
  id: string; employeeId: number; containerId: number; graphId: number; dateLabel: string;
  employee: string; shop: string; fromTime: string; toTime: string; shift: string; hoursText: string;
};
export type HomeDashboardData = { todayRows: HomeWhoWorksTodayRow[]; todayActiveEmployeesCount: number; hasPartialData: boolean };
function throwIfAborted(signal?: AbortSignal) {
  if (signal?.aborted) throw new DOMException(t("Request was canceled."), "AbortError");
}
export async function loadHomeDashboard(selectedDate: string, signal?: AbortSignal): Promise<HomeDashboardData> {
  const date = parseHomeDay(selectedDate);
  const year = date.getUTCFullYear(), month = date.getUTCMonth() + 1, day = date.getUTCDate();
  throwIfAborted(signal);
  const [containersResult, employeesResult, shopsResult] = await Promise.allSettled([containersApi.list(signal), employeesApi.list(signal), shopsApi.list(signal)]);
  throwIfAborted(signal);
  if (containersResult.status === "rejected") throw containersResult.reason;
  let hasPartialData = employeesResult.status === "rejected" || shopsResult.status === "rejected";
  const employees = new Map((employeesResult.status === "fulfilled" ? employeesResult.value : []).map(employee => [employee.id, employee]));
  const shops = new Map((shopsResult.status === "fulfilled" ? shopsResult.value : []).map(shop => [shop.id, shop]));
  const graphResults = await Promise.allSettled(containersResult.value.map(async container => ({ containerId: container.id, graphs: await containersApi.listGraphs(container.id, signal) })));
  throwIfAborted(signal);
  const monthGraphs = graphResults.flatMap(result => {
    if (result.status === "rejected") { hasPartialData = true; return []; }
    return result.value.graphs.filter(graph => graph.year === year && graph.month === month).map(graph => ({ containerId: result.value.containerId, graph }));
  });
  const slotResults = await Promise.allSettled(monthGraphs.map(async ({ containerId, graph }) => ({ containerId, graph, slots: await containersApi.listGraphSlots(containerId, graph.id, signal) })));
  throwIfAborted(signal);
  const rows = new Map<string, HomeWhoWorksTodayRow>();
  slotResults.forEach(result => {
    if (result.status === "rejected") { hasPartialData = true; return; }
    const { containerId, graph, slots } = result.value;
    slots.filter(slot => slot.employeeId != null && slot.employeeId > 0 && slot.dayOfMonth === day).forEach(slot => {
      const employeeId = slot.employeeId!;
      const id = `${containerId}:${graph.id}:${slot.id}`;
      rows.set(id, { id, employeeId, containerId, graphId: graph.id,
        employee: getEmployeeFullName(employees.get(employeeId), t("Employee {0}", employeeId)),
        shop: shops.get(graph.shopId)?.name?.trim() || t("Shop {0}", graph.shopId), dateLabel: formatHomeDate(selectedDate, true),
        fromTime: slot.fromTime, toTime: slot.toTime, shift: `${slot.fromTime} – ${slot.toTime}`, hoursText: formatHoursMinutes(getSlotDurationMinutes(slot)),
      });
    });
  });
  const todayRows = [...rows.values()].sort((a, b) => a.fromTime.localeCompare(b.fromTime) || a.employee.localeCompare(b.employee) || a.shop.localeCompare(b.shop) || a.id.localeCompare(b.id));
  return { todayRows, todayActiveEmployeesCount: new Set(todayRows.map(row => row.employeeId)).size, hasPartialData };
}
