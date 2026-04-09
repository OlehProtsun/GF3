import { request } from "@shared/api/httpClient";
import type {
  AdminDbExecuteResponse,
  AdminDbHashResponse,
  AdminDbImportResponse,
  AdminDbMetadataResponse,
  AdminDbQueryResponse,
  AdminDbSqlRequest,
} from "@entities/admin-db/model/types";

const endpoint = "admin/db";

export const adminDbApi = {
  metadata: ({ signal }: { signal?: AbortSignal } = {}) =>
    request<AdminDbMetadataResponse>(`${endpoint}/metadata`, { signal }),
  hash: ({ signal }: { signal?: AbortSignal } = {}) =>
    request<AdminDbHashResponse>(`${endpoint}/hash`, { signal }),
  query: (payload: AdminDbSqlRequest, { signal }: { signal?: AbortSignal } = {}) =>
    request<AdminDbQueryResponse>(`${endpoint}/query`, {
      method: "POST",
      body: payload,
      signal,
    }),
  execute: (payload: AdminDbSqlRequest) =>
    request<AdminDbExecuteResponse>(`${endpoint}/execute`, {
      method: "POST",
      body: payload,
    }),
  importSql: (file: File) => {
    const form = new FormData();
    form.append("file", file);
    return request<AdminDbImportResponse>(`${endpoint}/import`, {
      method: "POST",
      body: form,
    });
  },
};
