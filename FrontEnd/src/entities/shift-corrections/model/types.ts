export type ShiftCorrectionStatus = "pending" | "approved" | "rejected";

export type ShiftCorrectionRequest = {
  id: number;
  containerId: number;
  scheduleId: number;
  scheduleSlotId: number;
  scheduleName: string;
  shopName: string;
  year: number;
  month: number;
  dayOfMonth: number;
  employeeId: number;
  employeeName: string;
  originalFromTime: string;
  originalToTime: string;
  requestedFromTime: string;
  requestedToTime: string;
  status: ShiftCorrectionStatus;
  createdAtUtc: string;
  reviewedAtUtc?: string | null;
  reviewedByManagerName?: string | null;
};

export type CreateShiftCorrectionInput = {
  scheduleId: number;
  scheduleSlotId: number;
  requestedFromTime: string;
  requestedToTime: string;
};

export type ShiftCorrectionSetting = {
  highlightColor: string;
};
