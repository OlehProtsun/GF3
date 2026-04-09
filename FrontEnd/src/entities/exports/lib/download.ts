import { ApiError } from "@shared/api/httpClient";
import type { ExportFile } from "../model/types";

type ExportKind = "excel" | "sql";

function buildSafeName(value: string, fallback: string) {
  const safeName = value
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");

  return safeName || fallback;
}

export function buildGraphExportFallbackFilename(kind: ExportKind, graphName: string, year: number, month: number) {
  const suffix = `${year}-${String(month).padStart(2, "0")}`;
  const extension = kind === "excel" ? "xlsx" : "sql";

  return `${buildSafeName(graphName, "schedule")}-${suffix}.${extension}`;
}

export function buildContainerExportFallbackFilename(kind: ExportKind, containerName: string) {
  const extension = kind === "excel" ? "xlsx" : "sql";
  return `${buildSafeName(containerName, "container")}.${extension}`;
}

export function downloadExportFile(file: ExportFile, fallbackFileName: string) {
  const fileName = file.fileName?.trim() || fallbackFileName;
  const url = URL.createObjectURL(file.blob);
  const anchor = document.createElement("a");

  anchor.href = url;
  anchor.download = fileName;
  document.body.append(anchor);
  anchor.click();
  anchor.remove();

  window.setTimeout(() => URL.revokeObjectURL(url), 1_000);
}

export function getExportErrorMessage(error: unknown, fallbackMessage: string) {
  if (error instanceof ApiError) {
    return error.message;
  }

  if (error instanceof Error && error.message.trim()) {
    return error.message;
  }

  return fallbackMessage;
}

export function runMutation<TData, TVariables>(
  mutate: (
    variables: TVariables,
    callbacks?: { onSuccess?: (data: TData) => void; onError?: (error: unknown) => void },
  ) => void,
  variables: TVariables,
) {
  return new Promise<TData>((resolve, reject) => {
    mutate(variables, {
      onSuccess: resolve,
      onError: reject,
    });
  });
}
