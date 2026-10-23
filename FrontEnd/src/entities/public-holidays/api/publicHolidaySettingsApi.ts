import { request } from "@shared/api/httpClient";
import type { PublicHolidayDisplaySettings } from "../model/types";

const endpoint = "public-holiday-settings";

export const publicHolidaySettingsApi = {
  get: (signal?: AbortSignal) => request<PublicHolidayDisplaySettings>(endpoint, { signal }),
  update: (showPublicHolidays: boolean) => request<PublicHolidayDisplaySettings>(endpoint, {
    method: "PUT", body: { showPublicHolidays },
  }),
};
