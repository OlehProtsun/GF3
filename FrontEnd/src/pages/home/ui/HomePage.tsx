import { useCallback, useEffect, useLayoutEffect, useMemo, useRef, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { AvailabilityIcon, ContainerIcon, EyeIcon, InformationIcon, ScheduleDetailsIcon } from "@shared/ui/icons";
import { usePageScrollbarHidden } from "@shared/lib/usePageScrollbarHidden";
import { PageHeader } from "@shared/ui/PageHeader";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { CardSection } from "@shared/ui/sections/CardSection";
import { ContainerGraphMatrix } from "@entities/containers";
import { loadHomeDashboard, type HomeDashboardData } from "./homeDashboard";
import styles from "./HomePage.module.css";

const HOME_QUERY_KEY = ["home", "dashboard"] as const;
const CURRENT_TIME_FORMATTER = new Intl.DateTimeFormat("en-GB", {
  hour: "2-digit",
  minute: "2-digit",
  day: "2-digit",
  month: "2-digit",
  year: "numeric",
  hour12: false,
});

const EMPTY_HOME_DASHBOARD: HomeDashboardData = {
  monthSchedulesCount: 0,
  totalContainersCount: 0,
  todayAssignmentsCount: 0,
  activeShopsCount: 0,
  currentMonthContainerName: "No active containers",
  currentMonthLabel: "",
  currentMonthScheduleNames: [],
  currentMonthShopNames: [],
  currentMonthEmployeeNames: [],
  currentMonthTotalEmployees: 0,
  currentMonthTotalSchedules: 0,
  currentMonthTotalHoursText: "0h 00m",
  currentMonthTotalShops: 0,
  overallTotalEmployees: 0,
  overallTotalContainers: 0,
  overallTotalShops: 0,
  statusText: "Loading home data...",
  todayRows: [],
  activeSchedules: [],
};

type ActiveScheduleSelectionState = {
  graphId: number | null;
  keys: string[];
};

const EMPTY_ACTIVE_SCHEDULE_SELECTION = {
  graphId: null,
  keys: [],
} satisfies ActiveScheduleSelectionState;

function arraysEqual(left: string[], right: string[]) {
  return left.length === right.length && left.every((value, index) => value === right[index]);
}

function normalizeActiveScheduleSelection(
  selection: ActiveScheduleSelectionState,
  activeScheduleIds: number[],
): ActiveScheduleSelectionState {
  const activeScheduleIdSet = new Set(activeScheduleIds);

  return selection.graphId !== null && !activeScheduleIdSet.has(selection.graphId)
    ? EMPTY_ACTIVE_SCHEDULE_SELECTION
    : selection;
}

function formatCurrentTimeText() {
  return CURRENT_TIME_FORMATTER.format(new Date()).replace(",", "");
}

function useCurrentTimeText() {
  const [currentTimeText, setCurrentTimeText] = useState(formatCurrentTimeText);

  useEffect(() => {
    let timeoutId = 0;

    const scheduleNextUpdate = () => {
      const now = new Date();
      const millisecondsUntilNextMinute =
        (59 - now.getSeconds()) * 1_000 + (1_000 - now.getMilliseconds());

      timeoutId = window.setTimeout(() => {
        setCurrentTimeText(formatCurrentTimeText());
        scheduleNextUpdate();
      }, Math.max(1_000, millisecondsUntilNextMinute));
    };

    scheduleNextUpdate();

    return () => {
      window.clearTimeout(timeoutId);
    };
  }, []);

  return currentTimeText;
}

function useDesktopMatchedHeight() {
  const targetRef = useRef<HTMLDivElement | null>(null);
  const [matchedHeight, setMatchedHeight] = useState<number | null>(null);

  useLayoutEffect(() => {
    if (typeof window === "undefined") {
      return;
    }

    const targetElement = targetRef.current;
    if (!targetElement) {
      return;
    }

    const mediaQuery = window.matchMedia("(min-width: 1241px)");

    const updateMatchedHeight = () => {
      if (!mediaQuery.matches) {
        setMatchedHeight(null);
        return;
      }

      const nextHeight = Math.ceil(targetElement.getBoundingClientRect().height);
      setMatchedHeight(previousHeight => (previousHeight !== nextHeight ? nextHeight : previousHeight));
    };

    updateMatchedHeight();

    const resizeObserver = new ResizeObserver(updateMatchedHeight);
    resizeObserver.observe(targetElement);

    const handleMediaQueryChange = () => {
      updateMatchedHeight();
    };

    mediaQuery.addEventListener("change", handleMediaQueryChange);
    window.addEventListener("resize", updateMatchedHeight);

    return () => {
      resizeObserver.disconnect();
      mediaQuery.removeEventListener("change", handleMediaQueryChange);
      window.removeEventListener("resize", updateMatchedHeight);
    };
  }, []);

  return { targetRef, matchedHeight };
}

function HomePillList({
  title,
  items,
  emptyLabel,
}: {
  title: string;
  items: string[];
  emptyLabel: string;
}) {
  return (
    <div className={styles.pillSection}>
      <h3 className={styles.pillSectionTitle}>{title}</h3>
      <div className={styles.pillWrap}>
        {items.length > 0 ? (
          items.map(item => (
            <span key={item} className={styles.pill}>
              {item}
            </span>
          ))
        ) : (
          <span className={styles.emptyInline}>{emptyLabel}</span>
        )}
      </div>
    </div>
  );
}

export function HomePage() {
  const location = useLocation();
  const navigate = useNavigate();
  const currentTimeText = useCurrentTimeText();
  const { targetRef: sideColumnRef, matchedHeight: todayCardHeight } = useDesktopMatchedHeight();
  const [activeScheduleSelection, setActiveScheduleSelection] = useState<ActiveScheduleSelectionState>(
    EMPTY_ACTIVE_SCHEDULE_SELECTION,
  );
  usePageScrollbarHidden(true);
  const homeQuery = useQuery({
    queryKey: [...HOME_QUERY_KEY, location.key],
    cancelOnUnmount: true,
    staleTime: 60_000,
    queryFn: ({ signal }) => loadHomeDashboard(signal),
  });

  const homeData = homeQuery.data ?? EMPTY_HOME_DASHBOARD;
  const isInitialLoading = homeQuery.isLoading && !homeQuery.data;
  const activeScheduleIds = useMemo(
    () => homeData.activeSchedules.map(schedule => schedule.graph.id),
    [homeData.activeSchedules],
  );
  const normalizedActiveScheduleSelection = useMemo(
    () => normalizeActiveScheduleSelection(activeScheduleSelection, activeScheduleIds),
    [activeScheduleIds, activeScheduleSelection],
  );

  const handleScheduleSelectionChange = useCallback((graphId: number, keys: string[]) => {
    setActiveScheduleSelection(currentSelection => {
      const nextSelection = normalizeActiveScheduleSelection(currentSelection, activeScheduleIds);
      const currentKeys = nextSelection.graphId === graphId ? nextSelection.keys : [];

      if (nextSelection.graphId === graphId && arraysEqual(currentKeys, keys)) {
        return nextSelection;
      }

      if (keys.length === 0) {
        return nextSelection.graphId === graphId
          ? EMPTY_ACTIVE_SCHEDULE_SELECTION
          : nextSelection;
      }

      return {
        graphId,
        keys,
      };
    });
  }, [activeScheduleIds]);

  return (
    <div className={styles.page}>
      <PageHeader
        title="Home"
        subtitle="Overview of current-month schedules and assignments."
        rightSlot={(
          <div className={styles.clockPanel}>
            <span className={styles.clockLabel}>Current time</span>
            <strong className={styles.clockValue}>{currentTimeText}</strong>
          </div>
        )}
      />

      {homeQuery.error ? (
        <ErrorBanner className={styles.banner}>
          Could not load the home dashboard.
        </ErrorBanner>
      ) : null}

      <div className={styles.topGrid}>
        <CardSection
          className={styles.todayCard}
          style={todayCardHeight ? { height: `${todayCardHeight}px` } : undefined}
          title="Who Works Today?"
          icon={<AvailabilityIcon size={18} />}
          headerRightSlot={
            !isInitialLoading ? (
              <span className={styles.sectionBadge}>{`${homeData.todayRows.length} active rows`}</span>
            ) : null
          }
        >
          {isInitialLoading ? (
            <div className={styles.stateBlock}>Loading current-day assignments...</div>
          ) : homeData.todayRows.length === 0 ? (
            <div className={styles.stateBlock}>No assignments for today in the current month.</div>
          ) : (
            <div className={styles.tableShell}>
              <div className={styles.tableScroll}>
                <table className={styles.todayTable}>
                  <thead>
                    <tr>
                      <th>Date</th>
                      <th>Employee</th>
                      <th>Shift</th>
                      <th>Shop</th>
                    </tr>
                  </thead>
                  <tbody>
                    {homeData.todayRows.map(row => (
                      <tr
                        key={row.id}
                        className={styles.clickableRow}
                        onClick={() => navigate(row.route)}
                        title="Open schedule"
                      >
                        <td>{row.dateLabel}</td>
                        <td>{row.employee}</td>
                        <td>{row.shift}</td>
                        <td>{row.shop}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </CardSection>

        <div ref={sideColumnRef} className={styles.sideColumn}>
          <CardSection
            className={styles.monthCard}
            title="This Month"
            icon={<ScheduleDetailsIcon size={18} />}
          >
            {isInitialLoading ? (
              <div className={styles.stateBlock}>Loading month overview...</div>
            ) : (
              <div className={styles.monthContent}>
                <div className={styles.summaryHeader}>
                  <div className={styles.summaryCard}>
                    <span className={styles.summaryLabel}>Date</span>
                    <strong className={styles.summaryValue}>{homeData.currentMonthLabel || "Current month"}</strong>
                  </div>
                  <div className={styles.summaryCard}>
                    <span className={styles.summaryLabel}>Container focus</span>
                    <strong className={styles.summaryValue}>{homeData.currentMonthContainerName}</strong>
                  </div>
                </div>

                <div className={styles.totalGrid}>
                  <div className={styles.totalCard}>
                    <span className={styles.totalLabel}>Total hours</span>
                    <strong className={styles.totalValue}>{homeData.currentMonthTotalHoursText}</strong>
                  </div>
                  <div className={styles.totalCard}>
                    <span className={styles.totalLabel}>Employees</span>
                    <strong className={styles.totalValue}>{homeData.currentMonthTotalEmployees}</strong>
                  </div>
                  <div className={styles.totalCard}>
                    <span className={styles.totalLabel}>Schedules</span>
                    <strong className={styles.totalValue}>{homeData.currentMonthTotalSchedules}</strong>
                  </div>
                  <div className={styles.totalCard}>
                    <span className={styles.totalLabel}>Shops</span>
                    <strong className={styles.totalValue}>{homeData.currentMonthTotalShops}</strong>
                  </div>
                </div>

                <HomePillList
                  title="Schedules in this month"
                  items={homeData.currentMonthScheduleNames}
                  emptyLabel="No schedules yet."
                />
                <HomePillList
                  title="Shops in this month"
                  items={homeData.currentMonthShopNames}
                  emptyLabel="No shops connected yet."
                />
                <HomePillList
                  title="Employees in this month"
                  items={homeData.currentMonthEmployeeNames}
                  emptyLabel="No assigned employees yet."
                />
              </div>
            )}
          </CardSection>

          <CardSection
            className={styles.overallCard}
            title="Overall"
            icon={<InformationIcon size={18} />}
          >
            {isInitialLoading ? (
              <div className={styles.stateBlock}>Loading overall snapshot...</div>
            ) : (
              <div className={styles.overallGrid}>
                <div className={styles.overallMetric}>
                  <span className={styles.totalLabel}>Employees</span>
                  <strong className={styles.overallValue}>{homeData.overallTotalEmployees}</strong>
                </div>
                <div className={styles.overallMetric}>
                  <span className={styles.totalLabel}>Containers</span>
                  <strong className={styles.overallValue}>{homeData.overallTotalContainers}</strong>
                </div>
                <div className={styles.overallMetric}>
                  <span className={styles.totalLabel}>Shops</span>
                  <strong className={styles.overallValue}>{homeData.overallTotalShops}</strong>
                </div>
              </div>
            )}
          </CardSection>
        </div>
      </div>

      <CardSection
        className={styles.activeSchedulesCard}
        title="Active Schedules This Month"
        icon={<ScheduleDetailsIcon size={18} />}
        headerRightSlot={
          !isInitialLoading ? (
            <span className={styles.sectionBadge}>{`${homeData.activeSchedules.length} previews`}</span>
          ) : null
        }
      >
        {isInitialLoading ? (
          <div className={styles.stateBlock}>Building active schedule previews...</div>
        ) : homeData.activeSchedules.length === 0 ? (
          <div className={styles.stateBlock}>No active schedules found for the current month.</div>
        ) : (
          <div className={styles.scheduleGrid}>
            {homeData.activeSchedules.map(schedule => (
              <div key={schedule.graph.id} className={styles.scheduleCard}>
                <ContainerGraphMatrix
                  className={styles.scheduleMatrix}
                  graph={schedule.graph}
                  columns={schedule.columns}
                  cellMap={schedule.cellMap}
                  styleMap={schedule.styleMap}
                  dayConflictMap={schedule.dayConflictMap}
                  title={schedule.graph.name}
                  readOnly
                  enableSelectionWhenReadOnly
                  compactSize
                  selectedCellKeys={
                    normalizedActiveScheduleSelection.graphId === schedule.graph.id
                      ? normalizedActiveScheduleSelection.keys
                      : []
                  }
                  onSelectedCellKeysChange={(keys) => handleScheduleSelectionChange(schedule.graph.id, keys)}
                  headerRightSlot={(
                    <div className={styles.matrixHeaderActions}>
                      <span className={styles.matrixBadge}>{`Employees: ${schedule.totals.totalEmployees}`}</span>
                      <span className={styles.matrixBadge}>{`Hours: ${schedule.totals.totalHoursText}`}</span>
                      <button
                        type="button"
                        className={styles.matrixIconButton}
                        onClick={() => navigate(schedule.route)}
                        aria-label="Open schedule"
                        title="Open schedule"
                      >
                        <EyeIcon size={16} />
                      </button>
                      <button
                        type="button"
                        className={styles.matrixIconButton}
                        onClick={() => {
                          if (!schedule.container) {
                            return;
                          }

                          navigate(`/container?openContainerId=${schedule.container.id}`);
                        }}
                        aria-label="Open container"
                        title="Open container"
                        disabled={!schedule.container}
                      >
                        <ContainerIcon size={16} />
                      </button>
                    </div>
                  )}
                />
              </div>
            ))}
          </div>
        )}
      </CardSection>
    </div>
  );
}
