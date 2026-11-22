import { t } from "@shared/i18n";

export function shiftCorrectionTimeMinutes(value: string) {
  const [hours, minutes] = value.split(":").map(Number);
  return hours * 60 + minutes;
}

export function shiftCorrectionOriginalMinutes(shifts: readonly { fromTime: string; toTime: string }[]) {
  return shifts.reduce((total, shift) => {
    const duration = shiftCorrectionTimeMinutes(shift.toTime) - shiftCorrectionTimeMinutes(shift.fromTime);
    return total + (duration > 0 ? duration : duration + 24 * 60);
  }, 0);
}

export function formatShiftCorrectionDelta(minutes: number) {
  if (minutes === 0) return t("unchanged");
  const sign = minutes > 0 ? "+" : "−";
  const absolute = Math.abs(minutes);
  const hours = Math.floor(absolute / 60);
  const remainder = absolute % 60;
  return `${sign}${hours ? t("{0}h", hours) : ""}${hours && remainder ? " " : ""}${remainder ? t("{0}m", remainder) : ""}`;
}
