import { usePublicHolidayVisibility } from "@app/providers/PublicHolidayVisibilityProvider";
import { dateTimeFormat, t } from "@shared/i18n";
import { pinGeneratedColumnsFirst } from "../model/graphColumnOrder";
import { Fragment, useEffect, useId, useRef, useState } from "react";
import type { CSSProperties } from "react";
import type { AvailabilityGroup } from "@entities/availability-groups/model/types";
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
import { InformationIcon, ScheduleDetailsIcon, SearchIcon, StatisticsIcon, NoteIcon, ScheduleIcon, ShopIcon, CheckIcon, EmployeeIcon, ContainerIcon, ChevronRightIcon, CloseIcon, ClockIcon, GroupIcon, AvailabilityIcon, PencilIcon, TrashIcon } from "@shared/ui/icons";
import { formatScheduleLastUpdate } from "@shared/lib/scheduleLastUpdate";
import { CardSection } from "@shared/ui/sections/CardSection";
import { ContainerGraphMatrix } from "./ContainerGraphMatrix";
import { ContainerGraphRelatedHintDialog } from "./ContainerGraphRelatedHintDialog";
import styles from "./ContainerGraphProfileWorkspace.module.css";

type ContainerGraphProfileWorkspaceProps = {
  urlopCellKeys?: ReadonlySet<string>;
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
  showManagementActions?: boolean;
  onEdit: () => void;
  onDelete: () => void;
};

function joinClassNames(...values: Array<string | undefined | false>) {
  return values.filter(Boolean).join(" ");
}

function matchesEmployeeSearch(employeeName: string, searchQuery: string) {
  const normalizedEmployeeName = employeeName.toLocaleLowerCase();
  const searchTerms = searchQuery.trim().toLocaleLowerCase().split(/\s+/).filter(Boolean);

  return searchTerms.every(term => normalizedEmployeeName.includes(term));
}

function getEmployeeInitials(name: string) {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  return parts.length ? `${parts[0][0]}${parts[1]?.[0] ?? parts[0][1] ?? ""}`.toUpperCase() : "?";
}

const detailIcons = {
  month: ScheduleIcon, year: ScheduleIcon, shop: ShopIcon, status: CheckIcon,
  people: EmployeeIcon, shift1: ScheduleIcon, shift2: ScheduleIcon,
  "max-days": ScheduleDetailsIcon, "max-consecutive-full": ScheduleDetailsIcon,
  "max-full": ContainerIcon, availability: ContainerIcon,
};


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
  return dateTimeFormat("en-US", {
    month: "long",
    timeZone: "UTC",
  }).format(new Date(Date.UTC(year, month - 1, 1)));
}

function formatGraphHintDateLabel(year: number, month: number, dayOfMonth: number) {
  return dateTimeFormat("en-GB", {
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
  manualColumns: Array<{ id: number; source?: "generated-unfilled" }>,
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

  return pinGeneratedColumnsFirst(nextOrder, manualColumns.filter(column => column.source === "generated-unfilled").map(column => -column.id));
}

export function ContainerGraphProfileWorkspace({
  urlopCellKeys,
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
  showManagementActions = true,
  onEdit,
  onDelete,
}: ContainerGraphProfileWorkspaceProps) {
  const showPublicHolidays = usePublicHolidayVisibility();
  const informationDialogRef = useRef<HTMLDialogElement>(null);
  const informationTitleId = useId();
  const [activeRelatedHintCellKey, setActiveRelatedHintCellKey] = useState<string | null>(null);
  const [selectedCellKeys, setSelectedCellKeys] = useState<string[]>([]);
  const [summarySearchQuery, setSummarySearchQuery] = useState("");
  const [phoneInformationExpanded, setPhoneInformationExpanded] = useState(false);
  const [phoneSummaryExpanded, setPhoneSummaryExpanded] = useState(false);
  const phoneInformationPanelId = useId();
  const phoneSummaryPanelId = useId();
  const [viewportHeight, setViewportHeight] = useState(() => {
    if (typeof window === "undefined") {
      return 0;
    }

    return window.innerHeight;
  });
  useEffect(() => {
    if (typeof window === "undefined") {
      return;
    }

    const handleChange = () => {
      setViewportHeight(window.innerHeight);
    };

    handleChange();

    window.addEventListener("resize", handleChange);
    return () => window.removeEventListener("resize", handleChange);
  }, []);

  if (isLoading) {
    return <div className={styles.state}>{t("Loading schedule profile...")}</div>;
  }

  if (hasLoadError || !graph) {
    return <ErrorBanner className={styles.banner}>{t("Could not load this schedule.")}</ErrorBanner>;
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
  const summaryRows = buildGraphSummaryRows(graph, graphEmployees, employeesById, slots, urlopCellKeys);
  const hasSummarySearch = summarySearchQuery.trim().length > 0;
  const filteredSummaryRows = hasSummarySearch
    ? summaryRows.filter(row => matchesEmployeeSearch(row.employee, summarySearchQuery))
    : summaryRows;
  const showSummarySearchEmpty = hasSummarySearch && filteredSummaryRows.length === 0;
  const note = getGraphVisibleNote(graph.note).trim();
  const hasNote = note.length > 0;
  const lastUpdateLabel = formatScheduleLastUpdate(graph.lastUpdatedAtUtc);
  const matrixBaseMinHeight = Math.max(
    isHeaderCollapsed ? 680 : 660,
    viewportHeight - (isHeaderCollapsed ? 188 : 228),
  );
  const matrixCardMinHeight = Math.min(
    isHeaderCollapsed ? 1000 : 980,
    Math.round(matrixBaseMinHeight * 1.15),
  );
  const phoneMatrixViewport = compactSize && !showManagementActions;
  const shouldPreserveMatrixHeight = !compactSize;
  const activeRelatedHint =
    activeRelatedHintCellKey
      ? visualHintDetailMap[activeRelatedHintCellKey] ?? null
      : null;
  const activeHintEmployeeName =
    activeRelatedHint
      ? getEmployeeFullName(employeesById?.get(activeRelatedHint.employeeId), t("Employee {0}", activeRelatedHint.employeeId))
      : "";
  const activeHintDayLabel =
    activeRelatedHint
      ? formatGraphHintDateLabel(graph.year, graph.month, activeRelatedHint.dayOfMonth)
      : "";
  const isPublic = graph.publicationStatus === "public";
  const publicationStatusLabel = isPublic ? t("Public") : t("Private");
  const publicationStatusClass = isPublic ? styles.scheduleStatusPublic : styles.scheduleStatusPrivate;
  const scheduleDetails = [
    { key: "month", label: t("Month"), value: getGraphMonthLabel(graph.year, graph.month) },
    { key: "year", label: t("Year"), value: String(graph.year) },
    { key: "shop", label: t("Shop"), value: shop?.name ?? t("Shop {0}", graph.shopId) },
    { key: "status", label: t("Status"), value: publicationStatusLabel },
    { key: "people", label: t("People on Shift"), value: String(graph.peoplePerShift) },
    { key: "shift1", label: t("Shift1"), value: graph.shift1Time },
    { key: "shift2", label: t("Shift2"), value: graph.shift2Time },
    { key: "max-days", label: t("Max Consecutive Days"), value: String(graph.maxConsecutiveDays) },
    { key: "max-consecutive-full", label: t("Max Consecutive Full"), value: String(graph.maxConsecutiveFull) },
    { key: "max-full", label: t("Max Full"), value: String(graph.maxFullPerMonth) },
    { key: "availability", label: t("Availability"), value: availabilityGroup?.name ?? t("None") },
  ] as const;
  const matrixCardShellStyle =
    shouldPreserveMatrixHeight
      ? {
        minHeight: `${matrixCardMinHeight}px`,
        height: `${matrixCardMinHeight}px`,
        maxHeight: `${matrixCardMinHeight}px`,
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
  const lastUpdate = (
    <div className={styles.lastUpdateField} aria-label={t("Last Update: {0}", lastUpdateLabel)}>
      <span className={styles.lastUpdateLabel}>
        <span className={styles.lastUpdateDot} aria-hidden="true" />
        {t("Last Update")}
      </span>
      <strong className={styles.lastUpdateValue}>
        {graph.lastUpdatedAtUtc ? (
          <time dateTime={graph.lastUpdatedAtUtc}>{lastUpdateLabel}</time>
        ) : lastUpdateLabel}
      </strong>
    </div>
  );
  const badges = (
    <div className={styles.badges}>
      <span className={styles.badge}>
        {!phoneMatrixViewport && <GroupIcon size={15} />}
        {t("Total Employees: {0}", totals.totalEmployees)}
      </span>
      <span className={styles.badge}>
        {!phoneMatrixViewport && <ClockIcon size={15} />}
        {t("Total Hours: {0}", totals.totalHoursText)}
      </span>
    </div>
  );
  const matrix = (
    <ContainerGraphMatrix
      urlopCellKeys={urlopCellKeys}
      className={joinClassNames(styles.matrixCard, compactSize && styles.matrixCardCompact)}
      style={matrixCardStyle}
      headerClassName={phoneMatrixViewport ? styles.phoneMatrixHeader : styles.matrixHeader}
      titleClassName={phoneMatrixViewport ? styles.phoneSectionTitle : styles.matrixTitle}
      headerRightClassName={phoneMatrixViewport ? styles.phoneMatrixHeaderRight : styles.matrixHeaderRight}
      graph={graph}
      showShiftStaffingCounts
      compactSize={compactSize}
      mobileReadOnlyViewport={phoneMatrixViewport}
      maxAutoColumnWidth={phoneMatrixViewport ? 156 : undefined}
      stretchColumns={!phoneMatrixViewport}
      allowColumnResize={!phoneMatrixViewport}
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
      helperText={showManagementActions ? t("This schedule is read-only. Open edit to update details, assigned employees, matrix values or cell styles.") : t("This schedule is read-only.")}
      onVisualHintClick={detail => setActiveRelatedHintCellKey(`${detail.employeeId}:${detail.dayOfMonth}`)}
      headerRightSlot={phoneMatrixViewport ? badges : (
        <>
          <div className={styles.matrixActions}>
            {lastUpdate}
            <button
              type="button"
              className={joinClassNames(styles.roundButton, styles.infoAction)}
              aria-label={t("Schedule Information")}
              aria-haspopup="dialog"
              onClick={event => {
                event.currentTarget.focus({ preventScroll: true });
                informationDialogRef.current?.showModal();
              }}
            >
              <InformationIcon size={18} aria-hidden="true" />
            </button>
            {showManagementActions && (
              <>
                {showEditAction && (
                  <IosButton
                    className={joinClassNames(styles.matrixAction, styles.editAction)}
                    size="compact"
                    label={t("Edit Schedule")}
                    icon={<PencilIcon />}
                    onClick={onEdit}
                  />
                )}
                <IosButton
                  className={joinClassNames(styles.matrixAction, styles.deleteAction)}
                  size="compact"
                  label={isDeleting ? t("Deleting...") : t("Delete Schedule")}
                  icon={<TrashIcon />}
                  variant="secondary"
                  customColor="#fff0ef"
                  customBorderColor="#f7d5d1"
                  disabled={isDeleting}
                  onClick={onDelete}
                />
              </>
            )}
          </div>
          {badges}
        </>
      )}
    />
  );

  const informationContent = (
    <div className={styles.summaryContent}>
      <div className={styles.scheduleIdentity}>
        <div className={styles.scheduleAvatar} aria-hidden="true">
          {getGraphInitials(graph)}
        </div>
        <div className={styles.scheduleIdentityText}>
          <h2 className={styles.scheduleName}>{graph.name}</h2>
          <div className={styles.scheduleIdentityMeta}>
            <p className={styles.scheduleId}>{`ID ${graph.id}`}</p>
            {!phoneMatrixViewport && (
              <span className={joinClassNames(styles.publicationBadge, publicationStatusClass)}>{publicationStatusLabel}</span>
            )}
          </div>
        </div>
      </div>

      <div className={joinClassNames(styles.scheduleNote, !hasNote && styles.scheduleNoteEmpty)}>
        <span className={styles.scheduleInfoLabel}><NoteIcon size={16} aria-hidden="true" />{t("Note")}</span>
        <div className={styles.scheduleNoteValue} tabIndex={phoneMatrixViewport && hasNote ? 0 : undefined}>
          {note || <span className={styles.mutedValue}>{t("No notes yet.")}</span>}
        </div>
      </div>

      <div className={styles.scheduleInfoLabels} role="group" aria-label={t("Schedule details")}>
        {scheduleDetails.map(item => {
          const Icon = detailIcons[item.key];
          return (
            <div key={item.key} className={joinClassNames(styles.scheduleInfoItem, phoneMatrixViewport && item.key === "availability" && styles.phoneAvailabilityTile)}>
              <span className={styles.detailIcon} aria-hidden="true"><Icon size={16} /></span>
              <span className={styles.scheduleInfoLabel}>{item.label}</span>
              <strong className={joinClassNames(styles.scheduleInfoValue, item.key === "status" && publicationStatusClass)}>
                {item.value}
              </strong>
            </div>
          );
        })}
      </div>

      {phoneMatrixViewport && lastUpdate}
    </div>
  );
  const phoneInformationCard = (
    <section className={joinClassNames(styles.phoneCollapsibleCard, styles.phoneInformationCard)} data-phone-schedule-information>
      <h2 className={styles.phoneCollapsibleHeading}>
        <button type="button" className={styles.phoneCollapsibleToggle} aria-expanded={phoneInformationExpanded} aria-controls={phoneInformationPanelId}
          onClick={() => setPhoneInformationExpanded(current => !current)}>
          <span className={styles.phoneCollapsibleIcon}><ScheduleDetailsIcon size={19} /></span>
          <span>{t("Schedule Information")}</span>
          <span className={styles.phoneCollapsibleChevron}><ChevronRightIcon size={16} /></span>
        </button>
      </h2>
      <div id={phoneInformationPanelId} className={styles.phoneCollapsiblePanel} data-expanded={phoneInformationExpanded} inert={!phoneInformationExpanded} aria-hidden={!phoneInformationExpanded}>
        <div className={styles.phoneCollapsibleClip}><div className={styles.phoneInformationContent}>{informationContent}</div></div>
      </div>
    </section>
  );
  const phoneSummaryCard = (
    <section className={joinClassNames(styles.phoneCollapsibleCard, styles.phoneSummarySection)}>
      <h2 className={styles.phoneCollapsibleHeading}>
        <button type="button" className={styles.phoneCollapsibleToggle} aria-expanded={phoneSummaryExpanded} aria-controls={phoneSummaryPanelId}
          onClick={() => setPhoneSummaryExpanded(current => !current)}>
          <span className={styles.phoneCollapsibleIcon}><StatisticsIcon size={19} /></span>
          <span>{t("Schedule Summary")}</span>
          <span className={styles.phoneEmployeeCount}>{t("Employees: {0}", summaryRows.length)}</span>
          <span className={styles.phoneCollapsibleChevron}><ChevronRightIcon size={16} /></span>
        </button>
      </h2>
      <div id={phoneSummaryPanelId} className={styles.phoneCollapsiblePanel} data-expanded={phoneSummaryExpanded} inert={!phoneSummaryExpanded} aria-hidden={!phoneSummaryExpanded}>
        <div className={styles.phoneCollapsibleClip}>
          <div className={styles.phoneSummaryContent}>
            {summaryRows.length > 0 && (
              <label className={joinClassNames(styles.summarySearchField, styles.phoneSummarySearch)} htmlFor="schedule-summary-search">
                <SearchIcon className={styles.summarySearchIcon} />
                <input id="schedule-summary-search" className={styles.summarySearchInput} type="search" value={summarySearchQuery}
                  onChange={event => setSummarySearchQuery(event.target.value)} placeholder={t("Search by name or surname")}
                  aria-label={t("Search schedule summary by employee name or surname")} />
              </label>
            )}
            {summaryRows.length === 0 ? <div className={styles.emptyState}>{showManagementActions ? t("No employee schedule rows yet. Generate a schedule or assign matrix intervals to see the summary.") : t("No results")}</div>
              : showSummarySearchEmpty ? <div className={styles.emptyState} role="status">{t("No employees found for \"{0}\".", summarySearchQuery.trim())}</div>
                : <div className={styles.phoneSummary} data-phone-schedule-summary>
                  {filteredSummaryRows.map(row => <details className={styles.phoneSummaryEmployee} key={row.employeeId}>
                    <summary className={styles.phoneSummaryToggle}>
                      <span className={joinClassNames(styles.phoneEmployeeAvatar, styles[`phoneAvatar${Math.abs(row.employeeId) % 6}`])} aria-hidden="true">{getEmployeeInitials(row.employee)}</span>
                      <h3 className={styles.phoneEmployeeName}>{row.employee}</h3>
                      <dl className={joinClassNames(styles.phoneSummaryMetrics, showPublicHolidays && styles.phoneSummaryMetricsWithHolidays)}>
                        <div><dt><ScheduleDetailsIcon size={13} />{t("Work Days")}</dt><dd>{row.workDays}</dd></div>
                        <div><dt><AvailabilityIcon size={13} />{t("Free Days")}</dt><dd>{row.freeDays}</dd></div>
                        <div><dt><ScheduleIcon size={13} />{t("Urlop Days")}</dt><dd>{row.urlopDays}</dd></div>
                        <div><dt><ClockIcon size={13} />{t("Hours")}</dt><dd>{row.sum || "0"}</dd></div>
                        {showPublicHolidays && <div><dt><ScheduleDetailsIcon size={13} />{t("Holiday Days")}</dt><dd>{row.holidayDays}</dd></div>}
                      </dl>
                      <span className={styles.phoneEmployeeChevron} aria-hidden="true"><ChevronRightIcon size={18} /></span>
                    </summary>
                    <dl className={styles.phoneSummaryDays}>{summaryHeaders.map((header, dayIndex) => <div key={header.dayOfMonth}>
                      <dt>{header.label}</dt>
                      <dd>{row.dayRows.map((days, rowIndex) => <div className={styles.phoneSummaryShift} key={rowIndex}>
                        <span><small>{t("From")}</small>{days[dayIndex].from || "-"}</span><span><small>{t("To")}</small>{days[dayIndex].to || "-"}</span><span><small>{t("Hours")}</small>{days[dayIndex].hours || "-"}</span>
                      </div>)}</dd>
                    </div>)}</dl>
                  </details>)}
                </div>}
          </div>
        </div>
      </div>
    </section>
  );
  const summaryCard = (
    <CardSection
      className={styles.summaryCardSection}
      title={t("Schedule Summary")}
      headerClassName={phoneMatrixViewport ? styles.phoneSectionHeader : undefined}
      icon={phoneMatrixViewport ? <StatisticsIcon size={18} /> : <InformationIcon size={18} />}
      titleClassName={phoneMatrixViewport ? styles.phoneSectionTitle : undefined}
      headerRightClassName={phoneMatrixViewport ? styles.phoneSummaryHeaderRight : undefined}
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
                placeholder={t("Search by name or surname")}
                aria-label={t("Search schedule summary by employee name or surname")}
              />
            </label>
          ) : null}
          {!phoneMatrixViewport && <div className={styles.summaryMeta}>
            <span className={styles.metaBadge}>{t("Employees: {0}", totals.totalEmployees)}</span>
            <span className={styles.metaBadge}>{t("Hours: {0}", totals.totalHoursText)}</span>
          </div>}
        </div>
      }
    >
      {summaryRows.length === 0 ? (
        <div className={styles.emptyState}>
          {showManagementActions ? t("No employee schedule rows yet. Generate a schedule or assign matrix intervals to see the summary.") : t("No results")}</div>
      ) : showSummarySearchEmpty ? (
        <div className={styles.emptyState} role="status">
          {t("No employees found for \"{0}\".", summarySearchQuery.trim())}
        </div>
      ) : phoneMatrixViewport ? (
        <div className={styles.phoneSummary} data-phone-schedule-summary>
          {filteredSummaryRows.map(row => <details className={styles.phoneSummaryEmployee} key={row.employeeId}>
            <summary className={styles.phoneSummaryToggle}>
              <span className={joinClassNames(styles.phoneEmployeeAvatar, styles[`phoneAvatar${Math.abs(row.employeeId) % 6}`])} aria-hidden="true">{getEmployeeInitials(row.employee)}</span>
              <h3 className={styles.phoneEmployeeName}>{row.employee}</h3>
              <span className={styles.phoneEmployeeChevron} aria-hidden="true"><ChevronRightIcon size={18} /></span>
              <dl className={joinClassNames(styles.phoneSummaryMetrics, showPublicHolidays && styles.phoneSummaryMetricsWithHolidays)}>
                <div><dt>{t("Work Days")}</dt><dd>{row.workDays}</dd></div>
                <div><dt>{t("Free Days")}</dt><dd>{row.freeDays}</dd></div>
                <div><dt>{t("Urlop Days")}</dt><dd>{row.urlopDays}</dd></div>
                <div><dt>{t("Hours")}</dt><dd>{row.sum || "0"}</dd></div>
                {showPublicHolidays && <div><dt>{t("Holiday Days")}</dt><dd>{row.holidayDays}</dd></div>}
              </dl>
            </summary>
            <dl className={styles.phoneSummaryDays}>{summaryHeaders.map((header, dayIndex) => <div key={header.dayOfMonth}>
                <dt>{header.label}</dt>
                <dd>{row.dayRows.map((days, rowIndex) => <div className={styles.phoneSummaryShift} key={rowIndex}>
                  <span><small>{t("From")}</small>{days[dayIndex].from || "-"}</span>
                  <span><small>{t("To")}</small>{days[dayIndex].to || "-"}</span>
                  <span><small>{t("Hours")}</small>{days[dayIndex].hours || "-"}</span>
                </div>)}</dd>
              </div>)}</dl>
          </details>)}
        </div>
      ) : (
        <div className={styles.summaryTableScroll}>
          <table className={styles.summaryTable}>
            <thead>
              <tr>
                <th rowSpan={2} className={joinClassNames(styles.stickyColumn, styles.employeeColumn)}>{t("Employee")}</th>
                <th rowSpan={2} className={joinClassNames(styles.stickyColumnSecondary, styles.statColumn)}>{t("Work Days")}</th>
                <th rowSpan={2} className={joinClassNames(styles.stickyColumnTertiary, styles.statColumn)}>{t("Free Days")}</th>
                <th rowSpan={2} className={joinClassNames(styles.stickyColumnQuaternary, styles.statColumn)}>{t("Sum")}</th>
                {showPublicHolidays && <th rowSpan={2} className={joinClassNames(styles.stickyColumnQuinary, styles.statColumn, styles.holidayStatColumn)}>{t("Holiday Days")}</th>}
                <th rowSpan={2} className={styles.statColumn}>{t("Urlop Days")}</th>
                {summaryHeaders.map(header => (
                  <th key={header.dayOfMonth} colSpan={3}>{header.label}</th>
                ))}
              </tr>
              <tr>
                {summaryHeaders.map(header => (
                  <Fragment key={`${header.dayOfMonth}-subcolumns`}>
                    <th className={styles.subColumn}>{t("From")}</th>
                    <th className={styles.subColumn}>{t("To")}</th>
                    <th className={styles.subColumn}>{t("Hours")}</th>
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
                          {showPublicHolidays && <td rowSpan={row.dayRows.length} className={joinClassNames(styles.stickyColumnQuinary, styles.statColumn, styles.statValue)}>{row.holidayDays}</td>}
                          <td rowSpan={row.dayRows.length} className={styles.statValue}>{row.urlopDays}</td>
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
  );

  return (
    <>
      {phoneMatrixViewport ? <div className={joinClassNames(styles.workspace, styles.workspacePhone)}>
        {phoneInformationCard}
        <div className={joinClassNames(styles.mainColumn, styles.mainColumnCompact)}>{matrix}</div>
        {phoneSummaryCard}
      </div> : <div className={joinClassNames(styles.workspace, isHeaderCollapsed && styles.workspaceHeaderCollapsed)}>
        <div className={joinClassNames(styles.mainColumn, compactSize && styles.mainColumnCompact)}>
          {compactSize ? (
            matrix
          ) : (
            <div className={styles.matrixCardShell} style={matrixCardShellStyle}>
              {matrix}
            </div>
          )}
        </div>
        {summaryCard}
        <dialog ref={informationDialogRef} className={styles.informationDialog} aria-labelledby={informationTitleId}>
          <header className={styles.informationHeader}>
            <h2 id={informationTitleId}>{t("Schedule Information")}</h2>
            <button type="button" className={styles.roundButton} aria-label={t("Close")} onClick={() => informationDialogRef.current?.close()}>
              <CloseIcon size={18} aria-hidden="true" />
            </button>
          </header>
          <div className={styles.informationBody} tabIndex={0}>
            {informationContent}
          </div>
        </dialog>
      </div>}

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
