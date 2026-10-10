import { t } from "@shared/i18n";
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
  showManagementActions?: boolean;
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
  showManagementActions = true,
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
  const phoneReadOnly = !showManagementActions;
  const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(false);

  if (isLoading) {
    return <div className={styles.state}>{t("Loading availability profile...")}</div>;
  }

  if (hasLoadError || !group) {
    return <ErrorBanner className={styles.banner}>{t("Could not load this availability group.")}</ErrorBanner>;
  }

  if (phoneReadOnly) {
    return <div className={styles.phoneLayout}>
      <details className={styles.phoneInfo}>
        <summary>{t("Availability Information")}</summary>
        <AvailabilityProfileInfoCard showManagementActions={false} group={group} employeeCount={columns.length}
          isDeleting={isDeleting} onEdit={onEdit} onDelete={onDelete} />
      </details>
      <AvailabilityScheduleMatrix className={styles.matrixCard} year={group.year} month={group.month}
        columns={columns} cellMap={cellMap} visualHintMap={visualHintMap} readOnly compactSize mobileReadOnlyViewport
        title={t("Availability Schedule")} helperText={t("This schedule is read-only.")} onVisualHintClick={onVisualHintClick} />
    </div>;
  }

  return (
    <AvailabilityWorkspaceLayout
      className={joinClassNames(isSidebarCollapsed && styles.layoutCollapsed, phoneReadOnly && styles.phoneLayout)}
      sidebarColumnClassName={joinClassNames(styles.sidebarColumn, isSidebarCollapsed && styles.sidebarColumnCollapsed)}
      sidebarContentClassName={joinClassNames(styles.sidebar, isSidebarCollapsed && styles.sidebarCollapsed)}
      mainColumnClassName={joinClassNames(styles.mainColumn, isSidebarCollapsed && styles.mainColumnCollapsed)}
      mainBlockClassName={styles.mainBlock}
      sidebar={
        <AvailabilitySidebarSection
          label={t("Availability Information")}
          collapsed={isSidebarCollapsed}
          collapsedOffset="compact"
          onExpand={() => setIsSidebarCollapsed(false)}
        >
          <AvailabilityProfileInfoCard
            showManagementActions={showManagementActions}
            group={group}
            employeeCount={columns.length}
            isDeleting={isDeleting}
            headerRightSlot={
              <AvailabilitySidebarCollapseButton
                label={t("Availability Information")}
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
          compactSize={phoneReadOnly}
          mobileReadOnlyViewport={phoneReadOnly}
          title={t("Availability Schedule")}
          helperText={showManagementActions ? t("This schedule is read-only. Open edit if you want to update assigned employees or day codes.") : t("This schedule is read-only.")}
          onVisualHintClick={onVisualHintClick}
        />
      }
    />
  );
}
