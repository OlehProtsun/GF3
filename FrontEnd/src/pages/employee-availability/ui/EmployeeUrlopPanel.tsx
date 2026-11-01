import { useRef, useState } from "react";
import { useEmployeeUrlopCalendarQuery, useUrlopMutation, urlopApi, UrlopMonthCalendar, UrlopStatusLabel, formatUrlopDayRanges, occupiedDays, isoDate, monthDays, calendarMonths, toggleDay, validSelection, warsawToday } from "@entities/urlop";
import { t } from "@shared/i18n";
import { getAvailabilityMonthLabel } from "@entities/availability-groups/model/presentation";
import { getErrorMessage } from "@shared/api/httpClient";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { IosButton } from "@shared/ui/components/IosButton";
import { SearchableSelect } from "@shared/ui/components/SearchableSelect";
import styles from "./EmployeeUrlopPanel.module.css";

export function EmployeeUrlopPanel() {
  const query = useEmployeeUrlopCalendarQuery();
  const mutation = useUrlopMutation(urlopApi.submit);
  const [shop, setShop] = useState(0);
  const [monthIndex, setMonthIndex] = useState(0);
  const [selected, setSelected] = useState<number[]>([]);
  const controls = useRef<HTMLDivElement>(null);
  const shopId = shop || query.data?.shops[0]?.id || 0;
  const months = query.data?.months.filter(x => x.shopId === shopId) ?? [];
  const month = months[monthIndex];
  const today = warsawToday();
  const monthOptions = months.length ? months : calendarMonths(Number(today.slice(0, 4)), Number(today.slice(5, 7)));
  const blocked = occupiedDays(month?.requests ?? []);
  const disabledDays = new Set(month ? monthDays(month.year, month.month).filter(day => blocked.has(day) || isoDate(month.year, month.month, day) < today) : []);
  return <section className={styles.panel} aria-label={t("Urlop")} aria-busy={query.isLoading}>
    {query.isLoading ? <p className={styles.notice} role="status">{t("Loading")}</p> : null}
    {query.error ? <ErrorBanner>{getErrorMessage(query.error)}</ErrorBanner> : null}
    {mutation.error ? <ErrorBanner>{getErrorMessage(mutation.error)}</ErrorBanner> : null}
    {query.data && !query.data.shops.length ? <p className={styles.notice}>{t("No shops available for vacation requests.")}</p> : null}
    {query.data ? <div ref={controls} className={styles.controls} onKeyDown={event => {
      const trigger = controls.current?.querySelector<HTMLButtonElement>('button[aria-expanded="true"]');
      if (!trigger) return;
      if (event.key === "Escape") { event.preventDefault(); event.stopPropagation(); trigger.click(); trigger.focus({ preventScroll: true }); }
      if (event.key === "ArrowDown" || event.key === "ArrowUp" || event.key === "Tab" && event.target === trigger) {
        const options = [...document.querySelectorAll<HTMLButtonElement>('[role="listbox"] [role="option"]')];
        if (!options.length) return;
        event.preventDefault();
        const index = options.indexOf(document.activeElement as HTMLButtonElement);
        const backward = event.key === "ArrowUp" || event.shiftKey;
        const next = index < 0 ? backward ? options.length - 1 : 0 : (index + (backward ? -1 : 1) + options.length) % options.length;
        options[next]?.focus();
      }
    }}>
      {query.data.shops.length ? <div className={styles.field}><label htmlFor="employee-urlop-shop">{t("Shop")}</label>
        <SearchableSelect id="employee-urlop-shop" className={styles.select} value={String(shopId)} options={query.data.shops.map(x => ({ value: String(x.id), label: x.name }))}
          placeholder={t("Shop")} dropdownTitle={t("Shop")} ariaLabel={t("Shop")} size="compact" shadow="soft" showSelectedHint={false} searchEnabled dropdownWidth={260} disabled={mutation.isPending}
          onChange={value => { setShop(Number(value)); setSelected([]); document.getElementById("employee-urlop-shop")?.focus({ preventScroll: true }); }} />
      </div> : null}
      <div className={styles.field}><label htmlFor="employee-urlop-month">{t("Month")}</label>
        <SearchableSelect id="employee-urlop-month" className={styles.select} value={String(monthIndex)} options={monthOptions.map((x, i) => ({ value: String(i), label: `${getAvailabilityMonthLabel(x.month)} ${x.year}` }))}
          placeholder={t("Month")} dropdownTitle={t("Month")} ariaLabel={t("Month")} size="compact" shadow="soft" showSelectedHint={false} searchEnabled={false} dropdownWidth={260} disabled={mutation.isPending}
          onChange={value => { setMonthIndex(Number(value)); setSelected([]); document.getElementById("employee-urlop-month")?.focus({ preventScroll: true }); }} />
      </div>
    </div> : null}
    {month ? <>
      <p className={styles.monthStatus} data-open={month.isOpen} role="status">{t(month.isOpen ? "Open for requests" : "Month closed by manager.")}</p>
      <div className={styles.calendarSurface}><UrlopMonthCalendar year={month.year} month={month.month} selected={selected} approved={month.approved}
        pending={new Set(month.requests.filter(x => x.status === "pending").flatMap(x => x.days))}
        disabledDays={disabledDays} readOnly={!month.isOpen || mutation.isPending} onToggle={day => setSelected(toggleDay(selected, day))} /></div>
      <div className={styles.controls}>
        <IosButton label={t("Clear selection")} variant="secondary" onClick={() => setSelected([])} disabled={mutation.isPending || !selected.length} />
        <IosButton label={t("Submit request")} disabled={!month.isOpen || mutation.isPending || !validSelection(month.year, month.month, selected, blocked, today)}
          onClick={() => mutation.mutate({ shopId, year: month.year, month: month.month, days: selected }, { onSuccess: () => setSelected([]) })} />
      </div>
      <h3>{t("Your requests")}</h3>
      {!month.requests.length ? <p className={styles.notice}>{t("No vacation requests.")}</p> : month.requests.map(x => <article className={styles.request} key={x.requestId}>
        <UrlopStatusLabel status={x.status} /> <span>{formatUrlopDayRanges(x.year, x.month, x.days)}</span>
      </article>)}
      <h3>{t("Approved vacations of colleagues")}</h3>
      {month.approved.map(x => <p className={styles.colleague} key={`${x.employeeId}-${x.date}`}>{x.employeeName} — {x.date} <strong>UR</strong></p>)}
    </> : null}
  </section>;
}
