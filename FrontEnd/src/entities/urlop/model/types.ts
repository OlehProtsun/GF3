export type UrlopStatus = "pending" | "approved" | "rejected";
export type ApprovedUrlopDay = { shopId: number; employeeId: number; date: string; employeeName?: string };
export type UrlopRequest = {
  requestId: string; employeeId: number; employeeName: string; shopId: number;
  year: number; month: number; days: number[]; status: UrlopStatus; createdAtUtc: string;
  reviewedAtUtc: string | null; createdByManagerId: number | null; reviewedByManagerId: number | null;
};
export type UrlopMonth = { shopId: number; year: number; month: number; isOpen: boolean };
export type UrlopCalendarMonth = UrlopMonth & { requests: UrlopRequest[]; approved: ApprovedUrlopDay[] };
export type UrlopEmployeeCalendar = { shops: { id: number; name: string }[]; months: UrlopCalendarMonth[] };
export type UrlopManagerMonth = UrlopMonth & { shopName: string; requests: UrlopRequest[]; approvedDays: ApprovedUrlopDay[] };
export type SubmitUrlop = { shopId: number; year: number; month: number; days: number[] };
