import { useEffect, useState } from "react";
import { isoDate, monthDays, monthStartOffset, warsawToday, type ApprovedUrlopDay } from "@entities/urlop";
import { dateTimeFormat, t } from "@shared/i18n";
import { GroupIcon } from "@shared/ui/icons";
import styles from "./ManagerUrlopOverviewCalendar.module.css";

type Props = { year: number; month: number; approvedDays: readonly ApprovedUrlopDay[]; loading?: boolean };

export function ManagerUrlopOverviewCalendar({ year, month, approvedDays, loading = false }: Props) {
  const [expandedDate, setExpandedDate] = useState<string | null>(null);
  const period = isoDate(year, month, 1).slice(0, 7);
  useEffect(() => { setExpandedDate(null); }, [period]);
  const days = monthDays(year, month);
  const byDate = new Map<string, Map<number, ApprovedUrlopDay>>();
  for (const entry of approvedDays) {
    if (!days.some(day => isoDate(year, month, day) === entry.date)) continue;
    if (!byDate.has(entry.date)) byDate.set(entry.date, new Map());
    byDate.get(entry.date)!.set(entry.employeeId, entry);
  }
  const count = new Set([...byDate.values()].flatMap(entries => [...entries.keys()])).size;
  const name = (entry: ApprovedUrlopDay) => entry.employeeName || t("Employee #{0}", entry.employeeId);
  const chip = (entry: ApprovedUrlopDay, fullName = false) => {
    const employeeName = name(entry);
    const parts = employeeName.trim().split(/\s+/);
    const label = !fullName && entry.employeeName && parts.length > 1 ? `${parts[0][0]}. ${parts.slice(1).join(" ")}` : employeeName;
    return <span key={entry.employeeId} title={employeeName} className={`${styles.chip} ${styles[`tone${entry.employeeId % 6}`]}`}><i aria-hidden="true" /><span className={styles.chipText}>{label}</span></span>;
  };
  const expanded = expandedDate?.startsWith(isoDate(year, month, 1).slice(0, 7)) ? expandedDate : null;
  const offset = monthStartOffset(year, month);
  const previousMonth = new Date(`${isoDate(year, month, 1)}T12:00:00Z`);
  previousMonth.setUTCDate(0);
  const today = warsawToday();
  return <section className={styles.card} aria-label={t("Vacation calendar")} aria-busy={loading} onKeyDown={event => { if (event.key === "Escape") { setExpandedDate(null); if (expanded) document.getElementById(`vacation-${expanded}`)?.focus(); } }}>
    <header className={styles.header}><h2>{dateTimeFormat("en-GB", { month: "long", year: "numeric" }).format(new Date(`${isoDate(year, month, 1)}T12:00:00`))}</h2><span className={styles.count}><GroupIcon size={16} aria-hidden="true" />{t("{0} employees on vacation", count)}</span></header>
    <div className={styles.loading} role="status">{loading ? t("Loading") : ""}</div>
    <div className={styles.scroll}>
      <div className={styles.grid}>
        {Array.from({ length: 7 }, (_, i) => <span className={styles.weekday} key={`weekday-${i}`}>{dateTimeFormat("en-GB", { weekday: "short" }).format(new Date(2026, 0, 5 + i))}</span>)}
        {Array.from({ length: offset }, (_, i) => <span aria-hidden="true" className={styles.placeholder} key={`before-${i}`}>{previousMonth.getUTCDate() - offset + i + 1}</span>)}
        {days.map(day => {
          const date = isoDate(year, month, day);
          const entries = [...(byDate.get(date)?.values() ?? [])].sort((a, b) => a.employeeId - b.employeeId);
          return <button id={`vacation-${date}`} className={styles.day} disabled={loading} type="button" key={date} aria-current={date === today ? "date" : undefined} aria-label={`${date}${entries.length ? ` UR: ${entries.map(name).join(", ")}` : ""}`} aria-expanded={expanded === date} onClick={() => setExpandedDate(expanded === date ? null : date)}>
            <strong>{day}</strong>{entries.slice(0, 2).map(entry => chip(entry))}{entries.length > 2 ? <small aria-label={t("+{0} more", entries.length - 2)}>+{entries.length - 2}</small> : null}
          </button>;
        })}
        {Array.from({ length: 42 - offset - days.length }, (_, i) => <span aria-hidden="true" className={styles.placeholder} key={`after-${i}`}>{i + 1}</span>)}
      </div>
    </div>
    {expanded ? <div className={styles.details} role="region" aria-label={t("Approved vacations on {0}", expanded)}><div className={styles.header}><strong>{t("Approved vacations on {0}", expanded)}</strong><button type="button" onClick={() => { setExpandedDate(null); document.getElementById(`vacation-${expanded}`)?.focus(); }}>{t("Close")}</button></div>{[...(byDate.get(expanded)?.values() ?? [])].map(entry => chip(entry, true))}</div> : null}
  </section>;
}
