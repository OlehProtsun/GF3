import type { Container, Graph, GraphCellStyle, GraphEmployee, GraphSlot } from "@entities/containers/model/types";

export type ContainerDto = Container;
export type GraphDto = Graph;
export type GraphSlotDto = GraphSlot;
export type GraphEmployeeDto = GraphEmployee;
export type GraphCellStyleDto = GraphCellStyle;

export type SaveContainerDto = { name: string; note?: string };
export type SaveGraphDto = {
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
  note?: string;
  availabilityGroupId?: number | null;
};

export type GenerateGraphRequestDto = { overwrite?: boolean; dryRun?: boolean; returnSlots?: boolean };
export type GenerateGraphResponseDto = {
  containerId: number;
  graphId: number;
  generatedSlotsCount: number;
  writtenSlotsCount: number;
  slots?: GraphSlotDto[];
};

export type SaveGraphSlotDto = {
  dayOfMonth: number;
  slotNo: number;
  fromTime: string;
  toTime: string;
  employeeId?: number | null;
  status: number | string;
};

export type SaveGraphEmployeeDto = {
  employeeId: number;
  minHoursMonth?: number | null;
};

export type UpsertGraphCellStyleDto = {
  dayOfMonth: number;
  employeeId: number;
  backgroundColorArgb?: number | null;
  textColorArgb?: number | null;
};
