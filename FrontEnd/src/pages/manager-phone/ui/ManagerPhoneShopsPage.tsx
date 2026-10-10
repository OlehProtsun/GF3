import { useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useShopsQuery } from "@entities/shops/model/queries";
import { ShopListCard } from "@entities/shops/ui/ShopListCard";
import { queryKeys } from "@shared/api/queryKeys";
import { ManagerPhonePage } from "./ManagerPhonePage";

export function ManagerPhoneShopsPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const [query, setQuery] = useState("");
  const shops = useShopsQuery(query, location.key);
  return <ManagerPhonePage backTo="/more" query={query} onQueryChange={setQuery} queries={[shops]} queryKeys={[queryKeys.shops.all]}>
    <ShopListCard shops={shops.data ?? []} isLoading={false} searchQuery={query} onClearSearch={() => setQuery("")} onShopOpen={id => navigate(`/shop/${id}`)} />
  </ManagerPhonePage>;
}
