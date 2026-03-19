import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type { GraphEmployee, GraphSlot } from "@entities/containers/model/types";
import {
  buildGraphDraftSlots,
  diffGraphSlots,
  type GraphMatrixCellMap,
} from "@entities/containers/model/graphWorkspace";
import { queryKeys } from "@shared/api/queryKeys";
import { containersApi } from "./containersApi";
import type {
  GenerateGraphRequestDto,
  SaveContainerDto,
  SaveGraphDto,
  SaveGraphEmployeeDto,
  SaveGraphSlotDto,
  SaveSchedulePresetDto,
  UpsertGraphCellStyleDto,
} from "./dto";

export type SaveGraphWorkspaceEmployeeAssignment = {
  id: number | null;
  employeeId: number;
  minHoursMonth?: number | null;
};

export type SaveGraphWorkspaceInput = {
  containerId: number;
  graphId: number | null;
  payload: SaveGraphDto;
  employeeAssignments: SaveGraphWorkspaceEmployeeAssignment[];
  cellMap: GraphMatrixCellMap;
  existingEmployees?: GraphEmployee[];
  existingSlots?: GraphSlot[];
};

export type SaveGraphWorkspaceResult = {
  graphId: number;
};

async function syncGraphWorkspace({
  containerId,
  graphId,
  payload,
  employeeAssignments,
  cellMap,
  existingEmployees = [],
  existingSlots = [],
}: SaveGraphWorkspaceInput): Promise<SaveGraphWorkspaceResult> {
  let resolvedGraphId = graphId;

  if (resolvedGraphId === null) {
    const createdGraph = await containersApi.createGraph(containerId, payload);
    resolvedGraphId = createdGraph.id;
  } else {
    await containersApi.updateGraph(containerId, resolvedGraphId, payload);
  }

  const uniqueAssignments = [...employeeAssignments]
    .filter(assignment => Number.isInteger(assignment.employeeId) && assignment.employeeId > 0)
    .reduce<SaveGraphWorkspaceEmployeeAssignment[]>((accumulator, assignment) => {
      if (accumulator.some(item => item.employeeId === assignment.employeeId)) {
        return accumulator;
      }

      accumulator.push(assignment);
      return accumulator;
    }, []);

  const displayOrderByEmployeeId = new Map(
    uniqueAssignments.map((assignment, index) => [assignment.employeeId, index] as const),
  );
  const existingEmployeeByEmployeeId = new Map(existingEmployees.map(employee => [employee.employeeId, employee]));
  const selectedEmployeeIds = uniqueAssignments.map(assignment => assignment.employeeId);
  const selectedEmployeeIdSet = new Set(selectedEmployeeIds);

  const employeesToCreate = uniqueAssignments.filter(assignment => !existingEmployeeByEmployeeId.has(assignment.employeeId));
  const employeesToRemove = existingEmployees.filter(employee => !selectedEmployeeIdSet.has(employee.employeeId));

  if (employeesToCreate.length > 0) {
    const createdEmployees = await Promise.all(
      employeesToCreate.map(employee =>
        containersApi.createGraphEmployee(containerId, resolvedGraphId as number, {
          employeeId: employee.employeeId,
          minHoursMonth: employee.minHoursMonth ?? null,
          displayOrder: displayOrderByEmployeeId.get(employee.employeeId) ?? 0,
        }),
      ),
    );

    createdEmployees.forEach(employee => {
      existingEmployeeByEmployeeId.set(employee.employeeId, employee);
    });
  }

  const employeesToUpdate = uniqueAssignments.flatMap(assignment => {
    const existingEmployee = existingEmployeeByEmployeeId.get(assignment.employeeId);
    const displayOrder = displayOrderByEmployeeId.get(assignment.employeeId);

    if (!existingEmployee || displayOrder === undefined) {
      return [];
    }

    const minHoursChanged = (existingEmployee.minHoursMonth ?? null) !== (assignment.minHoursMonth ?? null);
    const displayOrderChanged = existingEmployee.displayOrder !== displayOrder;

    if (!minHoursChanged && !displayOrderChanged) {
      return [];
    }

    return [{ assignment, existingEmployee, displayOrder }];
  });

  if (employeesToUpdate.length > 0) {
    await Promise.all(
      employeesToUpdate.map(({ assignment, existingEmployee, displayOrder }) => {
        return containersApi.updateGraphEmployee(containerId, resolvedGraphId as number, existingEmployee.id, {
          employeeId: assignment.employeeId,
          minHoursMonth: assignment.minHoursMonth ?? null,
          displayOrder,
        });
      }),
    );
  }

  const draft = buildGraphDraftSlots({
    scheduleId: resolvedGraphId as number,
    existingSlots,
    employeeIds: selectedEmployeeIds,
    year: payload.year,
    month: payload.month,
    cellMap,
  });

  if (Object.keys(draft.errors).length > 0) {
    throw new Error(Object.values(draft.errors)[0] ?? "The schedule matrix contains invalid time ranges.");
  }

  const slotDiff = diffGraphSlots(existingSlots, draft.slots);

  if (slotDiff.create.length > 0) {
    await Promise.all(
      slotDiff.create.map(slot =>
        containersApi.createGraphSlot(containerId, resolvedGraphId as number, {
          dayOfMonth: slot.dayOfMonth,
          slotNo: slot.slotNo,
          fromTime: slot.fromTime,
          toTime: slot.toTime,
          employeeId: slot.employeeId ?? null,
          status: slot.status,
        }),
      ),
    );
  }

  if (slotDiff.update.length > 0) {
    await Promise.all(
      slotDiff.update.map(slot =>
        containersApi.updateGraphSlot(containerId, resolvedGraphId as number, slot.id, {
          dayOfMonth: slot.dayOfMonth,
          slotNo: slot.slotNo,
          fromTime: slot.fromTime,
          toTime: slot.toTime,
          employeeId: slot.employeeId ?? null,
          status: slot.status,
        }),
      ),
    );
  }

  if (slotDiff.remove.length > 0) {
    await Promise.all(
      slotDiff.remove.map(slot =>
        containersApi.removeGraphSlot(containerId, resolvedGraphId as number, slot.id),
      ),
    );
  }

  if (employeesToRemove.length > 0) {
    await Promise.all(
      employeesToRemove.map(employee =>
        containersApi.removeGraphEmployee(containerId, resolvedGraphId as number, employee.id),
      ),
    );
  }

  return { graphId: resolvedGraphId as number };
}

export const useContainersListQuery = () =>
  useQuery({
    queryKey: queryKeys.containers.list(),
    // Keep the containers list request alive through StrictMode remounts in dev.
    queryFn: () => containersApi.list(),
  });

export const useContainerByIdQuery = (id: number | null) =>
  useQuery({
    queryKey: ["containers", "byId", id ?? 0],
    enabled: id !== null,
    cancelOnUnmount: false,
    queryFn: ({ signal }) => containersApi.byId(id as number, signal),
  });

export function useCreateContainerMutation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: SaveContainerDto) => containersApi.create(payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.containers.all }),
  });
}

export function useUpdateContainerMutation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: number; payload: SaveContainerDto }) =>
      containersApi.update(id, payload),
    onSuccess: (_, variables) => {
      qc.invalidateQueries({ queryKey: queryKeys.containers.all });
      qc.invalidateQueries({ queryKey: queryKeys.containers.byId(variables.id) });
    },
  });
}

export function useDeleteContainerMutation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => containersApi.remove(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.containers.all }),
  });
}

export const useContainerGraphsQuery = (containerId: number | null) =>
  useQuery({
    queryKey: ["containers", containerId ?? 0, "graphs"],
    enabled: containerId !== null,
    cancelOnUnmount: false,
    queryFn: ({ signal }) => containersApi.listGraphs(containerId as number, signal),
  });

export const useSchedulePresetsQuery = (containerId: number | null) =>
  useQuery({
    queryKey: queryKeys.containers.schedulePresets(containerId ?? 0),
    enabled: containerId !== null,
    cancelOnUnmount: false,
    queryFn: ({ signal }) => containersApi.listSchedulePresets(containerId as number, signal),
  });

export function useCreateSchedulePresetMutation() {
  const qc = useQueryClient();

  return useMutation({
    mutationFn: ({ containerId, payload }: { containerId: number; payload: SaveSchedulePresetDto }) =>
      containersApi.createSchedulePreset(containerId, payload),
    onSuccess: (_, variables) => {
      qc.invalidateQueries({ queryKey: queryKeys.containers.schedulePresets(variables.containerId) });
    },
  });
}

export const useGraphByIdQuery = (containerId: number | null, graphId: number | null) =>
  useQuery({
    queryKey: ["containers", containerId ?? 0, "graphs", graphId ?? 0],
    enabled: containerId !== null && graphId !== null,
    cancelOnUnmount: false,
    queryFn: ({ signal }) => containersApi.graphById(containerId as number, graphId as number, signal),
  });

export function useCreateGraphMutation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ containerId, payload }: { containerId: number; payload: SaveGraphDto }) =>
      containersApi.createGraph(containerId, payload),
    onSuccess: (_, variables) =>
      qc.invalidateQueries({ queryKey: queryKeys.containers.graphs(variables.containerId) }),
  });
}

export function useUpdateGraphMutation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ containerId, graphId, payload }: { containerId: number; graphId: number; payload: SaveGraphDto }) =>
      containersApi.updateGraph(containerId, graphId, payload),
    onSuccess: (_, variables) => {
      qc.invalidateQueries({ queryKey: queryKeys.containers.graphs(variables.containerId) });
      qc.invalidateQueries({ queryKey: queryKeys.containers.graphById(variables.containerId, variables.graphId) });
    },
  });
}

export function useDeleteGraphMutation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ containerId, graphId }: { containerId: number; graphId: number }) =>
      containersApi.removeGraph(containerId, graphId),
    onSuccess: (_, variables) =>
      qc.invalidateQueries({ queryKey: queryKeys.containers.graphs(variables.containerId) }),
  });
}

export function useSaveGraphWorkspaceMutation() {
  const qc = useQueryClient();

  return useMutation({
    mutationFn: syncGraphWorkspace,
    onSuccess: (result, variables) => {
      qc.invalidateQueries({ queryKey: queryKeys.containers.graphs(variables.containerId) });
      qc.invalidateQueries({ queryKey: queryKeys.containers.graphById(variables.containerId, result.graphId) });
      qc.invalidateQueries({ queryKey: queryKeys.containers.graphEmployees(variables.containerId, result.graphId) });
      qc.invalidateQueries({ queryKey: queryKeys.containers.graphSlots(variables.containerId, result.graphId) });
    },
  });
}

export const useGenerateGraphMutation = () =>
  useMutation({
    mutationFn: ({ containerId, graphId, payload }: { containerId: number; graphId: number; payload: GenerateGraphRequestDto }) =>
      containersApi.generateGraph(containerId, graphId, payload),
  });

export const useGraphSlotsQuery = (containerId: number | null, graphId: number | null) =>
  useQuery({
    queryKey: ["containers", containerId ?? 0, "graphs", graphId ?? 0, "slots"],
    enabled: containerId !== null && graphId !== null,
    cancelOnUnmount: false,
    queryFn: ({ signal }) => containersApi.listGraphSlots(containerId as number, graphId as number, signal),
  });

export function useCreateGraphSlotMutation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ containerId, graphId, payload }: { containerId: number; graphId: number; payload: SaveGraphSlotDto }) =>
      containersApi.createGraphSlot(containerId, graphId, payload),
    onSuccess: (_, variables) =>
      qc.invalidateQueries({ queryKey: queryKeys.containers.graphSlots(variables.containerId, variables.graphId) }),
  });
}

export function useUpdateGraphSlotMutation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ containerId, graphId, slotId, payload }: { containerId: number; graphId: number; slotId: number; payload: SaveGraphSlotDto }) =>
      containersApi.updateGraphSlot(containerId, graphId, slotId, payload),
    onSuccess: (_, variables) =>
      qc.invalidateQueries({ queryKey: queryKeys.containers.graphSlots(variables.containerId, variables.graphId) }),
  });
}

export function useDeleteGraphSlotMutation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ containerId, graphId, slotId }: { containerId: number; graphId: number; slotId: number }) =>
      containersApi.removeGraphSlot(containerId, graphId, slotId),
    onSuccess: (_, variables) =>
      qc.invalidateQueries({ queryKey: queryKeys.containers.graphSlots(variables.containerId, variables.graphId) }),
  });
}

export const useGraphEmployeesQuery = (containerId: number | null, graphId: number | null) =>
  useQuery({
    queryKey: ["containers", containerId ?? 0, "graphs", graphId ?? 0, "employees"],
    enabled: containerId !== null && graphId !== null,
    cancelOnUnmount: false,
    queryFn: ({ signal }) => containersApi.listGraphEmployees(containerId as number, graphId as number, signal),
  });

export function useCreateGraphEmployeeMutation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ containerId, graphId, payload }: { containerId: number; graphId: number; payload: SaveGraphEmployeeDto }) =>
      containersApi.createGraphEmployee(containerId, graphId, payload),
    onSuccess: (_, variables) =>
      qc.invalidateQueries({ queryKey: queryKeys.containers.graphEmployees(variables.containerId, variables.graphId) }),
  });
}

export function useUpdateGraphEmployeeMutation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ containerId, graphId, graphEmployeeId, payload }: { containerId: number; graphId: number; graphEmployeeId: number; payload: SaveGraphEmployeeDto }) =>
      containersApi.updateGraphEmployee(containerId, graphId, graphEmployeeId, payload),
    onSuccess: (_, variables) =>
      qc.invalidateQueries({ queryKey: queryKeys.containers.graphEmployees(variables.containerId, variables.graphId) }),
  });
}

export function useDeleteGraphEmployeeMutation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ containerId, graphId, graphEmployeeId }: { containerId: number; graphId: number; graphEmployeeId: number }) =>
      containersApi.removeGraphEmployee(containerId, graphId, graphEmployeeId),
    onSuccess: (_, variables) =>
      qc.invalidateQueries({ queryKey: queryKeys.containers.graphEmployees(variables.containerId, variables.graphId) }),
  });
}

export const useGraphCellStylesQuery = (containerId: number | null, graphId: number | null) =>
  useQuery({
    queryKey: ["containers", containerId ?? 0, "graphs", graphId ?? 0, "cellStyles"],
    enabled: containerId !== null && graphId !== null,
    cancelOnUnmount: false,
    queryFn: ({ signal }) => containersApi.listGraphCellStyles(containerId as number, graphId as number, signal),
  });

export function useUpsertGraphCellStyleMutation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ containerId, graphId, payload }: { containerId: number; graphId: number; payload: UpsertGraphCellStyleDto }) =>
      containersApi.upsertGraphCellStyle(containerId, graphId, payload),
    onSuccess: (_, variables) =>
      qc.invalidateQueries({ queryKey: queryKeys.containers.graphCellStyles(variables.containerId, variables.graphId) }),
  });
}

export function useDeleteGraphCellStyleMutation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ containerId, graphId, styleId }: { containerId: number; graphId: number; styleId: number }) =>
      containersApi.removeGraphCellStyle(containerId, graphId, styleId),
    onSuccess: (_, variables) =>
      qc.invalidateQueries({ queryKey: queryKeys.containers.graphCellStyles(variables.containerId, variables.graphId) }),
  });
}
