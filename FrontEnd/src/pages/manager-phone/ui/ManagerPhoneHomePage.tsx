import { useEffect, useId, useLayoutEffect, useRef, useState, type ComponentPropsWithoutRef } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { formatHoursMinutes, getSlotDurationMinutes } from "@entities/containers";
import { formatHomeDate, moveSelectedDay, parseHomeDay, warsawToday } from "@pages/home/ui/homeActivity";
import {
  getHomeDashboardQuery, getHomeEmployeeInitials, getHomeScheduleRows, getHomeShiftStatusLabel,
} from "@pages/home/ui/homeSchedule";
import { getErrorMessage } from "@shared/api/httpClient";
import { dateTimeFormat, t } from "@shared/i18n";
import { useLanguageRevision } from "@shared/i18n/useLanguageRevision";
import { CheckIcon, ChevronLeftIcon, ChevronRightIcon, ClockIcon, ScheduleDetailsIcon, ShopIcon } from "@shared/ui/icons";
import { ManagerPhonePage } from "./ManagerPhonePage";
import styles from "./ManagerPhoneHomePage.module.css";

function ScrollFade({ axis, children, ...props }: ComponentPropsWithoutRef<"div"> & { axis: "x" | "y" }) {
  const frame = useRef<HTMLDivElement>(null);
  const viewport = useRef<HTMLDivElement>(null);
  const content = useRef<HTMLDivElement>(null);

  useLayoutEffect(() => {
    const wrapper = frame.current;
    const scroller = viewport.current;
    const track = content.current;
    if (!wrapper || !scroller || !track) return;
    const update = () => {
      const offset = axis === "x" ? scroller.scrollLeft : scroller.scrollTop;
      const remaining = axis === "x"
        ? scroller.scrollWidth - scroller.clientWidth
        : scroller.scrollHeight - scroller.clientHeight;
      wrapper.toggleAttribute("data-fade-before", remaining > 1 && offset > 1);
      wrapper.toggleAttribute("data-fade-after", remaining > 1 && remaining - offset > 1);
    };
    update();
    scroller.addEventListener("scroll", update, { passive: true });
    const observer = typeof ResizeObserver === "undefined" ? null : new ResizeObserver(update);
    observer?.observe(scroller);
    observer?.observe(track);
    window.addEventListener("resize", update);
    return () => {
      scroller.removeEventListener("scroll", update);
      window.removeEventListener("resize", update);
      observer?.disconnect();
    };
  }, [axis]);

  return (
    <div ref={frame} className={styles.scrollFade} data-axis={axis}>
      <div ref={viewport} {...props}>
        <div ref={content} className={axis === "x" ? styles.shopTrack : undefined}>{children}</div>
      </div>
    </div>
  );
}

export function ManagerPhoneHomePage() {
  useLanguageRevision();
  const titleId = useId();
  const [now, setNow] = useState(() => new Date());
  const [selectedDay, setSelectedDay] = useState(() => warsawToday());
  const [selectedShop, setSelectedShop] = useState<number | null>(null);
  const homeQuery = getHomeDashboardQuery(selectedDay);
  const home = useQuery(homeQuery);
  const client = useQueryClient();
  const retry = () => client.invalidateQueries({ queryKey: homeQuery.queryKey });
  const loading = home.isLoading;
  const { todayRows } = getHomeScheduleRows(loading || home.isError ? undefined : home.data, selectedDay, selectedShop, now);
  const today = warsawToday(now);
  const dateLabel = formatHomeDate(selectedDay, true);
  const compactDate = dateTimeFormat("en-GB", {
    day: "2-digit", month: "2-digit", timeZone: "UTC",
  }).format(parseHomeDay(selectedDay));
  const shops = [{ id: null, name: t("All") }, ...(home.data?.shops ?? [])];

  useEffect(() => {
    const updateClock = () => setNow(new Date());
    const timer = window.setInterval(updateClock, 30_000);
    window.addEventListener("focus", updateClock);
    document.addEventListener("visibilitychange", updateClock);
    return () => {
      window.clearInterval(timer);
      window.removeEventListener("focus", updateClock);
      document.removeEventListener("visibilitychange", updateClock);
    };
  }, []);

  return (
    <ManagerPhonePage title={t("Home")} fill>
      <section className={styles.section} aria-labelledby={titleId}>
        <header className={styles.header}>
          <span className={styles.headerIcon}><ScheduleDetailsIcon size={21} /></span>
          <div>
            <h2 id={titleId}>{selectedDay === today ? t("Who Works Today?") : t("Who Works on {0}?", dateLabel)}</h2>
            <p>{t("Employees scheduled for {0}", dateLabel)}</p>
          </div>
        </header>
        <div className={styles.filters}>
          <div className={styles.dateFilter} role="group" aria-label={t("Date")}>
            <div className={styles.filterHeading}>
              <span className={styles.filterLabel}><ScheduleDetailsIcon size={15} />{t("Date")}</span>
              <button type="button" className={`${styles.today} ${styles.inlineToday}`} onClick={() => {
                const current = new Date();
                setNow(current);
                setSelectedDay(warsawToday(current));
              }}><ScheduleDetailsIcon size={14} />{t("Today")}</button>
            </div>
            <div className={styles.dateControls}>
              <button type="button" className={styles.dayStep} aria-label={t("Previous day")} disabled={selectedDay === "0001-01-01"}
                onClick={() => setSelectedDay(day => moveSelectedDay(day, -1))}><ChevronLeftIcon size={16} /></button>
              <label className={styles.datePill} title={dateLabel}>
                <span aria-hidden="true">{compactDate}</span>
                <ChevronRightIcon size={11} className={styles.dateChevron} />
                <input type="date" aria-label={t("Select date")} value={selectedDay} min="0001-01-01" max="9999-12-31"
                  onChange={({ currentTarget }) => { if (currentTarget.value && currentTarget.validity.valid) setSelectedDay(currentTarget.value); }} />
              </label>
              <button type="button" className={styles.dayStep} aria-label={t("Next day")} disabled={selectedDay === "9999-12-31"}
                onClick={() => setSelectedDay(day => moveSelectedDay(day, 1))}><ChevronRightIcon size={16} /></button>
            </div>
          </div>
          <div className={styles.shopFilter}>
            <div className={styles.filterHeading}><span className={styles.filterLabel}><ShopIcon size={15} className={styles.shopIcon} />{t("Shop")}</span></div>
            <ScrollFade axis="x" className={styles.shopChips} role="group" aria-label={t("Filter by shop")}>
              {shops.map(shop => (
                <button type="button" key={shop.id ?? "all"} aria-pressed={selectedShop === shop.id} title={shop.name}
                  onClick={event => {
                    setSelectedShop(shop.id);
                    event.currentTarget.scrollIntoView?.({ block: "nearest", inline: "nearest" });
                  }}>{shop.name}</button>
              ))}
            </ScrollFade>
          </div>
        </div>
        {home.isError && (
          <div className={styles.message} role="alert">
            <p>{getErrorMessage(home.error, t("Could not load scheduled employees."))}</p>
            <button type="button" className={styles.today} onClick={retry}>{t("Retry")}</button>
          </div>
        )}
        {!home.isError && !loading && home.data?.hasPartialData && (
          <div className={styles.message} role="status">
            <p>{t("Some schedules could not be loaded. Results may be incomplete.")}</p>
            <button type="button" className={styles.today} onClick={retry}>{t("Retry")}</button>
          </div>
        )}
        {!home.isError && (
          <ScrollFade axis="y" key={`${selectedDay}:${selectedShop}`} className={styles.list} tabIndex={0} role="region"
            aria-label={t("Scheduled employees")} aria-busy={loading}>
            {loading ? (
              <>
                <p className={styles.srOnly} role="status">{t("Loading scheduled employees...")}</p>
                {[1, 2, 3].map(item => <div className={styles.skeleton} key={item} aria-hidden="true"><span /><span /></div>)}
              </>
            ) : todayRows.length ? (
              <ul className={styles.rows}>
                {todayRows.map(({ row, status }) => (
                  <li className={styles.employee} key={row.id}>
                    <span className={styles.avatar} data-tone={row.employeeId % 3} aria-hidden="true">{getHomeEmployeeInitials(row.employee)}</span>
                    <div className={styles.employeeContent}>
                      <div className={styles.employeeHead}>
                        <strong title={row.employee}>{row.employee}</strong>
                        <span className={styles.status} data-status={status}>
                          {status === "completed" && <CheckIcon size={12} />}{getHomeShiftStatusLabel(status)}
                        </span>
                      </div>
                      <div className={styles.metadata}>
                        <div className={styles.metadataGroups}>
                          <span><ClockIcon size={15} />{row.shift}</span>
                          <span className={styles.shopMetadata}><ShopIcon size={15} className={styles.shopIcon} /><span>{row.shop}</span></span>
                          <span>{formatHoursMinutes(getSlotDurationMinutes(row))}</span>
                        </div>
                      </div>
                    </div>
                  </li>
                ))}
              </ul>
            ) : (
              <p className={styles.empty}>{selectedShop === null
                ? t("No employees scheduled for this date.") : t("No employees scheduled for this shop and date.")}</p>
            )}
          </ScrollFade>
        )}
      </section>
    </ManagerPhonePage>
  );
}
