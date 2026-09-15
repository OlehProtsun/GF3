import { dateTimeFormat, t } from "@shared/i18n";
import { useEffect, useLayoutEffect, useMemo, useRef, useState } from "react";
import { AvailabilitySidebarCollapseButton, AvailabilitySidebarSection } from "@entities/availability-groups/ui/AvailabilitySidebarSection";
import {
  filterShiftSwaps,
  useCancelContainerShiftSwapMutation,
  useContainerShiftSwapsQuery,
  useDeleteContainerShiftSwapMutation,
  type ShiftSwap,
} from "@entities/shift-swaps";
import { getErrorMessage } from "@shared/api/httpClient";
import { useUpdateGraphsPublicationMutation } from "@entities/containers/api/queries";
import { ConfirmDialog } from "@shared/ui/ConfirmDialog";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { IosButton } from "@shared/ui/components/IosButton";
import { RecordGrid } from "@shared/ui/components/RecordGrid";
import { RecordTile } from "@shared/ui/components/RecordTile";
import { CheckIcon, EyeIcon, SearchIcon, ContainerInfoIcon, InformationIcon, PlusIcon, ScheduleIcon, SwapOffersIcon } from "@shared/ui/icons";
import { formatScheduleLastUpdate } from "@shared/lib/scheduleLastUpdate";
import { CardSection } from "@shared/ui/sections/CardSection";
import type { Container } from "@entities/containers/model/types";
import {
  getContainerDisplayName,
  getContainerInitials,
  getContainerProfileDetails,
  getContainerState,
} from "@entities/containers/model/presentation";
import type { ContainerGraphSummary, ContainerStatistics } from "@entities/containers/model/statistics";
import { ShiftSwapHistoryDialog } from "./ShiftSwapHistoryDialog";
import styles from "./ContainerProfileWorkspace.module.css";
type ContainerProfileWorkspaceProps = {
  container?: Container | null;
  graphs: ContainerGraphSummary[];
  totalGraphsCount: number;
  statistics: ContainerStatistics;
  isLoading: boolean;
  hasLoadError: boolean;
  isDeleting: boolean;
  isGraphsLoading: boolean;
  hasGraphsError: boolean;
  graphSearchQuery: string;
  isMultiOpenEnabled: boolean;
  selectedGraphIds: number[];
  onGraphSearchChange: (value: string) => void;
  onClearGraphSearch: () => void;
  onToggleMultiOpen: () => void;
  onToggleGraphSelection: (graphId: number) => void;
  onOpenSelectedGraphs: () => void;
  onAddGraph: () => void;
  onOpenGraph: (graphId: number) => void;
  onEditContainer: (containerId: number) => void;
  onDeleteContainer: () => void;
};

const DESKTOP_MEDIA_QUERY = "(min-width: 961px)";

function joinClassNames(...values: Array<string | false | null | undefined>) {
  return values.filter(Boolean).join(" ");
}

function matchesEmployeeSearch(employeeName: string, searchQuery: string) {
  const normalizedEmployeeName = employeeName.toLocaleLowerCase();
  const searchTerms = searchQuery.trim().toLocaleLowerCase().split(/\s+/).filter(Boolean);

  return searchTerms.every(term => normalizedEmployeeName.includes(term));
}

function formatSwapDate(swap: ShiftSwap) {
  return dateTimeFormat("en-GB", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    timeZone: "UTC",
  }).format(new Date(Date.UTC(swap.year, swap.month - 1, swap.dayOfMonth)));
}

function getSwapReceiver(swap: ShiftSwap) {
  if (swap.status === "accepted") {
    return swap.acceptedByEmployeeName ?? t("Employee");
  }

  return swap.targetEmployeeName ?? t("Everyone");
}

type SwapAction = { type: "cancel" | "delete"; swap: ShiftSwap } | null;
type SchedulePublicationAction = "publish" | "private" | null;

type MultiOpenHeaderToggleProps = {
  checked: boolean;
  onToggle: () => void;
};

function MultiOpenHeaderToggle({ checked, onToggle }: MultiOpenHeaderToggleProps) {
  return (
    <button
      type="button"
      className={joinClassNames(styles.multiOpenToggle, checked && styles.multiOpenToggleActive)}
      aria-pressed={checked}
      onClick={onToggle}
    >
      <span className={styles.multiOpenToggleTitle}>{t("MultiOpen")}</span>

      <span className={styles.multiOpenToggleTrack} aria-hidden="true">
        <span className={styles.multiOpenToggleThumb} />
      </span>
    </button>
  );
}

export function ContainerProfileWorkspace({
  container,
  graphs,
  totalGraphsCount,
  statistics,
  isLoading,
  hasLoadError,
  isDeleting,
  isGraphsLoading,
  hasGraphsError,
  graphSearchQuery,
  isMultiOpenEnabled,
  selectedGraphIds,
  onGraphSearchChange,
  onClearGraphSearch,
  onToggleMultiOpen,
  onToggleGraphSelection,
  onOpenSelectedGraphs,
  onAddGraph,
  onOpenGraph,
  onEditContainer,
  onDeleteContainer,
}: ContainerProfileWorkspaceProps) {
  const sidebarRef = useRef<HTMLElement | null>(null);
  const [isDesktopLayout, setIsDesktopLayout] = useState(() => {
    if (typeof window === "undefined") {
      return true;
    }

    return window.matchMedia(DESKTOP_MEDIA_QUERY).matches;
  });
  const [scheduleCardHeight, setScheduleCardHeight] = useState<number | null>(null);
  const [statisticsSearchQuery, setStatisticsSearchQuery] = useState("");
  const [swapSearchQuery, setSwapSearchQuery] = useState("");
  const [selectedSwap, setSelectedSwap] = useState<ShiftSwap | null>(null);
  const [swapAction, setSwapAction] = useState<SwapAction>(null);
  const [swapActionError, setSwapActionError] = useState<string | null>(null);
  const [schedulePublicationAction, setSchedulePublicationAction] = useState<SchedulePublicationAction>(null);
  const [bulkAllowSwap, setBulkAllowSwap] = useState(true);
  const [schedulePublicationError, setSchedulePublicationError] = useState<string | null>(null);
  const [collapsedSections, setCollapsedSections] = useState({ information: false, swaps: false });
  const allSectionsCollapsed = collapsedSections.information && collapsedSections.swaps;
  const swapsQuery = useContainerShiftSwapsQuery(container?.id ?? null, Boolean(container));
  const cancelSwapMutation = useCancelContainerShiftSwapMutation();
  const deleteSwapMutation = useDeleteContainerShiftSwapMutation();
  const updateGraphsPublicationMutation = useUpdateGraphsPublicationMutation();
  const swaps = useMemo(() => swapsQuery.data ?? [], [swapsQuery.data]);
  const filteredSwaps = useMemo(
    () => filterShiftSwaps(swaps, swapSearchQuery),
    [swapSearchQuery, swaps],
  );
  const hasSwapSearch = swapSearchQuery.trim().length > 0;
  const isSwapActionPending = cancelSwapMutation.isPending || deleteSwapMutation.isPending;

  useEffect(() => {
    if (typeof window === "undefined") {
      return;
    }

    const mediaQuery = window.matchMedia(DESKTOP_MEDIA_QUERY);
    const handleChange = () => {
      setIsDesktopLayout(mediaQuery.matches);
    };

    handleChange();

    if (typeof mediaQuery.addEventListener === "function") {
      mediaQuery.addEventListener("change", handleChange);
      return () => mediaQuery.removeEventListener("change", handleChange);
    }

    mediaQuery.addListener(handleChange);
    return () => mediaQuery.removeListener(handleChange);
  }, []);

  useLayoutEffect(() => {
    if (!isDesktopLayout) {
      return;
    }

    const sidebarElement = sidebarRef.current;
    if (!sidebarElement) {
      return;
    }

    const updateHeight = () => {
      const informationSection = sidebarElement.firstElementChild;
      const nextHeight = Math.ceil(
        collapsedSections.swaps && !collapsedSections.information && informationSection
          ? informationSection.getBoundingClientRect().height
          : sidebarElement.getBoundingClientRect().height,
      );
      setScheduleCardHeight(previousHeight => (previousHeight !== nextHeight ? nextHeight : previousHeight));
    };

    updateHeight();

    const observer = new ResizeObserver(updateHeight);
    observer.observe(sidebarElement);

    return () => {
      observer.disconnect();
    };
  }, [collapsedSections.information, collapsedSections.swaps, filteredSwaps.length, isDesktopLayout, totalGraphsCount, graphs.length, statistics.totalHoursText]);

  const handleSwapActionConfirm = () => {
    if (!container || !swapAction) {
      return;
    }

    setSwapActionError(null);
    const mutation = swapAction.type === "cancel" ? cancelSwapMutation : deleteSwapMutation;
    mutation.mutate(
      { containerId: container.id, id: swapAction.swap.id },
      {
        onSuccess: () => {
          setSwapAction(null);
          setSelectedSwap(current => current?.id === swapAction.swap.id ? null : current);
        },
        onError: error => setSwapActionError(getErrorMessage(error, t("Could not {0} this swap.", swapAction.type))),
      },
    );
  };

  const handleSchedulePublicationConfirm = () => {
    if (!container || !schedulePublicationAction) {
      return;
    }

    setSchedulePublicationError(null);
    updateGraphsPublicationMutation.mutate(
      {
        containerId: container.id,
        payload: {
          publicationStatus: schedulePublicationAction === "publish" ? "public" : "private",
          allowSwap: schedulePublicationAction === "publish" ? bulkAllowSwap : null,
        },
      },
      {
        onSuccess: () => setSchedulePublicationAction(null),
        onError: error => setSchedulePublicationError(getErrorMessage(error, t("Could not update all schedules."))),
      },
    );
  };

  if (isLoading) {
    return <div className={styles.state}>{t("Loading container profile...")}</div>;
  }

  if (hasLoadError || !container) {
    return <ErrorBanner className={styles.banner}>{t("Could not load this container.")}</ErrorBanner>;
  }

  const rawDetails = getContainerProfileDetails(container, {
    scheduleCount: totalGraphsCount,
    totalEmployees: statistics.totalEmployees,
    totalShops: statistics.totalShops,
    totalHoursText: statistics.totalHoursText,
  });
  const noteDetail = rawDetails.find(item => item.key === "note");
  const details = rawDetails
    .filter(item => item.key !== "note")
    .map(item => ({
      key: item.key,
      label: item.label,
      value: item.value ?? <span className={styles.mutedValue}>{t("Not provided")}</span>,
    }));
  const hasNote = typeof noteDetail?.value === "string" ? noteDetail.value.trim().length > 0 : Boolean(noteDetail?.value);
  const noteValue = noteDetail?.value ?? <span className={styles.mutedValue}>{t("Not provided")}</span>;
  const showGraphsEmpty = !isGraphsLoading && !hasGraphsError && totalGraphsCount === 0;
  const showSearchEmpty = !isGraphsLoading && totalGraphsCount > 0 && graphs.length === 0;
  const showStatisticsEmpty = statistics.pivotRows.length === 0;
  const hasStatisticsSearch = statisticsSearchQuery.trim().length > 0;
  const filteredStatisticsRows = hasStatisticsSearch
    ? statistics.pivotRows.filter(
        row => !row.isTotal && matchesEmployeeSearch(row.employee, statisticsSearchQuery),
      )
    : statistics.pivotRows;
  const showStatisticsSearchEmpty = hasStatisticsSearch && filteredStatisticsRows.length === 0;
  const selectedGraphIdSet = new Set(selectedGraphIds);
  const hasSelectedGraphs = selectedGraphIds.length > 0;
  const scheduleCardShellStyle =
    isDesktopLayout && scheduleCardHeight !== null
      ? { height: `${scheduleCardHeight}px` }
      : undefined;

  return (
    <div className={styles.workspace}>
      <div className={joinClassNames(styles.topRow, allSectionsCollapsed && styles.topRowAllCollapsed)}>
        <aside ref={sidebarRef} className={joinClassNames(styles.sidebar, allSectionsCollapsed && styles.sidebarAllCollapsed)}>
          <AvailabilitySidebarSection
            label={t("Container Information")}
            collapsed={collapsedSections.information}
            collapsedIcon={<ContainerInfoIcon size={18} />}
            collapsedOffset="flush"
            onExpand={() => setCollapsedSections(current => ({ ...current, information: false }))}
          >
            <CardSection
              className={joinClassNames(styles.sectionCard, styles.summaryCard)}
              title={t("Container Information")}
              icon={<ContainerInfoIcon size={18} />}
              headerRightSlot={
                <AvailabilitySidebarCollapseButton
                  label={t("Container Information")}
                  onCollapse={() => setCollapsedSections(current => ({ ...current, information: true }))}
                />
              }
            >
            <div className={styles.containerInfoContent}>
              <div className={styles.containerIdentity}>
                <div className={styles.containerAvatar} aria-hidden="true">
                  {getContainerInitials(container)}
                </div>
                <div className={styles.containerIdentityText}>
                  <h2 className={styles.containerName}>{getContainerDisplayName(container)}</h2>
                  <p className={styles.containerSubtitle}>
                    {getContainerState(totalGraphsCount, statistics.totalHoursText, container.note)}
                  </p>
                </div>
              </div>

              <div className={styles.containerInfoLabels} role="group" aria-label={t("Container details")}>
                <div className={styles.statisticsSummaryItem}>
                  <span className={styles.statisticsSummaryLabel}>ID</span>
                  <strong className={styles.statisticsSummaryValue}>{container.id}</strong>
                </div>
                {details.map(item => (
                  <div key={item.key} className={styles.statisticsSummaryItem}>
                    <span className={styles.statisticsSummaryLabel}>{item.label}</span>
                    <strong className={styles.statisticsSummaryValue}>{item.value}</strong>
                  </div>
                ))}
              </div>

              <div className={joinClassNames(styles.containerNote, !hasNote && styles.containerNoteEmpty)}>
                <span className={styles.containerNoteLabel}>{noteDetail?.label ?? t("Note")}</span>
                <div className={styles.containerNoteValue}>{noteValue}</div>
              </div>
              <div className={styles.containerInfoActions}>
                <IosButton label={t("Edit Container")} onClick={() => onEditContainer(container.id)} />
                <IosButton
                  label={isDeleting ? t("Deleting...") : t("Delete Container")}
                  variant="secondary"
                  customColor="#ef4444"
                  customBorderColor="#ef4444"
                  disabled={isDeleting}
                  onClick={onDeleteContainer}
                />
              </div>
            </div>
            </CardSection>
          </AvailabilitySidebarSection>

          <AvailabilitySidebarSection
            label={t("Swaps")}
            collapsed={collapsedSections.swaps}
            collapsedIcon={<SwapOffersIcon size={18} />}
            collapsedOffset="flush"
            onExpand={() => setCollapsedSections(current => ({ ...current, swaps: false }))}
          >
            <CardSection
              className={joinClassNames(styles.sectionCard, styles.swapsCard)}
              title={t("Swaps")}
              icon={<SwapOffersIcon size={18} />}
              headerRightSlot={
                <div className={styles.swapHeaderActions}>
                  <span className={styles.headerBadge}>{hasSwapSearch ? `${filteredSwaps.length}/${swaps.length}` : swaps.length}</span>
                  <AvailabilitySidebarCollapseButton
                    label={t("Swaps")}
                    onCollapse={() => setCollapsedSections(current => ({ ...current, swaps: true }))}
                  />
                </div>
              }
            >
              <div className={styles.swapsContent}>
                <div className={styles.swapMonthLabel}>
                  {t("All schedules in this container")}</div>

                <label className={joinClassNames(styles.searchField, styles.swapSearchField)} htmlFor="container-swaps-search">
                  <SearchIcon className={styles.searchIcon} />
                  <input
                    id="container-swaps-search"
                    className={styles.searchInput}
                    type="search"
                    value={swapSearchQuery}
                    onChange={event => setSwapSearchQuery(event.target.value)}
                    placeholder={t("Employee, date or schedule")}
                    aria-label={t("Search swaps by giver, receiver, date or schedule")}
                  />
                </label>

                {swapActionError ? <ErrorBanner className={styles.swapError}>{swapActionError}</ErrorBanner> : null}
                {swapsQuery.isLoading ? <div className={styles.swapState}>{t("Loading swaps...")}</div> : null}
                {swapsQuery.isError ? <div className={styles.swapState}>{t("Could not load swaps for this container.")}</div> : null}
                {!swapsQuery.isLoading && !swapsQuery.isError && swaps.length === 0 ? (
                  <div className={styles.swapState}>{t("No swaps in this container.")}</div>
                ) : null}
                {!swapsQuery.isLoading && swaps.length > 0 && filteredSwaps.length === 0 ? (
                  <div className={styles.swapState}>{t("No swaps match \"{0}\".", swapSearchQuery.trim())}</div>
                ) : null}

                {filteredSwaps.length > 0 ? (
                  <div className={styles.swapList}>
                    {filteredSwaps.map(swap => (
                      <article key={`${swap.status}-${swap.id}`} className={styles.swapItem}>
                        <div className={styles.swapItemHeader}>
                          <div className={styles.swapPeople}>
                            <strong>{swap.fromEmployeeName}</strong>
                            <span>{t("to")}</span>
                            <strong>{getSwapReceiver(swap)}</strong>
                          </div>
                          <span className={joinClassNames(styles.swapStatus, styles[`swapStatus${swap.status}`])}>{swap.status}</span>
                        </div>
                        <div className={styles.swapMeta}>
                          <strong>{swap.scheduleName}</strong>
                          <span>{`${formatSwapDate(swap)} | ${swap.fromTime.slice(0, 5)}-${swap.toTime.slice(0, 5)}`}</span>
                        </div>
                        <div className={styles.swapActions}>
                          <button type="button" onClick={() => setSelectedSwap(swap)} aria-label={t("View {0} swap", swap.scheduleName)}>
                            <EyeIcon size={16} />
                            <span>{t("View")}</span>
                          </button>
                          {swap.status === "open" ? (
                            <button type="button" disabled={isSwapActionPending} onClick={() => setSwapAction({ type: "cancel", swap })}>
                              {t("Cancel")}</button>
                          ) : null}
                          <button
                            type="button"
                            className={styles.swapDeleteButton}
                            disabled={isSwapActionPending}
                            onClick={() => setSwapAction({ type: "delete", swap })}
                          >
                            {t("Delete")}</button>
                        </div>
                      </article>
                    ))}
                  </div>
                ) : null}
              </div>
            </CardSection>
          </AvailabilitySidebarSection>
        </aside>

        <div className={styles.scheduleColumn}>
          <div className={styles.scheduleCardShell} style={scheduleCardShellStyle}>
            <CardSection
              className={joinClassNames(styles.sectionCard, styles.scheduleCard)}
              title={t("Schedules")}
              icon={<ScheduleIcon size={18} />}
              headerRightSlot={
                <div className={styles.scheduleHeaderRow}>
                  <div className={styles.scheduleBulkActions} aria-label={t("Schedule publication actions")}>
                    <button
                      type="button"
                      className={joinClassNames(styles.scheduleBulkButton, styles.scheduleBulkPublishButton)}
                      disabled={totalGraphsCount === 0 || updateGraphsPublicationMutation.isPending}
                      onClick={() => {
                        setBulkAllowSwap(true);
                        setSchedulePublicationError(null);
                        setSchedulePublicationAction("publish");
                      }}
                    >
                      {t("Publish all")}</button>
                    <button
                      type="button"
                      className={styles.scheduleBulkButton}
                      disabled={totalGraphsCount === 0 || updateGraphsPublicationMutation.isPending}
                      onClick={() => {
                        setSchedulePublicationError(null);
                        setSchedulePublicationAction("private");
                      }}
                    >
                      {t("Make private")}</button>
                  </div>

                  <label className={styles.searchField} htmlFor="container-graphs-search">
                    <SearchIcon className={styles.searchIcon} />
                    <input
                      id="container-graphs-search"
                      className={styles.searchInput}
                      value={graphSearchQuery}
                      onChange={event => onGraphSearchChange(event.target.value)}
                      placeholder={t("Search schedule")}
                      aria-label={t("Search schedule")}
                    />
                  </label>

                  {graphSearchQuery.trim() ? (
                    <IosButton label={t("Clear")} variant="secondary" onClick={onClearGraphSearch} />
                  ) : null}

                  <div className={styles.scheduleHeaderActions}>
                    <IosButton label={t("Add New")} icon={<PlusIcon size={18} />} onClick={onAddGraph} />
                    <MultiOpenHeaderToggle checked={isMultiOpenEnabled} onToggle={onToggleMultiOpen} />
                    {isMultiOpenEnabled ? (
                      <>
                        <IosButton
                          label={hasSelectedGraphs ? t("Open ({0})", selectedGraphIds.length) : t("Open")}
                          disabled={!hasSelectedGraphs}
                          onClick={onOpenSelectedGraphs}
                        />
                      </>
                    ) : null}
                    <span className={styles.headerBadge}>{t("Total: {0}", totalGraphsCount)}</span>
                  </div>
                </div>
              }
            >
              <div className={styles.scheduleBody}>
                <div className={styles.scheduleContent}>
                  {schedulePublicationError ? (
                    <ErrorBanner className={styles.inlineBanner}>{schedulePublicationError}</ErrorBanner>
                  ) : null}

                  {isGraphsLoading && totalGraphsCount === 0 ? (
                    <div className={styles.state}>{t("Loading schedules...")}</div>
                  ) : null}

                  {hasGraphsError && totalGraphsCount === 0 ? (
                    <ErrorBanner className={styles.inlineBanner}>{t("Could not load schedules for this container.")}</ErrorBanner>
                  ) : null}

                  {showGraphsEmpty ? (
                    <div className={`${styles.emptyState} ${styles.scheduleEmptyState}`}>
                      <div className={styles.emptyTitle}>{t("No schedules yet")}</div>
                      <div className={styles.emptyDescription}>
                        {t("This container has not been linked to any saved schedules.")}</div>
                    </div>
                  ) : null}

                  {showSearchEmpty ? (
                    <div className={styles.emptyState}>
                      <div className={styles.emptyTitle}>{t("Nothing found")}</div>
                      <div className={styles.emptyDescription}>
                        {t("No schedule matches \"")}{graphSearchQuery}".
                      </div>
                    </div>
                  ) : null}

                  {!showGraphsEmpty && !showSearchEmpty && graphs.length > 0 ? (
                    <RecordGrid className={styles.scheduleGrid}>
                      {graphs.map(summary => (
                        <RecordTile
                          key={summary.graph.id}
                          title={summary.graph.name}
                          headerSlot={
                            null
                          }
                          cornerSlot={
                            isMultiOpenEnabled ? (
                              <span
                                className={joinClassNames(
                                  styles.selectionMark,
                                  selectedGraphIdSet.has(summary.graph.id) && styles.selectionMarkActive,
                                )}
                                aria-hidden="true"
                              >
                                {selectedGraphIdSet.has(summary.graph.id) ? <CheckIcon size={14} /> : null}
                              </span>
                            ) : null
                          }
                          isSelected={isMultiOpenEnabled && selectedGraphIdSet.has(summary.graph.id)}
                          onClick={() => {
                            if (isMultiOpenEnabled) {
                              onToggleGraphSelection(summary.graph.id);
                              return;
                            }

                            onOpenGraph(summary.graph.id);
                          }}
                          ariaLabel={
                            isMultiOpenEnabled
                              ? t("{0} schedule {1}", selectedGraphIdSet.has(summary.graph.id) ? "Deselect" : "Select", summary.graph.name)
                              : t("Open schedule {0}", summary.graph.name)
                          }
                          metaItems={[
                            { key: "shop", label: t("Shop"), value: summary.shopName },
                            { key: "employees", label: t("Employees"), value: String(summary.employeeCount) },
                            { key: "hours", label: t("Hours"), value: summary.assignedHoursText },
                            { key: "month-year", label: t("Month Year"), value: summary.monthYearLabel },
                            {
                              key: "status",
                              label: t("Status"),
                              value: (
                                <span
                                  className={joinClassNames(
                                    styles.scheduleStatusValue,
                                    summary.graph.publicationStatus === "public"
                                      ? styles.scheduleStatusPublic
                                      : styles.scheduleStatusPrivate,
                                  )}
                                >
                                  {summary.graph.publicationStatus === "public" ? t("Public") : t("Private")}
                                </span>
                              ),
                            },
                            {
                              key: "last-update",
                              label: t("Last Update"),
                              value: (
                                <span className={styles.scheduleLastUpdateValue}>
                                  {formatScheduleLastUpdate(summary.graph.lastUpdatedAtUtc)}
                                </span>
                              ),
                            },
                          ]}
                        />
                      ))}
                    </RecordGrid>
                  ) : null}
                </div>
              </div>
            </CardSection>
          </div>
        </div>
      </div>

      <CardSection
        className={`${styles.sectionCard} ${styles.statisticsCard}`}
        headerClassName={styles.statisticsHeader}
        title={t("Container Statistics")}
        icon={<InformationIcon size={18} />}
      >
        <div className={styles.statisticsSummary} role="group" aria-label={t("Container totals")}>
          <div className={styles.statisticsSummaryItem}>
            <span className={styles.statisticsSummaryLabel}>{t("Total hours")}</span>
            <strong className={styles.statisticsSummaryValue}>{statistics.totalHoursText}</strong>
          </div>
          <div className={styles.statisticsSummaryItem}>
            <span className={styles.statisticsSummaryLabel}>{t("Employees")}</span>
            <strong className={styles.statisticsSummaryValue}>{statistics.totalEmployees}</strong>
          </div>
          <div className={styles.statisticsSummaryItem}>
            <span className={styles.statisticsSummaryLabel}>{t("Shops")}</span>
            <strong className={styles.statisticsSummaryValue}>{statistics.totalShops}</strong>
          </div>
          <div className={styles.statisticsSummaryItem}>
            <span className={styles.statisticsSummaryLabel}>{t("Schedules")}</span>
            <strong className={styles.statisticsSummaryValue}>{totalGraphsCount}</strong>
          </div>
          {!showStatisticsEmpty ? (
            <label
              className={joinClassNames(styles.searchField, styles.statisticsSearchField)}
              htmlFor="container-statistics-search"
            >
              <SearchIcon className={styles.searchIcon} />
              <input
                id="container-statistics-search"
                className={styles.searchInput}
                type="search"
                value={statisticsSearchQuery}
                onChange={event => setStatisticsSearchQuery(event.target.value)}
                placeholder={t("Search by name or surname")}
                aria-label={t("Search statistics by employee name or surname")}
              />
            </label>
          ) : null}
        </div>

        {showStatisticsEmpty ? (
          <div className={`${styles.emptyState} ${styles.statisticsEmptyState}`}>
            <div className={styles.emptyTitle}>{t("No statistics yet")}</div>
            <div className={styles.emptyDescription}>
              {t("Statistics will appear as soon as schedules contain assigned employees and slots.")}</div>
          </div>
        ) : showStatisticsSearchEmpty ? (
          <div className={`${styles.emptyState} ${styles.statisticsSearchEmptyState}`} role="status">
            <div className={styles.emptyTitle}>{t("No employees found")}</div>
            <div className={styles.emptyDescription}>
              {t("No employee matches \"")}{statisticsSearchQuery.trim()}".
            </div>
          </div>
        ) : (
          <>
            <div className={styles.tableWrap}>
              <table className={styles.table}>
                <thead>
                  <tr>
                    <th>{t("Employee")}</th>
                    <th>{t("Work Days")}</th>
                    <th>{t("Free Days")}</th>
                    <th>{t("Hours Sum")}</th>
                    {statistics.shopHeaders.map(shopHeader => (
                      <th key={shopHeader.key}>{shopHeader.name}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {filteredStatisticsRows.map(row => (
                    <tr key={`${row.employee}-${row.isTotal ? "total" : "row"}`} className={row.isTotal ? styles.totalRow : undefined}>
                      <td>{row.employee}</td>
                      <td>{row.workDays}</td>
                      <td>{row.freeDays}</td>
                      <td>{row.hoursSum}</td>
                      {statistics.shopHeaders.map(shopHeader => (
                        <td key={`${row.employee}-${shopHeader.key}`}>{row.hoursByShop[shopHeader.key] ?? "0"}</td>
                      ))}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>


          </>
        )}
      </CardSection>

      <ConfirmDialog
        open={schedulePublicationAction !== null}
        variant={schedulePublicationAction === "publish" ? "confirm" : "warning"}
        title={schedulePublicationAction === "publish" ? t("Publish all schedules") : t("Make all schedules private")}
        message={schedulePublicationError ?? (schedulePublicationAction === "publish"
          ? t("Publish all {0} schedules in this container?", totalGraphsCount)
          : t("Make all {0} schedules private? Employees will no longer see them.", totalGraphsCount))}
        confirmText={updateGraphsPublicationMutation.isPending
          ? t("Working...")
          : schedulePublicationAction === "publish" ? t("Publish all") : t("Make private")}
        confirmDisabled={updateGraphsPublicationMutation.isPending}
        cancelDisabled={updateGraphsPublicationMutation.isPending}
        footerSlot={schedulePublicationAction === "publish" ? (
          <div className={styles.swapPermissionChoice}>
            <span>{t("Can swap")}</span>
            <div className={styles.swapPermissionOptions}>
              <button
                type="button"
                className={bulkAllowSwap ? styles.swapPermissionOptionActive : undefined}
                aria-pressed={bulkAllowSwap}
                onClick={() => setBulkAllowSwap(true)}
              >
                {t("Yes")}</button>
              <button
                type="button"
                className={!bulkAllowSwap ? styles.swapPermissionOptionActive : undefined}
                aria-pressed={!bulkAllowSwap}
                onClick={() => setBulkAllowSwap(false)}
              >
                {t("No")}</button>
            </div>
          </div>
        ) : undefined}
        onCancel={() => setSchedulePublicationAction(null)}
        onConfirm={handleSchedulePublicationConfirm}
      />

      <ShiftSwapHistoryDialog open={selectedSwap !== null} swap={selectedSwap} onCancel={() => setSelectedSwap(null)} />
      <ConfirmDialog
        open={swapAction !== null}
        title={swapAction?.type === "cancel" ? t("Cancel swap") : t("Delete swap")}
        message={swapAction?.type === "cancel"
          ? t("Cancel this open swap? Employees will no longer be able to accept it.")
          : t("Delete this swap record? This does not reverse an already accepted schedule change.")}
        confirmText={isSwapActionPending ? t("Working...") : swapAction?.type === "cancel" ? t("Cancel swap") : t("Delete")}
        confirmDisabled={isSwapActionPending}
        cancelDisabled={isSwapActionPending}
        onCancel={() => setSwapAction(null)}
        onConfirm={handleSwapActionConfirm}
      />
    </div>
  );
}
