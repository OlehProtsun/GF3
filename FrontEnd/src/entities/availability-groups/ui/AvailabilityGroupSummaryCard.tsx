import type { AvailabilityGroup, AvailabilityGroupMember, AvailabilitySlot } from "@entities/availability-groups/model/types";
import {
  getAvailabilityGroupPeriodLabel,
  getAvailabilityKindLabel,
  getAvailabilityMemberNames,
  summarizeAvailabilitySlots,
} from "@entities/availability-groups/model/presentation";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { IosButton } from "@shared/ui/components/IosButton";
import { DetailItem, DetailList } from "@shared/ui/components/DetailList";
import { AvailabilityIcon } from "@shared/ui/icons";
import { CardSection } from "@shared/ui/sections/CardSection";
import styles from "./AvailabilityGroupSummaryCard.module.css";

type AvailabilityGroupSummaryCardProps = {
  group?: AvailabilityGroup | null;
  members: AvailabilityGroupMember[];
  slots: AvailabilitySlot[];
  employeeNameById: Map<number, string>;
  isLoading: boolean;
  hasLoadError: boolean;
  onEditGroup: () => void;
  onDeleteGroup: () => void;
};

export function AvailabilityGroupSummaryCard({
  group,
  members,
  slots,
  employeeNameById,
  isLoading,
  hasLoadError,
  onEditGroup,
  onDeleteGroup,
}: AvailabilityGroupSummaryCardProps) {
  const slotSummary = summarizeAvailabilitySlots(slots);
  const memberNames = getAvailabilityMemberNames(members, employeeNameById);
  const topKind =
    slots.length === 0
      ? "No slots yet"
      : slotSummary.interval > 0
        ? getAvailabilityKindLabel("Preferred")
        : slotSummary.any > 0
          ? getAvailabilityKindLabel("Available")
          : slotSummary.none > 0
            ? getAvailabilityKindLabel("Unavailable")
            : "Mixed coverage";

  return (
    <CardSection
      className={styles.card}
      title="Availability Summary"
      icon={<AvailabilityIcon size={18} />}
      headerRightSlot={group ? <span className={styles.headerBadge}>ID {group.id}</span> : null}
    >
      <div className={styles.layout}>
        {isLoading ? <p className={styles.stateText}>Loading selected availability group...</p> : null}
        {hasLoadError ? <ErrorBanner>Could not load selected availability group.</ErrorBanner> : null}

        {!group && !isLoading && !hasLoadError ? (
          <div className={styles.emptyState}>
            <h3>Select an availability group</h3>
            <p>Choose a record from the list to inspect its period, members and slot coverage.</p>
          </div>
        ) : null}

        {group ? (
          <>
            <div className={styles.hero}>
              <div className={styles.heroText}>
                <h2 className={styles.name}>{group.name}</h2>
                <p className={styles.subtitle}>{getAvailabilityGroupPeriodLabel(group)}</p>
              </div>

              <span className={styles.periodPill}>{topKind}</span>
            </div>

            <DetailList className={styles.detailList} columns={3}>
              <DetailItem label="Members" value={String(members.length)} className={styles.detailItem} valueClassName={styles.detailValue} />
              <DetailItem label="Slots" value={String(slots.length)} className={styles.detailItem} valueClassName={styles.detailValue} />
              <DetailItem label="Custom" value={String(slotSummary.interval)} className={styles.detailItem} valueClassName={styles.detailValue} />
              <DetailItem label="Any Shift" value={String(slotSummary.any)} className={styles.detailItem} valueClassName={styles.detailValue} />
              <DetailItem label="Unavailable" value={String(slotSummary.none)} className={styles.detailItem} valueClassName={styles.detailValue} />
              <DetailItem label="Period" value={getAvailabilityGroupPeriodLabel(group, "compact")} className={styles.detailItem} valueClassName={styles.detailValue} />
            </DetailList>

            <div className={styles.membersBlock}>
              <div className={styles.membersHeader}>
                <span>Assigned employees</span>
                <span>{memberNames.length}</span>
              </div>

              {memberNames.length > 0 ? (
                <div className={styles.memberList}>
                  {memberNames.map(name => (
                    <span key={name} className={styles.memberChip}>
                      {name}
                    </span>
                  ))}
                </div>
              ) : (
                <p className={styles.memberEmpty}>No employees assigned to this group yet.</p>
              )}
            </div>

            <div className={styles.actions}>
              <IosButton label="Edit Group" onClick={onEditGroup} />
              <IosButton
                label="Delete Group"
                variant="secondary"
                customColor="#ef4444"
                customBorderColor="#ef4444"
                onClick={onDeleteGroup}
              />
            </div>
          </>
        ) : null}
      </div>
    </CardSection>
  );
}
