export type ShiftCorrectionStatus = "pending" | "approved" | "rejected";

export type ShiftCorrectionRequest = {
  id: number;
  containerId: number;
  scheduleId: number;
  scheduleSlotId: number | null;
  isDayOffRequest: boolean;
  scheduleName: string;
  shopName: string;
  year: number;
  month: number;
  dayOfMonth: number;
  employeeId: number;
  employeeName: string;
  originalFromTime: string | null;
  originalToTime: string | null;
  requestedFromTime: string;
  requestedToTime: string;
  status: ShiftCorrectionStatus;
  createdAtUtc: string;
  reviewedAtUtc?: string | null;
  reviewedByManagerName?: string | null;
};

export type CreateShiftCorrectionInput = {
  scheduleId: number;
  requestedFromTime: string;
  requestedToTime: string;
} & ({ scheduleSlotId: number; dayOfMonth?: never } | { dayOfMonth: number; scheduleSlotId?: never });

export type ShiftCorrectionSetting = {
  highlightColor: string;
};
