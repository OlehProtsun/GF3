import { request } from "@shared/api/httpClient";
import type { SaveSystemNewsInput, SystemNewsMessage } from "../model/types";

const endpoint = "system-news";
const adminEndpoint = "admin/system-news";
const developerPasswordHeader = "X-GF3-Developer-Password";
let activeDeveloperPassword = "";

const developerHeaders = (password = activeDeveloperPassword) => ({ [developerPasswordHeader]: password });

export const systemNewsApi = {
  list: (signal?: AbortSignal) => request<SystemNewsMessage[]>(endpoint, { signal }),
  markRead: (messageId: number) => request<void>(`${endpoint}/${messageId}/read`, { method: "POST" }),
  markAllRead: () => request<void>(`${endpoint}/read-all`, { method: "POST" }),
  unlockAdmin: async (password: string) => {
    const messages = await request<SystemNewsMessage[]>(adminEndpoint, { headers: developerHeaders(password) });
    activeDeveloperPassword = password;
    return messages;
  },
  adminList: () => request<SystemNewsMessage[]>(adminEndpoint, { headers: developerHeaders() }),
  create: (payload: SaveSystemNewsInput) => request<SystemNewsMessage>(adminEndpoint, {
    method: "POST", body: payload, headers: developerHeaders(),
  }),
  update: (messageId: number, payload: SaveSystemNewsInput) => request<SystemNewsMessage>(`${adminEndpoint}/${messageId}`, {
    method: "PUT", body: payload, headers: developerHeaders(),
  }),
  delete: (messageId: number) => request<void>(`${adminEndpoint}/${messageId}`, {
    method: "DELETE", headers: developerHeaders(),
  }),
};
