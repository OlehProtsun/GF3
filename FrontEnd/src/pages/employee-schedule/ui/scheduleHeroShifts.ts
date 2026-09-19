import type { EmployeeSchedule } from "@entities/employee-schedule";

function minutes(value: string) {
  const match = /^(\d{1,2}):(\d{2})(?::00)?$/.exec(value.trim());
  if (!match) return null;
  const hour = Number(match[1]);
  const minute = Number(match[2]);
  return hour < 24 && minute < 60 ? hour * 60 + minute : null;
}

export function getHeroShifts(schedules: EmployeeSchedule[], employeeId: number | null, now: Date) {
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate()).getTime();
  const tomorrow = new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1).getTime();
  const nextDay = new Date(now.getFullYear(), now.getMonth(), now.getDate() + 2).getTime();
  const shifts = schedules.flatMap(schedule => schedule.slots.flatMap(slot => {
    if (employeeId === null || slot.employeeId !== employeeId || schedule.publicationStatus !== "public" || slot.status !== "ASSIGNED") return [];
    const from = minutes(slot.fromTime);
    const to = minutes(slot.toTime);
    const date = new Date(schedule.year, schedule.month - 1, slot.dayOfMonth);
    if (from === null || to === null || date.getMonth() !== schedule.month - 1 || date.getDate() !== slot.dayOfMonth) return [];
    const start = new Date(schedule.year, schedule.month - 1, slot.dayOfMonth, Math.floor(from / 60), from % 60).getTime();
    const end = new Date(schedule.year, schedule.month - 1, slot.dayOfMonth + (to <= from ? 1 : 0), Math.floor(to / 60), to % 60).getTime();
    return [{ key: schedule.id + ":" + slot.id, name: schedule.name, fromTime: slot.fromTime.slice(0, 5), toTime: slot.toTime.slice(0, 5), start, end }];
  })).sort((a, b) => a.start - b.start);
  return {
    now: shifts.filter(shift => (shift.start >= today && shift.start < tomorrow) || (shift.start < today && shift.end > now.getTime())),
    tomorrow: shifts.filter(shift => shift.start >= tomorrow && shift.start < nextDay),
  };
}
