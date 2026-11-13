import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "@shared/api/queryKeys";
import type { ManagerNotepadState, SaveManagerNoteInput } from "../model/types";
import { managerNotepadApi } from "./managerNotepadApi";

export const useManagerNotepadQuery = (enabled = true) => useQuery({
  queryKey: queryKeys.managerNotepad.current(),
  queryFn: ({ signal }) => managerNotepadApi.get(signal),
  enabled,
  staleTime: 30_000,
});

export function useCreateManagerNoteMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: SaveManagerNoteInput) => managerNotepadApi.createNote(payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.managerNotepad.current() }),
  });
}

export function useUpdateManagerNoteMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ noteId, payload }: { noteId: number; payload: SaveManagerNoteInput }) =>
      managerNotepadApi.updateNote(noteId, payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.managerNotepad.current() }),
  });
}

export function useDeleteManagerNoteMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (noteId: number) => managerNotepadApi.deleteNote(noteId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.managerNotepad.current() }),
  });
}

export const useSaveManagerNotepadStateMutation = () => useMutation({
  mutationFn: (state: ManagerNotepadState) => managerNotepadApi.saveState(state),
});
