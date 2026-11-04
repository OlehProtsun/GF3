import { request } from "@shared/api/httpClient";
import type {
  ManagerNote,
  ManagerNotepad,
  ManagerNotepadState,
  SaveManagerNoteInput,
} from "../model/types";

const endpoint = "manager-notepad";

export const managerNotepadApi = {
  get: (signal?: AbortSignal) => request<ManagerNotepad>(endpoint, { signal }),
  createNote: (payload: SaveManagerNoteInput) =>
    request<ManagerNote>(`${endpoint}/notes`, { method: "POST", body: payload }),
  updateNote: (noteId: number, payload: SaveManagerNoteInput) =>
    request<ManagerNote>(`${endpoint}/notes/${noteId}`, { method: "PUT", body: payload }),
  deleteNote: (noteId: number) =>
    request<void>(`${endpoint}/notes/${noteId}`, { method: "DELETE" }),
  saveState: (state: ManagerNotepadState) =>
    request<void>(`${endpoint}/state`, { method: "PUT", body: state }),
};
