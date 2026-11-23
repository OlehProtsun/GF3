import type { ShiftSwap, ShiftSwapEligibleWindow } from "@entities/shift-swaps/model/types";

const validTime = /^([01]\d|2[0-3]):[0-5]\d$/;

export function getSwapSlotWindows(slot: { id: number; fromTime: string; toTime: string }, openSwaps: ShiftSwap[]) {
  const { fromTime, toTime } = slot;
  const reserved: ShiftSwapEligibleWindow[] = [];
  const available: ShiftSwapEligibleWindow[] = [];
  if (!validTime.test(fromTime) || !validTime.test(toTime) || fromTime >= toTime) return { reserved, available };

  const periods: ShiftSwapEligibleWindow[] = [];
  const addPeriod = (from: string | null | undefined, to: string | null | undefined) => {
    if (from && to && validTime.test(from) && validTime.test(to) && from >= fromTime && to <= toTime && from < to)
      periods.push({ fromTime: from, toTime: to });
    else periods.push({ fromTime, toTime });
  };

  for (const swap of openSwaps) {
    if (swap.status !== "open") continue;
    if (swap.scheduleSlotId === slot.id) addPeriod(swap.isManagerCreated ? null : swap.fromTime, swap.toTime);
    if (swap.returnScheduleSlotId === slot.id) addPeriod(swap.isManagerCreated ? null : swap.returnFromTime, swap.returnToTime);
  }

  for (const period of periods.sort((left, right) => left.fromTime.localeCompare(right.fromTime))) {
    const last = reserved.at(-1);
    if (!last || last.toTime < period.fromTime) reserved.push(period);
    else if (last.toTime < period.toTime) last.toTime = period.toTime;
  }
  let cursor = fromTime;
  for (const period of reserved) {
    if (cursor < period.fromTime) available.push({ fromTime: cursor, toTime: period.fromTime });
    cursor = period.toTime;
  }
  if (cursor < toTime) available.push({ fromTime: cursor, toTime });
  return { reserved, available };
}
