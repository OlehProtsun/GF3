import type { AvailabilityGroup, AvailabilityGroupItem, AvailabilityGroupMember, AvailabilitySlot } from "@entities/availability-groups/model/types";

export type AvailabilityGroupDto = AvailabilityGroup;
export type AvailabilityGroupItemDto = AvailabilityGroupItem;
export type AvailabilityGroupMemberDto = AvailabilityGroupMember;
export type AvailabilitySlotDto = AvailabilitySlot;

export type SaveAvailabilityGroupDto = {
  name: string;
  year: number;
  month: number;
  publicationStatus?: AvailabilityGroup["publicationStatus"];
  visibleFromUtc?: string | null;
  visibleToUtc?: string | null;
};
export type SaveAvailabilityGroupMemberDto = { employeeId: number; displayOrder: number };
export type SaveAvailabilitySlotDto = {
  availabilityGroupMemberId: number;
  dayOfMonth: number;
  kind: number | string;
  intervalStr?: string | null;
};
