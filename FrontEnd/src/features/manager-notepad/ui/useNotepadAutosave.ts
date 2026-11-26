import { useCallback, useEffect, useLayoutEffect, useRef, useState } from "react";
import {
  useCreateManagerNoteMutation,
  useUpdateManagerNoteMutation,
  useUpdateManagerNoteMetadataMutation,
  type ManagerNote,
  type SaveManagerNoteInput,
} from "@entities/manager-notepad";
import { ApiError } from "@shared/api/httpClient";

export type NoteAccess = Required<Pick<SaveManagerNoteInput, "visibility" | "sharedWithManagerIds">>;

type AutosaveInput = SaveManagerNoteInput & {
  noteId: number | null;
  revision: number | null;
  dirty: boolean;
  hasPendingAccess?: boolean;
  enabled: boolean;
  explicitSave?: boolean;
  sessionRequired?: boolean;
  sessionToken?: string;
  onSaved: (note: ManagerNote, submitted: SaveManagerNoteInput) => void;
  onError: (error: unknown) => void;
};

export type NotepadSaveError = { kind: "conflict" | "error"; cause: unknown };

export const MAX_NOTE_CONTENT = 20_000;

export function useNotepadAutosave(input: AutosaveInput) {
  const create = useCreateManagerNoteMutation().mutateAsync;
  const update = useUpdateManagerNoteMutation().mutateAsync;
  const updateMetadata = useUpdateManagerNoteMetadataMutation().mutateAsync;
  const latest = useRef(input);
  const inFlight = useRef<Promise<boolean> | null>(null);
  const failure = useRef<NotepadSaveError | null>(null);
  const timer = useRef<number | undefined>(undefined);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<NotepadSaveError | null>(null);

  useLayoutEffect(() => {
    latest.current = input;
  });

  const clearTimer = useCallback(() => window.clearTimeout(timer.current), []);
  const restoreError = useCallback((nextError: NotepadSaveError | null) => {
    failure.current = nextError;
    setError(nextError);
  }, []);
  const resetError = useCallback(() => restoreError(null), [restoreError]);
  const readError = useCallback(() => failure.current, []);
  const waitForSave = useCallback(() => inFlight.current ?? Promise.resolve(false), []);

  const save = useCallback((retry = false, access?: NoteAccess): Promise<boolean> => {
    clearTimer();
    if (inFlight.current) return access ? Promise.resolve(false) : inFlight.current;
    const current = latest.current;
    if (!current.dirty && !access) return Promise.resolve(true);
    const metadataOnly = access && !current.dirty && current.noteId !== null;
    if ((current.sessionRequired && !current.sessionToken) || (!metadataOnly && !current.enabled) || current.content.length > MAX_NOTE_CONTENT || current.title.trim().length > 160 ||
      (current.noteId === null && !current.title.trim() && !current.content.trim()) ||
      failure.current?.kind === "conflict" || (failure.current && !retry)) {
      return Promise.resolve(false);
    }

    resetError();
    setIsSaving(true);
    const payload: SaveManagerNoteInput = { title: current.title, content: current.content, color: current.color, ...access };
    const operation = current.noteId === null
      ? create(payload)
      : metadataOnly
        ? updateMetadata({ noteId: current.noteId, token: current.sessionToken, payload: { ...access, expectedRevision: current.revision! } })
        : update({ noteId: current.noteId, token: current.sessionToken, payload: { ...payload, expectedRevision: current.revision! } });

    const promise = operation.then(note => {
      const next = latest.current;
      const changedSinceSubmission = next.title !== payload.title || next.content !== payload.content || next.color !== payload.color;
      latest.current = { ...next, noteId: note.id, revision: note.revision, dirty: changedSinceSubmission };
      next.onSaved(note, payload);
      return true;
    }, (cause: unknown) => {
      const nextError: NotepadSaveError = { kind: cause instanceof ApiError && cause.status === 409 && (cause.details as { code?: string } | null)?.code !== "notepad_edit_session_conflict" ? "conflict" : "error", cause };
      failure.current = nextError;
      setError(nextError);
      latest.current.onError(cause);
      return false;
    }).finally(() => {
      inFlight.current = null;
      setIsSaving(false);
    });
    inFlight.current = promise;
    return promise;
  }, [clearTimer, create, resetError, update, updateMetadata]);

  const flush = useCallback(async () => {
    clearTimer();
    do {
      if (!await save()) return false;
    } while (latest.current.dirty);
    return true;
  }, [clearTimer, save]);

  const { noteId, revision, title, content, color, enabled, dirty, explicitSave } = input;
  useEffect(() => {
    if (explicitSave || !enabled || !dirty || isSaving || error) return;
    timer.current = window.setTimeout(() => void save(), 1200);
    return clearTimer;
  }, [noteId, revision, title, content, color, enabled, dirty, explicitSave, isSaving, error, save, clearTimer]);

  useEffect(() => {
    const onBeforeUnload = (event: BeforeUnloadEvent) => {
      if (!latest.current.dirty && !latest.current.hasPendingAccess && !inFlight.current) return;
      if (!latest.current.explicitSave) void flush();
      event.preventDefault();
      event.returnValue = "";
    };
    window.addEventListener("beforeunload", onBeforeUnload);
    return () => window.removeEventListener("beforeunload", onBeforeUnload);
  }, [flush]);

  return { isSaving, error, save, flush, waitForSave, resetError, readError, restoreError };
}
