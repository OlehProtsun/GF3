import { useState } from "react";
import { useEmployeeUrlopCalendarQuery, useUrlopMutation, urlopApi, UrlopMonthCalendar, UrlopStatusLabel, occupiedDays, isoDate, monthDays, calendarMonths, toggleDay, validSelection, warsawToday } from "@entities/urlop";
import { t } from "@shared/i18n";
import { getAvailabilityMonthLabel } from "@entities/availability-groups/model/presentation";
import { getErrorMessage } from "@shared/api/httpClient";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { IosButton } from "@shared/ui/components/IosButton";
import styles from "./EmployeeUrlopPanel.module.css";

export function EmployeeUrlopPanel() {
  const query = useEmployeeUrlopCalendarQuery();
  const mutation = useUrlopMutation(urlopApi.submit);
  const [shop, setShop] = useState(0);
  const [monthIndex, setMonthIndex] = useState(0);
  const [selected, setSelected] = useState<number[]>([]);
  const shopId = shop || query.data?.shops[0]?.id || 0;
  const months = query.data?.months.filter(x => x.shopId === shopId) ?? [];
  const month = months[monthIndex];
  const today = warsawToday();
  const monthOptions = months.length ? months : calendarMonths(Number(today.slice(0, 4)), Number(today.slice(5, 7)));
  const blocked = occupiedDays(month?.requests ?? []);
  const disabledDays = new Set(month ? monthDays(month.year, month.month).filter(day => blocked.has(day) || isoDate(month.year, month.month, day) < today) : []);
  return <section className={styles.panel} aria-label={t("Urlop")}>
    {query.isLoading ? <p>{t("Loading")}</p> : null}
    {query.error ? <ErrorBanner>{getErrorMessage(query.error)}</ErrorBanner> : null}
    {mutation.error ? <ErrorBanner>{getErrorMessage(mutation.error)}</ErrorBanner> : null}
    {query.data && !query.data.shops.length ? <p>{t("No shops available for vacation requests.")}</p> : null}
    {query.data ? <div className={styles.controls}>
      {query.data.shops.length ? <label>{t("Shop")}<select aria-label={t("Shop")} value={shopId} disabled={mutation.isPending} onChange={event => { setShop(Number(event.target.value)); setSelected([]); }}>
        {query.data.shops.map(x => <option key={x.id} value={x.id}>{x.name}</option>)}
      </select></label> : null}
      <label>{t("Month")}<select aria-label={t("Month")} value={monthIndex} disabled={mutation.isPending} onChange={event => { setMonthIndex(Number(event.target.value)); setSelected([]); }}>
        {monthOptions.map((x, i) => <option key={`${x.year}-${x.month}`} value={i}>{getAvailabilityMonthLabel(x.month)} {x.year}</option>)}
      </select></label>
    </div> : null}
    {month ? <>
      <p role="status">{t(month.isOpen ? "Open for requests" : "Month closed by manager.")}</p>
      <UrlopMonthCalendar year={month.year} month={month.month} selected={selected} approved={month.approved}
        pending={new Set(month.requests.filter(x => x.status === "pending").flatMap(x => x.days))}
        disabledDays={disabledDays} readOnly={!month.isOpen || mutation.isPending} onToggle={day => setSelected(toggleDay(selected, day))} />
      <div className={styles.controls}>
        <IosButton label={t("Clear selection")} variant="secondary" onClick={() => setSelected([])} disabled={mutation.isPending || !selected.length} />
        <IosButton label={t("Submit request")} disabled={!month.isOpen || mutation.isPending || !validSelection(month.year, month.month, selected, blocked, today)}
          onClick={() => mutation.mutate({ shopId, year: month.year, month: month.month, days: selected }, { onSuccess: () => setSelected([]) })} />
      </div>
      <h3>{t("Your requests")}</h3>
      {!month.requests.length ? <p>{t("No vacation requests.")}</p> : month.requests.map(x => <article className={styles.request} key={x.requestId}>
        <UrlopStatusLabel status={x.status} /> <span>{x.days.map(day => isoDate(x.year, x.month, day)).join(", ")}</span>
      </article>)}
      <h3>{t("Approved vacations of colleagues")}</h3>
      {month.approved.map(x => <p key={`${x.employeeId}-${x.date}`}>{x.employeeName} — {x.date} <strong>UR</strong></p>)}
    </> : null}
  </section>;
}
