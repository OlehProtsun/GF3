export type SlotStatus = "Free" | "Working" | "Blocked" | number;
export type GraphPublicationStatus = "private" | "public";

export type Container = {
  id: number;
  name: string;
  note?: string | null;
};

export type Graph = {
  id: number;
  containerId: number;
  shopId: number;
  name: string;
  year: number;
  month: number;
  publicationStatus: GraphPublicationStatus;
  allowSwap?: boolean;
  peoplePerShift: number;
  shift1Time: string;
  shift2Time: string;
  maxHoursPerEmpMonth: number;
  maxConsecutiveDays: number;
  maxConsecutiveFull: number;
  maxFullPerMonth: number;
  note?: string | null;
  availabilityGroupId?: number | null;
  lastUpdatedAtUtc?: string | null;
};

export type SchedulePresetEmployee = {
  id: number;
  employeeId: number;
  minHoursMonth: number;
};

export type SchedulePreset = {
  id: number;
  containerId: number;
  name: string;
  scheduleName: string;
  shopId: number;
  year: number;
  month: number;
  peoplePerShift: number;
  shift1Time: string;
  shift2Time: string;
  maxHoursPerEmpMonth: number;
  maxConsecutiveDays: number;
  maxConsecutiveFull: number;
  maxFullPerMonth: number;
  availabilityGroupId?: number | null;
  employees: SchedulePresetEmployee[];
};

export type GraphSlot = {
  id: number;
  scheduleId: number;
  dayOfMonth: number;
  slotNo: number;
  fromTime: string;
  toTime: string;
  employeeId?: number | null;
  status: SlotStatus;
};

export type GraphEmployee = {
  id: number;
  scheduleId: number;
  employeeId: number;
  minHoursMonth?: number | null;
  displayOrder: number;
};

export type GraphCellStyle = {
  id: number;
  scheduleId: number;
  dayOfMonth: number;
  employeeId: number;
  backgroundColorArgb?: number | null;
  textColorArgb?: number | null;
};
