import type { PwaLaunchMode } from "@shared/lib/pwaDisplayMode";

export type EmployeeLaunchUsage = {
  employeeId: number;
  employeeName: string;
  username: string | null;
  hasLoginAccount: boolean;
  isOnline: boolean;
  lastLoginAtUtc: string | null;
  lastSeenAtUtc: string | null;
  lastBrowserLaunchAtUtc: string | null;
  lastPwaLaunchAtUtc: string | null;
  lastLaunchMode: PwaLaunchMode | null;
  lastLaunchAtUtc: string | null;
};
