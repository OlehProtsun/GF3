import { useEffect, useRef, useState } from "react";
import { NavLink as Link } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { dateTimeFormat, t } from "@shared/i18n";
import { useLanguageRevision } from "@shared/i18n/useLanguageRevision";
import { getErrorMessage } from "@shared/api/httpClient";
import { queryKeys } from "@shared/api/queryKeys";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { SearchableSelect } from "@shared/ui/components/SearchableSelect";
import { CardSection } from "@shared/ui/sections/CardSection";
import { PageHeader } from "@shared/ui/PageHeader";
import { CheckIcon, ChevronLeftIcon, ChevronRightIcon, ClockIcon, GroupIcon, NoteIcon, ScheduleDetailsIcon, ShopIcon, StatisticsIcon, SwapOffersIcon } from "@shared/ui/icons";
import type { HomeActivityTab } from "../api/managerHomeActivityApi";
import { formatHomeDate, getHomeActivityPresentation, getHomeActivityRoute, getWarsawShiftStatus, moveSelectedDay, parseHomeDay, setSelectedMonth, warsawToday } from "./homeActivity";
import { loadHomeDashboard } from "./homeDashboard";
import { useHomeActivityQuery } from "./useHomeActivityQuery";
import styles from "./HomePage.module.css";

const shiftStatusOrder = { in_progress: 0, completed: 1, upcoming: 2 };

function ManagerPcHomePage() {
  useLanguageRevision();
  const queryClient = useQueryClient();
  const [selectedDay, setSelectedDay] = useState(() => warsawToday());
  const [tab, setTab] = useState<HomeActivityTab>("all");
  const [now, setNow] = useState(() => new Date());
  const tabRefs = useRef<Array<HTMLButtonElement | null>>([]);
  const year = Number(selectedDay.slice(0, 4)), month = Number(selectedDay.slice(5, 7)), period = selectedDay.slice(0, 7);
  useEffect(() => {
    const timer = window.setInterval(() => setNow(new Date()), 30_000);
    return () => window.clearInterval(timer);
  }, []);
  useEffect(() => {
    const keys = [queryKeys.home.dashboard(selectedDay), [...queryKeys.home.activity(), year, month, tab]];
    const refresh = () => {
      if (document.visibilityState === "hidden") return;
      setNow(new Date());
      keys.forEach(queryKey => {
        if (Date.now() - queryClient.getQueryState(queryKey).updatedAt >= 30_000) queryClient.invalidateQueries({ queryKey });
      });
    };
    window.addEventListener("focus", refresh); document.addEventListener("visibilitychange", refresh);
    return () => { window.removeEventListener("focus", refresh); document.removeEventListener("visibilitychange", refresh); };
  }, [queryClient, selectedDay, year, month, tab]);
  const home = useQuery({ queryKey: queryKeys.home.dashboard(selectedDay), staleTime: 30_000, queryFn: ({ signal }) => loadHomeDashboard(selectedDay, signal) });
  const todayRows = (home.data?.todayRows ?? [])
    .map(row => ({ row, status: getWarsawShiftStatus(selectedDay, row.fromTime, row.toTime, now) }))
    .sort((left, right) => shiftStatusOrder[left.status] - shiftStatusOrder[right.status]);
  const activity = useHomeActivityQuery(year, month, tab);
  const counts = activity.data?.pages[0];
  const items = activity.data?.pages.flatMap(page => page.items) ?? [];
  const months = Array.from({ length: 25 }, (_, index) => {
    const date = parseHomeDay(`${period}-01`); date.setUTCMonth(date.getUTCMonth() + index - 12); return date;
  }).filter(date => date.getUTCFullYear() >= 1 && date.getUTCFullYear() <= 9999).map(date => ({ value: date.toISOString().slice(0, 7), label: dateTimeFormat("en-GB", { month: "long", year: "numeric", timeZone: "UTC" }).format(date) }));
  function selectTab(next: HomeActivityTab) {
    if (next === tab) return;
    const key = [...queryKeys.home.activity(), year, month, next];
    const cached = queryClient.getQueryState<NonNullable<typeof activity.data>>(key).data;
    if (cached) queryClient.setQueryData(key, { ...cached, pages: cached.pages.slice(0, 1) });
    setTab(next);
  }
  const dateLabel = formatHomeDate(selectedDay, true);
  return <div className={styles.page}>
    <PageHeader title={t("Home")} subtitle={t("Overview of today's schedules and recent application activity.")}
      className={styles.pageHeader} maxWidth="min(1500px, calc(100% - 24px))" />
    <div className={styles.dashboard}>
    <CardSection className={styles.card} headerClassName={styles.cardHeader} titleClassName={styles.cardTitle}
      icon={<span className={styles.headerIcon}><ScheduleDetailsIcon size={18} /></span>}
      title={<><h2>{selectedDay === warsawToday(now) ? t("Who Works Today?") : t("Who Works on {0}?", dateLabel)}</h2><span className={styles.subtitle}>{t("Employees scheduled for {0}", dateLabel)}</span></>}
      headerRightSlot={<span className={styles.employeeCount}><GroupIcon size={17} />{home.data ? t("{0} assigned employees", home.data.todayActiveEmployeesCount) : "—"}</span>}>
      {home.isError && <ErrorBanner dismissible={false}>{getErrorMessage(home.error, t("Could not load scheduled employees."))}<button className={styles.retry} onClick={() => queryClient.invalidateQueries({ queryKey: queryKeys.home.dashboard(selectedDay) })}>{t("Retry")}</button></ErrorBanner>}
      {home.data?.hasPartialData && <p role="status" className={styles.warning}>{t("Some schedules could not be loaded. Results may be incomplete.")}</p>}
      <div className={styles.tableScroll} tabIndex={0} role="region" aria-label={t("Scheduled employees")} aria-busy={home.isLoading}>
        <table className={styles.table}><thead><tr>{[t("Employee"), t("Shift"), t("Shop"), t("Hours"), t("Status")].map(label => <th key={label} scope="col">{label}</th>)}</tr></thead>
          <tbody>{todayRows.map(({ row, status }) => {
            const label = status === "upcoming" ? t("Upcoming") : status === "in_progress" ? t("In Progress") : t("Completed");
            const to = `/container/${row.containerId}/graphs/${row.graphId}`;
            const initials = row.employee.split(/\s+/).filter(Boolean).slice(0, 2).map(part => Array.from(part)[0]).join("").toUpperCase();
            return <tr key={row.id}>
              <td><Link to={to} className={styles.employeeLink} aria-label={t("Open schedule for {0}: {1}, {2}, {3}", row.employee, row.shift, row.shop, row.dateLabel)}><span className={styles.initials} aria-hidden="true">{initials}</span><strong title={row.employee}>{row.employee}</strong></Link></td>
              <td><Link to={to} tabIndex={-1}><ClockIcon size={15} />{row.shift}</Link></td>
              <td><Link to={to} tabIndex={-1}><ShopIcon size={15} className={styles.shopIcon} /><span title={row.shop}>{row.shop}</span></Link></td>
              <td><Link to={to} tabIndex={-1}>{row.hoursText}</Link></td>
              <td><Link to={to} tabIndex={-1}><span className={`${styles.status} ${styles[status]}`}>{status === "completed" && <CheckIcon size={12} />}{label}</span></Link></td>
            </tr>;
          })}</tbody>
        </table>
        {home.isLoading && <p role="status" className={styles.empty}>{t("Loading scheduled employees...")}</p>}
        {!home.isLoading && !home.isError && !home.data?.todayRows.length && <p className={styles.empty}>{t("No employees scheduled for this date.")}</p>}
      </div>
    </CardSection>
    <CardSection className={styles.card} headerClassName={styles.cardHeader} titleClassName={styles.cardTitle} headerRightClassName={styles.activityControls}
      icon={<span className={styles.headerIcon}><StatisticsIcon size={18} /></span>}
      title={<><h2>{t("Application Activity")}</h2><span className={styles.subtitle}>{t("Recent actions in the system")}</span></>}
      headerRightSlot={<>
        <SearchableSelect value={period} options={months} onChange={value => setSelectedDay(day => setSelectedMonth(day, value))} size="compact" searchEnabled={false} showSelectedHint={false} ariaLabel={t("Activity month")} placeholder={t("Select month")} dropdownTitle={t("Select month")} leadingIcon={<ScheduleDetailsIcon size={16} />} className={styles.monthSelect} />
        <div className={styles.dayControls}>
          <button type="button" className={styles.iconButton} aria-label={t("Previous day")} disabled={selectedDay === "0001-01-01"} onClick={() => setSelectedDay(day => moveSelectedDay(day, -1))}><ChevronLeftIcon size={16} /></button>
          <button type="button" className={styles.todayButton} onClick={() => setSelectedDay(warsawToday())}>{t("Today")}</button>
          <button type="button" className={styles.iconButton} aria-label={t("Next day")} disabled={selectedDay === "9999-12-31"} onClick={() => setSelectedDay(day => moveSelectedDay(day, 1))}><ChevronRightIcon size={16} /></button>
        </div>
      </>}>
      <div role="tablist" aria-label={t("Activity filters")} className={styles.tabs}>
        {(["all", "approvals"] as const).map((value, index) => <button key={value} ref={element => { tabRefs.current[index] = element; }} type="button" role="tab" id={`home-tab-${value}`} aria-selected={tab === value} aria-controls="home-activity-panel" tabIndex={tab === value ? 0 : -1} onClick={() => selectTab(value)} onKeyDown={event => {
          if (!["ArrowLeft", "ArrowRight", "Home", "End"].includes(event.key)) return;
          event.preventDefault(); const next = event.key === "Home" ? 0 : event.key === "End" ? 1 : 1 - index;
          selectTab(next === 0 ? "all" : "approvals"); tabRefs.current[next]?.focus();
        }}>{value === "all" ? t("All Activity") : t("Requires Approval")}<span className={value === "all" ? styles.allCount : styles.approvalCount}>{counts ? value === "all" ? counts.allActivityCount : counts.requiresApprovalCount : "—"}</span></button>)}
      </div>
      <p className={styles.scope}>{tab === "all" ? t("Events recorded in {0}", months.find(option => option.value === period)?.label) : t("All outstanding approvals, including earlier months")}</p>
      {activity.isError && <ErrorBanner dismissible={false}>{getErrorMessage(activity.error, t("Could not load application activity."))}<button className={styles.retry} onClick={activity.refetch}>{t("Retry")}</button></ErrorBanner>}
      <div id="home-activity-panel" role="tabpanel" aria-labelledby={`home-tab-${tab}`} className={styles.feed} aria-busy={activity.isFetching}>
        {activity.isLoading && <p role="status" className={styles.empty}>{t("Loading activity...")}</p>}
        {!activity.isLoading && !activity.isError && !items.length && <p className={styles.empty}>{tab === "all" ? t("No activity recorded in this month.") : t("No outstanding approvals.")}</p>}
        {items.map(item => {
          const view = getHomeActivityPresentation(item);
          const route = getHomeActivityRoute(item);
          const Icon = view.iconKind === "swap" ? SwapOffersIcon : view.iconKind === "correction" ? NoteIcon : ScheduleDetailsIcon;
          const content = <>
            <span className={`${styles.eventIcon} ${styles[view.tone]}`}><Icon size={18} /></span>
            <span className={styles.eventText}><strong>{view.title}</strong><span>{view.description}</span>{!route && <span>{t("This activity's resource is no longer available.")}</span>}</span>
            <span className={`${styles.status} ${styles[view.tone]}`}>{view.localizedStatus}</span>
            <time dateTime={item.occurredAtUtc} title={view.dateTimeLabel} aria-label={view.dateTimeLabel}>{view.time}</time>
          </>;
          return route ? <Link key={item.id} to={route} className={styles.event}>{content}</Link>
            : <div key={item.id} className={`${styles.event} ${styles.unavailableEvent}`} aria-disabled="true">{content}</div>;
        })}
      </div>
      {Boolean(activity.nextPageError) && <ErrorBanner dismissible={false}>{t("Could not load more activity.")}<button className={styles.retry} onClick={() => void activity.fetchNextPage()}>{t("Retry")}</button></ErrorBanner>}
      <div className={styles.feedFooter}>{activity.hasNextPage && <button type="button" className={styles.moreButton} disabled={activity.isFetchingNextPage || activity.isFetching} onClick={() => void activity.fetchNextPage()}>{activity.isFetchingNextPage ? t("Loading...") : t("Show more")}<ChevronRightIcon size={14} className={styles.downArrow} /></button>}</div>
    </CardSection>
    </div>
  </div>;
}
export function HomePage({ phoneMode = false }: { phoneMode?: boolean }) {
  if (phoneMode) {
    return <div className={`${styles.placeholder} ${styles.placeholderPhone}`}>
      <h1 className={styles.placeholderTitle}>{t("Coming Soon")}</h1>
    </div>;
  }
  return <ManagerPcHomePage />;
}
