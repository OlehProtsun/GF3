import { t } from "@shared/i18n";
import type { ReactNode } from "react";
import type { AvailabilityGroup } from "@entities/availability-groups/model/types";
import {
  formatAvailabilityDateTimeLabel,
  getAvailabilityGroupPeriodLabel,
  getAvailabilityMonthLabel,
  getAvailabilityPublicationStatusLabel,
  normalizeAvailabilityPublicationStatus,
} from "@entities/availability-groups/model/presentation";
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
  const publicationStatus = normalizeAvailabilityPublicationStatus(group.publicationStatus);
  const hasVisibilityWindow = Boolean(group.visibleFromUtc || group.visibleToUtc);
  const publicationOverview = (
    <div className={styles.publicationOverview}>
      <div className={styles.publicationStatusPanel}>
        <span className={styles.publicationLabel}>{t("Status")}</span>
        <span className={[styles.statusBadge, publicationStatus === "public" ? styles.statusBadgePublic : ""].filter(Boolean).join(" ")}>
          {getAvailabilityPublicationStatusLabel(group.publicationStatus)}
        </span>
      </div>

      <div className={styles.visibilityPanel}>
        <span className={styles.publicationLabel}>{t("Visible")}</span>
        {hasVisibilityWindow ? (
          <div className={styles.visibilityTimeline}>
            <span className={styles.visibilityEndpoint}>
              <small>{t("From")}</small>
              <strong>{formatAvailabilityDateTimeLabel(group.visibleFromUtc)}</strong>
            </span>
            <span className={styles.visibilityLine} aria-hidden="true" />
            <span className={styles.visibilityEndpoint}>
              <small>{t("To")}</small>
              <strong>{formatAvailabilityDateTimeLabel(group.visibleToUtc)}</strong>
            </span>
          </div>
        ) : (
          <span className={styles.visibilityEmpty}>{t("Not configured")}</span>
        )}
      </div>
    </div>
  );
  const detailItems = [
    { key: "month", label: t("Month"), value: getAvailabilityMonthLabel(group.month) },
    { key: "year", label: t("Year"), value: String(group.year) },
    { key: "employees", label: t("Employees"), value: String(employeeCount) },
  ];

  return (
    <ProfileSummaryCard
      className={styles.card}
      sectionTitle="Availability Information"
      icon={<AvailabilityIcon size={18} />}
      headerMeta={`ID ${group.id}`}
      headerRightWrap="nowrap"
      headerRightSlot={headerRightSlot}
      name={group.name}
      subtitle={getAvailabilityGroupPeriodLabel(group)}
      statusContent={publicationOverview}
      details={detailItems}
      actions={
        <>
          <IosButton label={t("Edit")} onClick={onEdit} />
          <IosButton
            label={isDeleting ? t("Deleting...") : t("Delete")}
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
