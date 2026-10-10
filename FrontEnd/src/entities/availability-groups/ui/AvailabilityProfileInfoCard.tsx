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
import { CardSection } from "@shared/ui/sections/CardSection";
import { AvailabilityIcon, CheckIcon, EmployeeIcon, ScheduleIcon } from "@shared/ui/icons";
import styles from "./AvailabilityProfileInfoCard.module.css";

type AvailabilityProfileInfoCardProps = {
  showManagementActions?: boolean;
  group: AvailabilityGroup;
  employeeCount: number;
  isDeleting: boolean;
  headerRightSlot?: ReactNode;
  onEdit: () => void;
  onDelete: () => void;
};

export function AvailabilityProfileInfoCard({
  showManagementActions = true,
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
        {!showManagementActions && <span className={styles.phoneDetailIcon} aria-hidden="true"><CheckIcon size={16} /></span>}
        <span className={styles.publicationLabel}>{t("Status")}</span>
        <span className={[styles.statusBadge, publicationStatus === "public" ? styles.statusBadgePublic : ""].filter(Boolean).join(" ")}>
          {getAvailabilityPublicationStatusLabel(group.publicationStatus)}
        </span>
      </div>

      <div className={styles.visibilityPanel}>
        {!showManagementActions && <span className={styles.phoneDetailIcon} aria-hidden="true"><ScheduleIcon size={16} /></span>}
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

  if (!showManagementActions) {
    const nameParts = group.name.trim().split(/\s+/).filter(Boolean);
    const initials = nameParts.length ? `${nameParts[0][0]}${nameParts[1]?.[0] ?? nameParts[0][1] ?? ""}`.toUpperCase() : "?";
    return <CardSection className={styles.phoneCard} title={t("Availability Information")}
      titleClassName={styles.phoneTitle} icon={<AvailabilityIcon size={18} />}>
      <div className={styles.phoneContent}>
        <div className={styles.phoneIdentity}>
          <span className={styles.phoneAvatar} aria-hidden="true">{initials}</span>
          <div className={styles.phoneIdentityText}>
            <h2>{group.name}</h2>
            <p>{`ID ${group.id}`}</p>
            <p>{getAvailabilityGroupPeriodLabel(group)}</p>
          </div>
        </div>
        <div className={styles.phoneDetailsGrid}>
          {detailItems.map(item => {
            const Icon = item.key === "employees" ? EmployeeIcon : ScheduleIcon;
            return <div className={styles.phoneDetailTile} key={item.key}>
              <span className={styles.phoneDetailIcon} aria-hidden="true"><Icon size={16} /></span>
              <span className={styles.publicationLabel}>{item.label}</span>
              <strong>{item.value}</strong>
            </div>;
          })}
          {publicationOverview}
        </div>
      </div>
    </CardSection>;
  }

  return (
    <ProfileSummaryCard
      className={styles.card}
      sectionTitle={t("Availability Information")}
      icon={<AvailabilityIcon size={18} />}
      headerMeta={`ID ${group.id}`}
      headerRightWrap="nowrap"
      headerRightSlot={headerRightSlot}
      name={group.name}
      subtitle={getAvailabilityGroupPeriodLabel(group)}
      statusContent={publicationOverview}
      details={detailItems}
      actions={showManagementActions ? (
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
      ) : undefined}
    />
  );
}
