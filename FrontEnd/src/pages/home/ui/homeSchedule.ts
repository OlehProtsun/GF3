import { queryKeys } from "@shared/api/queryKeys";
import { t } from "@shared/i18n";
import { getWarsawShiftStatus } from "./homeActivity";
import { loadHomeDashboard, type HomeDashboardData } from "./homeDashboard";

const shiftStatusOrder = { in_progress: 0, completed: 1, upcoming: 2 };

export function getHomeDashboardQuery(selectedDay: string) {
  return {
    queryKey: queryKeys.home.dashboard(selectedDay),
    staleTime: 30_000,
    queryFn: ({ signal }: { signal: AbortSignal }) => loadHomeDashboard(selectedDay, signal),
  };
}

export function getHomeScheduleRows(data: HomeDashboardData | undefined, selectedDay: string, selectedShop: number | null, now: Date) {
  const filteredRows = (data?.todayRows ?? []).filter(row => selectedShop === null || row.shopId === selectedShop);
  return {
    employeeCount: selectedShop === null ? data?.todayActiveEmployeesCount : new Set(filteredRows.map(row => row.employeeId)).size,
    todayRows: filteredRows
      .map(row => ({ row, status: getWarsawShiftStatus(selectedDay, row.fromTime, row.toTime, now) }))
      .sort((left, right) => shiftStatusOrder[left.status] - shiftStatusOrder[right.status]),
  };
}

export function getHomeShiftStatusLabel(status: ReturnType<typeof getWarsawShiftStatus>) {
  return status === "upcoming" ? t("Upcoming") : status === "in_progress" ? t("In Progress") : t("Completed");
}

export function getHomeEmployeeInitials(name: string) {
  return name.split(/\s+/).filter(Boolean).slice(0, 2).map(part => Array.from(part)[0]).join("").toUpperCase();
}
