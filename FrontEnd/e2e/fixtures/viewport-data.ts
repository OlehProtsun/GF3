import type { AvailabilityTransferSource } from "../../src/entities/availability-groups/model/transfer";
import type { ShiftSwap } from "../../src/entities/shift-swaps/model/types";
import type { EmployeeSchedule } from "../../src/entities/employee-schedule";

export const longName = "September team — extended schedule for the central branch and seasonal employees";
export const source: AvailabilityTransferSource = {
  groupId: 1, groupName: longName, memberId: 1, employeeId: 1,
  days: Array.from({ length: 30 }, (_, i) => ({ dayOfMonth: i + 1, kind: "Available", intervalStr: "08:00 - 16:00", canTransfer: true })),
};
export const schedule: EmployeeSchedule = {
  id: 9, containerId: 2, containerName: longName, shopId: 3, shopName: "Central", name: longName,
  year: 2026, month: 9, publicationStatus: "public",
  slots: Array.from({ length: 30 }, (_, i) => ({ id: i + 1, dayOfMonth: i + 1, slotNo: 1, employeeId: 1, fromTime: "08:00", toTime: "16:00", status: "ASSIGNED" })),
};
const snapshot = { rows: Array.from({ length: 16 }, (_, i) => ({ employeeId: i + 1, employeeName: `Employee ${i + 1} — Anna Kowalska`, kind: "employee" as const, dayValues: Object.fromEntries(Array.from({ length: 30 }, (_, day) => [day + 1, "08:00 - 16:00"])) })) };
export const swap: ShiftSwap = {
  id: 1, scheduleId: 9, scheduleSlotId: 1, scheduleName: longName, containerName: longName, shopName: "Central",
  year: 2026, month: 9, dayOfMonth: 1, fromTime: "08:00", toTime: "16:00", fromEmployeeId: 1,
  fromEmployeeName: "Anna Kowalska", acceptedByEmployeeName: "Aleksandra Nowak", visibility: "public", status: "accepted",
  createdAtUtc: "2026-09-01T00:00:00Z", shiftHours: 8,
  currentEmployeeHoursBefore: 0, currentEmployeeHoursAfter: 8, currentEmployeeWorkDaysBefore: 0, currentEmployeeWorkDaysAfter: 1,
  currentEmployeeFreeDaysBefore: 30, currentEmployeeFreeDaysAfter: 29, fromEmployeeHoursBefore: 8, fromEmployeeHoursAfter: 0,
  isManagerCreated: false, isCreatedByCurrentEmployee: false, isScheduleLocked: false, canAccept: false, canCancel: false,
  beforeSnapshot: snapshot, afterSnapshot: snapshot,
};
export const versions = {
  currentVersionId: 25,
  versions: Array.from({ length: 25 }, (_, i) => ({ id: i + 1, parentVersionId: i || null, versionNumber: i + 1,
    branchName: i % 5 === 0 ? `branch-${i}` : "main", createdAtUtc: "2026-09-01T00:00:00Z", authorName: "Aleksandra Nowak",
    employeeCount: 16, slotCount: 480, cellStyleCount: 40, isCurrent: i === 24 })),
};
export const communication = { id: 1, title: longName, body: "Please review the updated schedule before starting your next shift.\n".repeat(40), visibleFromUtc: "2026-09-01T00:00:00Z", deadlineAtUtc: "2027-01-01T00:00:00Z", createdAtUtc: "2026-09-01T00:00:00Z", createdByManagerName: "Manager", isActive: true };
export const regulation = { id: 1, title: longName, version: "September 2026", message: "Read this document carefully before accepting the updated workplace rules.\n".repeat(40), pdfFileName: "workplace-rules.pdf", pdfSha256: "fixture", isPublished: true, createdByManagerName: "Manager", createdAtUtc: "2026-09-01T00:00:00Z", updatedAtUtc: "2026-09-01T00:00:00Z", acceptanceCount: 0 };
