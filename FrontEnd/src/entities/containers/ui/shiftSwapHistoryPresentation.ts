import type { ShiftSwap } from "@entities/shift-swaps";
import { dateTimeFormat, t } from "@shared/i18n";

export function isCrossScheduleExchange(swap: ShiftSwap) {
  return swap.requestKind === "exchange" && swap.returnScheduleId != null &&
    swap.returnScheduleId > 0 && swap.returnScheduleId !== swap.scheduleId;
}

export function getSwapHistoryShifts(swap: ShiftSwap) {
  return {
    source: {
      employeeName: swap.fromEmployeeName,
      scheduleName: swap.scheduleName,
      shopName: swap.shopName,
      year: swap.year,
      month: swap.month,
      day: swap.dayOfMonth,
      from: swap.fromTime,
      to: swap.toTime,
      hours: swap.shiftHours,
    },
    returned: {
      employeeName: swap.acceptedByEmployeeName || swap.targetEmployeeName,
      scheduleName: swap.returnScheduleName,
      shopName: swap.returnShopName,
      year: swap.returnYear,
      month: swap.returnMonth,
      day: swap.returnDayOfMonth,
      from: swap.returnFromTime,
      to: swap.returnToTime,
      hours: swap.returnShiftHours,
    },
  };
}

export type SwapHistoryShift = ReturnType<typeof getSwapHistoryShifts>["source" | "returned"];

export function getHistoryMonth({ year, month }: SwapHistoryShift) {
  if (!year || !month || !Number.isInteger(year) || !Number.isInteger(month) ||
    year < 1 || year > 9999 || month < 1 || month > 12) return null;
  return { year, month };
}

export function formatHistoryDate(shift: SwapHistoryShift) {
  const graph = getHistoryMonth(shift);
  if (!graph || !shift.day || !Number.isInteger(shift.day) || shift.day < 1 || shift.day > 31) {
    return t("Not available");
  }
  const date = new Date(0);
  date.setUTCFullYear(graph.year, graph.month - 1, shift.day);
  if (date.getUTCMonth() !== graph.month - 1) return t("Not available");

  return dateTimeFormat("en-GB", { day: "2-digit", month: "short", year: "numeric", timeZone: "UTC" }).format(date);
}

export function formatHistoryPeriod({ from, to }: SwapHistoryShift) {
  return from && to ? `${from.slice(0, 5)}–${to.slice(0, 5)}` : t("Not available");
}
