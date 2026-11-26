import { request } from "@shared/api/httpClient";
import type {
  ManagerNote,
  ManagerNoteShareTarget,
  ManagerNoteVersion,
  UpdateManagerNoteMetadataInput,
  ManagerNotepad,
  ManagerNotepadState,
  SaveManagerNoteInput,
} from "../model/types";

const endpoint = "manager-notepad";

const sessionHeaders = (token?: string) => token ? { "X-Notepad-Edit-Session": token } : undefined;

export const managerNotepadApi = {
  get: (signal?: AbortSignal) => request<ManagerNotepad>(endpoint, { signal }),
  getNote: (noteId: number, signal?: AbortSignal) =>
    request<ManagerNote>(`${endpoint}/notes/${noteId}`, { signal }),
  editSession: (noteId: number, token: string, acquire: boolean, signal?: AbortSignal) =>
    request<ManagerNote>(`${endpoint}/notes/${noteId}/edit-session`, {
      method: acquire ? "POST" : "PUT", headers: sessionHeaders(token), signal, responseType: "json",
    }),
  releaseEditSession: (noteId: number, token: string, keepalive = false) =>
    request<void>(`${endpoint}/notes/${noteId}/edit-session`, { method: "DELETE", headers: sessionHeaders(token), keepalive }),
  createNote: (payload: SaveManagerNoteInput) =>
    request<ManagerNote>(`${endpoint}/notes`, { method: "POST", body: payload }),
  updateNote: (noteId: number, payload: SaveManagerNoteInput, token?: string) =>
    request<ManagerNote>(`${endpoint}/notes/${noteId}`, { method: "PUT", body: payload, headers: sessionHeaders(token) }),
  updateMetadata: (noteId: number, payload: UpdateManagerNoteMetadataInput, token?: string) =>
    request<ManagerNote>(`${endpoint}/notes/${noteId}/metadata`, { method: "PATCH", body: payload, headers: sessionHeaders(token) }),
  getShareTargets: (signal?: AbortSignal) =>
    request<ManagerNoteShareTarget[]>(`${endpoint}/share-targets`, { signal }),
  getVersions: (noteId: number, signal?: AbortSignal) =>
    request<ManagerNoteVersion[]>(`${endpoint}/notes/${noteId}/versions`, { signal }),
  restoreVersion: (noteId: number, versionId: number, expectedRevision: number, token?: string) =>
    request<ManagerNote>(`${endpoint}/notes/${noteId}/restore/${versionId}`, {
      method: "POST", body: { expectedRevision }, headers: sessionHeaders(token),
    }),
  deleteNote: (noteId: number, token?: string) =>
    request<void>(`${endpoint}/notes/${noteId}`, { method: "DELETE", headers: sessionHeaders(token) }),
  saveState: (state: ManagerNotepadState) =>
    request<void>(`${endpoint}/state`, { method: "PUT", body: state }),
};
