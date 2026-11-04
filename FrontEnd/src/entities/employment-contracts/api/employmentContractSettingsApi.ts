import { request } from "@shared/api/httpClient";
import type { EmploymentContractSettings } from "../model/types";
const endpoint = "employment-contract-settings";
export const employmentContractSettingsApi = {
  get: (signal?: AbortSignal) => request<EmploymentContractSettings>(endpoint, { signal }),
  update: (body: EmploymentContractSettings) => request<EmploymentContractSettings>(endpoint, { method: "PUT", body }),
};
