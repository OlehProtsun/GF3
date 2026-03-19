export type AvailabilityKind = "Unavailable" | "Available" | "Preferred" | number;

export type AvailabilityGroup = {
  id: number;
  name: string;
  year: number;
  month: number;
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
};

export type AvailabilitySlot = {
  id: number;
  availabilityGroupMemberId: number;
  dayOfMonth: number;
  kind: AvailabilityKind;
  intervalStr?: string | null;
};
