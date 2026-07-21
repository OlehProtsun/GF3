import type {
  Container,
  Graph,
  GraphCellStyle,
  GraphEmployee,
  GraphSlot,
  SchedulePreset,
  SchedulePresetEmployee,
} from "@entities/containers/model/types";

export type ContainerDto = Container;
export type GraphDto = Graph;
export type GraphSlotDto = GraphSlot;
export type GraphEmployeeDto = GraphEmployee;
export type GraphCellStyleDto = GraphCellStyle;
export type SchedulePresetDto = SchedulePreset;
export type SchedulePresetEmployeeDto = SchedulePresetEmployee;

export type SaveContainerDto = { name: string; note?: string };
export type SaveGraphDto = {
  shopId: number;
  name: string;
  year: number;
  month: number;
  publicationStatus?: Graph["publicationStatus"];
  allowSwap?: boolean;
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
export type GenerateGraphPreviewEmployeeDto = {
  employeeId: number;
  minHoursMonth?: number | null;
  displayOrder: number;
};
export type GenerateGraphPreviewRequestDto = {
  graphId?: number | null;
  graph: SaveGraphDto;
  employees: GenerateGraphPreviewEmployeeDto[];
};
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

export type ReplaceGraphSlotsDto = {
  slots: SaveGraphSlotDto[];
};

export type SaveGraphEmployeeDto = {
  employeeId: number;
  minHoursMonth?: number | null;
  displayOrder: number;
};

export type SaveSchedulePresetEmployeeDto = {
  employeeId: number;
  minHoursMonth: number;
};

export type SaveSchedulePresetDto = {
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
  employees: SaveSchedulePresetEmployeeDto[];
};

export type UpsertGraphCellStyleDto = {
  dayOfMonth: number;
  employeeId: number;
  backgroundColorArgb?: number | null;
  textColorArgb?: number | null;
};
