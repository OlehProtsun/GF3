import { t } from "@shared/i18n";
import type { Shop } from "@entities/shops/model/types";
import {
  getShopDetailsState,
  getShopDisplayName,
  getShopInitials,
  getShopProfileDetails,
} from "@entities/shops/model/presentation";
import { IosButton } from "@shared/ui/components/IosButton";
import { RecordProfileCard, renderRecordDetailValue } from "@shared/ui/components/RecordProfileCard";
import { type ProfileSummaryDetail } from "@shared/ui/components/ProfileSummaryCard";
import { ShopIcon } from "@shared/ui/icons";

type ShopProfileCardProps = {
  shop?: Shop;
  isLoading: boolean;
  hasLoadError: boolean;
  isDeleting: boolean;
  onEditShop: (shopId: Shop["id"]) => void;
  onDeleteShop: () => void;
};

export function ShopProfileCard({
  shop,
  isLoading,
  hasLoadError,
  isDeleting,
  onEditShop,
  onDeleteShop,
}: ShopProfileCardProps) {
  const displayName = getShopDisplayName(shop, t("Shop Profile"));
  const initials = getShopInitials(shop);
  const detailsState = getShopDetailsState(shop);
  const details: ProfileSummaryDetail[] = shop
    ? getShopProfileDetails(shop).map(item => ({
        key: item.key,
        label: item.label,
        value: renderRecordDetailValue(item.value),
      }))
    : [];

  return (
    <RecordProfileCard
      sectionTitle={t("Shop Profile")}
      icon={<ShopIcon size={18} style={{ transform: "scaleY(-1)" }} />}
      headerMeta={shop ? `ID ${shop.id}` : undefined}
      isLoading={isLoading}
      hasLoadError={hasLoadError}
      loadingMessage={t("Loading shop details...")}
      errorMessage={t("Could not load shop.")}
      avatar={shop ? initials : undefined}
      name={shop ? displayName : undefined}
      subtitle={shop ? detailsState : undefined}
      details={details}
      actions={
        shop ? (
          <>
            <IosButton label={t("Edit Shop")} onClick={() => onEditShop(shop.id)} />
            <IosButton
              label={isDeleting ? t("Deleting...") : t("Delete Shop")}
              variant="secondary"
              customColor="#ef4444"
              customBorderColor="#ef4444"
              onClick={onDeleteShop}
              disabled={isDeleting}
            />
          </>
        ) : undefined
      }
    />
  );
}
