import { request } from "@shared/api/httpClient";
import type { HealthStatus } from "@entities/health/model/types";

export const healthApi = {
  get: (signal?: AbortSignal) => request<HealthStatus>("health", { signal }),
};
