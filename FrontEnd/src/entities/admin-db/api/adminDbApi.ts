import { request } from "@shared/api/httpClient";
import type {
  AdminDbExecuteResponse,
  AdminDbFileEntry,
  AdminDbHashResponse,
  AdminDbImportResponse,
  AdminDbMetadataResponse,
  AdminDbQueryResponse,
  AdminDbSelectDatabaseRequest,
  AdminDbSqlRequest,
} from "@entities/admin-db/model/types";

const endpoint = "admin/db";
const developerPasswordHeader = "X-GF3-Developer-Password";
let activeDeveloperPassword = "";

function developerAccessHeaders(password = activeDeveloperPassword) {
  return { [developerPasswordHeader]: password };
}

export const adminDbApi = {
  unlock: async (password: string) => {
    const metadata = await request<AdminDbMetadataResponse>(`${endpoint}/metadata`, {
      headers: developerAccessHeaders(password),
    });
    activeDeveloperPassword = password;
    return metadata;
  },
  metadata: ({ signal }: { signal?: AbortSignal } = {}) =>
    request<AdminDbMetadataResponse>(`${endpoint}/metadata`, {
      headers: developerAccessHeaders(),
      signal,
    }),
  hash: ({ signal }: { signal?: AbortSignal } = {}) =>
    request<AdminDbHashResponse>(`${endpoint}/hash`, {
      headers: developerAccessHeaders(),
      signal,
    }),
  query: (payload: AdminDbSqlRequest, { signal }: { signal?: AbortSignal } = {}) =>
    request<AdminDbQueryResponse>(`${endpoint}/query`, {
      method: "POST",
      body: payload,
      headers: developerAccessHeaders(),
      signal,
    }),
  execute: (payload: AdminDbSqlRequest) =>
    request<AdminDbExecuteResponse>(`${endpoint}/execute`, {
      method: "POST",
      body: payload,
      headers: developerAccessHeaders(),
    }),
  importSql: (file: File) => {
    const form = new FormData();
    form.append("file", file);
    return request<AdminDbImportResponse>(`${endpoint}/import`, {
      method: "POST",
      body: form,
      headers: developerAccessHeaders(),
    });
  },
  createManualCopy: () =>
    request<AdminDbFileEntry>(`${endpoint}/manual-copy`, {
      method: "POST",
      headers: developerAccessHeaders(),
    }),
  selectDatabase: (payload: AdminDbSelectDatabaseRequest) =>
    request<AdminDbFileEntry>(`${endpoint}/select`, {
      method: "POST",
      body: payload,
      headers: developerAccessHeaders(),
    }),
};
