import { request } from "@shared/api/httpClient";
import type { ExportFile, ExportOptions } from "@entities/exports/model/types";

function toQuery(options?: ExportOptions) {
  return {
    includeStyles: options?.includeStyles ?? true,
    includeEmployees: options?.includeEmployees ?? true,
  };
}

export const exportsApi = {
  exportGraphExcel: (containerId: number, graphId: number, options?: ExportOptions) =>
    request<ExportFile>(`exports/containers/${containerId}/graphs/${graphId}/export/excel`, { query: toQuery(options), responseType: "blob" }),
  exportGraphSql: (containerId: number, graphId: number, options?: ExportOptions) =>
    request<ExportFile>(`exports/containers/${containerId}/graphs/${graphId}/export/sql`, { query: toQuery(options), responseType: "blob" }),
  exportContainerExcel: (containerId: number, options?: ExportOptions) =>
    request<ExportFile>(`exports/containers/${containerId}/export/excel`, { query: toQuery(options), responseType: "blob" }),
  exportContainerSql: (containerId: number, options?: ExportOptions) =>
    request<ExportFile>(`exports/containers/${containerId}/export/sql`, { query: toQuery(options), responseType: "blob" }),
};
