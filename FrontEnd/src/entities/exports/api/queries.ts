import { useMutation } from "@tanstack/react-query";
import { exportsApi } from "./exportsApi";
import type { ExportOptions } from "@entities/exports/model/types";

export const useExportGraphExcelMutation = () => useMutation({ mutationFn: ({ containerId, graphId, options }: { containerId: number; graphId: number; options?: ExportOptions }) => exportsApi.exportGraphExcel(containerId, graphId, options) });
export const useExportGraphSqlMutation = () => useMutation({ mutationFn: ({ containerId, graphId, options }: { containerId: number; graphId: number; options?: ExportOptions }) => exportsApi.exportGraphSql(containerId, graphId, options) });
export const useExportContainerExcelMutation = () => useMutation({ mutationFn: ({ containerId, options }: { containerId: number; options?: ExportOptions }) => exportsApi.exportContainerExcel(containerId, options) });
export const useExportContainerSqlMutation = () => useMutation({ mutationFn: ({ containerId, options }: { containerId: number; options?: ExportOptions }) => exportsApi.exportContainerSql(containerId, options) });
