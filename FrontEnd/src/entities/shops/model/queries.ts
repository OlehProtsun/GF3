import { useDeferredValue, useMemo } from "react";
import { useShopsListQuery } from "@entities/shops/api/queries";

export function useShopsQuery(searchText: string, refreshKey?: string) {
  const query = useShopsListQuery({ search: searchText, refreshKey });
  const deferredSearchText = useDeferredValue(searchText);

  const data = useMemo(() => {
    const shops = query.data ?? [];
    const search = deferredSearchText.trim().toLowerCase();
    if (!search) return shops;

    return shops.filter((shop) =>
      [shop.name, shop.address, shop.description ?? ""].some((value) =>
        value.toLowerCase().includes(search),
      ),
    );
  }, [deferredSearchText, query.data]);

  return { ...query, data };
}
