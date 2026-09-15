import { t } from "@shared/i18n";
import type { Employee } from "@entities/employees/model/types";
import {
  getEmployeeContactState,
  getEmployeeFullName,
  getEmployeePresenceTone,
} from "@entities/employees/model/presentation";
import { usePinnedRecords } from "@shared/lib/records/usePinnedRecords";
import { IosButton } from "@shared/ui/components/IosButton";
import { ListCardSection } from "@shared/ui/components/ListCardSection";
import { PresenceBadge } from "@shared/ui/components/PresenceBadge";
import { RecordGrid } from "@shared/ui/components/RecordGrid";
import type { RecordTileMetaItem } from "@shared/ui/components/RecordTile";
import { RecordTile } from "@shared/ui/components/RecordTile";
import { PlusIcon } from "@shared/ui/icons";
import styles from "./EmployeeListCard.module.css";

type EmployeeListCardProps = {
  employees: Employee[];
  error?: unknown;
  isLoading: boolean;
  searchQuery: string;
  onClearSearch: () => void;
  onAddEmployee: () => void;
  onEmployeeOpen: (employeeId: Employee["id"]) => void;
};

export function EmployeeListCard({
  employees,
  error,
  isLoading,
  searchQuery,
  onClearSearch,
  onAddEmployee,
  onEmployeeOpen,
}: EmployeeListCardProps) {
  const { sortedItems: sortedEmployees, pinnedIdSet, togglePin } = usePinnedRecords(
    "employees:list:pinned",
    employees
  );

  const addEmployeeAction = (
    <IosButton label={t("Add New")} icon={<PlusIcon size={18} />} onClick={onAddEmployee} />
  );

  return (
    <ListCardSection
      error={error}
      isFetching={isLoading}
      hasData={employees.length > 0}
      searchQuery={searchQuery}
      loadingMessage="Loading employees..."
      errorMessage="Could not load employees."
      emptyTitle="No employees yet"
      emptyDescription="Start by creating your first employee record."
      emptyAction={addEmployeeAction}
      searchEmptyTitle="Nothing found"
      searchEmptyDescription={t("No employee matches \"{0}\".", searchQuery)}
      searchEmptyAction={
        <IosButton label={t("Clear Search")} variant="secondary" onClick={onClearSearch} />
      }
    >
      <RecordGrid className={styles.grid}>
        {sortedEmployees.map(employee => {
          const fullName = getEmployeeFullName(employee);
          const isPinned = pinnedIdSet.has(String(employee.id));
          const presenceLabel = getEmployeeContactState(employee, "compact");
          const presenceTone = getEmployeePresenceTone(employee);
          const metaItems: RecordTileMetaItem[] = [
            {
              key: "username",
              label: t("Login"),
              value: employee.username ?? t("Not created"),
            },
            {
              key: "email",
              label: t("Email"),
              value: employee.email ?? t("Not provided"),
            },
            {
              key: "phone",
              label: t("Phone"),
              value: employee.phone ?? t("Not provided"),
            },
          ];

          return (
            <RecordTile
              key={employee.id}
              title={fullName}
              headerSlot={<PresenceBadge label={presenceLabel} tone={presenceTone} size="sm" />}
              badge={`#${employee.id}`}
              density="compact"
              metaLayout="stacked"
              metaItems={metaItems}
              isPinned={isPinned}
              onTogglePin={() => togglePin(employee.id)}
              pinLabel={isPinned ? t("Unpin {0}", fullName) : t("Pin {0}", fullName)}
              onClick={() => onEmployeeOpen(employee.id)}
              ariaLabel={t("Open {0} profile", fullName)}
            />
          );
        })}
      </RecordGrid>
    </ListCardSection>
  );
}
