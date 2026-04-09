import { useMemo } from "react";
import { useAvailabilityGroupsListQuery } from "@entities/availability-groups/api/queries";
import { filterAvailabilityGroups, sortAvailabilityGroups } from "./presentation";

export function useAvailabilityGroupsQuery(searchText: string, refreshKey?: string) {
  const query = useAvailabilityGroupsListQuery(refreshKey);

  const data = useMemo(() => {
    const groups = query.data ?? [];
    return filterAvailabilityGroups(sortAvailabilityGroups(groups), searchText);
  }, [query.data, searchText]);

  return { ...query, data };
}
