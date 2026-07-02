export type EmployeeScheduleSlot = {
  id: number;
  dayOfMonth: number;
  slotNo: number;
  employeeId?: number | null;
  fromTime: string;
  toTime: string;
  status: string;
};

export type EmployeeScheduleEmployee = {
  id: number;
  employeeId: number;
  firstName: string;
  lastName: string;
  displayName: string;
  minHoursMonth?: number | null;
  displayOrder: number;
};

export type EmployeeScheduleRelatedAssignment = {
  employeeId: number;
  dayOfMonth: number;
  scheduleId: number;
  scheduleName: string;
};

export type EmployeeSchedule = {
  id: number;
  containerId: number;
  containerName: string;
  shopId: number;
  shopName: string;
  name: string;
  note?: string | null;
  year: number;
  month: number;
  publicationStatus: "public" | "private";
  lastUpdatedAtUtc?: string | null;
  employees?: EmployeeScheduleEmployee[];
  slots: EmployeeScheduleSlot[];
  relatedScheduleAssignments?: EmployeeScheduleRelatedAssignment[];
};
