import type { ReactNode } from "react";
import type { AvailabilityGroup } from "@entities/availability-groups/model/types";
import { getAvailabilityGroupPeriodLabel, getAvailabilityMonthLabel } from "@entities/availability-groups/model/presentation";
import { IosButton } from "@shared/ui/components/IosButton";
import { ProfileSummaryCard } from "@shared/ui/components/ProfileSummaryCard";
import { AvailabilityIcon } from "@shared/ui/icons";
import styles from "./AvailabilityProfileInfoCard.module.css";

type AvailabilityProfileInfoCardProps = {
  group: AvailabilityGroup;
  employeeCount: number;
  isDeleting: boolean;
  headerRightSlot?: ReactNode;
  onEdit: () => void;
  onDelete: () => void;
};

export function AvailabilityProfileInfoCard({
  group,
  employeeCount,
  isDeleting,
  headerRightSlot,
  onEdit,
  onDelete,
}: AvailabilityProfileInfoCardProps) {
  const detailItems = [
    { key: "month", label: "Month", value: getAvailabilityMonthLabel(group.month) },
    { key: "year", label: "Year", value: String(group.year) },
    { key: "employees", label: "Employees", value: String(employeeCount) },
  ];

  return (
    <ProfileSummaryCard
      className={styles.card}
      sectionTitle="Availability Information"
      icon={<AvailabilityIcon size={18} style={{ transform: "scaleY(-1)" }} />}
      headerMeta={`ID ${group.id}`}
      headerRightWrap="nowrap"
      headerRightSlot={headerRightSlot}
      name={group.name}
      subtitle={getAvailabilityGroupPeriodLabel(group)}
      details={detailItems}
      actions={
        <>
          <IosButton label="Edit" onClick={onEdit} />
          <IosButton
            label={isDeleting ? "Deleting..." : "Delete"}
            variant="secondary"
            customColor="#dc2626"
            customBorderColor="#dc2626"
            onClick={onDelete}
            disabled={isDeleting}
          />
        </>
      }
    />
  );
}
