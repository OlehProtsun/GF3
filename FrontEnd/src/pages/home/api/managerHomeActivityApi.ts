import { request } from "@shared/api/httpClient";
export type ManagerNavBadges = { totalPending: number; graphPending: number; urlopPending: number };
export type HomeActivityTab = "all" | "approvals";
export type HomeActivityKind = "swap_created" | "swap_accepted" | "swap_cancelled" | "correction_requested" | "correction_reviewed" | "day_off_requested" | "day_off_reviewed" | "approval_requested" | "approval_reviewed" | "urlop_requested" | "urlop_reviewed" | "urlop_manager_created" | "availability_updated";
export type ManagerHomeActivityItem = {
  id: string; kind: HomeActivityKind; occurredAtUtc: string; employeeName: string; status: string; requiresApproval: boolean;
  targetType: "graph" | "availability" | "urlop";
  counterpartyName?: string | null; scheduleName?: string | null; shopName?: string | null;
  shiftDate?: string | null; fromTime?: string | null; toTime?: string | null;
  returnShiftDate?: string | null; returnFromTime?: string | null; returnToTime?: string | null;
  vacationDays?: string[] | null;
  containerId?: number | null; graphId?: number | null; availabilityGroupId?: number | null;
  shopId?: number | null; employeeId?: number | null; year?: number | null; month?: number | null;
};
export type ManagerHomeActivityPage = {
  items: ManagerHomeActivityItem[]; allActivityCount: number; requiresApprovalCount: number; hasMore: boolean; nextOffset: number;
};
export const managerHomeActivityApi = {
  navBadges: (signal?: AbortSignal): Promise<ManagerNavBadges> =>
    request<ManagerNavBadges>("manager-home/nav-badges", { signal }),
  list: (query: { year: number; month: number; tab: HomeActivityTab; offset: number; limit: number }, signal?: AbortSignal) =>
    request<ManagerHomeActivityPage>("manager-home/activity", { query, signal }),
};
