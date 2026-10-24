import { dateTimeFormat, t } from "@shared/i18n";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ArrowIcon, AvailabilityIcon, ContainerIcon, EyeIcon, ScheduleDetailsIcon } from "@shared/ui/icons";
import { queryKeys } from "@shared/api/queryKeys";
import { usePageScrollbarHidden } from "@shared/lib/usePageScrollbarHidden";
import { PageHeader } from "@shared/ui/PageHeader";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { CardSection } from "@shared/ui/sections/CardSection";
import {
  ContainerGraphMatrix,
  ContainerGraphRelatedHintDialog,
  getGraphCellKey,
} from "@entities/containers";
import { loadHomeDashboard, type HomeDashboardData } from "./homeDashboard";
import styles from "./HomePage.module.css";

const CURRENT_TIME_FORMATTER = dateTimeFormat("en-GB", {
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
  get currentMonthContainerName() { return t("No active containers"); },
  currentMonthLabel: "",
  currentMonthScheduleNames: [],
  currentMonthShopNames: [],
  currentMonthEmployeeNames: [],
  currentMonthTotalEmployees: 0,
  currentMonthTotalSchedules: 0,
  currentMonthTotalHoursText: "0h 00m",
  currentMonthTotalShops: 0,
  todayActiveEmployeesCount: 0,
  get statusText() { return t("Loading home data..."); },
  todayRows: [],
  activeSchedules: [],
};

type ActiveScheduleSelectionState = {
  graphId: number | null;
  keys: string[];
};

type ActiveRelatedHintSelection = {
  graphId: number;
  cellKey: string;
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

function formatGraphHintDateLabel(year: number, month: number, dayOfMonth: number) {
  return dateTimeFormat("en-GB", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    timeZone: "UTC",
  }).format(new Date(Date.UTC(year, month - 1, dayOfMonth)));
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

function ManagerPcHomePage() {
  const location = useLocation();
  const navigate = useNavigate();
  const currentTimeText = useCurrentTimeText();
  const activeSchedulesSectionRef = useRef<HTMLElement | null>(null);
  const scheduleCardRefs = useRef(new Map<number, HTMLDivElement>());
  const scheduleScrollTimeoutRef = useRef(0);
  const scheduleHighlightTimeoutRef = useRef(0);
  const [isMonthDetailsExpanded, setIsMonthDetailsExpanded] = useState(false);
  const [expandedScheduleIds, setExpandedScheduleIds] = useState<Set<number>>(() => new Set());
  const [focusedScheduleId, setFocusedScheduleId] = useState<number | null>(null);
  const [activeRelatedHintSelection, setActiveRelatedHintSelection] =
    useState<ActiveRelatedHintSelection | null>(null);
  const [activeScheduleSelection, setActiveScheduleSelection] = useState<ActiveScheduleSelectionState>(
    EMPTY_ACTIVE_SCHEDULE_SELECTION,
  );
  usePageScrollbarHidden(true);
  const homeQuery = useQuery({
    queryKey: [...queryKeys.home.dashboard(), location.key],
    staleTime: 60_000,
    // Keep the dashboard bootstrap request alive through StrictMode remounts after sign-in.
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
  const activeRelatedHintSchedule = activeRelatedHintSelection
    ? homeData.activeSchedules.find(schedule => schedule.graph.id === activeRelatedHintSelection.graphId) ?? null
    : null;
  const activeRelatedHint = activeRelatedHintSchedule && activeRelatedHintSelection
    ? activeRelatedHintSchedule.visualHintDetailMap[activeRelatedHintSelection.cellKey] ?? null
    : null;
  const activeRelatedHintEmployeeName = activeRelatedHint
    ? activeRelatedHintSchedule?.columns.find(column => column.employeeId === activeRelatedHint.employeeId)?.label
      ?? t("Employee {0}", activeRelatedHint.employeeId)
    : "";
  const activeRelatedHintDayLabel = activeRelatedHint && activeRelatedHintSchedule
    ? formatGraphHintDateLabel(
      activeRelatedHintSchedule.graph.year,
      activeRelatedHintSchedule.graph.month,
      activeRelatedHint.dayOfMonth,
    )
    : "";
  const allSchedulesExpanded =
    activeScheduleIds.length > 0 && activeScheduleIds.every(graphId => expandedScheduleIds.has(graphId));
  const hasExpandedSchedules = activeScheduleIds.some(graphId => expandedScheduleIds.has(graphId));

  useEffect(() => () => {
    window.clearTimeout(scheduleScrollTimeoutRef.current);
    window.clearTimeout(scheduleHighlightTimeoutRef.current);
  }, []);

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

  const handleScheduleToggle = useCallback((graphId: number) => {
    setExpandedScheduleIds(current => {
      const next = new Set(current);

      if (next.has(graphId)) {
        next.delete(graphId);
      } else {
        next.add(graphId);
      }

      return next;
    });
  }, []);

  const handleTodayEmployeeClick = useCallback((graphId: number) => {
    if (!activeScheduleIds.includes(graphId)) {
      return;
    }

    setExpandedScheduleIds(current => new Set(current).add(graphId));
    setFocusedScheduleId(graphId);

    const prefersReducedMotion = window.matchMedia?.("(prefers-reduced-motion: reduce)").matches ?? false;
    const behavior: ScrollBehavior = prefersReducedMotion ? "auto" : "smooth";
    activeSchedulesSectionRef.current?.scrollIntoView({ behavior, block: "start" });

    window.clearTimeout(scheduleScrollTimeoutRef.current);
    scheduleScrollTimeoutRef.current = window.setTimeout(() => {
      scheduleCardRefs.current.get(graphId)?.scrollIntoView({ behavior, block: "start" });
    }, prefersReducedMotion ? 0 : 220);

    window.clearTimeout(scheduleHighlightTimeoutRef.current);
    scheduleHighlightTimeoutRef.current = window.setTimeout(() => {
      setFocusedScheduleId(current => current === graphId ? null : current);
    }, prefersReducedMotion ? 400 : 1_600);
  }, [activeScheduleIds]);

  return (
    <div className={styles.page}>
      <PageHeader
        title={t("Home")}
        subtitle={t("Overview of current-month schedules and assignments.")}
        rightSlot={(
          <div className={styles.clockPanel}>
            <span className={styles.clockLabel}>{t("Current time")}</span>
            <strong className={styles.clockValue}>{currentTimeText}</strong>
          </div>
        )}
      />

      {homeQuery.error ? (
        <ErrorBanner className={styles.banner}>
          {t("Could not load the home dashboard.")}</ErrorBanner>
      ) : null}

      <div className={styles.topGrid}>
        <CardSection
          className={styles.todayCard}
          title={t("Who Works Today?")}
          icon={<AvailabilityIcon size={18} />}
          headerRightSlot={
            !isInitialLoading ? (
              <span className={styles.sectionBadge}>
                {t("{0} active employees", homeData.todayActiveEmployeesCount)}
              </span>
            ) : null
          }
        >
          {isInitialLoading ? (
            <div className={styles.stateBlock}>{t("Loading current-day assignments...")}</div>
          ) : homeData.todayRows.length === 0 ? (
            <div className={styles.stateBlock}>{t("No assignments for today in the current month.")}</div>
          ) : (
            <div className={styles.tableShell}>
              <div className={styles.tableScroll}>
                <table className={styles.todayTable}>
                  <thead>
                    <tr>
                      <th>{t("Date")}</th>
                      <th>{t("Employee")}</th>
                      <th>{t("Shift")}</th>
                      <th>{t("Shop")}</th>
                    </tr>
                  </thead>
                  <tbody>
                    {homeData.todayRows.map(row => (
                      <tr key={row.id}>
                        <td>{row.dateLabel}</td>
                        <td>
                          <button
                            type="button"
                            className={styles.employeeFocusButton}
                            onClick={() => handleTodayEmployeeClick(row.graphId)}
                            aria-label={t("Show {0}'s schedule", row.employee)}
                            title={t("Show schedule on this page")}
                          >
                            {row.employee}
                          </button>
                        </td>
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

        <div className={styles.sideColumn}>
          <CardSection
            className={styles.monthCard}
            title={t("This Month")}
            icon={<ScheduleDetailsIcon size={18} />}
            headerRightSlot={(
              <button
                type="button"
                className={`${styles.matrixIconButton} ${styles.collapseButton}`}
                aria-label={isMonthDetailsExpanded ? t("Collapse this month details") : t("Expand this month details")}
                aria-expanded={isMonthDetailsExpanded}
                aria-controls="home-month-details"
                title={isMonthDetailsExpanded ? t("Collapse details") : t("Expand details")}
                onClick={() => setIsMonthDetailsExpanded(current => !current)}
              >
                <ArrowIcon size={13} />
              </button>
            )}
          >
            <div className={styles.monthContent}>
              <div className={styles.summaryHeader}>
                <div className={styles.summaryCard}>
                  <span className={styles.summaryLabel}>{t("Date")}</span>
                  <strong className={styles.summaryValue}>
                    {isInitialLoading ? t("Loading...") : homeData.currentMonthLabel || t("Current month")}
                  </strong>
                </div>
                <div className={styles.summaryCard}>
                  <span className={styles.summaryLabel}>{t("Container focus")}</span>
                  <strong className={styles.summaryValue}>
                    {isInitialLoading ? t("Loading...") : homeData.currentMonthContainerName}
                  </strong>
                </div>
              </div>

              <div
                id="home-month-details"
                className={[
                  styles.collapsibleRegion,
                  isMonthDetailsExpanded ? styles.collapsibleRegionExpanded : "",
                ].filter(Boolean).join(" ")}
                aria-hidden={!isMonthDetailsExpanded}
                inert={isMonthDetailsExpanded ? undefined : true}
              >
                <div className={styles.collapsibleInner}>
                  {isInitialLoading ? (
                    <div className={styles.compactStateBlock}>{t("Loading month overview...")}</div>
                  ) : (
                    <div className={styles.monthDetails}>
                      <div className={styles.totalGrid}>
                        <div className={styles.totalCard}>
                          <span className={styles.totalLabel}>{t("Total hours")}</span>
                          <strong className={styles.totalValue}>{homeData.currentMonthTotalHoursText}</strong>
                        </div>
                        <div className={styles.totalCard}>
                          <span className={styles.totalLabel}>{t("Employees")}</span>
                          <strong className={styles.totalValue}>{homeData.currentMonthTotalEmployees}</strong>
                        </div>
                        <div className={styles.totalCard}>
                          <span className={styles.totalLabel}>{t("Schedules")}</span>
                          <strong className={styles.totalValue}>{homeData.currentMonthTotalSchedules}</strong>
                        </div>
                        <div className={styles.totalCard}>
                          <span className={styles.totalLabel}>{t("Shops")}</span>
                          <strong className={styles.totalValue}>{homeData.currentMonthTotalShops}</strong>
                        </div>
                      </div>

                      <HomePillList
                        title={t("Schedules in this month")}
                        items={homeData.currentMonthScheduleNames}
                        emptyLabel={t("No schedules yet.")}
                      />
                      <HomePillList
                        title={t("Shops in this month")}
                        items={homeData.currentMonthShopNames}
                        emptyLabel={t("No shops connected yet.")}
                      />
                      <HomePillList
                        title={t("Employees in this month")}
                        items={homeData.currentMonthEmployeeNames}
                        emptyLabel={t("No assigned employees yet.")}
                      />
                    </div>
                  )}
                </div>
              </div>
            </div>
          </CardSection>
        </div>
      </div>

      <CardSection
        elementRef={activeSchedulesSectionRef}
        className={styles.activeSchedulesCard}
        headerClassName={styles.activeSchedulesHeader}
        headerRightClassName={styles.activeSchedulesHeaderRight}
        title={t("Active Schedules This Month")}
        icon={<ScheduleDetailsIcon size={18} />}
        headerRightSlot={
          !isInitialLoading ? (
            <div className={styles.schedulesHeaderActions}>
              <span className={styles.sectionBadge}>{t("{0} previews", homeData.activeSchedules.length)}</span>
              {homeData.activeSchedules.length > 0 ? (
                <div className={styles.bulkControls} role="group" aria-label={t("Schedule preview controls")}>
                  <button
                    type="button"
                    className={styles.bulkButton}
                    onClick={() => setExpandedScheduleIds(new Set(activeScheduleIds))}
                    disabled={allSchedulesExpanded}
                  >
                    {t("Expand all")}</button>
                  <button
                    type="button"
                    className={styles.bulkButton}
                    onClick={() => setExpandedScheduleIds(new Set())}
                    disabled={!hasExpandedSchedules}
                  >
                    {t("Collapse all")}</button>
                </div>
              ) : null}
            </div>
          ) : null
        }
      >
        {isInitialLoading ? (
          <div className={styles.stateBlock}>{t("Building active schedule previews...")}</div>
        ) : homeData.activeSchedules.length === 0 ? (
          <div className={styles.stateBlock}>{t("No active schedules found for the current month.")}</div>
        ) : (
          <div className={styles.scheduleGrid}>
            {homeData.activeSchedules.map(schedule => {
              const isExpanded = expandedScheduleIds.has(schedule.graph.id);
              const contentId = `home-schedule-${schedule.graph.id}-content`;

              return (
                <div
                  key={schedule.graph.id}
                  ref={element => {
                    if (element) {
                      scheduleCardRefs.current.set(schedule.graph.id, element);
                    } else {
                      scheduleCardRefs.current.delete(schedule.graph.id);
                    }
                  }}
                  className={[
                    styles.scheduleCard,
                    focusedScheduleId === schedule.graph.id ? styles.scheduleCardFocused : "",
                  ].filter(Boolean).join(" ")}
                >
                  <div className={styles.scheduleCardHeader}>
                    <div className={styles.scheduleIdentity}>
                      <strong className={styles.scheduleTitle}>{schedule.graph.name}</strong>
                      <span className={styles.scheduleMeta}>
                        {[schedule.monthLabel, schedule.shop?.name, schedule.container?.name].filter(Boolean).join(" / ")}
                      </span>
                    </div>

                    <div className={styles.matrixHeaderActions}>
                      <span className={styles.matrixBadge}>{t("Employees: {0}", schedule.totals.totalEmployees)}</span>
                      <span className={styles.matrixBadge}>{t("Hours: {0}", schedule.totals.totalHoursText)}</span>
                      <button
                        type="button"
                        className={styles.matrixIconButton}
                        onClick={() => navigate(schedule.route)}
                        aria-label={t("Open schedule")}
                        title={t("Open schedule")}
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
                        aria-label={t("Open container")}
                        title={t("Open container")}
                        disabled={!schedule.container}
                      >
                        <ContainerIcon size={16} />
                      </button>
                      <button
                        type="button"
                        className={`${styles.matrixIconButton} ${styles.collapseButton}`}
                        onClick={() => handleScheduleToggle(schedule.graph.id)}
                        aria-label={isExpanded ? t("Collapse {0}", schedule.graph.name) : t("Expand {0}", schedule.graph.name)}
                        aria-expanded={isExpanded}
                        aria-controls={contentId}
                        title={isExpanded ? t("Collapse schedule") : t("Expand schedule")}
                      >
                        <ArrowIcon size={13} />
                      </button>
                    </div>
                  </div>

                  <div
                    id={contentId}
                    className={[
                      styles.collapsibleRegion,
                      styles.scheduleBody,
                      isExpanded ? styles.collapsibleRegionExpanded : "",
                    ].filter(Boolean).join(" ")}
                    aria-hidden={!isExpanded}
                    inert={isExpanded ? undefined : true}
                  >
                    <div className={styles.collapsibleInner}>
                      <ContainerGraphMatrix
                        className={styles.scheduleMatrix}
                        graph={schedule.graph}
                        columns={schedule.columns}
                        cellMap={schedule.cellMap}
                        visualHintMap={schedule.visualHintMap}
                        visualHintDetailMap={schedule.visualHintDetailMap}
                        styleMap={schedule.styleMap}
                        dayConflictMap={schedule.dayConflictMap}
                        title={null}
                        icon={null}
                        readOnly
                        enableSelectionWhenReadOnly
                        compactSize
                        selectedCellKeys={
                          normalizedActiveScheduleSelection.graphId === schedule.graph.id
                            ? normalizedActiveScheduleSelection.keys
                            : []
                        }
                        onSelectedCellKeysChange={(keys) => handleScheduleSelectionChange(schedule.graph.id, keys)}
                        onVisualHintClick={detail => setActiveRelatedHintSelection({
                          graphId: schedule.graph.id,
                          cellKey: getGraphCellKey(detail.employeeId, detail.dayOfMonth),
                        })}
                      />
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </CardSection>

      <ContainerGraphRelatedHintDialog
        open={activeRelatedHint !== null}
        graphName={activeRelatedHintSchedule?.graph.name ?? ""}
        employeeName={activeRelatedHintEmployeeName}
        year={activeRelatedHintSchedule?.graph.year ?? new Date().getFullYear()}
        month={activeRelatedHintSchedule?.graph.month ?? 1}
        dayLabel={activeRelatedHintDayLabel}
        currentCellMap={activeRelatedHintSchedule?.cellMap ?? {}}
        detail={activeRelatedHint}
        onCancel={() => setActiveRelatedHintSelection(null)}
      />
    </div>
  );
}

export function HomePage({ phoneMode = false }: { phoneMode?: boolean }) {
  if (phoneMode) {
    return <div className={`${styles.placeholder} ${styles.placeholderPhone}`}>
      <h1 className={styles.placeholderTitle}>{t("Coming Soon")}</h1>
    </div>;
  }
  return <ManagerPcHomePage />;
}
