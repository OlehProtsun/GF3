import { useMemo } from "react";
import { useShopsListQuery } from "@entities/shops/api/queries";

export function useShopsQuery(searchText: string) {
  const query = useShopsListQuery({ search: searchText });

  const data = useMemo(() => {
    const shops = query.data ?? [];
    const search = searchText.trim().toLowerCase();
    if (!search) return shops;

    return shops.filter((shop) =>
      [shop.name, shop.address, shop.description ?? ""].some((value) =>
        value.toLowerCase().includes(search),
      ),
    );
  }, [query.data, searchText]);

  return { ...query, data };
}
