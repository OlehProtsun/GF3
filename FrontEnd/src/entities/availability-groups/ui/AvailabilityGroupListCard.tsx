import { t } from "@shared/i18n";
import type { AvailabilityGroup } from "@entities/availability-groups/model/types";
import {
  getAvailabilityGroupPeriodLabel,
  getAvailabilityMonthLabel,
  getAvailabilityPublicationStatusLabel,
  getAvailabilityWindowStatusLabel,
} from "@entities/availability-groups/model/presentation";
import { usePinnedRecords } from "@shared/lib/records/usePinnedRecords";
import { IosButton } from "@shared/ui/components/IosButton";
import { ListCardSection } from "@shared/ui/components/ListCardSection";
import { RecordGrid } from "@shared/ui/components/RecordGrid";
import type { RecordTileMetaItem } from "@shared/ui/components/RecordTile";
import { RecordTile } from "@shared/ui/components/RecordTile";
import { PlusIcon } from "@shared/ui/icons";
import styles from "./AvailabilityGroupListCard.module.css";

type AvailabilityGroupListCardProps = {
  groups: AvailabilityGroup[];
  error?: unknown;
  isLoading: boolean;
  searchQuery: string;
  onClearSearch: () => void;
  onAddGroup: () => void;
  onOpenGroup: (groupId: number) => void;
};

export function AvailabilityGroupListCard({
  groups,
  error,
  isLoading,
  searchQuery,
  onClearSearch,
  onAddGroup,
  onOpenGroup,
}: AvailabilityGroupListCardProps) {
  const { sortedItems: sortedGroups, pinnedIdSet, togglePin } = usePinnedRecords(
    "availability-groups:list:pinned",
    groups
  );

  const addAction = <IosButton label={t("Add New")} icon={<PlusIcon size={18} />} onClick={onAddGroup} />;

  return (
    <ListCardSection
      error={error}
      isFetching={isLoading}
      hasData={groups.length > 0}
      searchQuery={searchQuery}
      loadingMessage="Loading availability groups..."
      errorMessage="Could not load availability groups."
      emptyTitle="No availability groups yet"
      emptyDescription="Start by creating your first availability group."
      emptyAction={addAction}
      searchEmptyTitle="Nothing found"
      searchEmptyDescription={t("No availability group matches \"{0}\".", searchQuery)}
      searchEmptyAction={<IosButton label={t("Clear Search")} variant="secondary" onClick={onClearSearch} />}
    >
      <RecordGrid>
        {sortedGroups.map(group => {
          const isPinned = pinnedIdSet.has(String(group.id));
          const periodLabel = getAvailabilityGroupPeriodLabel(group);
          const windowStatusLabel = getAvailabilityWindowStatusLabel(group);
          const publicationStatusLabel = getAvailabilityPublicationStatusLabel(group.publicationStatus);
          const metaItems: RecordTileMetaItem[] = [
            { key: "month", label: t("Month"), value: getAvailabilityMonthLabel(group.month, "short") },
            { key: "year", label: t("Year"), value: String(group.year) },
            {
              key: "window-status",
              label: t("Status"),
              value: (
                <span className={[
                  styles.statusPill,
                  windowStatusLabel === "Open" ? styles.statusPillOpen : styles.statusPillClosed,
                ].filter(Boolean).join(" ")}
                >
                  {windowStatusLabel}
                </span>
              ),
            },
            {
              key: "publication-status",
              label: t("Public"),
              value: (
                <span className={[
                  styles.statusPill,
                  publicationStatusLabel === "Public" ? styles.publicationPillPublic : styles.publicationPillPrivate,
                ].filter(Boolean).join(" ")}
                >
                  {publicationStatusLabel}
                </span>
              ),
            },
          ];

          return (
            <RecordTile
              key={group.id}
              title={group.name}
              description={t("Availability schedule for {0}", periodLabel)}
              badge={`ID ${group.id}`}
              metaItems={metaItems}
              isPinned={isPinned}
              onTogglePin={() => togglePin(group.id)}
              pinLabel={isPinned ? t("Unpin {0}", group.name) : t("Pin {0}", group.name)}
              onClick={() => onOpenGroup(group.id)}
              ariaLabel={t("Open availability group {0}", group.name)}
            />
          );
        })}
      </RecordGrid>
    </ListCardSection>
  );
}
