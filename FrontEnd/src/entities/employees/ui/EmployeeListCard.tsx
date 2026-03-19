import type { Employee } from "@entities/employees/model/types";
import { getEmployeeFullName } from "@entities/employees/model/presentation";
import { usePinnedRecords } from "@shared/lib/records/usePinnedRecords";
import { IosButton } from "@shared/ui/components/IosButton";
import { ListCardSection } from "@shared/ui/components/ListCardSection";
import { RecordGrid } from "@shared/ui/components/RecordGrid";
import type { RecordTileMetaItem } from "@shared/ui/components/RecordTile";
import { RecordTile } from "@shared/ui/components/RecordTile";
import { PlusIcon } from "@shared/ui/icons";

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
    <IosButton label="Add New" icon={<PlusIcon size={18} />} onClick={onAddEmployee} />
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
      searchEmptyDescription={`No employee matches "${searchQuery}".`}
      searchEmptyAction={
        <IosButton label="Clear Search" variant="secondary" onClick={onClearSearch} />
      }
    >
      <RecordGrid>
        {sortedEmployees.map(employee => {
          const fullName = getEmployeeFullName(employee);
          const isPinned = pinnedIdSet.has(String(employee.id));
          const metaItems: RecordTileMetaItem[] = [
            {
              key: "email",
              label: "Email",
              value: employee.email ?? "Not provided",
            },
            {
              key: "phone",
              label: "Phone",
              value: employee.phone ?? "Not provided",
            },
          ];

          return (
            <RecordTile
              key={employee.id}
              title={fullName}
              badge={`ID ${employee.id}`}
              metaItems={metaItems}
              isPinned={isPinned}
              onTogglePin={() => togglePin(employee.id)}
              pinLabel={isPinned ? `Unpin ${fullName}` : `Pin ${fullName}`}
              onClick={() => onEmployeeOpen(employee.id)}
              ariaLabel={`Open ${fullName} profile`}
            />
          );
        })}
      </RecordGrid>
    </ListCardSection>
  );
}
