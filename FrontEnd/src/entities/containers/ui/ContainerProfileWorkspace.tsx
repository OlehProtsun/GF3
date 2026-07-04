import { useEffect, useLayoutEffect, useRef, useState } from "react";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { IosButton } from "@shared/ui/components/IosButton";
import { RecordGrid } from "@shared/ui/components/RecordGrid";
import { RecordTile } from "@shared/ui/components/RecordTile";
import { CheckIcon, SearchIcon, ContainerInfoIcon, InformationIcon, PlusIcon, ScheduleIcon } from "@shared/ui/icons";
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
      <span className={styles.multiOpenToggleTitle}>MultiOpen</span>

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
      const nextHeight = Math.ceil(sidebarElement.getBoundingClientRect().height);
      setScheduleCardHeight(previousHeight => (previousHeight !== nextHeight ? nextHeight : previousHeight));
    };

    updateHeight();

    const observer = new ResizeObserver(updateHeight);
    observer.observe(sidebarElement);

    return () => {
      observer.disconnect();
    };
  }, [isDesktopLayout, totalGraphsCount, graphs.length, statistics.totalHoursText]);

  if (isLoading) {
    return <div className={styles.state}>Loading container profile...</div>;
  }

  if (hasLoadError || !container) {
    return <ErrorBanner className={styles.banner}>Could not load this container.</ErrorBanner>;
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
      value: item.value ?? <span className={styles.mutedValue}>Not provided</span>,
    }));
  const hasNote = typeof noteDetail?.value === "string" ? noteDetail.value.trim().length > 0 : Boolean(noteDetail?.value);
  const noteValue = noteDetail?.value ?? <span className={styles.mutedValue}>Not provided</span>;
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
      <div className={styles.topRow}>
        <aside ref={sidebarRef} className={styles.sidebar}>
          <CardSection
            className={joinClassNames(styles.sectionCard, styles.summaryCard)}
            title="Container Information"
            icon={<ContainerInfoIcon size={18} />}
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

              <div className={styles.containerInfoLabels} role="group" aria-label="Container details">
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
                <span className={styles.containerNoteLabel}>{noteDetail?.label ?? "Note"}</span>
                <div className={styles.containerNoteValue}>{noteValue}</div>
              </div>
              <div className={styles.containerInfoActions}>
                <IosButton label="Edit Container" onClick={() => onEditContainer(container.id)} />
                <IosButton
                  label={isDeleting ? "Deleting..." : "Delete Container"}
                  variant="secondary"
                  customColor="#ef4444"
                  customBorderColor="#ef4444"
                  disabled={isDeleting}
                  onClick={onDeleteContainer}
                />
              </div>
            </div>
          </CardSection>
        </aside>

        <div className={styles.scheduleColumn}>
          <div className={styles.scheduleCardShell} style={scheduleCardShellStyle}>
            <CardSection
              className={joinClassNames(styles.sectionCard, styles.scheduleCard)}
              title="Schedules"
              icon={<ScheduleIcon size={18} />}
              headerRightSlot={
                <div className={styles.scheduleHeaderRow}>
                  <label className={styles.searchField} htmlFor="container-graphs-search">
                    <SearchIcon className={styles.searchIcon} />
                    <input
                      id="container-graphs-search"
                      className={styles.searchInput}
                      value={graphSearchQuery}
                      onChange={event => onGraphSearchChange(event.target.value)}
                      placeholder="Search schedule"
                      aria-label="Search schedule"
                    />
                  </label>

                  {graphSearchQuery.trim() ? (
                    <IosButton label="Clear" variant="secondary" onClick={onClearGraphSearch} />
                  ) : null}

                  <div className={styles.scheduleHeaderActions}>
                    <IosButton label="Add New" icon={<PlusIcon size={18} />} onClick={onAddGraph} />
                    <MultiOpenHeaderToggle checked={isMultiOpenEnabled} onToggle={onToggleMultiOpen} />
                    {isMultiOpenEnabled ? (
                      <>
                        <IosButton
                          label={hasSelectedGraphs ? `Open (${selectedGraphIds.length})` : "Open"}
                          disabled={!hasSelectedGraphs}
                          onClick={onOpenSelectedGraphs}
                        />
                      </>
                    ) : null}
                    <span className={styles.headerBadge}>{`Total: ${totalGraphsCount}`}</span>
                  </div>
                </div>
              }
            >
              <div className={styles.scheduleBody}>
                <div className={styles.scheduleContent}>
                  {isGraphsLoading && totalGraphsCount === 0 ? (
                    <div className={styles.state}>Loading schedules...</div>
                  ) : null}

                  {hasGraphsError && totalGraphsCount === 0 ? (
                    <ErrorBanner className={styles.inlineBanner}>Could not load schedules for this container.</ErrorBanner>
                  ) : null}

                  {showGraphsEmpty ? (
                    <div className={`${styles.emptyState} ${styles.scheduleEmptyState}`}>
                      <div className={styles.emptyTitle}>No schedules yet</div>
                      <div className={styles.emptyDescription}>
                        This container has not been linked to any saved schedules.
                      </div>
                    </div>
                  ) : null}

                  {showSearchEmpty ? (
                    <div className={styles.emptyState}>
                      <div className={styles.emptyTitle}>Nothing found</div>
                      <div className={styles.emptyDescription}>
                        No schedule matches "{graphSearchQuery}".
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
                              ? `${selectedGraphIdSet.has(summary.graph.id) ? "Deselect" : "Select"} schedule ${summary.graph.name}`
                              : `Open schedule ${summary.graph.name}`
                          }
                          metaItems={[
                            { key: "shop", label: "Shop", value: summary.shopName },
                            { key: "employees", label: "Employees", value: String(summary.employeeCount) },
                            { key: "hours", label: "Hours", value: summary.assignedHoursText },
                            { key: "month-year", label: "Month Year", value: summary.monthYearLabel },
                            {
                              key: "status",
                              label: "Status",
                              value: (
                                <span
                                  className={joinClassNames(
                                    styles.scheduleStatusValue,
                                    summary.graph.publicationStatus === "public"
                                      ? styles.scheduleStatusPublic
                                      : styles.scheduleStatusPrivate,
                                  )}
                                >
                                  {summary.graph.publicationStatus === "public" ? "Public" : "Private"}
                                </span>
                              ),
                            },
                            {
                              key: "last-update",
                              label: "Last Update",
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
        title="Container Statistics"
        icon={<InformationIcon size={18} />}
      >
        <div className={styles.statisticsSummary} role="group" aria-label="Container totals">
          <div className={styles.statisticsSummaryItem}>
            <span className={styles.statisticsSummaryLabel}>Total hours</span>
            <strong className={styles.statisticsSummaryValue}>{statistics.totalHoursText}</strong>
          </div>
          <div className={styles.statisticsSummaryItem}>
            <span className={styles.statisticsSummaryLabel}>Employees</span>
            <strong className={styles.statisticsSummaryValue}>{statistics.totalEmployees}</strong>
          </div>
          <div className={styles.statisticsSummaryItem}>
            <span className={styles.statisticsSummaryLabel}>Shops</span>
            <strong className={styles.statisticsSummaryValue}>{statistics.totalShops}</strong>
          </div>
          <div className={styles.statisticsSummaryItem}>
            <span className={styles.statisticsSummaryLabel}>Schedules</span>
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
                placeholder="Search by name or surname"
                aria-label="Search statistics by employee name or surname"
              />
            </label>
          ) : null}
        </div>

        {showStatisticsEmpty ? (
          <div className={`${styles.emptyState} ${styles.statisticsEmptyState}`}>
            <div className={styles.emptyTitle}>No statistics yet</div>
            <div className={styles.emptyDescription}>
              Statistics will appear as soon as schedules contain assigned employees and slots.
            </div>
          </div>
        ) : showStatisticsSearchEmpty ? (
          <div className={`${styles.emptyState} ${styles.statisticsSearchEmptyState}`} role="status">
            <div className={styles.emptyTitle}>No employees found</div>
            <div className={styles.emptyDescription}>
              No employee matches "{statisticsSearchQuery.trim()}".
            </div>
          </div>
        ) : (
          <>
            <div className={styles.tableWrap}>
              <table className={styles.table}>
                <thead>
                  <tr>
                    <th>Employee</th>
                    <th>Work Days</th>
                    <th>Free Days</th>
                    <th>Hours Sum</th>
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
    </div>
  );
}
