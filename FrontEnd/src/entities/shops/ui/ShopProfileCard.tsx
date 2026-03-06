import type { Shop } from "@entities/shops/model/types";
import {
  getShopDetailsState,
  getShopDisplayName,
  getShopInitials,
  getShopProfileDetails,
} from "@entities/shops/model/presentation";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { IosButton } from "@shared/ui/components/IosButton";
import {
  ProfileSummaryCard,
  type ProfileSummaryDetail,
} from "@shared/ui/components/ProfileSummaryCard";
import { ShopIcon } from "@shared/ui/icons";
import styles from "./ShopProfileCard.module.css";

type ShopProfileCardProps = {
  shop?: Shop;
  isLoading: boolean;
  hasLoadError: boolean;
  isDeleting: boolean;
  onEditShop: (shopId: Shop["id"]) => void;
  onDeleteShop: () => void;
};

function renderDetailValue(value?: string | null) {
  if (!value) {
    return <span className={styles.mutedValue}>Not provided</span>;
  }

  return value;
}

export function ShopProfileCard({
  shop,
  isLoading,
  hasLoadError,
  isDeleting,
  onEditShop,
  onDeleteShop,
}: ShopProfileCardProps) {
  const displayName = getShopDisplayName(shop, "Shop Profile");
  const initials = getShopInitials(shop);
  const detailsState = getShopDetailsState(shop);
  const details: ProfileSummaryDetail[] = shop
    ? getShopProfileDetails(shop).map(item => ({
        key: item.key,
        label: item.label,
        value: renderDetailValue(item.value),
      }))
    : [];

  return (
    <ProfileSummaryCard
      sectionTitle="Shop Profile"
      icon={<ShopIcon size={18} />}
      headerMeta={shop ? `ID ${shop.id}` : undefined}
      statusContent={
        <>
          {isLoading ? <p className={styles.loading}>Loading shop details...</p> : null}
          {hasLoadError ? <ErrorBanner>Could not load shop.</ErrorBanner> : null}
        </>
      }
      avatar={shop ? initials : undefined}
      name={shop ? displayName : undefined}
      subtitle={shop ? detailsState : undefined}
      details={details}
      actions={
        shop ? (
          <>
            <IosButton label="Edit Shop" onClick={() => onEditShop(shop.id)} />
            <IosButton
              label={isDeleting ? "Deleting..." : "Delete Shop"}
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
