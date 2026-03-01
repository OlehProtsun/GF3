export type SlotStatus = "Free" | "Working" | "Blocked" | number;

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
  peoplePerShift: number;
  shift1Time: string;
  shift2Time: string;
  maxHoursPerEmpMonth: number;
  maxConsecutiveDays: number;
  maxConsecutiveFull: number;
  maxFullPerMonth: number;
  note?: string | null;
  availabilityGroupId?: number | null;
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
};

export type GraphCellStyle = {
  id: number;
  scheduleId: number;
  dayOfMonth: number;
  employeeId: number;
  backgroundColorArgb?: number | null;
  textColorArgb?: number | null;
};
