import { useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useAvailabilityGroupsQuery } from "@entities/availability-groups";
import { AvailabilityGroupListCard } from "@entities/availability-groups/ui";
import { queryKeys } from "@shared/api/queryKeys";
import { ManagerPhonePage } from "./ManagerPhonePage";

export function ManagerPhoneAvailabilityPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const [query, setQuery] = useState("");
  const groups = useAvailabilityGroupsQuery(query, location.key);
  return <ManagerPhonePage query={query} onQueryChange={setQuery} queries={[groups]} queryKeys={[queryKeys.availabilityGroups.all]}>
    <AvailabilityGroupListCard groups={groups.data ?? []} isLoading={false} searchQuery={query} onClearSearch={() => setQuery("")} onOpenGroup={id => navigate(`/availability/${id}`)} />
  </ManagerPhonePage>;
}
