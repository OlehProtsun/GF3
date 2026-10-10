import { t } from "@shared/i18n";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  AVAILABILITY_NONE_MARK,
  getAvailabilityCellKey,
  getAvailabilityGroupNameForSave,
  parseAvailabilityCode,
  type AvailabilityMatrixCellMap,
} from "@entities/availability-groups/model/editor";
import type { AvailabilityGroupMember, AvailabilitySlot } from "@entities/availability-groups/model/types";
import type { StagedAvailabilityTransfer } from "@entities/availability-groups/model/transfer";
import { queryKeys } from "@shared/api/queryKeys";
import { availabilityGroupsApi } from "./availabilityGroupsApi";
import type { SaveAvailabilityGroupDto, SaveAvailabilityGroupMemberDto, SaveAvailabilitySlotDto } from "./dto";

export type SaveAvailabilityGroupGraphInput = {
  id: number | null;
  payload: SaveAvailabilityGroupDto;
  employeeIds: number[];
  cellMap: AvailabilityMatrixCellMap;
  transfers?: StagedAvailabilityTransfer[];
  existingMembers?: AvailabilityGroupMember[];
  existingSlots?: AvailabilitySlot[];
};

export type SaveAvailabilityGroupGraphResult = {
  id: number;
  sourceGroupIds: number[];
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
  existingMembers = [],
  existingSlots = [],
}: SaveAvailabilityGroupGraphInput): Promise<SaveAvailabilityGroupGraphResult> {
  const isCreate = id === null;
  const groupPayload = {
    ...payload,
    name: getAvailabilityGroupNameForSave(payload.name, payload.month, payload.year, isCreate),
  };

  let groupId = id;

  if (groupId === null) {
    const created = await availabilityGroupsApi.create(groupPayload);
    groupId = created.id;
  } else {
    await availabilityGroupsApi.update(groupId, groupPayload);
  }

  const selectedEmployeeIds = [...new Set(employeeIds)];
  const displayOrderByEmployeeId = new Map(selectedEmployeeIds.map((employeeId, index) => [employeeId, index]));
  const selectedEmployeeIdSet = new Set(selectedEmployeeIds);
  const removedMembers = existingMembers.filter(member => !selectedEmployeeIdSet.has(member.employeeId));
  const removedMemberIdSet = new Set(removedMembers.map(member => member.id));

  const memberByEmployeeId = new Map(
    existingMembers
      .filter(member => !removedMemberIdSet.has(member.id))
      .map(member => [member.employeeId, member] as const)
  );

  const membersToCreate = selectedEmployeeIds.filter(employeeId => !memberByEmployeeId.has(employeeId));
  if (membersToCreate.length > 0) {
    const createdMembers = await Promise.all(
      membersToCreate.map(employeeId =>
        availabilityGroupsApi.createMember(groupId as number, {
          employeeId,
          displayOrder: displayOrderByEmployeeId.get(employeeId) ?? 0,
        } satisfies SaveAvailabilityGroupMemberDto)
      )
    );

    createdMembers.forEach(member => {
      memberByEmployeeId.set(member.employeeId, member);
    });
  }

  const membersToUpdate = selectedEmployeeIds.flatMap(employeeId => {
    const member = memberByEmployeeId.get(employeeId);
    const displayOrder = displayOrderByEmployeeId.get(employeeId);

    if (!member || displayOrder === undefined || member.displayOrder === displayOrder) {
      return [];
    }

    return [
      availabilityGroupsApi.updateMember(groupId as number, member.id, {
        employeeId,
        displayOrder,
      } satisfies SaveAvailabilityGroupMemberDto),
    ];
  });

  const daysInMonth = new Date(payload.year, payload.month, 0).getDate();
  const desiredSlots = selectedEmployeeIds.flatMap(employeeId => {
    const member = memberByEmployeeId.get(employeeId);
    if (!member) {
      throw new Error(t("Employee #{0} could not be mapped to an availability member.", employeeId));
    }

    return Array.from({ length: daysInMonth }, (_, index) => {
      const dayOfMonth = index + 1;
      const rawCode = cellMap[getAvailabilityCellKey(employeeId, dayOfMonth)] ?? AVAILABILITY_NONE_MARK;
      const parsedCode = parseAvailabilityCode(rawCode);

      if (!parsedCode.ok) {
        throw new Error(t("Employee #{0}, day {1}: {2}", employeeId, dayOfMonth, parsedCode.error));
      }

      return {
        key: `${member.id}:${dayOfMonth}`,
        payload: {
          availabilityGroupMemberId: member.id,
          dayOfMonth,
          kind: parsedCode.value.kind,
          intervalStr: parsedCode.value.intervalStr,
        } satisfies SaveAvailabilitySlotDto,
      };
    });
  });

  const desiredSlotKeys = new Set<string>(desiredSlots.map(slot => slot.key));

  const slotsToDelete = existingSlots.filter(slot => !desiredSlotKeys.has(`${slot.availabilityGroupMemberId}:${slot.dayOfMonth}`));
  if (slotsToDelete.length > 0) {
    await Promise.all(slotsToDelete.map(slot => availabilityGroupsApi.removeSlot(groupId as number, slot.id)));
  }

  if (removedMembers.length > 0) {
    await Promise.all(removedMembers.map(member => availabilityGroupsApi.removeMember(groupId as number, member.id)));
  }

  if (membersToUpdate.length > 0) {
    await Promise.all(membersToUpdate);
  }

  const normalizedTransfers = transfers
    .filter(transfer =>
      selectedEmployeeIdSet.has(transfer.employeeId) &&
      transfer.sourceGroupId !== groupId &&
      transfer.dayOfMonths.length > 0)
    .map(transfer => ({
      ...transfer,
      dayOfMonths: [...new Set(transfer.dayOfMonths)].sort((left, right) => left - right),
    }));
  const sourceGroupIds = new Set<number>();

  for (const transfer of normalizedTransfers) {
    const member = memberByEmployeeId.get(transfer.employeeId);
    if (!member) {
      throw new Error(t("Employee #{0} could not be mapped to an availability member for CFA.", transfer.employeeId));
    }

    await availabilityGroupsApi.transferDays(groupId as number, member.id, {
      sourceGroupId: transfer.sourceGroupId,
      dayOfMonths: transfer.dayOfMonths,
    });
    sourceGroupIds.add(transfer.sourceGroupId);
  }

  const synchronizedExistingSlots = normalizedTransfers.length > 0
    ? await availabilityGroupsApi.slots(groupId as number)
    : existingSlots;
  const existingSlotByKey = new Map<string, AvailabilitySlot>(
    synchronizedExistingSlots.map(slot => [`${slot.availabilityGroupMemberId}:${slot.dayOfMonth}`, slot]),
  );

  const slotCreates: Promise<unknown>[] = [];
  const slotUpdates: Promise<unknown>[] = [];

  desiredSlots.forEach(slot => {
    const existingSlot = existingSlotByKey.get(slot.key);
    if (!existingSlot) {
      slotCreates.push(availabilityGroupsApi.createSlot(groupId as number, slot.payload));
      return;
    }

    const existingInterval = (existingSlot.intervalStr ?? "").trim() || null;
    const nextInterval = (slot.payload.intervalStr ?? "").trim() || null;
    const kindChanged = String(existingSlot.kind) !== String(slot.payload.kind);
    const intervalChanged = existingInterval !== nextInterval;

    if (kindChanged || intervalChanged) {
      slotUpdates.push(availabilityGroupsApi.updateSlot(groupId as number, existingSlot.id, slot.payload));
    }
  });

  await Promise.all([...slotCreates, ...slotUpdates]);

  return { id: groupId as number, sourceGroupIds: [...sourceGroupIds] };
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
    onSuccess: result => {
      qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.all });
      qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.byId(result.id) });
      qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.items(result.id) });
      qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.members(result.id) });
      qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.slots(result.id) });
      result.sourceGroupIds.forEach(sourceGroupId => {
        qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.slots(sourceGroupId) });
        qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.items(sourceGroupId) });
        qc.invalidateQueries({ queryKey: queryKeys.availabilityGroups.transferHints(sourceGroupId) });
      });
    },
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
