import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import { containersApi } from "./containersApi";
import type {
  GenerateGraphRequestDto,
  SaveContainerDto,
  SaveGraphDto,
  SaveGraphEmployeeDto,
  SaveGraphSlotDto,
  UpsertGraphCellStyleDto,
} from "./dto";

export const useContainersListQuery = () => useQuery({ queryKey: queryKeys.containers.list(), queryFn: ({ signal }) => containersApi.list(signal) });
export const useContainerByIdQuery = (id: number | null) =>
  useQuery({ queryKey: ["containers", "byId", id ?? 0], enabled: id !== null, queryFn: ({ signal }) => containersApi.byId(id as number, signal) });

export function useCreateContainerMutation() {
  const qc = useQueryClient();
  return useMutation({ mutationFn: (payload: SaveContainerDto) => containersApi.create(payload), onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.containers.all }) });
}
export function useUpdateContainerMutation() {
  const qc = useQueryClient();
  return useMutation({ mutationFn: ({ id, payload }: { id: number; payload: SaveContainerDto }) => containersApi.update(id, payload), onSuccess: (_, v) => { qc.invalidateQueries({ queryKey: queryKeys.containers.all }); qc.invalidateQueries({ queryKey: queryKeys.containers.byId(v.id) }); } });
}
export function useDeleteContainerMutation() {
  const qc = useQueryClient();
  return useMutation({ mutationFn: (id: number) => containersApi.remove(id), onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.containers.all }) });
}

export const useContainerGraphsQuery = (containerId: number | null) =>
  useQuery({ queryKey: ["containers", containerId ?? 0, "graphs"], enabled: containerId !== null, queryFn: ({ signal }) => containersApi.listGraphs(containerId as number, signal) });
export const useGraphByIdQuery = (containerId: number | null, graphId: number | null) =>
  useQuery({ queryKey: ["containers", containerId ?? 0, "graphs", graphId ?? 0], enabled: containerId !== null && graphId !== null, queryFn: ({ signal }) => containersApi.graphById(containerId as number, graphId as number, signal) });

export function useCreateGraphMutation() {
  const qc = useQueryClient();
  return useMutation({ mutationFn: ({ containerId, payload }: { containerId: number; payload: SaveGraphDto }) => containersApi.createGraph(containerId, payload), onSuccess: (_, v) => qc.invalidateQueries({ queryKey: queryKeys.containers.graphs(v.containerId) }) });
}
export function useUpdateGraphMutation() {
  const qc = useQueryClient();
  return useMutation({ mutationFn: ({ containerId, graphId, payload }: { containerId: number; graphId: number; payload: SaveGraphDto }) => containersApi.updateGraph(containerId, graphId, payload), onSuccess: (_, v) => { qc.invalidateQueries({ queryKey: queryKeys.containers.graphs(v.containerId) }); qc.invalidateQueries({ queryKey: queryKeys.containers.graphById(v.containerId, v.graphId) }); } });
}
export function useDeleteGraphMutation() {
  const qc = useQueryClient();
  return useMutation({ mutationFn: ({ containerId, graphId }: { containerId: number; graphId: number }) => containersApi.removeGraph(containerId, graphId), onSuccess: (_, v) => qc.invalidateQueries({ queryKey: queryKeys.containers.graphs(v.containerId) }) });
}
export const useGenerateGraphMutation = () => useMutation({ mutationFn: ({ containerId, graphId, payload }: { containerId: number; graphId: number; payload: GenerateGraphRequestDto }) => containersApi.generateGraph(containerId, graphId, payload) });

export const useGraphSlotsQuery = (containerId: number | null, graphId: number | null) =>
  useQuery({ queryKey: ["containers", containerId ?? 0, "graphs", graphId ?? 0, "slots"], enabled: containerId !== null && graphId !== null, queryFn: ({ signal }) => containersApi.listGraphSlots(containerId as number, graphId as number, signal) });
export function useCreateGraphSlotMutation() {
  const qc = useQueryClient();
  return useMutation({ mutationFn: ({ containerId, graphId, payload }: { containerId: number; graphId: number; payload: SaveGraphSlotDto }) => containersApi.createGraphSlot(containerId, graphId, payload), onSuccess: (_, v) => qc.invalidateQueries({ queryKey: queryKeys.containers.graphSlots(v.containerId, v.graphId) }) });
}
export function useUpdateGraphSlotMutation() {
  const qc = useQueryClient();
  return useMutation({ mutationFn: ({ containerId, graphId, slotId, payload }: { containerId: number; graphId: number; slotId: number; payload: SaveGraphSlotDto }) => containersApi.updateGraphSlot(containerId, graphId, slotId, payload), onSuccess: (_, v) => qc.invalidateQueries({ queryKey: queryKeys.containers.graphSlots(v.containerId, v.graphId) }) });
}
export function useDeleteGraphSlotMutation() {
  const qc = useQueryClient();
  return useMutation({ mutationFn: ({ containerId, graphId, slotId }: { containerId: number; graphId: number; slotId: number }) => containersApi.removeGraphSlot(containerId, graphId, slotId), onSuccess: (_, v) => qc.invalidateQueries({ queryKey: queryKeys.containers.graphSlots(v.containerId, v.graphId) }) });
}

export const useGraphEmployeesQuery = (containerId: number | null, graphId: number | null) =>
  useQuery({ queryKey: ["containers", containerId ?? 0, "graphs", graphId ?? 0, "employees"], enabled: containerId !== null && graphId !== null, queryFn: ({ signal }) => containersApi.listGraphEmployees(containerId as number, graphId as number, signal) });
export function useCreateGraphEmployeeMutation() {
  const qc = useQueryClient();
  return useMutation({ mutationFn: ({ containerId, graphId, payload }: { containerId: number; graphId: number; payload: SaveGraphEmployeeDto }) => containersApi.createGraphEmployee(containerId, graphId, payload), onSuccess: (_, v) => qc.invalidateQueries({ queryKey: queryKeys.containers.graphEmployees(v.containerId, v.graphId) }) });
}
export function useUpdateGraphEmployeeMutation() {
  const qc = useQueryClient();
  return useMutation({ mutationFn: ({ containerId, graphId, graphEmployeeId, payload }: { containerId: number; graphId: number; graphEmployeeId: number; payload: SaveGraphEmployeeDto }) => containersApi.updateGraphEmployee(containerId, graphId, graphEmployeeId, payload), onSuccess: (_, v) => qc.invalidateQueries({ queryKey: queryKeys.containers.graphEmployees(v.containerId, v.graphId) }) });
}
export function useDeleteGraphEmployeeMutation() {
  const qc = useQueryClient();
  return useMutation({ mutationFn: ({ containerId, graphId, graphEmployeeId }: { containerId: number; graphId: number; graphEmployeeId: number }) => containersApi.removeGraphEmployee(containerId, graphId, graphEmployeeId), onSuccess: (_, v) => qc.invalidateQueries({ queryKey: queryKeys.containers.graphEmployees(v.containerId, v.graphId) }) });
}

export const useGraphCellStylesQuery = (containerId: number | null, graphId: number | null) =>
  useQuery({ queryKey: ["containers", containerId ?? 0, "graphs", graphId ?? 0, "cellStyles"], enabled: containerId !== null && graphId !== null, queryFn: ({ signal }) => containersApi.listGraphCellStyles(containerId as number, graphId as number, signal) });
export function useUpsertGraphCellStyleMutation() {
  const qc = useQueryClient();
  return useMutation({ mutationFn: ({ containerId, graphId, payload }: { containerId: number; graphId: number; payload: UpsertGraphCellStyleDto }) => containersApi.upsertGraphCellStyle(containerId, graphId, payload), onSuccess: (_, v) => qc.invalidateQueries({ queryKey: queryKeys.containers.graphCellStyles(v.containerId, v.graphId) }) });
}
export function useDeleteGraphCellStyleMutation() {
  const qc = useQueryClient();
  return useMutation({ mutationFn: ({ containerId, graphId, styleId }: { containerId: number; graphId: number; styleId: number }) => containersApi.removeGraphCellStyle(containerId, graphId, styleId), onSuccess: (_, v) => qc.invalidateQueries({ queryKey: queryKeys.containers.graphCellStyles(v.containerId, v.graphId) }) });
}
