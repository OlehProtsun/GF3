import type { EmployeeSchedule } from "@entities/employee-schedule";
import type { ShiftCorrectionRequest } from "@entities/shift-corrections";

export function getPendingCorrectionDays(
  schedule: EmployeeSchedule | null,
  employeeId: number | null,
  requests: readonly ShiftCorrectionRequest[],
) {
  const days = new Set<number>();
  if (!schedule || employeeId === null) return days;

  for (const request of requests) {
    if (request.status !== "pending" || request.scheduleId !== schedule.id || request.employeeId !== employeeId ||
      request.year !== schedule.year || request.month !== schedule.month) continue;

    const slots = schedule.slots.filter(slot => slot.employeeId === employeeId && slot.dayOfMonth === request.dayOfMonth);
    const originalSlot = schedule.slots.find(slot => slot.id === request.scheduleSlotId);
    const applies = request.requestType === "removeShift"
      ? slots.some(slot => request.originalShifts?.some(original =>
        original.slotId === slot.id && original.fromTime === slot.fromTime && original.toTime === slot.toTime))
      : request.isDayOffRequest || request.requestType === "addShift" || slots.filter(slot =>
        (!originalSlot || slot.id === originalSlot.id) &&
        slot.fromTime === request.originalFromTime && slot.toTime === request.originalToTime).length === 1;

    if (applies) days.add(request.dayOfMonth);
  }
  return days;
}
