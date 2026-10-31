export type EmployeeUiState = {
  scheduleColumnOrders: Record<string, number[]>;
  readNotificationIds: string[];
  pinnedSwapIds?: number[];
};

export type EmployeeAvailabilityTimePresets = { presets: string[] | null };
