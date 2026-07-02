import { useState } from "react";
import type { AvailabilityGroup } from "@entities/availability-groups/model/types";
import { type AvailabilityMatrixCellMap, type AvailabilityMatrixColumn } from "@entities/availability-groups/model/editor";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { AvailabilityScheduleMatrix } from "./AvailabilityScheduleMatrix";
import { AvailabilityProfileInfoCard } from "./AvailabilityProfileInfoCard";
import { AvailabilitySidebarCollapseButton, AvailabilitySidebarSection } from "./AvailabilitySidebarSection";
import { AvailabilityWorkspaceLayout } from "./AvailabilityWorkspaceLayout";
import styles from "./AvailabilityGroupProfileCard.module.css";

type AvailabilityGroupProfileCardProps = {
  group?: AvailabilityGroup | null;
  columns: AvailabilityMatrixColumn[];
  cellMap: AvailabilityMatrixCellMap;
  visualHintMap?: AvailabilityMatrixCellMap;
  isLoading: boolean;
  hasLoadError: boolean;
  isDeleting: boolean;
  onEdit: () => void;
  onDelete: () => void;
  onVisualHintClick?: (employeeId: number, dayOfMonth: number) => void;
};

function joinClassNames(...values: Array<string | undefined | false>) {
  return values.filter(Boolean).join(" ");
}

export function AvailabilityGroupProfileCard({
  group,
  columns,
  cellMap,
  visualHintMap,
  isLoading,
  hasLoadError,
  isDeleting,
  onEdit,
  onDelete,
  onVisualHintClick,
}: AvailabilityGroupProfileCardProps) {
  const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(false);

  if (isLoading) {
    return <div className={styles.state}>Loading availability profile...</div>;
  }

  if (hasLoadError || !group) {
    return <ErrorBanner className={styles.banner}>Could not load this availability group.</ErrorBanner>;
  }

  return (
    <AvailabilityWorkspaceLayout
      className={isSidebarCollapsed ? styles.layoutCollapsed : undefined}
      sidebarColumnClassName={joinClassNames(styles.sidebarColumn, isSidebarCollapsed && styles.sidebarColumnCollapsed)}
      sidebarContentClassName={joinClassNames(styles.sidebar, isSidebarCollapsed && styles.sidebarCollapsed)}
      mainColumnClassName={joinClassNames(styles.mainColumn, isSidebarCollapsed && styles.mainColumnCollapsed)}
      mainBlockClassName={styles.mainBlock}
      sidebar={
        <AvailabilitySidebarSection
          label="Availability Information"
          collapsed={isSidebarCollapsed}
          collapsedOffset="compact"
          onExpand={() => setIsSidebarCollapsed(false)}
        >
          <AvailabilityProfileInfoCard
            group={group}
            employeeCount={columns.length}
            isDeleting={isDeleting}
            headerRightSlot={
              <AvailabilitySidebarCollapseButton
                label="Availability Information"
                onCollapse={() => setIsSidebarCollapsed(true)}
              />
            }
            onEdit={onEdit}
            onDelete={onDelete}
          />
        </AvailabilitySidebarSection>
      }
      main={
        <AvailabilityScheduleMatrix
          className={styles.matrixCard}
          year={group.year}
          month={group.month}
          columns={columns}
          cellMap={cellMap}
          visualHintMap={visualHintMap}
          readOnly
          title="Availability Schedule"
          helperText="This schedule is read-only. Open edit if you want to update assigned employees or day codes."
          onVisualHintClick={onVisualHintClick}
        />
      }
    />
  );
}
