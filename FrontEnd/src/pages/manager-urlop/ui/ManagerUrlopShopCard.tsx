import type { Shop } from "@entities/shops/model/types";
import { getShopDisplayName } from "@entities/shops/model/presentation";
import type { UrlopShopSummary } from "@entities/urlop";
import { t } from "@shared/i18n";
import { ArrowRightIcon, ShopIcon } from "@shared/ui/icons";
import styles from "./ManagerUrlopShopCard.module.css";

type ManagerUrlopShopCardProps = {
  shop: Shop;
  summary: UrlopShopSummary | null;
  onOpen: (shopId: number) => void;
};

export function ManagerUrlopShopCard({ shop, summary, onOpen }: ManagerUrlopShopCardProps) {
  const name = getShopDisplayName(shop);
  return (
    <button type="button" className={styles.card} data-variant={shop.id % 6}
      aria-label={t("Open vacation management for {0}", name)} onClick={() => onOpen(shop.id)}>
      <span className={styles.art} aria-hidden="true">
        <span className={styles.arch} />
        <span className={styles.orb} />
        <span className={styles.storefront} />
      </span>
      <span className={styles.identity}>
        <span className={styles.icon} aria-hidden="true"><ShopIcon size={28} /></span>
        <span className={styles.details}>
          <span className={styles.name} title={name}>{name}</span>
          {shop.address?.trim() ? <span className={styles.address} title={shop.address}>{shop.address}</span> : null}
        </span>
      </span>
      <span className={styles.counts}>
        <span className={styles.approved}>{summary?.approvedRequests ?? "—"}</span>
        <span className={styles.caption}>{t("Approved vacations")}</span>
        <span className={styles.pending}>{t("Pending vacation requests")}: <strong>{summary?.pendingRequests ?? "—"}</strong></span>
        <span className={styles.accent} aria-hidden="true" />
      </span>
      <span className={styles.arrow} aria-hidden="true"><ArrowRightIcon size={25} /></span>
    </button>
  );
}
