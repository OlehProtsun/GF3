import type { AvailabilityKind, AvailabilitySlot } from "@entities/availability-groups/model/types";

export type EmployeeAvailabilityGroup = {
  id: number;
  name: string;
  year: number;
  month: number;
  visibleFromUtc?: string | null;
  visibleToUtc?: string | null;
  canSubmit: boolean;
  employeeLastModifiedAtUtc?: string | null;
  slots: AvailabilitySlot[];
};

export type SaveEmployeeAvailabilitySlot = {
  dayOfMonth: number;
  kind: AvailabilityKind;
  intervalStr?: string | null;
};

export type SaveEmployeeAvailabilityPayload = {
  slots: SaveEmployeeAvailabilitySlot[];
};
