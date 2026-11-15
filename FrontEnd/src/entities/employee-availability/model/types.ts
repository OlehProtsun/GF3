import type { AvailabilityKind, AvailabilitySlot } from "@entities/availability-groups/model/types";

export type EmployeeAvailabilityMember = {
  employeeId: number;
  firstName: string;
  lastName: string;
  displayOrder: number;
  slots: AvailabilitySlot[];
};

export type EmployeeAvailabilityGroup = {
  isAutomatic?: boolean;
  id: number;
  name: string;
  year: number;
  month: number;
  visibleFromUtc?: string | null;
  visibleToUtc?: string | null;
  publishedAtUtc?: string | null;
  notificationId?: string | null;
  notificationOccurredAtUtc?: string | null;
  canSubmit: boolean;
  isEditLocked: boolean;
  editLockedBy?: string | null;
  employeeLastModifiedAtUtc?: string | null;
  slots: AvailabilitySlot[];
  members?: EmployeeAvailabilityMember[];
};

export type SaveEmployeeAvailabilitySlot = {
  dayOfMonth: number;
  kind: AvailabilityKind;
  intervalStr?: string | null;
};

export type SaveEmployeeAvailabilityPayload = {
  slots: SaveEmployeeAvailabilitySlot[];
};
