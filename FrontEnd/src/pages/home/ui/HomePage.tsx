import { useEffect, useLayoutEffect, useMemo, useRef, useState } from "react";
import { NavLink as Link } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { dateTimeFormat, getLocale, t } from "@shared/i18n";
import { useLanguageRevision } from "@shared/i18n/useLanguageRevision";
import { usePageScrollbarHidden } from "@shared/lib/usePageScrollbarHidden";
import { getErrorMessage } from "@shared/api/httpClient";
import { queryKeys } from "@shared/api/queryKeys";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { SearchableSelect } from "@shared/ui/components/SearchableSelect";
import { CardSection } from "@shared/ui/sections/CardSection";
import { PageHeader } from "@shared/ui/PageHeader";
import { AvailabilityIcon, CheckIcon, ChevronLeftIcon, ChevronRightIcon, ClockIcon, GroupIcon, NoteIcon, ScheduleDetailsIcon, ShopIcon, StatisticsIcon, SwapOffersIcon, WarnIcon } from "@shared/ui/icons";
import { managerHomeActivityApi, type HomeActivityTab } from "../api/managerHomeActivityApi";
import { formatHomeDate, getHomeActivityPresentation, getHomeActivityRoute, getWarsawShiftStatus, moveSelectedDay, parseHomeDay, warsawToday } from "./homeActivity";
import { loadHomeDashboard } from "./homeDashboard";
import { useHomeActivityQuery } from "./useHomeActivityQuery";
import styles from "./HomePage.module.css";

const shiftStatusOrder = { in_progress: 0, completed: 1, upcoming: 2 };
const activityTabs = [
  { value: "all", label: "All Activity", Icon: StatisticsIcon },
  { value: "swaps", label: "Swaps", Icon: SwapOffersIcon },
  { value: "vacations", label: "Vacations", Icon: ScheduleDetailsIcon },
  { value: "corrections", label: "Corrections", Icon: NoteIcon },
  { value: "approvals", label: "Requires Approval", Icon: WarnIcon },
] as const;

function getMonthOptions(period: string) {
  const formatter = dateTimeFormat("en-GB", { month: "long", year: "numeric", timeZone: "UTC" });
  return Array.from({ length: 25 }, (_, index) => {
    const date = parseHomeDay(`${period}-01`);
    date.setUTCMonth(date.getUTCMonth() + index - 12);
    return date;
  }).filter(date => date.getUTCFullYear() >= 1 && date.getUTCFullYear() <= 9999)
    .map(date => ({ value: date.toISOString().slice(0, 7), label: formatter.format(date) }));
}

function ManagerPcHomePage() {
  useLanguageRevision();
  usePageScrollbarHidden();
  const queryClient = useQueryClient();
  const [now, setNow] = useState(() => new Date());
  const [selectedDay, setSelectedDay] = useState(() => warsawToday(now));
  const [period, setPeriod] = useState(() => warsawToday(now).slice(0, 7));
  const [selectedShop, setSelectedShop] = useState<number | null>(null);
  const [tab, setTab] = useState<HomeActivityTab>("all");
  const tabRefs = useRef<Array<HTMLButtonElement | null>>([]);
  const feedRef = useRef<HTMLDivElement>(null);
  const feedContentRef = useRef<HTMLDivElement>(null);
  const sentinelRef = useRef<HTMLDivElement>(null);
  const [overflow, setOverflow] = useState({ top: false, bottom: false });
  const schedulePeriod = selectedDay.slice(0, 7);
  const locale = getLocale();
  const scheduleDates = useMemo(() => {
    const formatter = new Intl.DateTimeFormat(locale, { day: "numeric", month: "long", year: "numeric", timeZone: "UTC" });
    const periods = getMonthOptions(schedulePeriod).map(option => option.value);
    return [schedulePeriod, ...periods.filter(value => value !== schedulePeriod)].flatMap(value => {
      const date = parseHomeDay(`${value}-01`);
      const month = date.getUTCMonth();
      const lastDay = new Date(date);
      lastDay.setUTCMonth(month + 1, 0);
      return Array.from({ length: lastDay.getUTCDate() }, (_, index) => {
        date.setUTCDate(index + 1);
        return { value: date.toISOString().slice(0, 10), label: formatter.format(date) };
      });
    });
  }, [schedulePeriod, locale]);
  const year = Number(period.slice(0, 4)), month = Number(period.slice(5, 7));
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
  const filteredRows = (home.data?.todayRows ?? []).filter(row => selectedShop === null || row.shopId === selectedShop);
  const employeeCount = selectedShop === null ? home.data?.todayActiveEmployeesCount : new Set(filteredRows.map(row => row.employeeId)).size;
  const todayRows = filteredRows
    .map(row => ({ row, status: getWarsawShiftStatus(selectedDay, row.fromTime, row.toTime, now) }))
    .sort((left, right) => shiftStatusOrder[left.status] - shiftStatusOrder[right.status]);
  const activity = useHomeActivityQuery(year, month, tab);
  const counts = activity.data?.pages[0] ??
    queryClient.getQueryState<NonNullable<typeof activity.data>>([...queryKeys.home.activity(), year, month, "all"]).data?.pages[0];
  const navBadges = useQuery({
    queryKey: queryKeys.home.navBadges(),
    queryFn: ({ signal }) => managerHomeActivityApi.navBadges(signal),
    staleTime: 30_000,
  });
  const requiresApprovalCount = activity.data?.pages[0]?.requiresApprovalCount ?? navBadges.data?.totalPending ?? counts?.requiresApprovalCount;
  const items = activity.data?.pages.flatMap(page => page.items) ?? [];
  const lastOffset = activity.data?.pages.at(-1)?.nextOffset;
  const canLoadMore = activity.hasNextPage && !activity.isError && !activity.isLoading && !activity.isFetching &&
    !activity.isFetchingNextPage && !activity.nextPageError;
  const fetchNextPageRef = useRef(activity.fetchNextPage);
  useLayoutEffect(() => {
    fetchNextPageRef.current = activity.fetchNextPage;
  }, [activity.fetchNextPage]);
  useLayoutEffect(() => {
    if (feedRef.current) feedRef.current.scrollTop = 0;
  }, [period, tab]);
  useLayoutEffect(() => {
    const feed = feedRef.current;
    const content = feedContentRef.current;
    if (!feed || !content) return;
    const updateOverflow = () => {
      const top = feed.scrollTop > 2;
      const bottom = feed.scrollTop + feed.clientHeight < feed.scrollHeight - 2;
      setOverflow(previous => previous.top === top && previous.bottom === bottom ? previous : { top, bottom });
    };
    updateOverflow();
    feed.addEventListener("scroll", updateOverflow, { passive: true });
    window.addEventListener("resize", updateOverflow);
    const observer = typeof ResizeObserver !== "undefined" ? new ResizeObserver(updateOverflow) : null;
    observer?.observe(feed);
    observer?.observe(content);
    return () => {
      feed.removeEventListener("scroll", updateOverflow);
      window.removeEventListener("resize", updateOverflow);
      observer?.disconnect();
    };
  }, [period, tab, items.length, activity.isFetchingNextPage]);
  useLayoutEffect(() => {
    const root = feedRef.current;
    const sentinel = sentinelRef.current;
    if (!canLoadMore || !root || !sentinel || typeof IntersectionObserver === "undefined") return;
    let observing = true;
    const observer = new IntersectionObserver(entries => {
      if (!observing || !entries.some(entry => entry.isIntersecting)) return;
      observing = false;
      void fetchNextPageRef.current();
    }, { root, rootMargin: "0px 0px 80px 0px" });
    observer.observe(sentinel);
    return () => {
      observing = false;
      observer.disconnect();
    };
  }, [canLoadMore, period, tab, items.length, lastOffset]);
  const activityPeriods = getMonthOptions(period);
  const periodStart = parseHomeDay(`${period}-01`);
  const periodEnd = new Date(periodStart);
  periodEnd.setUTCMonth(month, 0);
  const periodLabel = dateTimeFormat("en-GB", { month: "long", year: "numeric", timeZone: "UTC" }).format(periodStart);
  const rangeLabel = new Intl.DateTimeFormat(getLocale(), {
    day: "numeric", month: "short", year: "numeric", timeZone: "UTC",
  }).formatRange(periodStart, periodEnd);
  function moveActivityMonth(delta: number) {
    setPeriod(current => {
      const date = parseHomeDay(`${current}-01`);
      date.setUTCMonth(date.getUTCMonth() + delta);
      return date.getUTCFullYear() >= 1 && date.getUTCFullYear() <= 9999 ? date.toISOString().slice(0, 7) : current;
    });
  }
  function selectTab(next: HomeActivityTab) {
    if (next === tab) return;
    const key = [...queryKeys.home.activity(), year, month, next];
    const cached = queryClient.getQueryState<NonNullable<typeof activity.data>>(key).data;
    if (cached) queryClient.setQueryData(key, { ...cached, pages: cached.pages.slice(0, 1) });
    setTab(next);
  }
  const dateLabel = formatHomeDate(selectedDay, true);
  const scheduleDateLabel = dateTimeFormat("en-GB", {
    weekday: "long", day: "numeric", month: "short", year: "numeric", timeZone: "UTC",
  }).format(parseHomeDay(selectedDay));
  return <div className={styles.page}>
    <PageHeader title={t("Home")} subtitle={t("Overview of today's schedules and recent application activity.")} />
    <div className={styles.dashboard}>
    <CardSection className={styles.card} headerClassName={styles.cardHeader} titleClassName={styles.cardTitle} headerRightClassName={styles.todayControls}
      icon={<span className={styles.headerIcon}><ScheduleDetailsIcon size={18} /></span>}
      title={<><h2>{selectedDay === warsawToday(now) ? t("Who Works Today?") : t("Who Works on {0}?", dateLabel)}</h2><span className={styles.subtitle}>{t("Employees scheduled for {0}", dateLabel)}</span></>}
      headerRightSlot={<span className={styles.employeeCount}><GroupIcon size={17} />{home.data ? t("{0} assigned employees", employeeCount) : "—"}</span>}>
      <div className={`${styles.filterPanel} ${styles.scheduleFilters}`}>
        <div className={styles.periodRow} role="group" aria-label={t("Date")}>
          <span className={styles.periodLabel}><ScheduleDetailsIcon size={16} />{t("Date")}</span>
          <div className={styles.periodControls}>
            <button type="button" className={styles.iconButton} aria-label={t("Previous day")} disabled={selectedDay === "0001-01-01"} onClick={() => setSelectedDay(day => moveSelectedDay(day, -1))}><ChevronLeftIcon size={16} /></button>
            <SearchableSelect value={selectedDay} options={scheduleDates} onChange={setSelectedDay}
              size="compact" showSelectedHint={false} ariaLabel={t("Select date")} placeholder={t("Select date")} dropdownTitle={t("Select date")} className={styles.scheduleDateSelect} />
            <button type="button" className={styles.iconButton} aria-label={t("Next day")} disabled={selectedDay === "9999-12-31"} onClick={() => setSelectedDay(day => moveSelectedDay(day, 1))}><ChevronRightIcon size={16} /></button>
            <button type="button" className={styles.todayButton} onClick={() => setSelectedDay(warsawToday())}>{t("Today")}</button>
          </div>
          <div className={styles.periodRange}><span>{scheduleDateLabel}</span></div>
        </div>
        <div className={styles.shopFilters}>
          <span className={styles.periodLabel}><ShopIcon className={styles.shopIcon} size={16} />{t("Shop")}</span>
          <div className={styles.shopPills} role="group" aria-label={t("Filter by shop")}>
            <button type="button" aria-pressed={selectedShop === null} onClick={event => {
              setSelectedShop(null);
              event.currentTarget.scrollIntoView?.({ block: "nearest", inline: "nearest" });
            }}>{t("All")}</button>
            {home.data?.shops.map(shop => <button type="button" key={shop.id} aria-pressed={selectedShop === shop.id}
              onClick={event => {
                setSelectedShop(shop.id);
                event.currentTarget.scrollIntoView?.({ block: "nearest", inline: "nearest" });
              }}>{shop.name}</button>)}
          </div>
        </div>
      </div>
      {home.isError && <ErrorBanner dismissible={false}>{getErrorMessage(home.error, t("Could not load scheduled employees."))}<button className={styles.retry} onClick={() => queryClient.invalidateQueries({ queryKey: queryKeys.home.dashboard(selectedDay) })}>{t("Retry")}</button></ErrorBanner>}
      {home.data?.hasPartialData && <p role="status" className={styles.warning}>{t("Some schedules could not be loaded. Results may be incomplete.")}</p>}
      <div className={styles.tableScroll} tabIndex={0} role="region" aria-label={t("Scheduled employees")} aria-busy={home.isLoading}>
        <table className={styles.table}><thead><tr>{[t("Employee"), t("Shift"), t("Shop"), t("Hours"), t("Status")].map(label => <th key={label} scope="col">{label}</th>)}</tr></thead>
          <tbody>{todayRows.map(({ row, status }) => {
            const label = status === "upcoming" ? t("Upcoming") : status === "in_progress" ? t("In Progress") : t("Completed");
            const to = `/container/${row.containerId}/graphs/${row.graphId}`;
            const initials = row.employee.split(/\s+/).filter(Boolean).slice(0, 2).map(part => Array.from(part)[0]).join("").toUpperCase();
            return <tr key={row.id}>
              <td><Link to={to} className={styles.employeeLink} aria-label={t("Open schedule for {0}: {1}, {2}, {3}", row.employee, row.shift, row.shop, row.dateLabel)}><span className={styles.initials} data-tone={row.employeeId % 3} aria-hidden="true">{initials}</span><strong title={row.employee}>{row.employee}</strong></Link></td>
              <td><Link to={to} tabIndex={-1}><ClockIcon size={15} />{row.shift}</Link></td>
              <td><Link to={to} tabIndex={-1}><ShopIcon size={15} className={styles.shopIcon} /><span title={row.shop}>{row.shop}</span></Link></td>
              <td><Link to={to} tabIndex={-1}>{row.hoursText}</Link></td>
              <td><Link to={to} tabIndex={-1}><span className={`${styles.status} ${styles[status]}`}>{status === "completed" && <CheckIcon size={12} />}{label}</span></Link></td>
            </tr>;
          })}</tbody>
        </table>
        {home.isLoading && <p role="status" className={styles.empty}>{t("Loading scheduled employees...")}</p>}
        {!home.isLoading && !home.isError && !todayRows.length && <p className={styles.empty}>{selectedShop === null
          ? t("No employees scheduled for this date.") : t("No employees scheduled for this shop and date.")}</p>}
      </div>
    </CardSection>
    <CardSection className={styles.card} headerClassName={styles.cardHeader} titleClassName={styles.cardTitle}
      icon={<span className={styles.headerIcon}><StatisticsIcon size={18} /></span>}
      title={<><h2>{t("Application Activity")}</h2><span className={styles.subtitle}>{t("Recent actions in the system")}</span></>}>
      <div className={styles.filterPanel}>
        <div className={styles.periodRow} role="group" aria-label={t("Period")}>
          <span className={styles.periodLabel}><ScheduleDetailsIcon size={16} />{t("Period")}</span>
          <div className={styles.periodControls}>
            <button type="button" className={styles.iconButton} aria-label={t("Previous month")} disabled={period === "0001-01"} onClick={() => moveActivityMonth(-1)}><ChevronLeftIcon size={16} /></button>
            <SearchableSelect value={period} options={activityPeriods} onChange={setPeriod}
              size="compact" searchEnabled={false} showSelectedHint={false} ariaLabel={t("Period")} placeholder={t("Period")} dropdownTitle={t("Period")} className={styles.activityPeriodSelect} />
            <button type="button" className={styles.iconButton} aria-label={t("Next month")} disabled={period === "9999-12"} onClick={() => moveActivityMonth(1)}><ChevronRightIcon size={16} /></button>
          </div>
          <div className={styles.periodRange}><span>{rangeLabel}</span></div>
        </div>
      <div role="tablist" aria-label={t("Activity filters")} className={styles.tabs}>
        {activityTabs.map(({ value, label, Icon }, index) => <button key={value} ref={element => { tabRefs.current[index] = element; }} type="button" role="tab" id={`home-tab-${value}`}
          className={value === "approvals" ? [styles.approvalTab, (requiresApprovalCount ?? 0) > 0 && styles.approvalTabPending].filter(Boolean).join(" ") : undefined}
          aria-selected={tab === value} aria-controls="home-activity-panel" tabIndex={tab === value ? 0 : -1} onClick={() => selectTab(value)} onKeyDown={event => {
          if (!["ArrowLeft", "ArrowRight", "Home", "End"].includes(event.key)) return;
          event.preventDefault();
          const last = activityTabs.length - 1;
          const next = event.key === "Home" ? 0 : event.key === "End" ? last
            : (index + (event.key === "ArrowRight" ? 1 : last)) % activityTabs.length;
          selectTab(activityTabs[next].value); tabRefs.current[next]?.focus();
        }}><Icon size={12} />{t(label)}<span className={value === "approvals" ? `${styles.tabCount} ${styles.approvalCount}` : styles.tabCount}>{(value === "all" ? counts?.allActivityCount
          : value === "approvals" ? requiresApprovalCount : counts?.categoryCounts?.[value]) ?? "—"}</span></button>)}
      </div>
      </div>
      <p className={styles.scope}>{tab === "approvals" ? t("All outstanding approvals, including earlier months") : t("Events recorded in {0}", periodLabel)}</p>
      {activity.isError && <ErrorBanner dismissible={false}>{getErrorMessage(activity.error, t("Could not load application activity."))}<button className={styles.retry} onClick={activity.refetch}>{t("Retry")}</button></ErrorBanner>}
      <div ref={feedRef} id="home-activity-panel" role="tabpanel" aria-labelledby={`home-tab-${tab}`} className={styles.feed}
        tabIndex={0} aria-busy={activity.isFetching || activity.isFetchingNextPage}
        data-overflow-top={overflow.top || undefined} data-overflow-bottom={overflow.bottom || undefined}>
        <div ref={feedContentRef} className={styles.feedContent}>
        {activity.isLoading && <p role="status" className={styles.empty}>{t("Loading activity...")}</p>}
        {!activity.isLoading && !activity.isError && !items.length && <p className={styles.empty}>{tab === "approvals" ? t("No outstanding approvals.") : t("No activity recorded in this month.")}</p>}
        {items.map(item => {
          const view = getHomeActivityPresentation(item, item.employeeId != null ? home.data?.employees.get(item.employeeId) : undefined);
          const route = getHomeActivityRoute(item);
          const Icon = view.iconKind === "swap" ? SwapOffersIcon : view.iconKind === "correction" ? NoteIcon
            : view.iconKind === "availability" ? AvailabilityIcon : ScheduleDetailsIcon;
          const content = <>
            <span className={`${styles.eventIcon} ${styles[view.tone]}`}><Icon size={18} /></span>
            <span className={styles.eventText}><strong>{view.title}</strong><span>{view.description}</span>{!route && <span>{t("This activity's resource is no longer available.")}</span>}</span>
            <span className={`${styles.status} ${styles[view.tone]}`}>{view.localizedStatus}</span>
            <time dateTime={item.occurredAtUtc} title={view.dateTimeLabel} aria-label={view.dateTimeLabel}>{view.time}</time>
          </>;
          return route ? <Link key={item.id} to={route} className={styles.event}>{content}</Link>
            : <div key={item.id} className={`${styles.event} ${styles.unavailableEvent}`} aria-disabled="true">{content}</div>;
        })}
        <div ref={sentinelRef} aria-hidden="true" />
        {activity.isFetchingNextPage && <p role="status" className={styles.loadingMore}>{t("Loading more activity...")}</p>}
        </div>
      </div>
      {Boolean(activity.nextPageError) && <ErrorBanner dismissible={false}>{t("Could not load more activity.")}<button className={styles.retry} onClick={() => void activity.fetchNextPage()}>{t("Retry")}</button></ErrorBanner>}
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
