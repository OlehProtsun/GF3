import type { AvailabilityGroup } from "@entities/availability-groups/model/types";
import {
  getAvailabilityGroupPeriodLabel,
  getAvailabilityMonthLabel,
} from "@entities/availability-groups/model/presentation";
import { usePinnedRecords } from "@shared/lib/records/usePinnedRecords";
import { IosButton } from "@shared/ui/components/IosButton";
import { ListCardSection } from "@shared/ui/components/ListCardSection";
import { RecordGrid } from "@shared/ui/components/RecordGrid";
import type { RecordTileMetaItem } from "@shared/ui/components/RecordTile";
import { RecordTile } from "@shared/ui/components/RecordTile";
import { PlusIcon } from "@shared/ui/icons";

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

  const addAction = <IosButton label="Add New" icon={<PlusIcon size={18} />} onClick={onAddGroup} />;

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
      searchEmptyDescription={`No availability group matches "${searchQuery}".`}
      searchEmptyAction={<IosButton label="Clear Search" variant="secondary" onClick={onClearSearch} />}
    >
      <RecordGrid>
        {sortedGroups.map(group => {
          const isPinned = pinnedIdSet.has(String(group.id));
          const periodLabel = getAvailabilityGroupPeriodLabel(group);
          const metaItems: RecordTileMetaItem[] = [
            { key: "month", label: "Month", value: getAvailabilityMonthLabel(group.month, "short") },
            { key: "year", label: "Year", value: String(group.year) },
          ];

          return (
            <RecordTile
              key={group.id}
              title={group.name}
              description={`Availability schedule for ${periodLabel}`}
              badge={`ID ${group.id}`}
              metaItems={metaItems}
              isPinned={isPinned}
              onTogglePin={() => togglePin(group.id)}
              pinLabel={isPinned ? `Unpin ${group.name}` : `Pin ${group.name}`}
              onClick={() => onOpenGroup(group.id)}
              ariaLabel={`Open availability group ${group.name}`}
            />
          );
        })}
      </RecordGrid>
    </ListCardSection>
  );
}
