import { t, dateTimeFormat } from "@shared/i18n";
import { isoDate, monthDays, monthStartOffset } from "../model/calendar";
import type { ApprovedUrlopDay } from "../model/types";
import styles from "./UrlopMonthCalendar.module.css";

type Props = {
  year: number; month: number; selected?: readonly number[]; pending?: ReadonlySet<number>;
  approved?: readonly ApprovedUrlopDay[]; disabledDays?: ReadonlySet<number>; readOnly?: boolean;
  onToggle?: (day: number) => void;
};
export function UrlopMonthCalendar({ year, month, selected = [], pending = new Set(), approved = [], disabledDays = new Set(), readOnly, onToggle }: Props) {
  const offset = monthStartOffset(year, month);
  const weekday = dateTimeFormat("en-GB", { weekday: "short" });
  return <div className={styles.calendar} role="group" aria-label={t("Vacation calendar")}>
    {Array.from({ length: 7 }, (_, i) => <span key={`weekday-${i}`} className={styles.weekday}>{weekday.format(new Date(2026, 0, 5 + i))}</span>)}
    {Array.from({ length: offset }, (_, i) => <span key={`empty-${i}`} />)}
    {monthDays(year, month).map(day => {
      const date = isoDate(year, month, day);
      const names = approved.filter(x => x.date === date).map(x => x.employeeName || t("Employee #{0}", x.employeeId));
      return <button key={day} type="button" aria-label={`${date}${names.length ? ` UR: ${names.join(", ")}` : ""}`}
        aria-pressed={selected.includes(day)} disabled={readOnly || disabledDays.has(day)} title={names.join(", ")}
        className={[styles.day, selected.includes(day) ? styles.selected : "", pending.has(day) ? styles.pending : "", names.length ? styles.approved : ""].join(" ")}
        onClick={() => onToggle?.(day)}><strong>{day}</strong>{names.length ? <><b>UR</b><small>{names.join(", ")}</small></> : pending.has(day) ? <small>{t("Pending")}</small> : null}</button>;
    })}
  </div>;
}
