import { Fragment, useEffect, useState } from "react";
import type { CSSProperties } from "react";
import type { AvailabilityGroup } from "@entities/availability-groups/model/types";
import { AvailabilitySidebarCollapseButton, AvailabilitySidebarSection } from "@entities/availability-groups/ui/AvailabilitySidebarSection";
import {
  buildGraphCellMap,
  buildGraphConflictDayMap,
  buildGraphRelatedScheduleHintData,
  buildGraphMatrixColumns,
  buildGraphStyleMap,
  buildGraphSummaryHeaders,
  buildGraphSummaryRows,
  buildGraphTotals,
} from "@entities/containers/model/graphWorkspace";
import {
  getGraphVisibleNote,
  parseGraphNoteContent,
  rehydrateGraphNoteCellStyles,
  rehydrateGraphNoteTextCells,
} from "@entities/containers/model/graphNote";
import { getEmployeeFullName } from "@entities/employees/model/presentation";
import type { Container, Graph, GraphCellStyle, GraphEmployee, GraphSlot } from "@entities/containers/model/types";
import type { Employee } from "@entities/employees/model/types";
import type { Shop } from "@entities/shops/model/types";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { IosButton } from "@shared/ui/components/IosButton";
import { InformationIcon, ScheduleDetailsIcon, SearchIcon } from "@shared/ui/icons";
import { formatScheduleLastUpdate } from "@shared/lib/scheduleLastUpdate";
import { CardSection } from "@shared/ui/sections/CardSection";
import { ContainerGraphMatrix } from "./ContainerGraphMatrix";
import { ContainerGraphRelatedHintDialog } from "./ContainerGraphRelatedHintDialog";
import styles from "./ContainerGraphProfileWorkspace.module.css";

type ContainerGraphProfileWorkspaceProps = {
  container?: Container | null;
  graph?: Graph | null;
  shop?: Shop | null;
  availabilityGroup?: AvailabilityGroup | null;
  graphEmployees: GraphEmployee[];
  slots: GraphSlot[];
  cellStyles: GraphCellStyle[];
  relatedGraphs?: Graph[];
  relatedGraphSlotsById?: Record<number, GraphSlot[]>;
  employeesById?: Map<number, Employee>;
  isLoading: boolean;
  hasLoadError: boolean;
  isDeleting: boolean;
  isHeaderCollapsed?: boolean;
  compactSize?: boolean;
  showEditAction?: boolean;
  onEdit: () => void;
  onDelete: () => void;
};

const DESKTOP_MEDIA_QUERY = "(min-width: 1181px)";

function joinClassNames(...values: Array<string | undefined | false>) {
  return values.filter(Boolean).join(" ");
}

function matchesEmployeeSearch(employeeName: string, searchQuery: string) {
  const normalizedEmployeeName = employeeName.toLocaleLowerCase();
  const searchTerms = searchQuery.trim().toLocaleLowerCase().split(/\s+/).filter(Boolean);

  return searchTerms.every(term => normalizedEmployeeName.includes(term));
}

function getGraphInitials(graph?: Graph | null) {
  const parts = (graph?.name ?? "")
    .split(/\s+/)
    .map(part => part.trim())
    .filter(Boolean);

  if (parts.length === 0) {
    return "SC";
  }

  return `${parts[0]?.[0] ?? ""}${parts[1]?.[0] ?? parts[0]?.[1] ?? ""}`.toUpperCase();
}

function getGraphMonthLabel(year: number, month: number) {
  return new Intl.DateTimeFormat("en-US", {
    month: "long",
    timeZone: "UTC",
  }).format(new Date(Date.UTC(year, month - 1, 1)));
}

function formatGraphHintDateLabel(year: number, month: number, dayOfMonth: number) {
  return new Intl.DateTimeFormat("en-GB", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    timeZone: "UTC",
  }).format(new Date(Date.UTC(year, month - 1, dayOfMonth)));
}

function buildManualColumnEmployeeId(columnId: number) {
  return -Math.abs(columnId);
}

function buildDefaultScheduleColumnOrder(
  graphEmployees: GraphEmployee[],
  manualColumns: Array<{ id: number }>,
) {
  return [
    ...graphEmployees.map(employee => employee.employeeId),
    ...manualColumns.map(column => buildManualColumnEmployeeId(column.id)),
  ];
}

function sanitizeScheduleColumnOrder(
  columnOrder: number[],
  graphEmployees: GraphEmployee[],
  manualColumns: Array<{ id: number }>,
) {
  const fallbackOrder = buildDefaultScheduleColumnOrder(graphEmployees, manualColumns);
  const validIds = new Set(fallbackOrder);
  const nextOrder: number[] = [];

  columnOrder.forEach(columnId => {
    if (!validIds.has(columnId) || nextOrder.includes(columnId)) {
      return;
    }

    nextOrder.push(columnId);
  });

  fallbackOrder.forEach(columnId => {
    if (!nextOrder.includes(columnId)) {
      nextOrder.push(columnId);
    }
  });

  return nextOrder;
}

export function ContainerGraphProfileWorkspace({
  graph,
  shop,
  availabilityGroup,
  graphEmployees,
  slots,
  cellStyles,
  relatedGraphs = [],
  relatedGraphSlotsById = {},
  employeesById,
  isLoading,
  hasLoadError,
  isDeleting,
  isHeaderCollapsed = false,
  compactSize = false,
  showEditAction = true,
  onEdit,
  onDelete,
}: ContainerGraphProfileWorkspaceProps) {
  const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(true);
  const [activeRelatedHintCellKey, setActiveRelatedHintCellKey] = useState<string | null>(null);
  const [selectedCellKeys, setSelectedCellKeys] = useState<string[]>([]);
  const [summarySearchQuery, setSummarySearchQuery] = useState("");
  const [viewportHeight, setViewportHeight] = useState(() => {
    if (typeof window === "undefined") {
      return 0;
    }

    return window.innerHeight;
  });
  const [isDesktopLayout, setIsDesktopLayout] = useState(() => {
    if (typeof window === "undefined") {
      return true;
    }

    return window.matchMedia(DESKTOP_MEDIA_QUERY).matches;
  });

  useEffect(() => {
    if (typeof window === "undefined") {
      return;
    }

    const mediaQuery = window.matchMedia(DESKTOP_MEDIA_QUERY);
    const handleChange = () => {
      setIsDesktopLayout(mediaQuery.matches);
      setViewportHeight(window.innerHeight);
    };

    handleChange();

    window.addEventListener("resize", handleChange);

    if (typeof mediaQuery.addEventListener === "function") {
      mediaQuery.addEventListener("change", handleChange);
      return () => {
        window.removeEventListener("resize", handleChange);
        mediaQuery.removeEventListener("change", handleChange);
      };
    }

    mediaQuery.addListener(handleChange);
    return () => {
      window.removeEventListener("resize", handleChange);
      mediaQuery.removeListener(handleChange);
    };
  }, []);

  if (isLoading) {
    return <div className={styles.state}>Loading schedule profile...</div>;
  }

  if (hasLoadError || !graph) {
    return <ErrorBanner className={styles.banner}>Could not load this schedule.</ErrorBanner>;
  }

  const parsedGraphNote = parseGraphNoteContent(graph.note);
  const baseColumns = buildGraphMatrixColumns(graphEmployees, employeesById, slots);
  const manualColumns = parsedGraphNote.manualColumns.map(column => ({
    employeeId: buildManualColumnEmployeeId(column.id),
    kind: "manual" as const,
    manualColumnId: column.id,
    graphEmployeeId: null,
    label: column.label,
    minHoursMonth: null,
    totalMinutes: 0,
    totalText: "",
  }));
  const columnByEmployeeId = new Map(
    [...baseColumns, ...manualColumns].map(column => [column.employeeId, column] as const),
  );
  const columns = sanitizeScheduleColumnOrder(parsedGraphNote.columnOrder, graphEmployees, parsedGraphNote.manualColumns)
    .map(columnId => columnByEmployeeId.get(columnId))
    .filter((column): column is (typeof baseColumns)[number] => Boolean(column));
  const manualCellMap = parsedGraphNote.manualColumns.reduce<Record<string, string>>((accumulator, column) => {
    Object.entries(column.cells).forEach(([dayOfMonth, value]) => {
      if (!value.trim()) {
        return;
      }

      accumulator[`${buildManualColumnEmployeeId(column.id)}:${dayOfMonth}`] = value;
    });

    return accumulator;
  }, {});
  const cellMap = {
    ...buildGraphCellMap(slots),
    ...rehydrateGraphNoteTextCells(parsedGraphNote.textCells),
    ...manualCellMap,
  };
  const styleMap = buildGraphStyleMap([
    ...cellStyles,
    ...rehydrateGraphNoteCellStyles(parsedGraphNote.cellStyles, graph.id),
  ]);
  const relatedScheduleHintData = buildGraphRelatedScheduleHintData({
    currentGraph: graph,
    columns,
    cellMap,
    relatedGraphs: relatedGraphs.map(relatedGraph => ({
      graph: relatedGraph,
      slots: relatedGraphSlotsById[relatedGraph.id] ?? [],
    })),
  });
  const visualHintMap = relatedScheduleHintData.visualHintMap;
  const visualHintDetailMap = relatedScheduleHintData.detailMap;
  const dayConflictMap = buildGraphConflictDayMap(graph, slots);
  const totals = buildGraphTotals(graphEmployees, slots, employeesById);
  const summaryHeaders = buildGraphSummaryHeaders(graph.year, graph.month);
  const summaryRows = buildGraphSummaryRows(graph, graphEmployees, employeesById, slots);
  const hasSummarySearch = summarySearchQuery.trim().length > 0;
  const filteredSummaryRows = hasSummarySearch
    ? summaryRows.filter(row => matchesEmployeeSearch(row.employee, summarySearchQuery))
    : summaryRows;
  const showSummarySearchEmpty = hasSummarySearch && filteredSummaryRows.length === 0;
  const note = getGraphVisibleNote(graph.note).trim();
  const hasNote = note.length > 0;
  const lastUpdateLabel = formatScheduleLastUpdate(graph.lastUpdatedAtUtc);
  const topRowBaseMinHeight =
    isDesktopLayout
      ? Math.max(isHeaderCollapsed ? 680 : 660, viewportHeight - (isHeaderCollapsed ? 188 : 228))
      : null;
  const topRowCardMinHeight =
    topRowBaseMinHeight !== null
      ? Math.min(
        isHeaderCollapsed ? 1000 : 980,
        Math.round(topRowBaseMinHeight * 1.15),
      )
      : null;
  const shouldPreserveMatrixHeight = isDesktopLayout && !compactSize;
  const activeRelatedHint =
    activeRelatedHintCellKey
      ? visualHintDetailMap[activeRelatedHintCellKey] ?? null
      : null;
  const activeHintEmployeeName =
    activeRelatedHint
      ? getEmployeeFullName(employeesById?.get(activeRelatedHint.employeeId), `Employee ${activeRelatedHint.employeeId}`)
      : "";
  const activeHintDayLabel =
    activeRelatedHint
      ? formatGraphHintDateLabel(graph.year, graph.month, activeRelatedHint.dayOfMonth)
      : "";
  const scheduleDetails = [
    { key: "month", label: "Month", value: getGraphMonthLabel(graph.year, graph.month) },
    { key: "year", label: "Year", value: String(graph.year) },
    { key: "shop", label: "Shop", value: shop?.name ?? `Shop ${graph.shopId}` },
    { key: "status", label: "Status", value: graph.publicationStatus === "public" ? "Public" : "Private" },
    { key: "people", label: "People on Shift", value: String(graph.peoplePerShift) },
    { key: "shift1", label: "Shift1", value: graph.shift1Time },
    { key: "shift2", label: "Shift2", value: graph.shift2Time },
    { key: "max-days", label: "Max Consecutive Days", value: String(graph.maxConsecutiveDays) },
    { key: "max-consecutive-full", label: "Max Consecutive Full", value: String(graph.maxConsecutiveFull) },
    { key: "max-full", label: "Max Full", value: String(graph.maxFullPerMonth) },
    { key: "availability", label: "Availability", value: availabilityGroup?.name ?? "None" },
  ] as const;
  const matrixCardShellStyle =
    shouldPreserveMatrixHeight && topRowCardMinHeight !== null
      ? {
        minHeight: `${topRowCardMinHeight}px`,
        height: `${topRowCardMinHeight}px`,
        maxHeight: `${topRowCardMinHeight}px`,
      }
      : undefined;
  const matrixCardStyle =
    shouldPreserveMatrixHeight
      ? ({ height: "100%", maxHeight: "100%" } satisfies CSSProperties)
      : ({
        height: "auto",
        maxHeight: "none",
        "--matrix-card-padding-bottom": "0px",
        "--matrix-layout-padding-bottom": "0px",
      } as CSSProperties);
  const matrix = (
    <ContainerGraphMatrix
      className={joinClassNames(styles.matrixCard, compactSize && styles.matrixCardCompact)}
      style={matrixCardStyle}
      graph={graph}
      showShiftStaffingCounts
      compactSize={compactSize}
      columns={columns}
      cellMap={cellMap}
      visualHintMap={visualHintMap}
      visualHintDetailMap={visualHintDetailMap}
      styleMap={styleMap}
      dayConflictMap={dayConflictMap}
      readOnly
      regularCellText
      enableSelectionWhenReadOnly
      selectedCellKeys={selectedCellKeys}
      onSelectedCellKeysChange={setSelectedCellKeys}
      helperText="This schedule is read-only. Open edit to update details, assigned employees, matrix values or cell styles."
      onVisualHintClick={detail => setActiveRelatedHintCellKey(`${detail.employeeId}:${detail.dayOfMonth}`)}
      headerRightSlot={
        <div className={styles.badges}>
          <span className={styles.badge}>{`Total Employees: ${totals.totalEmployees}`}</span>
          <span className={styles.badge}>{`Total Hours: ${totals.totalHoursText}`}</span>
        </div>
      }
    />
  );

  return (
    <>
      <div className={joinClassNames(styles.workspace, isHeaderCollapsed && styles.workspaceHeaderCollapsed)}>
        <div className={joinClassNames(styles.topRow, isSidebarCollapsed && styles.topRowCollapsed)}>
          <aside className={joinClassNames(styles.sidebar, isSidebarCollapsed && styles.sidebarCollapsed)}>
          <AvailabilitySidebarSection
            label="Schedule Information"
            collapsed={isSidebarCollapsed}
            collapsedOffset="compact"
            onExpand={() => setIsSidebarCollapsed(false)}
          >
            <div className={styles.summaryCardMeasure}>
              <CardSection
                className={styles.summaryCard}
                title="Schedule Information"
                icon={<ScheduleDetailsIcon size={18} />}
                headerRightSlot={
                  <AvailabilitySidebarCollapseButton
                    label="Schedule Information"
                    onCollapse={() => setIsSidebarCollapsed(true)}
                  />
                }
              >
                <div className={styles.summaryContent}>
                  <div className={styles.scheduleIdentity}>
                    <div className={styles.scheduleAvatar} aria-hidden="true">
                      {getGraphInitials(graph)}
                    </div>
                    <div className={styles.scheduleIdentityText}>
                      <h2 className={styles.scheduleName}>{graph.name}</h2>
                      <p className={styles.scheduleId}>{`ID ${graph.id}`}</p>
                    </div>
                  </div>

                  <div className={joinClassNames(styles.scheduleNote, !hasNote && styles.scheduleNoteEmpty)}>
                    <span className={styles.scheduleInfoLabel}>Note</span>
                    <div className={styles.scheduleNoteValue}>
                      {note || <span className={styles.mutedValue}>No notes yet.</span>}
                    </div>
                  </div>

                  <div className={styles.scheduleInfoLabels} role="group" aria-label="Schedule details">
                    {scheduleDetails.map(item => (
                      <div key={item.key} className={styles.scheduleInfoItem}>
                        <span className={styles.scheduleInfoLabel}>{item.label}</span>
                        <strong
                          className={joinClassNames(
                            styles.scheduleInfoValue,
                            item.key === "status" && (
                              graph.publicationStatus === "public"
                                ? styles.scheduleStatusPublic
                                : styles.scheduleStatusPrivate
                            ),
                          )}
                        >
                          {item.value}
                        </strong>
                      </div>
                    ))}
                  </div>

                  <div className={styles.lastUpdateField} aria-label={`Last Update: ${lastUpdateLabel}`}>
                    <span className={styles.lastUpdateLabel}>
                      <span className={styles.lastUpdateDot} aria-hidden="true" />
                      Last Update
                    </span>
                    <strong className={styles.lastUpdateValue}>
                      {graph.lastUpdatedAtUtc ? (
                        <time dateTime={graph.lastUpdatedAtUtc}>{lastUpdateLabel}</time>
                      ) : lastUpdateLabel}
                    </strong>
                  </div>

                  <div className={styles.scheduleInfoActions}>
                    {showEditAction ? <IosButton label="Edit Schedule" onClick={onEdit} /> : null}
                    <IosButton
                      label={isDeleting ? "Deleting..." : "Delete Schedule"}
                      variant="secondary"
                      customColor="#ef4444"
                      customBorderColor="#ef4444"
                      disabled={isDeleting}
                      onClick={onDelete}
                    />
                  </div>
                </div>
              </CardSection>
            </div>
          </AvailabilitySidebarSection>
          </aside>

          <div className={joinClassNames(styles.mainColumn, compactSize && styles.mainColumnCompact)}>
            {compactSize ? (
              matrix
            ) : (
              <div className={styles.matrixCardShell} style={matrixCardShellStyle}>
                {matrix}
              </div>
            )}
          </div>
        </div>
        <CardSection
          className={styles.summaryCardSection}
          title="Schedule Summary"
          icon={<InformationIcon size={18} />}
          headerRightSlot={
            <div className={styles.summaryHeaderActions}>
              {summaryRows.length > 0 ? (
                <label className={styles.summarySearchField} htmlFor="schedule-summary-search">
                  <SearchIcon className={styles.summarySearchIcon} />
                  <input
                    id="schedule-summary-search"
                    className={styles.summarySearchInput}
                    type="search"
                    value={summarySearchQuery}
                    onChange={event => setSummarySearchQuery(event.target.value)}
                    placeholder="Search by name or surname"
                    aria-label="Search schedule summary by employee name or surname"
                  />
                </label>
              ) : null}
              <div className={styles.summaryMeta}>
                <span className={styles.metaBadge}>{`Employees: ${totals.totalEmployees}`}</span>
                <span className={styles.metaBadge}>{`Hours: ${totals.totalHoursText}`}</span>
              </div>
            </div>
          }
        >
          {summaryRows.length === 0 ? (
            <div className={styles.emptyState}>
              No employee schedule rows yet. Generate a schedule or assign matrix intervals to see the summary.
            </div>
          ) : showSummarySearchEmpty ? (
            <div className={styles.emptyState} role="status">
              {`No employees found for "${summarySearchQuery.trim()}".`}
            </div>
          ) : (
            <div className={styles.summaryTableScroll}>
              <table className={styles.summaryTable}>
                <thead>
                  <tr>
                    <th rowSpan={2} className={joinClassNames(styles.stickyColumn, styles.employeeColumn)}>Employee</th>
                    <th rowSpan={2} className={joinClassNames(styles.stickyColumnSecondary, styles.statColumn)}>Work Days</th>
                    <th rowSpan={2} className={joinClassNames(styles.stickyColumnTertiary, styles.statColumn)}>Free Days</th>
                    <th rowSpan={2} className={joinClassNames(styles.stickyColumnQuaternary, styles.statColumn)}>Sum</th>
                    {summaryHeaders.map(header => (
                      <th key={header.dayOfMonth} colSpan={3}>{header.label}</th>
                    ))}
                  </tr>
                  <tr>
                    {summaryHeaders.map(header => (
                      <Fragment key={`${header.dayOfMonth}-subcolumns`}>
                        <th className={styles.subColumn}>From</th>
                        <th className={styles.subColumn}>To</th>
                        <th className={styles.subColumn}>Hours</th>
                      </Fragment>
                    ))}
                  </tr>
                </thead>

                <tbody>
                  {filteredSummaryRows.map(row => (
                    <Fragment key={row.employeeId}>
                      {row.dayRows.map((days, rowIndex) => (
                        <tr key={`${row.employeeId}-${rowIndex}`}>
                          {rowIndex === 0 ? (
                            <>
                              <td rowSpan={row.dayRows.length} className={joinClassNames(styles.stickyColumn, styles.employeeValue)}>{row.employee}</td>
                              <td rowSpan={row.dayRows.length} className={joinClassNames(styles.stickyColumnSecondary, styles.statValue)}>{row.workDays}</td>
                              <td rowSpan={row.dayRows.length} className={joinClassNames(styles.stickyColumnTertiary, styles.statValue)}>{row.freeDays}</td>
                              <td rowSpan={row.dayRows.length} className={joinClassNames(styles.stickyColumnQuaternary, styles.statValue)}>{row.sum || "0"}</td>
                            </>
                          ) : null}
                          {days.map((day, index) => (
                            <Fragment key={`${row.employeeId}-${rowIndex}-${index}`}>
                              <td>{day.from || "-"}</td>
                              <td>{day.to || "-"}</td>
                              <td>{day.hours || "-"}</td>
                            </Fragment>
                          ))}
                        </tr>
                      ))}
                    </Fragment>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardSection>
      </div>

      <ContainerGraphRelatedHintDialog
        open={activeRelatedHint !== null}
        graphName={graph.name}
        employeeName={activeHintEmployeeName}
        year={graph.year}
        month={graph.month}
        dayLabel={activeHintDayLabel}
        currentCellMap={cellMap}
        detail={activeRelatedHint}
        onCancel={() => setActiveRelatedHintCellKey(null)}
      />
    </>
  );
}
