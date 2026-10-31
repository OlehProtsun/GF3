import { t } from "@shared/i18n";
import type { Container, Graph } from "@entities/containers/model/types";
import { usePinnedRecords } from "@shared/lib/records/usePinnedRecords";
import { IosButton } from "@shared/ui/components/IosButton";
import { ListCardSection } from "@shared/ui/components/ListCardSection";
import { RecordGrid } from "@shared/ui/components/RecordGrid";
import { ContainerFolderTile } from "./ContainerFolderTile";
import styles from "./ContainerListCard.module.css";
import { PlusIcon } from "@shared/ui/icons";

type ContainerListCardProps = {
  containers: Container[];
  error?: unknown;
  isLoading: boolean;
  searchQuery: string;
  onClearSearch: () => void;
  onAddContainer?: () => void;
  onContainerOpen: (containerId: Container["id"]) => void;
  onGraphOpen: (containerId: Container["id"], graphId: Graph["id"]) => void;
};

export function ContainerListCard({
  containers,
  error,
  isLoading,
  searchQuery,
  onClearSearch,
  onAddContainer,
  onContainerOpen,
  onGraphOpen,
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
      <RecordGrid className={styles.folderGrid}>
        {sortedContainers.map(container => (
          <ContainerFolderTile
            key={container.id}
            container={container}
            isPinned={pinnedIdSet.has(String(container.id))}
            onTogglePin={togglePin}
            onContainerOpen={onContainerOpen}
            onGraphOpen={onGraphOpen}
          />
        ))}
      </RecordGrid>
    </ListCardSection>
  );
}
