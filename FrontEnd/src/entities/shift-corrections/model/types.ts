export type ShiftCorrectionStatus = "pending" | "approved" | "rejected";

export type ShiftCorrectionRequest = {
  id: number;
  containerId: number;
  scheduleId: number;
  scheduleSlotId: number | null;
  isDayOffRequest: boolean;
  requestType?: "adjustShift" | "addShift" | "removeShift";
  originalShifts?: Array<{ slotId: number; fromTime: string; toTime: string }>;
  scheduleName: string;
  shopName: string;
  year: number;
  month: number;
  dayOfMonth: number;
  employeeId: number;
  employeeName: string;
  originalFromTime: string | null;
  originalToTime: string | null;
  requestedFromTime: string | null;
  requestedToTime: string | null;
  comment: string | null;
  status: ShiftCorrectionStatus;
  createdAtUtc: string;
  reviewedAtUtc?: string | null;
  reviewedByManagerName?: string | null;
};

export type CreateShiftCorrectionInput = {
  scheduleId: number;
  comment?: string | null;
} & (
  | ({ requestType?: never; requestedFromTime: string; requestedToTime: string } &
    ({ scheduleSlotId: number; dayOfMonth?: never } | { dayOfMonth: number; scheduleSlotId?: never }))
  | { requestType: "removeShift"; dayOfMonth: number; scheduleSlotId?: never; requestedFromTime?: never; requestedToTime?: never }
);

export type ShiftCorrectionSetting = {
  highlightColor: string;
};
