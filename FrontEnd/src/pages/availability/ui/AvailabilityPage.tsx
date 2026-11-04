import { t } from "@shared/i18n";
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
        title={t("Availability List")}
        subtitle={t("Browse and search availability schedules")}
        backTo="/"
        rightSlot={<IosButton label={t("Add New")} icon={<PlusIcon size={18} />} onClick={handleAddGroup} />}
        searchMeta={t("Total: {0}", groups.length)}
        search={{
          value: query,
          onChange: setQuery,
          placeholder: t("Search availability"),
          ariaLabel: t("Search availability"),
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
