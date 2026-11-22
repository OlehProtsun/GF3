import type { EmployeeSchedule } from "@entities/employee-schedule";
import type { ShiftSwap, ShiftSwapScheduleSnapshot } from "@entities/shift-swaps";

type SwapLeg = {
  scheduleId: number;
  scheduleName?: string | null;
  year: number;
  month: number;
  day: number;
  from: string;
  to: string;
  sourceId?: number | null;
  recipientId?: number | null;
  sourceName?: string | null;
  recipientName?: string | null;
  original?: string;
};

type ScheduleSwap = { swap: ShiftSwap; legs: SwapLeg[] };

export type ScheduleShiftStatus = {
  label: string;
  correctionPending?: boolean;
  open: ScheduleSwap[];
  accepted: ScheduleSwap[];
};

export function getScheduleStatusTone(status: ScheduleShiftStatus) {
  return status.correctionPending ? "correction" : status.open.length ? "open" : "accepted";
}

function savedOriginalInterval(
  snapshot: ShiftSwapScheduleSnapshot | null | undefined, employeeId: number | null | undefined,
  day: number, from: string, to: string,
) {
  const row = snapshot?.rows.find(row => row.employeeId === employeeId);
  for (const value of row?.dayValues[day]?.split(",") ?? []) {
    const match = value.trim().match(/^(\d{2}:\d{2})\s*-\s*(\d{2}:\d{2})$/);
    if (match && match[1] <= from && match[2] >= to) return `${match[1]} – ${match[2]}`;
  }
}

function getSwapLegs(swap: ShiftSwap): SwapLeg[] {
  const sourceId = swap.fromEmployeeId ?? (swap.manualColumnId ? -swap.manualColumnId : null);
  const recipientId = swap.acceptedByEmployeeId;
  const sourceName = swap.beforeSnapshot?.rows.find(row => row.employeeId === sourceId)?.employeeName ?? swap.fromEmployeeName;
  const recipientName = swap.afterSnapshot?.rows.find(row => row.employeeId === recipientId)?.employeeName ?? swap.acceptedByEmployeeName;
  const legs: SwapLeg[] = [{
    scheduleId: swap.scheduleId, scheduleName: swap.scheduleName, year: swap.year, month: swap.month,
    day: swap.dayOfMonth, from: swap.fromTime, to: swap.toTime, sourceId, sourceName, recipientId, recipientName,
    original: savedOriginalInterval(swap.beforeSnapshot, sourceId, swap.dayOfMonth, swap.fromTime, swap.toTime),
  }];
  if (swap.status === "accepted" && swap.requestKind === "exchange" && swap.returnScheduleId &&
      swap.returnYear && swap.returnMonth && swap.returnDayOfMonth && swap.returnFromTime && swap.returnToTime) {
    legs.push({
      scheduleId: swap.returnScheduleId, scheduleName: swap.returnScheduleName, year: swap.returnYear,
      month: swap.returnMonth, day: swap.returnDayOfMonth, from: swap.returnFromTime, to: swap.returnToTime,
      sourceId: recipientId, sourceName: recipientName, recipientId: sourceId, recipientName: sourceName,
      original: savedOriginalInterval(swap.returnBeforeSnapshot, recipientId, swap.returnDayOfMonth, swap.returnFromTime, swap.returnToTime),
    });
  }
  return legs;
}

export function buildScheduleShiftStatuses(
  schedule: EmployeeSchedule | null, employeeId: number | null, employeeName: string,
  correctionDays: ReadonlySet<number>, swaps: ShiftSwap[],
) {
  const byDay = new Map<number, Map<number, ScheduleShiftStatus>>();
  if (!schedule || employeeId === null) return byDay;

  function getStatus(day: number, id: number, label: string) {
    let employees = byDay.get(day);
    if (!employees) {
      employees = new Map();
      byDay.set(day, employees);
    }
    let status = employees.get(id);
    if (!status) {
      status = { label, open: [], accepted: [] };
      employees.set(id, status);
    }
    return status;
  }

  for (const day of correctionDays) getStatus(day, employeeId, employeeName).correctionPending = true;

  const slotsById = new Map(schedule.slots.map(slot => [slot.id, slot]));
  for (const swap of swaps) {
    if (swap.status === "cancelled" || (swap.status === "open" && swap.isFinished)) continue;
    const isParticipant = swap.fromEmployeeId === employeeId || swap.acceptedByEmployeeId === employeeId;
    if (!isParticipant && (swap.status === "accepted" ||
        (swap.visibility === "private" && swap.targetEmployeeId !== employeeId))) continue;
    if (swap.status === "accepted" && !swap.acceptedByEmployeeId) continue;

    const legs = getSwapLegs(swap);
    for (const leg of legs) {
      if (leg.scheduleId !== schedule.id || leg.year !== schedule.year || leg.month !== schedule.month) continue;
      if (swap.status === "open") {
        const slot = slotsById.get(swap.scheduleSlotId);
        if (!swap.fromEmployeeId || !slot || slot.employeeId !== swap.fromEmployeeId || slot.dayOfMonth !== leg.day ||
            slot.fromTime.slice(0, 5) > leg.from || slot.toTime.slice(0, 5) < leg.to) continue;
        getStatus(leg.day, swap.fromEmployeeId, swap.fromEmployeeName).open.push({ swap, legs });
      } else {
        for (const [id, name] of [[leg.sourceId, leg.sourceName], [leg.recipientId, leg.recipientName]] as const) {
          if (!id) continue;
          const status = getStatus(leg.day, id, name ?? "");
          if (!status.accepted.some(item => item.swap.id === swap.id)) status.accepted.push({ swap, legs });
        }
      }
    }
  }
  return byDay;
}
