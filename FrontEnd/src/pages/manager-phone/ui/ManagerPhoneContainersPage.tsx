import { useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { ContainerListCard, matchesContainerSearch, useContainersListQuery } from "@entities/containers";
import { queryKeys } from "@shared/api/queryKeys";
import { ManagerPhonePage } from "./ManagerPhonePage";

export function ManagerPhoneContainersPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const [query, setQuery] = useState("");
  const containers = useContainersListQuery(location.key);
  const data = (containers.data ?? []).filter(item => matchesContainerSearch(item, query)).sort((a, b) => b.id - a.id);
  return <ManagerPhonePage query={query} onQueryChange={setQuery} queries={[containers]} queryKeys={[queryKeys.containers.list()]}>
    <ContainerListCard containers={data} isLoading={false} searchQuery={query} onClearSearch={() => setQuery("")} onContainerOpen={id => navigate(`/container/${id}`)} />
  </ManagerPhonePage>;
}
