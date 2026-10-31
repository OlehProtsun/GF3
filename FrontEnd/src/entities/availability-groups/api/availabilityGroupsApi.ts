import { request } from "@shared/api/httpClient";
import type {
  AvailabilityTransferHint,
  AvailabilityTransferResult,
  AvailabilityTransferSource,
} from "@entities/availability-groups/model/transfer";
import type {
  AvailabilityGroupDto,
  AvailabilityGroupItemDto,
  AvailabilityGroupMemberDto,
  AvailabilitySlotDto,
  SaveAvailabilityGroupDto,
  SaveAvailabilityGroupMemberDto,
  SaveAvailabilitySlotDto,
} from "./dto";

const endpoint = "availability-groups";

export const availabilityGroupsApi = {
  list: (signal?: AbortSignal) => request<AvailabilityGroupDto[]>(endpoint, { signal }),
  byId: (id: number, signal?: AbortSignal) => request<AvailabilityGroupDto>(`${endpoint}/${id}`, { signal }),
  items: (id: number, signal?: AbortSignal) => request<AvailabilityGroupItemDto[]>(`${endpoint}/${id}/items`, { signal }),
  create: (payload: SaveAvailabilityGroupDto) => request<AvailabilityGroupDto>(endpoint, { method: "POST", body: payload }),
  update: (id: number, payload: SaveAvailabilityGroupDto) => request<void>(`${endpoint}/${id}`, { method: "PUT", body: payload }),
  remove: (id: number) => request<void>(`${endpoint}/${id}`, { method: "DELETE" }),

  members: (groupId: number, signal?: AbortSignal) => request<AvailabilityGroupMemberDto[]>(`${endpoint}/${groupId}/members`, { signal }),
  createMember: (groupId: number, payload: SaveAvailabilityGroupMemberDto) =>
    request<AvailabilityGroupMemberDto>(`${endpoint}/${groupId}/members`, { method: "POST", body: payload }),
  updateMember: (groupId: number, memberId: number, payload: SaveAvailabilityGroupMemberDto) =>
    request<void>(`${endpoint}/${groupId}/members/${memberId}`, { method: "PUT", body: payload }),
  removeMember: (groupId: number, memberId: number) => request<void>(`${endpoint}/${groupId}/members/${memberId}`, { method: "DELETE" }),

  slots: (groupId: number, signal?: AbortSignal) => request<AvailabilitySlotDto[]>(`${endpoint}/${groupId}/slots`, { signal }),
  createSlot: (groupId: number, payload: SaveAvailabilitySlotDto) =>
    request<AvailabilitySlotDto>(`${endpoint}/${groupId}/slots`, { method: "POST", body: payload }),
  updateSlot: (groupId: number, slotId: number, payload: SaveAvailabilitySlotDto) =>
    request<void>(`${endpoint}/${groupId}/slots/${slotId}`, { method: "PUT", body: payload }),
  removeSlot: (groupId: number, slotId: number) => request<void>(`${endpoint}/${groupId}/slots/${slotId}`, { method: "DELETE" }),

  transferSources: (groupId: number, memberId: number, signal?: AbortSignal) =>
    request<AvailabilityTransferSource[]>(endpoint + "/" + groupId + "/members/" + memberId + "/transfer-sources", { signal }),
  transferPreview: (
    employeeIds: number[],
    year: number,
    month: number,
    targetGroupId: number | null,
    signal?: AbortSignal,
  ) => request<AvailabilityTransferSource[]>(endpoint + "/transfer-preview", {
    signal,
    query: {
      employeeIds: [...new Set(employeeIds)].sort((left, right) => left - right).join(","),
      year,
      month,
      targetGroupId,
    },
  }),
  transferHints: (groupId: number, signal?: AbortSignal) =>
    request<AvailabilityTransferHint[]>(endpoint + "/" + groupId + "/transfer-hints", { signal }),
  transferDays: (
    groupId: number,
    memberId: number,
    payload: { sourceGroupId: number; dayOfMonths: number[] },
  ) => request<AvailabilityTransferResult>(
    endpoint + "/" + groupId + "/members/" + memberId + "/transfer-days",
    { method: "POST", body: payload },
  ),
};
