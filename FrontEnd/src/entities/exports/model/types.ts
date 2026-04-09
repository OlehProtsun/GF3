import type { DownloadedFile } from "@shared/api/httpClient";

export type ExportOptions = {
  includeStyles?: boolean;
  includeEmployees?: boolean;
};

export type ExportFile = DownloadedFile;
