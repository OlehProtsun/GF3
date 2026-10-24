import { t } from "@shared/i18n";
import { useMemo, type ReactNode } from "react";
import type { Employee } from "@entities/employees/model/types";
import { getEmployeeFullName } from "@entities/employees/model/presentation";
import { IosButton } from "@shared/ui/components/IosButton";
import { SearchableSelect, type SearchableSelectOption } from "@shared/ui/components/SearchableSelect";
import { ErrorPill } from "@shared/ui/forms/Field";
import { CardSection } from "@shared/ui/sections/CardSection";
import { CloseIcon, EmployeeIcon, ImportIcon, PlusIcon } from "@shared/ui/icons";
import styles from "./AvailabilityEmployeeCard.module.css";

type AvailabilityEmployeeCardProps = {
  employees: Employee[];
  selectedEmployeeId: number | null;
  assignedEmployees: { id: number; label: string; canChooseFromAnother: boolean }[];
  groupError?: string;
  headerRightSlot?: ReactNode;
  onSelectedEmployeeIdChange: (value: number | null) => void;
  onAddEmployee: () => void;
  onRemoveEmployee: (employeeId: number) => void;
  onChooseFromAnother: (employeeId: number) => void;
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
  onChooseFromAnother,
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
          hint: t("Employee ID: {0}", employee.id),
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
      title={t("Employee")}
      icon={<EmployeeIcon size={18} />}
      headerRightSlot={headerRightSlot}
    >
      <div className={styles.layout}>
        <div className={styles.field}>
          <div className={styles.labelRow}>
            <span className={styles.label}>{t("Selected Employee")}</span>
            <span className={styles.metaText}>{t("Employee ID:")} {selectedEmployee?.id ?? "-"}</span>
          </div>

          <SearchableSelect
            id="availability-employee-select"
            value={selectedEmployeeId !== null ? String(selectedEmployeeId) : ""}
            options={employeeOptions}
            placeholder={employeeOptions.length > 0 ? t("Select employee...") : t("No employees available")}
            dropdownTitle={t("Employee list")}
            searchPlaceholder={t("Search employee...")}
            emptyMessage={t("No employees match your search.")}
            fallbackHint={t("{0} employees found", employeeOptions.length)}
            invalid={Boolean(groupError)}
            ariaDescribedBy={employeeGroupErrorId}
            ariaLabel={t("employee list")}
            onChange={value => onSelectedEmployeeIdChange(value ? Number(value) : null)}
          />
        </div>

        <div className={styles.assignedBlock}>
          <div className={`${styles.assignedPanel} ${groupError ? styles.assignedPanelInvalid : ""}`}>
            <div className={styles.labelRow}>
              <span className={styles.label}>{t("Assigned employees")}</span>
              <span className={styles.metaText}>{assignedEmployees.length}  {t("total")}</span>
            </div>

            {assignedEmployees.length > 0 ? (
              <div className={styles.assignedList}>
                {assignedEmployees.map(employee => (
                  <div key={employee.id} className={styles.employeeRow}>
                    <span className={styles.employeeName}>{employee.label}</span>
                    <div className={styles.employeeActions}>
                      <button
                        type="button"
                        className={styles.chooseFromAnotherButton}
                        aria-label={t("Choose availability from another schedule for {0}", employee.label)}
                        title={t("Choose from another")}
                        onClick={() => onChooseFromAnother(employee.id)}
                        disabled={!employee.canChooseFromAnother}
                      >
                        <ImportIcon size={15} />
                      </button>
                      <button
                        type="button"
                        className={styles.removeEmployeeButton}
                        aria-label={t("Remove {0} from availability", employee.label)}
                        title={t("Remove employee")}
                        onClick={() => onRemoveEmployee(employee.id)}
                      >
                        <CloseIcon size={15} />
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <p className={styles.emptyText}>{t("No employees added to this group yet.")}</p>
            )}
          </div>

          {groupError ? <ErrorPill id={employeeGroupErrorId}>{groupError}</ErrorPill> : null}
        </div>

        <div className={styles.actions}>
          <IosButton label={t("Add")} icon={<PlusIcon size={16} />} onClick={onAddEmployee} />
        </div>
      </div>
    </CardSection>
  );
}
