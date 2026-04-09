import { requestFile } from "@shared/api/httpClient";
import type { ExportOptions } from "@entities/exports/model/types";

function toQuery(options?: ExportOptions) {
  return {
    includeStyles: options?.includeStyles ?? true,
    includeEmployees: options?.includeEmployees ?? true,
  };
}

export const exportsApi = {
  exportGraphExcel: (containerId: number, graphId: number, options?: ExportOptions) =>
    requestFile(`containers/${containerId}/graphs/${graphId}/export/excel`, { query: toQuery(options) }),
  exportGraphSql: (containerId: number, graphId: number, options?: ExportOptions) =>
    requestFile(`containers/${containerId}/graphs/${graphId}/export/sql`, { query: toQuery(options) }),
  exportContainerExcel: (containerId: number, options?: ExportOptions) =>
    requestFile(`containers/${containerId}/export/excel`, { query: toQuery(options) }),
  exportContainerSql: (containerId: number, options?: ExportOptions) =>
    requestFile(`containers/${containerId}/export/sql`, { query: toQuery(options) }),
};
