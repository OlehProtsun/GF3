import { request } from "@shared/api/httpClient";
import type { PriorityGenerationSettings } from "../model/types";

const endpoint = "priority-generation-settings";
export const priorityGenerationSettingsApi = {
  get: (signal?: AbortSignal) => request<PriorityGenerationSettings>(endpoint, { signal }),
  update: (settings: PriorityGenerationSettings) => request<PriorityGenerationSettings>(endpoint, {
    method: "PUT", body: settings,
  }),
};
