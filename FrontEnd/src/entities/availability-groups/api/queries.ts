import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import { availabilityGroupsApi } from "./availabilityGroupsApi";
import type { SaveAvailabilityGroupDto, SaveAvailabilityGroupMemberDto, SaveAvailabilitySlotDto } from "./dto";

export const useAvailabilityGroupsListQuery = () => useQuery({ queryKey: queryKeys.availabilityGroups.list(), queryFn: ({ signal }) => availabilityGroupsApi.list(signal) });
export const useAvailabilityGroupByIdQuery = (id: number | null) => useQuery({ queryKey: ["availabilityGroups", "byId", id ?? 0], enabled: id !== null, queryFn: ({ signal }) => availabilityGroupsApi.byId(id as number, signal) });
export const useAvailabilityGroupItemsQuery = (id: number | null) => useQuery({ queryKey: ["availabilityGroups", "items", id ?? 0], enabled: id !== null, queryFn: ({ signal }) => availabilityGroupsApi.items(id as number, signal) });

export function useCreateAvailabilityGroupMutation() {
  const qc = useQueryClient();
  return useMutation({ mutationFn: (payload: SaveAvailabilityGroupDto) => availabilityGroupsApi.create(payload), onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.all }) });
}
export function useUpdateAvailabilityGroupMutation() {
  const qc = useQueryClient();
  return useMutation({ mutationFn: ({ id, payload }: { id: number; payload: SaveAvailabilityGroupDto }) => availabilityGroupsApi.update(id, payload), onSuccess: (_, v) => { qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.all }); qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.byId(v.id) }); } });
}
export function useDeleteAvailabilityGroupMutation() {
  const qc = useQueryClient();
  return useMutation({ mutationFn: (id: number) => availabilityGroupsApi.remove(id), onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.all }) });
}

export const useAvailabilityGroupMembersQuery = (groupId: number | null) => useQuery({ queryKey: ["availabilityGroups", groupId ?? 0, "members"], enabled: groupId !== null, queryFn: ({ signal }) => availabilityGroupsApi.members(groupId as number, signal) });
export function useCreateAvailabilityGroupMemberMutation() {
  const qc = useQueryClient();
  return useMutation({ mutationFn: ({ groupId, payload }: { groupId: number; payload: SaveAvailabilityGroupMemberDto }) => availabilityGroupsApi.createMember(groupId, payload), onSuccess: (_, v) => qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.members(v.groupId) }) });
}
export function useUpdateAvailabilityGroupMemberMutation() {
  const qc = useQueryClient();
  return useMutation({ mutationFn: ({ groupId, memberId, payload }: { groupId: number; memberId: number; payload: SaveAvailabilityGroupMemberDto }) => availabilityGroupsApi.updateMember(groupId, memberId, payload), onSuccess: (_, v) => qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.members(v.groupId) }) });
}
export function useDeleteAvailabilityGroupMemberMutation() {
  const qc = useQueryClient();
  return useMutation({ mutationFn: ({ groupId, memberId }: { groupId: number; memberId: number }) => availabilityGroupsApi.removeMember(groupId, memberId), onSuccess: (_, v) => qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.members(v.groupId) }) });
}

export const useAvailabilityGroupSlotsQuery = (groupId: number | null) => useQuery({ queryKey: ["availabilityGroups", groupId ?? 0, "slots"], enabled: groupId !== null, queryFn: ({ signal }) => availabilityGroupsApi.slots(groupId as number, signal) });
export function useCreateAvailabilitySlotMutation() {
  const qc = useQueryClient();
  return useMutation({ mutationFn: ({ groupId, payload }: { groupId: number; payload: SaveAvailabilitySlotDto }) => availabilityGroupsApi.createSlot(groupId, payload), onSuccess: (_, v) => qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.slots(v.groupId) }) });
}
export function useUpdateAvailabilitySlotMutation() {
  const qc = useQueryClient();
  return useMutation({ mutationFn: ({ groupId, slotId, payload }: { groupId: number; slotId: number; payload: SaveAvailabilitySlotDto }) => availabilityGroupsApi.updateSlot(groupId, slotId, payload), onSuccess: (_, v) => qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.slots(v.groupId) }) });
}
export function useDeleteAvailabilitySlotMutation() {
  const qc = useQueryClient();
  return useMutation({ mutationFn: ({ groupId, slotId }: { groupId: number; slotId: number }) => availabilityGroupsApi.removeSlot(groupId, slotId), onSuccess: (_, v) => qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.slots(v.groupId) }) });
}
