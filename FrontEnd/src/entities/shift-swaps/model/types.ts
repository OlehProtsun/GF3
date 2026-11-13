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
  isAwaitingManagerApproval?: boolean;
  pickupApprovalPolicyApplies?: boolean;
  requiresManagerApproval?: boolean;
  canRequestManagerApproval?: boolean;
  approvalPendingForCurrentEmployee?: boolean;
  allowPartialAcceptance: boolean;
  canAcceptFull: boolean;
  eligibleClaimWindows: Array<{ fromTime: string; toTime: string }>;
  requestKind?: "give" | "exchange";
  returnScheduleId?: number | null;
  returnScheduleSlotId?: number | null;
  returnScheduleName?: string | null;
  returnShopName?: string | null;
  returnYear?: number | null;
  returnMonth?: number | null;
  returnDayOfMonth?: number | null;
  returnFromTime?: string | null;
  returnToTime?: string | null;
  returnShiftHours?: number | null;
  returnBeforeSnapshot?: ShiftSwapScheduleSnapshot | null;
  returnAfterSnapshot?: ShiftSwapScheduleSnapshot | null;
  currentEmployeeProjectedMonths?: ShiftSwapPreviewMonthStats[] | null;
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
  requiresWholeEightHourShifts?: boolean;
  id: number;
  firstName: string;
  lastName: string;
  displayName: string;
};

export type CreateShiftSwapInput = {
  allowPartialAcceptance?: boolean;
  requestKind?: "give" | "exchange";
  returnScheduleId?: number;
  returnScheduleSlotId?: number;
  returnFromTime?: string;
  returnToTime?: string;
  scheduleId: number;
  scheduleSlotId: number;
  fromTime: string;
  toTime: string;
  targetEmployeeId?: number | null;
};

export type SwapWizardRequest = CreateShiftSwapInput & (
  | { requestKind: "give"; returnScheduleId?: never; returnScheduleSlotId?: never; returnFromTime?: never; returnToTime?: never }
  | { requestKind: "exchange"; targetEmployeeId: number; returnScheduleId: number; returnScheduleSlotId: number; returnFromTime: string; returnToTime: string }
);

export type ShiftSwapReturnCandidate = {
  requiresWholeShift?: boolean;
  scheduleId: number; scheduleName: string; shopName: string;
  year: number; month: number; dayOfMonth: number; slotId: number;
  fromTime: string; toTime: string; durationMinutes: number;
  canSelect: boolean; unavailableReason?: string | null;
  suggestedFromTime?: string | null; suggestedToTime?: string | null;
  eligibleWindows?: { fromTime: string; toTime: string }[];
};
export type ShiftSwapReturnCandidatePage = {
  items: ShiftSwapReturnCandidate[]; page: number; pageSize: number; hasMore: boolean;
};
export type ShiftSwapCandidateParams = {
  sourceScheduleId: number; sourceSlotId: number; fromTime: string; toTime: string;
  targetEmployeeId: number; page?: number; pageSize?: number;
};
export type ShiftSwapPreviewMonthStats = {
  year: number; month: number; hoursBefore: number; hoursAfter: number;
  workDaysBefore: number; workDaysAfter: number; freeDaysBefore: number; freeDaysAfter: number;
};
export type ShiftSwapOfferPreview = {
  requiresManagerException?: boolean;
  managerExceptionReason?: string | null;
  minimumMinutes?: number | null;
  projectedMinutes?: number | null;
  allowPartialAcceptance: boolean;
  requestKind: "give" | "exchange"; targetEmployeeId?: number | null; targetEmployeeName: string;
  source: ShiftSwapReturnCandidate; return?: ShiftSwapReturnCandidate | null;
  projectedMonths: ShiftSwapPreviewMonthStats[]; isValid: boolean; unavailableReason?: string | null;
};

export type CreateManagerShiftSwapInput = {
  allowPartialAcceptance: boolean;
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

export type AcceptShiftSwapInput = { id: number; fromTime?: string; toTime?: string };
