import { useCallback, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useAvailabilityGroupsQuery } from "@entities/availability-groups";
import { AvailabilityGroupListCard } from "@entities/availability-groups/ui";
import { usePageScrollbarHidden } from "@shared/lib/usePageScrollbarHidden";
import { IosButton } from "@shared/ui/components/IosButton";
import { PageHeader } from "@shared/ui/PageHeader";
import { PlusIcon } from "@shared/ui/icons";
import styles from "./AvailabilityPage.module.css";

export function AvailabilityPage() {
  usePageScrollbarHidden();

  const location = useLocation();
  const navigate = useNavigate();
  const [query, setQuery] = useState("");
  const groupsQuery = useAvailabilityGroupsQuery(query, location.key);
  const groups = groupsQuery.data ?? [];

  const handleAddGroup = useCallback(() => {
    navigate("/availability/new");
  }, [navigate]);

  const handleOpenGroup = useCallback(
    (groupId: number) => {
      navigate(`/availability/${groupId}`);
    },
    [navigate]
  );

  return (
    <div className={styles.page}>
      <PageHeader
        title="Availability List"
        subtitle="Browse and search availability schedules"
        backTo="/"
        rightSlot={<IosButton label="Add New" icon={<PlusIcon size={18} />} onClick={handleAddGroup} />}
        searchMeta={`Total: ${groups.length}`}
        search={{
          value: query,
          onChange: setQuery,
          placeholder: "Search availability",
          ariaLabel: "Search availability",
        }}
      />

      <AvailabilityGroupListCard
        groups={groups}
        error={groupsQuery.error}
        isLoading={groupsQuery.isLoading}
        searchQuery={query}
        onClearSearch={() => setQuery("")}
        onAddGroup={handleAddGroup}
        onOpenGroup={handleOpenGroup}
      />
    </div>
  );
}
