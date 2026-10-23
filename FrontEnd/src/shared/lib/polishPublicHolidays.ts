export type PolishPublicHoliday = {
  readonly id: string;
  readonly name: string;
};

const fixedHolidays: Record<string, PolishPublicHoliday> = {
  "1-1": { id: "new-year", name: "New Year's Day" },
  "1-6": { id: "epiphany", name: "Epiphany" },
  "5-1": { id: "labour-day", name: "Labour Day" },
  "5-3": { id: "constitution-day", name: "Constitution Day" },
  "8-15": { id: "assumption", name: "Assumption of Mary" },
  "11-1": { id: "all-saints", name: "All Saints' Day" },
  "11-11": { id: "independence-day", name: "National Independence Day" },
  "12-24": { id: "christmas-eve", name: "Christmas Eve" },
  "12-25": { id: "christmas-day", name: "Christmas Day" },
  "12-26": { id: "second-christmas-day", name: "Second Day of Christmas" },
};

const easterHolidays: Record<number, PolishPublicHoliday> = {
  0: { id: "easter-sunday", name: "Easter Sunday" },
  1: { id: "easter-monday", name: "Easter Monday" },
  49: { id: "pentecost", name: "Pentecost" },
  60: { id: "corpus-christi", name: "Corpus Christi" },
};

/** Gregorian Easter (Meeus/Jones/Butcher), with one-based calendar months. */
function easterDate(year: number) {
  const a = year % 19;
  const b = Math.floor(year / 100);
  const c = year % 100;
  const d = Math.floor(b / 4);
  const e = b % 4;
  const f = Math.floor((b + 8) / 25);
  const g = Math.floor((b - f + 1) / 3);
  const h = (19 * a + b - d - g + 15) % 30;
  const i = Math.floor(c / 4);
  const k = c % 4;
  const l = (32 + 2 * e + 2 * i - h - k) % 7;
  const m = Math.floor((a + 11 * h + 22 * l) / 451);
  const value = h + l - 7 * m + 114;
  return { month: Math.floor(value / 31), day: value % 31 + 1 };
}

/** No Date objects or timezone conversions; invalid dates are never holidays. */
export function getPolishPublicHoliday(year: number, month: number, day: number): PolishPublicHoliday | null {
  if (!Number.isInteger(year) || year < 1 || year > 9999 || !Number.isInteger(month) || month < 1 || month > 12 || !Number.isInteger(day)) return null;
  const leapYear = year % 4 === 0 && (year % 100 !== 0 || year % 400 === 0);
  const monthLengths = [31, leapYear ? 29 : 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];
  if (day < 1 || day > monthLengths[month - 1]) return null;
  if (month === 12 && day === 24 && year < 2025) return null;
  const fixed = fixedHolidays[`${month}-${day}`];
  if (fixed) return fixed;
  const ordinal = (m: number, d: number) => monthLengths.slice(0, m - 1).reduce((sum, length) => sum + length, d);
  const easter = easterDate(year);
  return easterHolidays[ordinal(month, day) - ordinal(easter.month, easter.day)] ?? null;
}
