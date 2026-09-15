import { t } from "@shared/i18n";
﻿import type { Shop } from "./types";

type ShopIdentity = Pick<Shop, "name">;
type ShopDetails = Pick<Shop, "address" | "description">;

export function getShopDisplayName(shop?: ShopIdentity | null, fallback = t("Shop")) {
  const name = shop?.name?.trim();
  return name || fallback;
}

export function getShopInitials(shop?: ShopIdentity | null, fallback = "SH") {
  const initials =
    shop?.name
      ?.trim()
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0])
      .join("")
      .toUpperCase() ?? "";

  return initials || fallback;
}

export function getShopDetailsState(shop?: ShopDetails | null) {
  const hasAddress = Boolean(shop?.address?.trim());
  const hasDescription = Boolean(shop?.description?.trim());

  if (hasAddress && hasDescription) {
    return t("Address and description available");
  }

  if (hasAddress) {
    return t("Address available");
  }

  if (hasDescription) {
    return t("Description available");
  }

  return t("Shop details missing");
}

export function getShopProfileDetails(shop?: ShopDetails | null) {
  return [
    {
      key: "address",
      label: t("Address"),
      value: shop?.address?.trim() || null,
    },
    {
      key: "description",
      label: t("Description"),
      value: shop?.description?.trim() || null,
    },
  ] as const;
}
