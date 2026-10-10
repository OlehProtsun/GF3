export type EmployeeUiState = {
  scheduleColumnOrders: Record<string, number[]>;
  readNotificationIds: string[];
  pinnedSwapIds?: number[];
};
