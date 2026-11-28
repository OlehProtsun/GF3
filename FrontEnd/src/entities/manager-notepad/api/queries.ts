import { useCallback, useEffect, useMemo } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import type {
  ManagerNote, ManagerNotepad, ManagerNotepadState,
  SaveManagerNoteInput, UpdateManagerNoteMetadataInput,
} from "../model/types";
import { managerNotepadApi } from "./managerNotepadApi";

export function useManagerNotepadQuery(enabled = true, watchShared = false) {
  const queryClient = useQueryClient();
  const key = queryKeys.managerNotepad.current();
  const query = useQuery<ManagerNotepad>({
    queryKey: key,
    queryFn: ({ signal }) => managerNotepadApi.get(signal),
    enabled,
    staleTime: 30_000,
  });
  const refresh = useCallback(() => queryClient.invalidateQueries({ queryKey: queryKeys.managerNotepad.current() }), [queryClient]);
  useEffect(() => {
    if (!enabled || !watchShared) return;
    const checkAccess = () => { if (!document.hidden) refresh(); };
    const timer = window.setInterval(checkAccess, 15_000);
    window.addEventListener("focus", checkAccess);
    return () => {
      window.clearInterval(timer);
      window.removeEventListener("focus", checkAccess);
    };
  }, [enabled, refresh, watchShared]);
  const nextExpiry = query.data?.notes.reduce((earliest, note) =>
    Math.min(earliest, note.activeEditor?.expiresAtMs ?? Infinity), Infinity);
  useEffect(() => {
    if (!enabled || !watchShared || nextExpiry === undefined || !Number.isFinite(nextExpiry)) return;
    const delay = nextExpiry - Date.now();
    // The periodic refresh remains the fallback on devices with mismatched wall clocks.
    if (delay < -5_000 || delay > 65_000) return;
    const timer = window.setTimeout(() => { if (!document.hidden) void refresh(); }, Math.max(500, delay + 150));
    return () => window.clearTimeout(timer);
  }, [enabled, nextExpiry, refresh, watchShared]);
  return { ...query, refresh };
}

function useRefreshNotepad() {
  const queryClient = useQueryClient();
  return useCallback((note?: ManagerNote, versions = false) => {
    const key = queryKeys.managerNotepad.current();
    const { data: current } = queryClient.getQueryState<ManagerNotepad>(key);
    if (note && current) {
      const previous = current.notes.find(item => item.id === note.id);
      if (!previous || previous.revision <= note.revision) {
        const notes = previous ? current.notes.map(item => item.id === note.id ? note : item) : [...current.notes, note];
        queryClient.setQueryData(key, { ...current, notes });
      }
    }
    queryClient.invalidateQueries({ queryKey: key });
    if (note && versions) queryClient.invalidateQueries({ queryKey: queryKeys.managerNotepad.versions(note.id) });
  }, [queryClient]);
}

function useNoteWrite<TInput>(mutationFn: (payload: TInput) => Promise<ManagerNote>, versions = false) {
  const refresh = useRefreshNotepad();
  const options = useMemo(() => ({
    mutationFn,
    meta: { handlesErrorInline: true },
    onSuccess: (note: ManagerNote) => refresh(note, versions),
  }), [mutationFn, refresh, versions]);
  const mutation = useMutation(options);
  const mutate = mutation.mutate;
  const mutateAsync = useCallback((payload: TInput) =>
    new Promise<ManagerNote>((onSuccess, onError) => mutate(payload, { onSuccess, onError })), [mutate]);
  return { ...mutation, mutateAsync };
}

function createNote(payload: SaveManagerNoteInput) {
  return managerNotepadApi.createNote(payload);
}

function updateNote({ noteId, payload, token }: { noteId: number; payload: SaveManagerNoteInput; token?: string }) {
  return managerNotepadApi.updateNote(noteId, payload, token);
}

function updateMetadata({ noteId, payload, token }: { noteId: number; payload: UpdateManagerNoteMetadataInput; token?: string }) {
  return managerNotepadApi.updateMetadata(noteId, payload, token);
}

export function useCreateManagerNoteMutation() {
  return useNoteWrite(createNote);
}

export function useUpdateManagerNoteMutation() {
  return useNoteWrite(updateNote, true);
}

export function useDeleteManagerNoteMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ noteId, token }: { noteId: number; token?: string }) => managerNotepadApi.deleteNote(noteId, token),
    meta: { handlesErrorInline: true },
    onSuccess: (_, { noteId }) => {
      const key = queryKeys.managerNotepad.current();
      const { data: current } = queryClient.getQueryState<ManagerNotepad>(key);
      if (current) queryClient.setQueryData(key, { ...current, notes: current.notes.filter(note => note.id !== noteId) });
      queryClient.setQueryData(queryKeys.managerNotepad.versions(noteId), undefined);
      queryClient.invalidateQueries({ queryKey: key });
    },
  });
}

export const useSaveManagerNotepadStateMutation = () => useMutation({
  mutationFn: (state: ManagerNotepadState) => managerNotepadApi.saveState(state),
  meta: { handlesErrorInline: true },
});

export function useManagerNoteShareTargetsQuery(enabled: boolean) {
  const queryClient = useQueryClient();
  const key = queryKeys.managerNotepad.shareTargets();
  const query = useQuery({ queryKey: key, queryFn: ({ signal }) => managerNotepadApi.getShareTargets(signal), enabled });
  return { ...query, refresh: () => queryClient.invalidateQueries({ queryKey: key }) };
}

export function useManagerNoteVersionsQuery(noteId: number) {
  const queryClient = useQueryClient();
  const key = queryKeys.managerNotepad.versions(noteId);
  const query = useQuery({ queryKey: key, queryFn: ({ signal }) => managerNotepadApi.getVersions(noteId, signal) });
  return { ...query, refresh: () => queryClient.invalidateQueries({ queryKey: key }) };
}

export function useUpdateManagerNoteMetadataMutation() {
  return useNoteWrite(updateMetadata);
}

export function useRestoreManagerNoteVersionMutation() {
  const refresh = useRefreshNotepad();
  return useMutation({
    mutationFn: ({ noteId, versionId, expectedRevision, token }: { noteId: number; versionId: number; expectedRevision: number; token?: string }) =>
      managerNotepadApi.restoreVersion(noteId, versionId, expectedRevision, token),
    meta: { handlesErrorInline: true },
    onSuccess: note => refresh(note, true),
  });
}
