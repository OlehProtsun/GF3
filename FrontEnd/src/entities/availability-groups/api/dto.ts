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

export type SaveAvailabilityGroupGraphDayDto = {
  dayOfMonth: number;
  kind: number;
  intervalStr: string | null;
};

export type SaveAvailabilityGroupGraphMemberDto = {
  employeeId: number;
  days: SaveAvailabilityGroupGraphDayDto[];
};

export type SaveAvailabilityGroupGraphTransferDto = {
  employeeId: number;
  sourceGroupId: number;
  dayOfMonths: number[];
};

export type SaveAvailabilityGroupGraphDto = SaveAvailabilityGroupDto & {
  id: number | null;
  members: SaveAvailabilityGroupGraphMemberDto[];
  transfers: SaveAvailabilityGroupGraphTransferDto[];
};

export type SaveAvailabilityGroupGraphResult = {
  id: number;
  sourceGroupIds: number[];
  changed: boolean;
};
