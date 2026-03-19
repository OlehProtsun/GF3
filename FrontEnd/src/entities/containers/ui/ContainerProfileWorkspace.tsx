import { useEffect, useLayoutEffect, useRef, useState } from "react";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { DetailItem, DetailList } from "@shared/ui/components/DetailList";
import { IosButton } from "@shared/ui/components/IosButton";
import { RecordGrid } from "@shared/ui/components/RecordGrid";
import { RecordTile } from "@shared/ui/components/RecordTile";
import { ProfileSummaryCard, type ProfileSummaryDetail } from "@shared/ui/components/ProfileSummaryCard";
import { SearchIcon, ContainerInfoIcon, InformationIcon, PlusIcon, ScheduleIcon } from "@shared/ui/icons";
import { CardSection } from "@shared/ui/sections/CardSection";
import type { Container } from "@entities/containers/model/types";
import { getGraphVisibleNote } from "@entities/containers/model/graphNote";
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
  onGraphSearchChange: (value: string) => void;
  onClearGraphSearch: () => void;
  onAddGraph: () => void;
  onOpenGraph: (graphId: number) => void;
  onEditContainer: (containerId: number) => void;
  onDeleteContainer: () => void;
};

const DESKTOP_MEDIA_QUERY = "(min-width: 961px)";

function joinClassNames(...values: Array<string | false | null | undefined>) {
  return values.filter(Boolean).join(" ");
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
  onGraphSearchChange,
  onClearGraphSearch,
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
      setScheduleCardHeight(null);
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
  const details: ProfileSummaryDetail[] = rawDetails
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
  const scheduleCardShellStyle =
    isDesktopLayout && scheduleCardHeight !== null
      ? { height: `${scheduleCardHeight}px` }
      : undefined;

  return (
    <div className={styles.workspace}>
      <div className={styles.topRow}>
        <aside ref={sidebarRef} className={styles.sidebar}>
          <ProfileSummaryCard
            className={styles.summaryCard}
            sectionTitle="Container Information"
            icon={<ContainerInfoIcon size={18} />}
            headerMeta={`ID ${container.id}`}
            avatar={getContainerInitials(container)}
            name={getContainerDisplayName(container)}
            subtitle={getContainerState(totalGraphsCount, statistics.totalHoursText, container.note)}
            contentAfterIdentity={
              <div className={styles.summaryDetails}>
                <DetailList columns={1} className={styles.noteList}>
                  <DetailItem
                    label={noteDetail?.label ?? "Note"}
                    value={noteValue}
                    className={joinClassNames(styles.profileNoteItem, !hasNote && styles.profileNoteItemEmpty)}
                    valueClassName={joinClassNames(styles.profileNoteValue, !hasNote && styles.profileNoteValueEmpty)}
                  />
                </DetailList>

                {details.length > 0 ? (
                  <DetailList columns={2} className={styles.metricsList}>
                    {details.map((item, index) => (
                      <DetailItem
                        key={item.key ?? index}
                        label={item.label}
                        value={item.value}
                        className={styles.profileMetricItem}
                        valueClassName={styles.profileMetricValue}
                      />
                    ))}
                  </DetailList>
                ) : null}
              </div>
            }
            actions={
              <>
                <IosButton label="Edit Container" onClick={() => onEditContainer(container.id)} />
                <IosButton
                  label={isDeleting ? "Deleting..." : "Delete Container"}
                  variant="secondary"
                  customColor="#ef4444"
                  customBorderColor="#ef4444"
                  disabled={isDeleting}
                  onClick={onDeleteContainer}
                />
              </>
            }
          />
        </aside>

        <div className={styles.scheduleColumn}>
          <div className={styles.scheduleCardShell} style={scheduleCardShellStyle}>
            <CardSection
              className={joinClassNames(styles.sectionCard, styles.scheduleCard)}
              title="Schedules"
              icon={<ScheduleIcon size={18} />}
              headerRightSlot={
                <div className={styles.scheduleHeaderActions}>
                  <IosButton label="Add New" icon={<PlusIcon size={18} />} onClick={onAddGraph} />
                  <span className={styles.headerBadge}>{`Total: ${totalGraphsCount}`}</span>
                </div>
              }
            >
              <div className={styles.scheduleBody}>
                <div className={styles.searchRow}>
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
                </div>

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
                          description={getGraphVisibleNote(summary.graph.note).trim() || undefined}
                          badge={summary.monthYearLabel}
                          onClick={() => onOpenGraph(summary.graph.id)}
                          ariaLabel={`Open schedule ${summary.graph.name}`}
                          metaItems={[
                            { key: "shop", label: "Shop", value: summary.shopName },
                            { key: "employees", label: "Employees", value: String(summary.employeeCount) },
                            { key: "hours", label: "Hours", value: summary.assignedHoursText },
                            { key: "days", label: "Days", value: String(summary.coverageDays) },
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
        title="Container Statistics"
        icon={<InformationIcon size={18} />}
      >
        <div className={styles.metricRow}>
          <div className={styles.metricCard}>
            <span className={styles.metricLabel}>Total hours</span>
            <strong className={styles.metricValue}>{statistics.totalHoursText}</strong>
          </div>
          <div className={styles.metricCard}>
            <span className={styles.metricLabel}>Employees</span>
            <strong className={styles.metricValue}>{statistics.totalEmployees}</strong>
          </div>
          <div className={styles.metricCard}>
            <span className={styles.metricLabel}>Shops</span>
            <strong className={styles.metricValue}>{statistics.totalShops}</strong>
          </div>
          <div className={styles.metricCard}>
            <span className={styles.metricLabel}>Schedules</span>
            <strong className={styles.metricValue}>{totalGraphsCount}</strong>
          </div>
        </div>

        <div className={styles.summaryMeta}>
          <p>
            <span className={styles.summaryMetaLabel}>Employees:</span>
            <span>{statistics.totalEmployeesListText}</span>
          </p>
          <p>
            <span className={styles.summaryMetaLabel}>Shops:</span>
            <span>{statistics.totalShopsListText}</span>
          </p>
        </div>

        {showStatisticsEmpty ? (
          <div className={`${styles.emptyState} ${styles.statisticsEmptyState}`}>
            <div className={styles.emptyTitle}>No statistics yet</div>
            <div className={styles.emptyDescription}>
              Statistics will appear as soon as schedules contain assigned employees and slots.
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
                  {statistics.pivotRows.map(row => (
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
