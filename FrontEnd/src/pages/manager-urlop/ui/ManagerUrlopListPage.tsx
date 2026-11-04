import { useMemo, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useLocation, useNavigate } from "react-router-dom";
import { useShopsQuery } from "@entities/shops/model/queries";
import { useManagerUrlopShopSummariesQuery, type UrlopShopSummary } from "@entities/urlop";
import { t } from "@shared/i18n";
import { queryKeys } from "@shared/api/queryKeys";
import { usePageScrollbarHidden } from "@shared/lib/usePageScrollbarHidden";
import { PageHeader } from "@shared/ui/PageHeader";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { IosButton } from "@shared/ui/components/IosButton";
import { ListCardSection } from "@shared/ui/components/ListCardSection";
import { RecordGrid } from "@shared/ui/components/RecordGrid";
import { ManagerUrlopShopCard } from "./ManagerUrlopShopCard";
import styles from "./ManagerUrlopListPage.module.css";

export function ManagerUrlopListPage() {
  usePageScrollbarHidden();
  const navigate = useNavigate();
  const client = useQueryClient();
  const location = useLocation();
  const [searchText, setSearchText] = useState("");
  const shopsQuery = useShopsQuery(searchText, location.key);
  const shops = shopsQuery.data ?? [];
  const summaryQuery = useManagerUrlopShopSummariesQuery();
  const summaries = useMemo(() => new Map<number, UrlopShopSummary>(
    summaryQuery.data?.map(summary => [summary.shopId, summary]) ?? [],
  ), [summaryQuery.data]);
  const summariesLoaded = summaryQuery.data !== undefined && !summaryQuery.error;

  return (
    <div className={styles.page}>
      <PageHeader title={t("Urlop")} subtitle={t("Choose a shop to manage vacation requests.")} backTo="/"
        search={{ value: searchText, onChange: setSearchText, placeholder: t("Search vacation shop"), ariaLabel: t("Search vacation shop") }}
        searchMeta={t("Total: {0}", shops.length)} />
      {shops.length > 0 && summaryQuery.error ? (
        <ErrorBanner>
          {t("Vacation counters unavailable.")}
          <IosButton label={t("Retry")} variant="secondary" disabled={summaryQuery.isFetching} onClick={() => client.invalidateQueries({ queryKey: queryKeys.urlop.managerShopSummaries() })} />
        </ErrorBanner>
      ) : null}
      {shops.length > 0 && summaryQuery.isLoading ? <p role="status">{t("Loading vacation counters...")}</p> : null}
      <ListCardSection error={shopsQuery.error} errorMessage={t("Could not load shops.")}
        isFetching={shopsQuery.isLoading} hasData={shops.length > 0} loadingMessage={t("Loading shops...")}
        searchQuery={searchText} emptyTitle={t("No shops yet")} searchEmptyTitle={t("Nothing found")}
        searchEmptyDescription={t('No shop matches "{0}".', searchText)}
        searchEmptyAction={<IosButton label={t("Clear Search")} variant="secondary" onClick={() => setSearchText("")} />}>
        <RecordGrid className={styles.grid}>
          {shops.map(shop => <ManagerUrlopShopCard key={shop.id} shop={shop}
            summary={summariesLoaded ? summaries.get(shop.id) ?? { shopId: shop.id, approvedRequests: 0, pendingRequests: 0 } : null}
            onOpen={shopId => navigate(`/urlop/${shopId}`)} />)}
        </RecordGrid>
      </ListCardSection>
    </div>
  );
}
