import { t } from "@shared/i18n";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  AVAILABILITY_NONE_MARK,
  getAvailabilityCellKey,
  getAvailabilityGroupNameForSave,
  parseAvailabilityCode,
  type AvailabilityMatrixCellMap,
} from "@entities/availability-groups/model/editor";
import type { StagedAvailabilityTransfer } from "@entities/availability-groups/model/transfer";
import { queryKeys } from "@shared/api/queryKeys";
import { availabilityGroupsApi } from "./availabilityGroupsApi";
import type { SaveAvailabilityGroupDto, SaveAvailabilityGroupGraphResult, SaveAvailabilityGroupMemberDto, SaveAvailabilitySlotDto } from "./dto";

export type SaveAvailabilityGroupGraphInput = {
  id: number | null;
  payload: SaveAvailabilityGroupDto;
  employeeIds: number[];
  cellMap: AvailabilityMatrixCellMap;
  transfers?: StagedAvailabilityTransfer[];
};

export const useAvailabilityGroupsListQuery = (refreshKey?: string) =>
  useQuery({
    queryKey: refreshKey
      ? [...queryKeys.availabilityGroups.list(), refreshKey]
      : queryKeys.availabilityGroups.list(),
    staleTime: 300_000,
    queryFn: ({ signal }) => availabilityGroupsApi.list(signal),
  });

export const useAvailabilityGroupByIdQuery = (id: number | null) =>
  useQuery({
    queryKey: queryKeys.availabilityGroups.byId(id ?? 0),
    enabled: id !== null,
    queryFn: ({ signal }) => availabilityGroupsApi.byId(id as number, signal),
  });

export const useAvailabilityGroupItemsQuery = (id: number | null) =>
  useQuery({
    queryKey: queryKeys.availabilityGroups.items(id ?? 0),
    enabled: id !== null,
    queryFn: ({ signal }) => availabilityGroupsApi.items(id as number, signal),
  });

async function syncAvailabilityGroupGraph({
  id,
  payload,
  employeeIds,
  cellMap,
  transfers = [],
}: SaveAvailabilityGroupGraphInput): Promise<SaveAvailabilityGroupGraphResult> {
  const selectedEmployees = new Set(employeeIds);
  const daysInMonth = new Date(payload.year, payload.month, 0).getDate();
  const members = [...selectedEmployees].map(employeeId => ({
    employeeId,
    days: Array.from({ length: daysInMonth }, (_, index) => {
      const dayOfMonth = index + 1;
      const rawCode = cellMap[getAvailabilityCellKey(employeeId, dayOfMonth)] ?? AVAILABILITY_NONE_MARK;
      const parsedCode = parseAvailabilityCode(rawCode);
      if (!parsedCode.ok) {
        throw new Error(t("Employee #{0}, day {1}: {2}", employeeId, dayOfMonth, parsedCode.error));
      }

      return { dayOfMonth, kind: parsedCode.value.kind, intervalStr: parsedCode.value.intervalStr };
    }),
  }));

  const normalizedTransfers = transfers
    .filter(transfer =>
      selectedEmployees.has(transfer.employeeId) &&
      transfer.sourceGroupId !== id &&
      transfer.dayOfMonths.length > 0)
    .map(transfer => ({
      ...transfer,
      dayOfMonths: [...new Set(transfer.dayOfMonths)].sort((left, right) => left - right),
    }));

  return availabilityGroupsApi.saveGraph({
    ...payload,
    id,
    name: getAvailabilityGroupNameForSave(payload.name, payload.month, payload.year, id === null),
    members,
    transfers: normalizedTransfers,
  });
}

export function useCreateAvailabilityGroupMutation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: SaveAvailabilityGroupDto) => availabilityGroupsApi.create(payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.all }),
  });
}

export function useUpdateAvailabilityGroupMutation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: number; payload: SaveAvailabilityGroupDto }) =>
      availabilityGroupsApi.update(id, payload),
    onSuccess: (_, variables) => {
      qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.all });
      qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.byId(variables.id) });
      qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.items(variables.id) });
      qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.members(variables.id) });
      qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.slots(variables.id) });
    },
  });
}

export function useSaveAvailabilityGroupGraphMutation() {
  const qc = useQueryClient();

  return useMutation({
    mutationFn: syncAvailabilityGroupGraph,
    onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.all }),
  });
}

export function useDeleteAvailabilityGroupMutation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => availabilityGroupsApi.remove(id),
    onSuccess: (_, deletedId) => {
      qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.all });
      qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.byId(deletedId) });
      qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.items(deletedId) });
      qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.members(deletedId) });
      qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.slots(deletedId) });
    },
  });
}

export const useAvailabilityGroupMembersQuery = (groupId: number | null) =>
  useQuery({
    queryKey: queryKeys.availabilityGroups.members(groupId ?? 0),
    enabled: groupId !== null,
    queryFn: ({ signal }) => availabilityGroupsApi.members(groupId as number, signal),
  });

export function useCreateAvailabilityGroupMemberMutation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ groupId, payload }: { groupId: number; payload: SaveAvailabilityGroupMemberDto }) =>
      availabilityGroupsApi.createMember(groupId, payload),
    onSuccess: (_, variables) => {
      qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.members(variables.groupId) });
      qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.items(variables.groupId) });
    },
  });
}

export function useUpdateAvailabilityGroupMemberMutation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ groupId, memberId, payload }: { groupId: number; memberId: number; payload: SaveAvailabilityGroupMemberDto }) =>
      availabilityGroupsApi.updateMember(groupId, memberId, payload),
    onSuccess: (_, variables) => {
      qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.members(variables.groupId) });
      qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.items(variables.groupId) });
    },
  });
}

export function useDeleteAvailabilityGroupMemberMutation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ groupId, memberId }: { groupId: number; memberId: number }) =>
      availabilityGroupsApi.removeMember(groupId, memberId),
    onSuccess: (_, variables) => {
      qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.members(variables.groupId) });
      qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.items(variables.groupId) });
    },
  });
}

export const useAvailabilityGroupSlotsQuery = (groupId: number | null) =>
  useQuery({
    queryKey: queryKeys.availabilityGroups.slots(groupId ?? 0),
    enabled: groupId !== null,
    queryFn: ({ signal }) => availabilityGroupsApi.slots(groupId as number, signal),
  });

export function useCreateAvailabilitySlotMutation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ groupId, payload }: { groupId: number; payload: SaveAvailabilitySlotDto }) =>
      availabilityGroupsApi.createSlot(groupId, payload),
    onSuccess: (_, variables) => {
      qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.slots(variables.groupId) });
      qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.items(variables.groupId) });
    },
  });
}

export function useUpdateAvailabilitySlotMutation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ groupId, slotId, payload }: { groupId: number; slotId: number; payload: SaveAvailabilitySlotDto }) =>
      availabilityGroupsApi.updateSlot(groupId, slotId, payload),
    onSuccess: (_, variables) => {
      qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.slots(variables.groupId) });
      qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.items(variables.groupId) });
    },
  });
}

export function useDeleteAvailabilitySlotMutation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ groupId, slotId }: { groupId: number; slotId: number }) =>
      availabilityGroupsApi.removeSlot(groupId, slotId),
    onSuccess: (_, variables) => {
      qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.slots(variables.groupId) });
      qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.items(variables.groupId) });
    },
  });
}



export const useAvailabilityTransferPreviewQuery = (
  employeeIds: number[],
  year: number,
  month: number,
  targetGroupId: number | null,
) => {
  const normalizedEmployeeIds = [...new Set(employeeIds)].sort((left, right) => left - right);
  const employeeIdsKey = normalizedEmployeeIds.join(",");

  return useQuery({
    queryKey: queryKeys.availabilityGroups.transferPreview(employeeIdsKey, year, month, targetGroupId),
    enabled: normalizedEmployeeIds.length > 0 && month >= 1 && month <= 12,
    queryFn: ({ signal }) =>
      availabilityGroupsApi.transferPreview(normalizedEmployeeIds, year, month, targetGroupId, signal),
  });
};

export const useAvailabilityTransferSourcesQuery = (groupId: number | null, memberId: number | null) =>
  useQuery({
    queryKey: queryKeys.availabilityGroups.transferSources(groupId ?? 0, memberId ?? 0),
    enabled: groupId !== null && memberId !== null,
    queryFn: ({ signal }) => availabilityGroupsApi.transferSources(groupId as number, memberId as number, signal),
  });

export const useAvailabilityTransferHintsQuery = (groupId: number | null) =>
  useQuery({
    queryKey: queryKeys.availabilityGroups.transferHints(groupId ?? 0),
    enabled: groupId !== null,
    queryFn: ({ signal }) => availabilityGroupsApi.transferHints(groupId as number, signal),
  });

export type TransferAvailabilityDaysInput = {
  groupId: number;
  memberId: number;
  sourceGroupId: number;
  dayOfMonths: number[];
};

export function useTransferAvailabilityDaysMutation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      groupId,
      memberId,
      sourceGroupId,
      dayOfMonths,
    }: TransferAvailabilityDaysInput) =>
      availabilityGroupsApi.transferDays(
        groupId,
        memberId,
        { sourceGroupId, dayOfMonths },
      ),
    onSuccess: (_, variables) => {
      const sourceGroupId = variables.sourceGroupId;
      const targetGroupId = variables.groupId;
      qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.all });
      qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.slots(targetGroupId) });
      qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.items(targetGroupId) });
      qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.transferHints(targetGroupId) });
      qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.slots(sourceGroupId) });
      qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.items(sourceGroupId) });
      qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.transferHints(sourceGroupId) });
      qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.transferSources(targetGroupId, variables.memberId) });
    },
  });
}
