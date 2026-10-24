export type ManualShiftInterval = { fromTime: string; toTime: string };
export type ManualShiftReservation = ManualShiftInterval & { manualColumnId: number; dayOfMonth: number };
const minutes = (time: string) => Number(time.slice(0, 2)) * 60 + Number(time.slice(3));
const time = (value: number) => `${String(Math.floor(value / 60)).padStart(2, "0")}:${String(value % 60).padStart(2, "0")}`;

export function parseManualShiftIntervals(raw: string): ManualShiftInterval[] | null {
  if (!raw.trim()) return [];
  const result: ManualShiftInterval[] = [];
  for (const token of raw.split(",")) {
    const match = token.trim().match(/^(\d{1,2}):([0-5]\d)\s*[-–—]\s*(\d{1,2}):([0-5]\d)$/);
    if (!match || Number(match[1]) > 23 || Number(match[3]) > 23) return null;
    const fromTime = `${match[1].padStart(2, "0")}:${match[2]}`;
    const toTime = `${match[3].padStart(2, "0")}:${match[4]}`;
    if (fromTime >= toTime) return null;
    result.push({ fromTime, toTime });
  }
  return result.sort((a, b) => a.fromTime.localeCompare(b.fromTime) || a.toTime.localeCompare(b.toTime));
}
export function formatManualShiftIntervals(intervals: ManualShiftInterval[]): string {
  return [...intervals].sort((a, b) => a.fromTime.localeCompare(b.fromTime) || a.toTime.localeCompare(b.toTime))
    .map(p => `${p.fromTime} - ${p.toTime}`).join(", ");
}
export function manualShiftCapacity(intervals: ManualShiftInterval[]): number[] {
  const capacity = Array<number>(1440).fill(0);
  for (const interval of intervals) for (let m = minutes(interval.fromTime); m < minutes(interval.toTime); m++) capacity[m]++;
  return capacity;
}
export function intervalsFromCapacity(capacity: number[]): ManualShiftInterval[] {
  const result: ManualShiftInterval[] = [];
  for (let level = 1; level <= Math.max(0, ...capacity); level++) {
    let start = -1;
    for (let m = 0; m <= 1440; m++) {
      if (m < 1440 && capacity[m] >= level) { if (start < 0) start = m; }
      else if (start >= 0) { result.push({ fromTime: time(start), toTime: time(m) }); start = -1; }
    }
  }
  return result.sort((a, b) => a.fromTime.localeCompare(b.fromTime) || a.toTime.localeCompare(b.toTime));
}
export function canReserveManualShift(raw: string, reserved: ManualShiftInterval[], requested: ManualShiftInterval): boolean {
  const parsed = parseManualShiftIntervals(raw);
  const request = parseManualShiftIntervals(`${requested.fromTime}-${requested.toTime}`);
  if (!parsed || !request?.length) return false;
  const capacity = manualShiftCapacity(parsed), occupied = manualShiftCapacity(reserved);
  for (let m = minutes(request[0].fromTime); m < minutes(request[0].toTime); m++) if (capacity[m] <= occupied[m]) return false;
  return true;
}
export function enumerateManualShiftOptions(raw: string, reserved: ManualShiftInterval[] = []) {
  const parsed = parseManualShiftIntervals(raw);
  if (!parsed) return [];
  const capacity = manualShiftCapacity(parsed), occupied = manualShiftCapacity(reserved);
  const remaining = capacity.map((count, m) => Math.max(0, count - occupied[m]));
  const candidates = [...parsed];
  const boundaries = [...new Set(parsed.flatMap(p => [p.fromTime, p.toTime]))].sort();
  for (let i = 0; i < boundaries.length; i++) for (let j = i + 1; j < boundaries.length; j++) {
    const p = { fromTime: boundaries[i], toTime: boundaries[j] };
    if (!parsed.some(x => x.fromTime === p.fromTime && x.toTime === p.toTime) &&
      canReserveManualShift(raw, [], p)) candidates.push(p);
  }
  // Include free pieces when an earlier offer reserved part of a larger entry.
  candidates.push(...intervalsFromCapacity(remaining).filter(p => !candidates.some(x => x.fromTime === p.fromTime && x.toTime === p.toTime)));
  const occurrences = new Map<string, number>();
  return candidates.flatMap(p => {
    const key = `${p.fromTime}:${p.toTime}`;
    const occurrence = occurrences.get(key) ?? 0;
    occurrences.set(key, occurrence + 1);
    const available = Math.min(...remaining.slice(minutes(p.fromTime), minutes(p.toTime)));
    return available > occurrence ? [{ ...p, occurrence, combined: !parsed.some(x => x.fromTime === p.fromTime && x.toTime === p.toTime) }] : [];
  }).sort((a, b) => a.fromTime.localeCompare(b.fromTime) || a.toTime.localeCompare(b.toTime) || a.occurrence - b.occurrence);
}
