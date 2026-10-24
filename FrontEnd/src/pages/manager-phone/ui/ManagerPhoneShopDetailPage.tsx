import { parsePhoneId } from "./parsePhoneId";
import { useParams } from "react-router-dom";
import { useShopByIdQuery } from "@entities/shops/api/queries";
import { ShopProfileCard } from "@entities/shops/ui/ShopProfileCard";
import { queryKeys } from "@shared/api/queryKeys";
import { ManagerPhonePage } from "./ManagerPhonePage";

export function ManagerPhoneShopDetailPage() {
  const id = parsePhoneId(useParams<{ shopId: string }>().shopId);
  const shop = useShopByIdQuery(id);
  return <ManagerPhonePage backTo="/shop" valid={id !== null} missing={!shop.data} queries={[shop]} queryKeys={[queryKeys.shops.all]}>
    <ShopProfileCard shop={shop.data} showManagementActions={false} isLoading={false} hasLoadError={false} isDeleting={false} onEditShop={() => {}} onDeleteShop={() => {}} />
  </ManagerPhonePage>;
}
