import { useMemo, type ReactNode } from "react";
import type { Employee } from "@entities/employees/model/types";
import { getEmployeeFullName } from "@entities/employees/model/presentation";
import { IosButton } from "@shared/ui/components/IosButton";
import { SearchableSelect, type SearchableSelectOption } from "@shared/ui/components/SearchableSelect";
import { ErrorPill } from "@shared/ui/forms/Field";
import { CardSection } from "@shared/ui/sections/CardSection";
import { CloseIcon, EmployeeIcon, PlusIcon } from "@shared/ui/icons";
import styles from "./AvailabilityEmployeeCard.module.css";

type AvailabilityEmployeeCardProps = {
  employees: Employee[];
  selectedEmployeeId: number | null;
  assignedEmployees: { id: number; label: string }[];
  groupError?: string;
  headerRightSlot?: ReactNode;
  onSelectedEmployeeIdChange: (value: number | null) => void;
  onAddEmployee: () => void;
  onRemoveEmployee: () => void;
};

export function AvailabilityEmployeeCard({
  employees,
  selectedEmployeeId,
  assignedEmployees,
  groupError,
  headerRightSlot,
  onSelectedEmployeeIdChange,
  onAddEmployee,
  onRemoveEmployee,
}: AvailabilityEmployeeCardProps) {
  const selectedEmployee = employees.find(employee => employee.id === selectedEmployeeId) ?? null;
  const employeeGroupErrorId = groupError ? "availability-employee-group-error" : undefined;
  const employeeOptions = useMemo<SearchableSelectOption[]>(
    () =>
      [...employees]
        .sort((left, right) => getEmployeeFullName(left).localeCompare(getEmployeeFullName(right)))
        .map(employee => ({
          value: String(employee.id),
          label: getEmployeeFullName(employee),
          hint: `Employee ID: ${employee.id}`,
          keywords: [
            String(employee.id),
            employee.firstName,
            employee.lastName,
            employee.email ?? "",
            employee.phone ?? "",
            getEmployeeFullName(employee),
          ].join(" "),
        })),
    [employees],
  );

  return (
    <CardSection
      className={styles.card}
      title="Employee"
      icon={<EmployeeIcon size={18} />}
      headerRightSlot={headerRightSlot}
    >
      <div className={styles.layout}>
        <div className={styles.field}>
          <div className={styles.labelRow}>
            <span className={styles.label}>Selected Employee</span>
            <span className={styles.metaText}>Employee ID: {selectedEmployee?.id ?? "-"}</span>
          </div>

          <SearchableSelect
            id="availability-employee-select"
            value={selectedEmployeeId !== null ? String(selectedEmployeeId) : ""}
            options={employeeOptions}
            placeholder={employeeOptions.length > 0 ? "Select employee..." : "No employees available"}
            dropdownTitle="Employee list"
            searchPlaceholder="Search employee..."
            emptyMessage="No employees match your search."
            fallbackHint={`${employeeOptions.length} employees found`}
            invalid={Boolean(groupError)}
            ariaDescribedBy={employeeGroupErrorId}
            ariaLabel="employee list"
            onChange={value => onSelectedEmployeeIdChange(value ? Number(value) : null)}
          />
        </div>

        <div className={styles.assignedBlock}>
          <div className={`${styles.assignedPanel} ${groupError ? styles.assignedPanelInvalid : ""}`}>
            <div className={styles.labelRow}>
              <span className={styles.label}>Assigned employees</span>
              <span className={styles.metaText}>{assignedEmployees.length} total</span>
            </div>

            {assignedEmployees.length > 0 ? (
              <div className={styles.chipRow}>
                {assignedEmployees.map(employee => (
                  <span key={employee.id} className={styles.chip}>
                    {employee.label}
                  </span>
                ))}
              </div>
            ) : (
              <p className={styles.emptyText}>No employees added to this group yet.</p>
            )}
          </div>

          {groupError ? <ErrorPill id={employeeGroupErrorId}>{groupError}</ErrorPill> : null}
        </div>

        <div className={styles.actions}>
          <IosButton
            label="Remove"
            icon={<CloseIcon size={16} />}
            variant="secondary"
            customColor="#dc2626"
            customBorderColor="#dc2626"
            onClick={onRemoveEmployee}
          />
          <IosButton label="Add" icon={<PlusIcon size={16} />} onClick={onAddEmployee} />
        </div>
      </div>
    </CardSection>
  );
}
