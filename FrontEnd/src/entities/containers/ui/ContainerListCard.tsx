import { t } from "@shared/i18n";
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
  onAddContainer?: () => void;
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

  const addContainerAction = onAddContainer ? (
    <IosButton label={t("Add New")} icon={<PlusIcon size={18} />} onClick={onAddContainer} />
  ) : undefined;

  return (
    <ListCardSection
      error={error}
      isFetching={isLoading}
      hasData={containers.length > 0}
      searchQuery={searchQuery}
      loadingMessage={t("Loading containers...")}
      errorMessage={t("Could not load containers.")}
      emptyTitle={t("No containers yet")}
      emptyDescription={onAddContainer ? t("Start by creating your first container workspace.") : t("No results")}
      emptyAction={addContainerAction}
      searchEmptyTitle={t("Nothing found")}
      searchEmptyDescription={t("No container matches \"{0}\".", searchQuery)}
      searchEmptyAction={
        <IosButton label={t("Clear Search")} variant="secondary" onClick={onClearSearch} />
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
              label: t("State"),
              value: note ? t("Note available") : t("No note yet"),
            },
          ];

          return (
            <RecordTile
              key={container.id}
              title={displayName}
              description={note || t("Schedule workspace for container planning and statistics.")}
              badge={`ID ${container.id}`}
              metaItems={metaItems}
              isPinned={isPinned}
              onTogglePin={() => togglePin(container.id)}
              pinLabel={isPinned ? t("Unpin {0}", displayName) : t("Pin {0}", displayName)}
              onClick={() => onContainerOpen(container.id)}
              ariaLabel={t("Open {0} profile", displayName)}
            />
          );
        })}
      </RecordGrid>
    </ListCardSection>
  );
}
