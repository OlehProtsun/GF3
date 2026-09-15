import { t } from "@shared/i18n";
import type { Shop } from "@entities/shops/model/types";
import { getShopDisplayName } from "@entities/shops/model/presentation";
import { usePinnedRecords } from "@shared/lib/records/usePinnedRecords";
import { IosButton } from "@shared/ui/components/IosButton";
import { ListCardSection } from "@shared/ui/components/ListCardSection";
import { RecordGrid } from "@shared/ui/components/RecordGrid";
import type { RecordTileMetaItem } from "@shared/ui/components/RecordTile";
import { RecordTile } from "@shared/ui/components/RecordTile";
import { PlusIcon } from "@shared/ui/icons";

type ShopListCardProps = {
  shops: Shop[];
  error?: unknown;
  isLoading: boolean;
  searchQuery: string;
  onClearSearch: () => void;
  onAddShop: () => void;
  onShopOpen: (shopId: Shop["id"]) => void;
};

export function ShopListCard({
  shops,
  error,
  isLoading,
  searchQuery,
  onClearSearch,
  onAddShop,
  onShopOpen,
}: ShopListCardProps) {
  const { sortedItems: sortedShops, pinnedIdSet, togglePin } = usePinnedRecords(
    "shops:list:pinned",
    shops
  );

  const addShopAction = (
    <IosButton label={t("Add New")} icon={<PlusIcon size={18} />} onClick={onAddShop} />
  );

  return (
    <ListCardSection
      error={error}
      isFetching={isLoading}
      hasData={shops.length > 0}
      searchQuery={searchQuery}
      loadingMessage="Loading shops..."
      errorMessage="Could not load shops."
      emptyTitle="No shops yet"
      emptyDescription="Start by creating your first shop record."
      emptyAction={addShopAction}
      searchEmptyTitle="Nothing found"
      searchEmptyDescription={t("No shop matches \"{0}\".", searchQuery)}
      searchEmptyAction={
        <IosButton label={t("Clear Search")} variant="secondary" onClick={onClearSearch} />
      }
    >
      <RecordGrid>
        {sortedShops.map(shop => {
          const displayName = getShopDisplayName(shop);
          const isPinned = pinnedIdSet.has(String(shop.id));
          const metaItems: RecordTileMetaItem[] = [
            {
              key: "address",
              label: t("Address"),
              value: shop.address.trim() || t("Not provided"),
            },
          ];

          return (
            <RecordTile
              key={shop.id}
              title={displayName}
              description={shop.description?.trim() || undefined}
              badge={`ID ${shop.id}`}
              metaItems={metaItems}
              isPinned={isPinned}
              onTogglePin={() => togglePin(shop.id)}
              pinLabel={isPinned ? t("Unpin {0}", displayName) : t("Pin {0}", displayName)}
              onClick={() => onShopOpen(shop.id)}
              ariaLabel={t("Open {0} profile", displayName)}
            />
          );
        })}
      </RecordGrid>
    </ListCardSection>
  );
}
