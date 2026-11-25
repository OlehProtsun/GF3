import type { EmployeeSchedule } from "@entities/employee-schedule";
import { parseFlexibleTimeSegment } from "@shared/lib/timeRange";

function countAssignedShifts(schedule: EmployeeSchedule, employeeId: number) {
  const shifts = new Set<string>();
  for (const slot of schedule.slots) {
    if (slot.employeeId !== employeeId || slot.status !== "ASSIGNED") continue;
    const from = parseFlexibleTimeSegment(slot.fromTime)?.label ?? slot.fromTime.trim();
    const to = parseFlexibleTimeSegment(slot.toTime)?.label ?? slot.toTime.trim();
    if (from && to) shifts.add(`${slot.dayOfMonth}|${from}|${to}`);
  }
  return shifts.size;
}

export function rankEmployeeSchedules(schedules: readonly EmployeeSchedule[], employeeId: number, currentMonth: string) {
  const currentPeriod = Number(currentMonth.replace("-", ""));
  return schedules.map(schedule => ({
    schedule,
    period: schedule.year * 100 + schedule.month,
    shiftCount: countAssignedShifts(schedule, employeeId),
  })).sort((left, right) =>
    Number(right.period === currentPeriod) - Number(left.period === currentPeriod) ||
    right.period - left.period ||
    right.shiftCount - left.shiftCount ||
    left.schedule.name.localeCompare(right.schedule.name) ||
    left.schedule.id - right.schedule.id,
  ).map(item => item.schedule);
}
