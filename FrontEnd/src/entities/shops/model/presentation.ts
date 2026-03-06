import type { Shop } from "./types";

type ShopIdentity = Pick<Shop, "name">;
type ShopDetails = Pick<Shop, "address" | "description">;

export function getShopDisplayName(shop?: ShopIdentity | null, fallback = "Shop") {
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
    return "Address and description available";
  }

  if (hasAddress) {
    return "Address available";
  }

  if (hasDescription) {
    return "Description available";
  }

  return "Shop details missing";
}

export function getShopProfileDetails(shop?: ShopDetails | null) {
  return [
    {
      key: "address",
      label: "Address",
      value: shop?.address?.trim() || null,
    },
    {
      key: "description",
      label: "Description",
      value: shop?.description?.trim() || null,
    },
  ] as const;
}
