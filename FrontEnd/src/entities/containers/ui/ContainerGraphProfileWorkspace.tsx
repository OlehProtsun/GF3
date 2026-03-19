import { Fragment, useEffect, useState } from "react";
import type { AvailabilityGroup } from "@entities/availability-groups/model/types";
import { AvailabilitySidebarCollapseButton, AvailabilitySidebarSection } from "@entities/availability-groups/ui/AvailabilitySidebarSection";
import {
  buildGraphCellMap,
  buildGraphConflictDayMap,
  buildGraphMatrixColumns,
  buildGraphStyleMap,
  buildGraphSummaryHeaders,
  buildGraphSummaryRows,
  buildGraphTotals,
} from "@entities/containers/model/graphWorkspace";
import { getGraphVisibleNote } from "@entities/containers/model/graphNote";
import type { Container, Graph, GraphCellStyle, GraphEmployee, GraphSlot } from "@entities/containers/model/types";
import type { Employee } from "@entities/employees/model/types";
import type { Shop } from "@entities/shops/model/types";
import { DetailItem, DetailList } from "@shared/ui/components/DetailList";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { IosButton } from "@shared/ui/components/IosButton";
import { ProfileSummaryCard } from "@shared/ui/components/ProfileSummaryCard";
import { InformationIcon, ScheduleDetailsIcon } from "@shared/ui/icons";
import { CardSection } from "@shared/ui/sections/CardSection";
import { ContainerGraphMatrix } from "./ContainerGraphMatrix";
import styles from "./ContainerGraphProfileWorkspace.module.css";

type ContainerGraphProfileWorkspaceProps = {
  container?: Container | null;
  graph?: Graph | null;
  shop?: Shop | null;
  availabilityGroup?: AvailabilityGroup | null;
  graphEmployees: GraphEmployee[];
  slots: GraphSlot[];
  cellStyles: GraphCellStyle[];
  employeesById?: Map<number, Employee>;
  isLoading: boolean;
  hasLoadError: boolean;
  isDeleting: boolean;
  isHeaderCollapsed?: boolean;
  onEdit: () => void;
  onDelete: () => void;
};

const DESKTOP_MEDIA_QUERY = "(min-width: 1181px)";

function joinClassNames(...values: Array<string | undefined | false>) {
  return values.filter(Boolean).join(" ");
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

export function ContainerGraphProfileWorkspace({
  graph,
  shop,
  graphEmployees,
  slots,
  cellStyles,
  employeesById,
  isLoading,
  hasLoadError,
  isDeleting,
  isHeaderCollapsed = false,
  onEdit,
  onDelete,
}: ContainerGraphProfileWorkspaceProps) {
  const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(false);
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

  const columns = buildGraphMatrixColumns(graphEmployees, employeesById, slots);
  const cellMap = buildGraphCellMap(slots);
  const styleMap = buildGraphStyleMap(cellStyles);
  const dayConflictMap = buildGraphConflictDayMap(graph, slots);
  const totals = buildGraphTotals(graphEmployees, slots, employeesById);
  const summaryHeaders = buildGraphSummaryHeaders(graph.year, graph.month);
  const summaryRows = buildGraphSummaryRows(graph, graphEmployees, employeesById, slots);
  const note = getGraphVisibleNote(graph.note).trim();
  const hasNote = note.length > 0;
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
  const scheduleDetails = [
    { key: "month", label: "Month", value: getGraphMonthLabel(graph.year, graph.month) },
    { key: "year", label: "Year", value: String(graph.year) },
    { key: "shop", label: "Shop", value: shop?.name ?? `Shop ${graph.shopId}` },
  ] as const;
  const matrixCardShellStyle =
    isDesktopLayout && topRowCardMinHeight !== null
      ? {
        minHeight: `${topRowCardMinHeight}px`,
        height: `${topRowCardMinHeight}px`,
        maxHeight: `${topRowCardMinHeight}px`,
      }
      : undefined;
  const matrixCardStyle =
    isDesktopLayout
      ? { height: "100%", maxHeight: "100%" }
      : undefined;

  return (
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
              <ProfileSummaryCard
                className={styles.summaryCard}
                sectionTitle="Schedule Information"
                icon={<ScheduleDetailsIcon size={18} />}
                headerMeta={`ID ${graph.id}`}
                headerRightSlot={
                  <AvailabilitySidebarCollapseButton
                    label="Schedule Information"
                    onCollapse={() => setIsSidebarCollapsed(true)}
                  />
                }
                avatar={getGraphInitials(graph)}
                name={graph.name}
                contentAfterIdentity={
                  <div className={styles.summaryContent}>
                    <DetailList columns={1} className={styles.noteList}>
                      <DetailItem
                        label="Note"
                        value={note || <span className={styles.mutedValue}>No notes yet.</span>}
                        className={joinClassNames(styles.profileNoteItem, !hasNote && styles.profileNoteItemEmpty)}
                        valueClassName={joinClassNames(styles.profileNoteValue, !hasNote && styles.profileNoteValueEmpty)}
                      />
                    </DetailList>

                    <DetailList columns={3} className={styles.metricsList}>
                      {scheduleDetails.map(item => (
                        <DetailItem
                          key={item.key}
                          label={item.label}
                          value={item.value}
                          className={styles.profileMetricItem}
                          valueClassName={styles.profileMetricValue}
                        />
                      ))}
                    </DetailList>
                  </div>
                }
                actions={
                  <>
                    <IosButton label="Edit Schedule" onClick={onEdit} />
                    <IosButton
                      label={isDeleting ? "Deleting..." : "Delete Schedule"}
                      variant="secondary"
                      customColor="#ef4444"
                      customBorderColor="#ef4444"
                      disabled={isDeleting}
                      onClick={onDelete}
                    />
                  </>
                }
              />
            </div>
          </AvailabilitySidebarSection>
        </aside>

        <div className={styles.mainColumn}>
          <div className={styles.matrixCardShell} style={matrixCardShellStyle}>
            <ContainerGraphMatrix
              className={styles.matrixCard}
              style={matrixCardStyle}
              graph={graph}
              columns={columns}
              cellMap={cellMap}
              styleMap={styleMap}
              dayConflictMap={dayConflictMap}
              readOnly
              helperText="This schedule is read-only. Open edit to update details, assigned employees, matrix values or cell styles."
              headerRightSlot={
                <div className={styles.badges}>
                  <span className={styles.badge}>{`Total Employees: ${totals.totalEmployees}`}</span>
                  <span className={styles.badge}>{`Total Hours: ${totals.totalHoursText}`}</span>
                </div>
              }
            />
          </div>
        </div>
      </div>

      <CardSection
        className={styles.summaryCardSection}
        title="Schedule Summary"
        icon={<InformationIcon size={18} />}
        headerRightSlot={
          <div className={styles.summaryMeta}>
            <span className={styles.metaBadge}>{`Employees: ${totals.totalEmployees}`}</span>
            <span className={styles.metaBadge}>{`Hours: ${totals.totalHoursText}`}</span>
          </div>
        }
      >
        {summaryRows.length === 0 ? (
          <div className={styles.emptyState}>
            No employee schedule rows yet. Generate a schedule or assign matrix intervals to see the summary.
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
                {summaryRows.map(row => (
                  <tr key={row.employeeId}>
                    <td className={joinClassNames(styles.stickyColumn, styles.employeeValue)}>{row.employee}</td>
                    <td className={joinClassNames(styles.stickyColumnSecondary, styles.statValue)}>{row.workDays}</td>
                    <td className={joinClassNames(styles.stickyColumnTertiary, styles.statValue)}>{row.freeDays}</td>
                    <td className={joinClassNames(styles.stickyColumnQuaternary, styles.statValue)}>{row.sum || "0"}</td>
                    {row.days.map((day, index) => (
                      <Fragment key={`${row.employeeId}-${index}`}>
                        <td>{day.from || "-"}</td>
                        <td>{day.to || "-"}</td>
                        <td>{day.hours || "-"}</td>
                      </Fragment>
                    ))}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </CardSection>
    </div>
  );
}
