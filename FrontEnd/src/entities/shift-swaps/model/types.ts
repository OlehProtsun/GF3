export type ShiftSwapStatus = "open" | "accepted" | "cancelled";
export type ShiftSwapVisibility = "public" | "private";

export type ShiftSwapScheduleSnapshotRow = {
  employeeId: number;
  employeeName: string;
  kind: "employee" | "manual";
  dayValues: Record<number, string>;
};

export type ShiftSwapScheduleSnapshot = {
  rows: ShiftSwapScheduleSnapshotRow[];
};

export type ShiftSwap = {
  id: number;
  scheduleId: number;
  scheduleSlotId: number;
  scheduleName: string;
  containerName: string;
  shopName: string;
  year: number;
  month: number;
  dayOfMonth: number;
  fromTime: string;
  toTime: string;
  fromEmployeeId?: number | null;
  fromEmployeeName: string;
  targetEmployeeId?: number | null;
  targetEmployeeName?: string | null;
  acceptedByEmployeeId?: number | null;
  acceptedByEmployeeName?: string | null;
  visibility: ShiftSwapVisibility;
  status: ShiftSwapStatus;
  createdAtUtc: string;
  acceptedAtUtc?: string | null;
  shiftHours: number;
  currentEmployeeHoursBefore: number;
  currentEmployeeHoursAfter: number;
  currentEmployeeWorkDaysBefore: number;
  currentEmployeeWorkDaysAfter: number;
  currentEmployeeFreeDaysBefore: number;
  currentEmployeeFreeDaysAfter: number;
  fromEmployeeHoursBefore: number;
  fromEmployeeHoursAfter: number;
  isManagerCreated: boolean;
  manualColumnId?: number | null;
  manualColumnName?: string | null;
  isCreatedByCurrentEmployee: boolean;
  isScheduleLocked: boolean;
  canAccept: boolean;
  acceptanceUnavailableReason?: string | null;
  canCancel: boolean;
  beforeSnapshot?: ShiftSwapScheduleSnapshot | null;
  afterSnapshot?: ShiftSwapScheduleSnapshot | null;
};

export type ShiftSwapEmployee = {
  id: number;
  firstName: string;
  lastName: string;
  displayName: string;
};

export type CreateShiftSwapInput = {
  scheduleId: number;
  scheduleSlotId: number;
  fromTime: string;
  toTime: string;
  targetEmployeeId?: number | null;
};

export type CreateManagerShiftSwapInput = {
  containerId: number;
  graphId: number;
  manualColumnId: number;
  dayOfMonth: number;
  fromTime: string;
  toTime: string;
  targetEmployeeId?: number | null;
};

export type ShiftSwapHighlightSetting = {
  highlightColor: string;
};
