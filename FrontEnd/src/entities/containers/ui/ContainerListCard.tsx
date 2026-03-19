import type { Container } from "@entities/containers/model/types";
import { getContainerDisplayName } from "@entities/containers/model/presentation";
import { usePinnedRecords } from "@shared/lib/records/usePinnedRecords";
import { IosButton } from "@shared/ui/components/IosButton";
import { ListCardSection } from "@shared/ui/components/ListCardSection";
import { RecordGrid } from "@shared/ui/components/RecordGrid";
import type { RecordTileMetaItem } from "@shared/ui/components/RecordTile";
import { RecordTile } from "@shared/ui/components/RecordTile";
import { PlusIcon } from "@shared/ui/icons";

type ContainerListCardProps = {
  containers: Container[];
  error?: unknown;
  isLoading: boolean;
  searchQuery: string;
  onClearSearch: () => void;
  onAddContainer: () => void;
  onContainerOpen: (containerId: Container["id"]) => void;
};

export function ContainerListCard({
  containers,
  error,
  isLoading,
  searchQuery,
  onClearSearch,
  onAddContainer,
  onContainerOpen,
}: ContainerListCardProps) {
  const { sortedItems: sortedContainers, pinnedIdSet, togglePin } = usePinnedRecords(
    "containers:list:pinned",
    containers,
  );

  const addContainerAction = (
    <IosButton label="Add New" icon={<PlusIcon size={18} />} onClick={onAddContainer} />
  );

  return (
    <ListCardSection
      error={error}
      isFetching={isLoading}
      hasData={containers.length > 0}
      searchQuery={searchQuery}
      loadingMessage="Loading containers..."
      errorMessage="Could not load containers."
      emptyTitle="No containers yet"
      emptyDescription="Start by creating your first container workspace."
      emptyAction={addContainerAction}
      searchEmptyTitle="Nothing found"
      searchEmptyDescription={`No container matches "${searchQuery}".`}
      searchEmptyAction={
        <IosButton label="Clear Search" variant="secondary" onClick={onClearSearch} />
      }
    >
      <RecordGrid>
        {sortedContainers.map(container => {
          const displayName = getContainerDisplayName(container);
          const note = container.note?.trim();
          const isPinned = pinnedIdSet.has(String(container.id));
          const metaItems: RecordTileMetaItem[] = [
            {
              key: "noteState",
              label: "State",
              value: note ? "Note available" : "No note yet",
            },
          ];

          return (
            <RecordTile
              key={container.id}
              title={displayName}
              description={note || "Schedule workspace for container planning and statistics."}
              badge={`ID ${container.id}`}
              metaItems={metaItems}
              isPinned={isPinned}
              onTogglePin={() => togglePin(container.id)}
              pinLabel={isPinned ? `Unpin ${displayName}` : `Pin ${displayName}`}
              onClick={() => onContainerOpen(container.id)}
              ariaLabel={`Open ${displayName} profile`}
            />
          );
        })}
      </RecordGrid>
    </ListCardSection>
  );
}
