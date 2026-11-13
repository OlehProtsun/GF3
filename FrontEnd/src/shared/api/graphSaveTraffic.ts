import type { QueryClient } from "@tanstack/react-query";
import { queryKeys } from "./queryKeys";

const activeSaves = new WeakMap<QueryClient, Map<number, Set<number>>>();
export const graphEditorSaveMutationKey = ["graphEditorSave"] as const;

export function beginGraphSaveTraffic(queryClient: QueryClient, containerId: number): void {
  const saves = activeSaves.get(queryClient) ?? new Map<number, Set<number>>();
  if (saves.has(containerId)) throw new Error("A graph save is already active for this container.");
  saves.set(containerId, new Set());
  activeSaves.set(queryClient, saves);
}

export function isGraphSaveTrafficActive(queryClient: QueryClient, containerId: number): boolean {
  return activeSaves.get(queryClient)?.has(containerId) ?? false;
}

export function deferGraphRealtimeRefresh(queryClient: QueryClient, containerId: number, graphId: number): boolean {
  const deferred = activeSaves.get(queryClient)?.get(containerId);
  if (!deferred) return false;
  deferred.add(graphId);
  return true;
}

export async function finishGraphSaveTraffic(queryClient: QueryClient, containerId: number, changedGraphIds: readonly number[]): Promise<void> {
  const saves = activeSaves.get(queryClient);
  const deferred = saves?.get(containerId);
  if (!saves || !deferred) return;
  saves.delete(containerId);
  if (saves.size === 0) activeSaves.delete(queryClient);
  const ids = new Set([...changedGraphIds, ...deferred]);
  if (!ids.size) return;

  const { containers } = queryKeys;
  const markStale = (queryKey: readonly unknown[], exact = false) =>
    queryClient.invalidateQueries({ queryKey, exact, refetchType: "none" });
  markStale(containers.graphs(containerId), true);
  for (const id of ids) {
    for (const key of [containers.graphById, containers.graphSlots, containers.graphEmployees, containers.graphCellStyles, containers.graphVersions]) {
      markStale(key(containerId, id), true);
    }
  }
  const batches = containers.graphSlotsBatches(containerId);
  queryClient.invalidateQueries({
    queryKey: batches, refetchType: "none",
    predicate: ({ queryKey }) => queryKey.slice(batches.length).some(id => typeof id === "number" && ids.has(id)),
  });
  markStale(containers.graphRecordsPrefix(containerId));
  markStale(queryKeys.employeeSchedules.all);
  markStale(queryKeys.shiftSwaps.all);
  markStale(queryKeys.home.dashboard());
  markStale(queryKeys.home.activity());
  markStale(queryKeys.workflowLogs.all);
}
