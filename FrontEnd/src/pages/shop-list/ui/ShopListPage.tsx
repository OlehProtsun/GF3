import { t } from "@shared/i18n";
import { useCallback, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useShopsQuery } from "@entities/shops/model/queries";
import { ShopListCard } from "@entities/shops/ui/ShopListCard";
import { usePageScrollbarHidden } from "@shared/lib/usePageScrollbarHidden";
import { IosButton } from "@shared/ui/components/IosButton";
import { PageHeader } from "@shared/ui/PageHeader";
import { PlusIcon } from "@shared/ui/icons";
import styles from "./ShopListPage.module.css";

export function ShopListPage() {
  usePageScrollbarHidden();

  const location = useLocation();
  const navigate = useNavigate();
  const [query, setQuery] = useState("");
  const shopsQuery = useShopsQuery(query, location.key);
  const shops = shopsQuery.data ?? [];

  const handleAddShop = useCallback(() => {
    navigate("/shop/new");
  }, [navigate]);

  const handleShopOpen = useCallback(
    (shopId: number) => {
      navigate(`/shop/${shopId}`);
    },
    [navigate]
  );

  return (
    <div className={styles.page}>
      <PageHeader
        title={t("Shop List")}
        subtitle={t("Browse and search shop records")}
        backTo="/"
        rightSlot={
          <IosButton label={t("Add New")} icon={<PlusIcon size={18} />} onClick={handleAddShop} />
        }
        searchMeta={t("Total: {0}", shops.length)}
        search={{
          value: query,
          onChange: setQuery,
          placeholder: t("Search shop"),
          ariaLabel: t("Search shop"),
        }}
      />

      <ShopListCard
        shops={shops}
        error={shopsQuery.error}
        isLoading={shopsQuery.isLoading}
        searchQuery={query}
        onClearSearch={() => setQuery("")}
        onAddShop={handleAddShop}
        onShopOpen={handleShopOpen}
      />
    </div>
  );
}
