export type ContractShiftApproval = {
  id: number; kind: "claim" | "below_minimum_give";
  status: "pending" | "approved" | "rejected" | "cancelled" | "expired";
  employeeId: number; employeeName: string; scheduleId: number; scheduleSlotId: number; scheduleName: string;
  fromEmployeeId?: number | null; fromEmployeeName?: string | null; toEmployeeId?: number | null; toEmployeeName?: string | null;
  isManagerCreated?: boolean | null; returnDayOfMonth?: number | null; returnYear?: number | null; returnMonth?: number | null;
  shopId: number; shopName: string; year: number; month: number; dayOfMonth: number; fromTime: string; toTime: string;
  shiftSwapRequestId?: number | null; resultingShiftSwapRequestId?: number | null;
  requestKind?: "give" | "exchange" | null; targetEmployeeId?: number | null;
  returnScheduleId?: number | null; returnScheduleSlotId?: number | null; returnFromTime?: string | null; returnToTime?: string | null;
  createdAtUtc: string; reviewedAtUtc?: string | null; reviewedByManagerId?: number | null; cancelledAtUtc?: string | null;
  approvalReason?: string | null; failureReason?: string | null; isStale: boolean;
  minimumShopName?: string | null; minimumYear?: number | null; minimumMonth?: number | null;
  minimumMinutes?: number | null; projectedMinutes?: number | null;
};
