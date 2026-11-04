export type AvailabilityKind = "Unavailable" | "Available" | "Preferred" | number;
export type AvailabilityPublicationStatus = "private" | "public";

export type AvailabilityGroup = {
  id: number;
  name: string;
  year: number;
  month: number;
  publicationStatus: AvailabilityPublicationStatus;
  visibleFromUtc?: string | null;
  visibleToUtc?: string | null;
};

export type AvailabilityGroupItem = {
  memberId: number;
  employeeId: number;
  displayOrder: number;
  dayId: number;
  dayOfMonth: number;
  kind: AvailabilityKind;
  intervalStr?: string | null;
};

export type AvailabilityGroupMember = {
  id: number;
  availabilityGroupId: number;
  employeeId: number;
  displayOrder: number;
  employeeLastModifiedAtUtc?: string | null;
};

export type AvailabilitySlot = {
  id: number;
  availabilityGroupMemberId: number;
  dayOfMonth: number;
  kind: AvailabilityKind;
  intervalStr?: string | null;
};
