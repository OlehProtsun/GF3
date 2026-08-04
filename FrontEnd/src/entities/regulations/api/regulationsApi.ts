import { developerAccessHeaders } from "@entities/admin-db/api/adminDbApi";
import type { RegulationAcceptance, RegulationDocument, SaveRegulationInput } from "@entities/regulations/model/types";
import { request, requestFile } from "@shared/api/httpClient";

function toFormData(input: SaveRegulationInput) {
  const form = new FormData();
  form.append("title", input.title);
  form.append("version", input.version);
  form.append("message", input.message);
  if (input.pdf) {
    form.append("pdf", input.pdf);
  }
  return form;
}

export const regulationsApi = {
  pending: ({ signal }: { signal?: AbortSignal } = {}) =>
    request<RegulationDocument[]>("regulations/pending", { signal }),
  myHistory: ({ signal }: { signal?: AbortSignal } = {}) =>
    request<RegulationAcceptance[]>("regulations/history/me", { signal }),
  employeeHistory: (employeeId: number, signal?: AbortSignal) =>
    request<RegulationAcceptance[]>(`regulations/history/employees/${employeeId}`, { signal }),
  accept: (documentId: number) =>
    request<RegulationAcceptance>(`regulations/${documentId}/accept`, { method: "POST" }),
  downloadPdf: (documentId: number) => requestFile(`regulations/${documentId}/pdf`),
  adminList: ({ signal }: { signal?: AbortSignal } = {}) =>
    request<RegulationDocument[]>("admin/regulations", { headers: developerAccessHeaders(), signal }),
  adminAcceptances: ({ signal }: { signal?: AbortSignal } = {}) =>
    request<RegulationAcceptance[]>("admin/regulations/acceptances", { headers: developerAccessHeaders(), signal }),
  adminCreate: (input: SaveRegulationInput) =>
    request<RegulationDocument>("admin/regulations", {
      method: "POST",
      body: toFormData(input),
      headers: developerAccessHeaders(),
    }),
  adminUpdate: (documentId: number, input: SaveRegulationInput) =>
    request<RegulationDocument>(`admin/regulations/${documentId}`, {
      method: "PUT",
      body: toFormData(input),
      headers: developerAccessHeaders(),
    }),
  adminPublish: (documentId: number) =>
    request<RegulationDocument>(`admin/regulations/${documentId}/publish`, {
      method: "POST",
      headers: developerAccessHeaders(),
    }),
  adminDelete: (documentId: number) =>
    request<void>(`admin/regulations/${documentId}`, {
      method: "DELETE",
      headers: developerAccessHeaders(),
    }),
  adminDownloadPdf: (documentId: number) =>
    requestFile(`admin/regulations/${documentId}/pdf`, { headers: developerAccessHeaders() }),
};
