import { t } from "@shared/i18n";
import { useState, type FocusEvent } from "react";
import { ArrowIcon, CheckIcon, EmployeeIcon } from "@shared/ui/icons";
import styles from "./EmployeeTargetCombobox.module.css";

export type EmployeeTargetComboboxEmployee = {
  id: number;
  firstName?: string | null;
  lastName?: string | null;
  displayName?: string | null;
};

type EmployeeTargetComboboxProps<TEmployee extends EmployeeTargetComboboxEmployee> = {
  employees: TEmployee[];
  selectedEmployeeId: number | null;
  loading?: boolean;
  ariaLabel?: string;
  onChange: (employeeId: number) => void;
};

function getEmployeeLabel(employee: EmployeeTargetComboboxEmployee) {
  const fullName = `${employee.firstName ?? ""} ${employee.lastName ?? ""}`.trim();
  return employee.displayName?.trim() || fullName || t("Employee #{0}", employee.id);
}

function getEmployeeInitials(employee: EmployeeTargetComboboxEmployee) {
  const label = getEmployeeLabel(employee);
  const nameParts = label.split(/\s+/).filter(Boolean);
  const initials = nameParts.length >= 2
    ? `${nameParts[0][0]}${nameParts[1][0]}`
    : label.slice(0, 2);

  return initials.toUpperCase();
}

export function EmployeeTargetCombobox<TEmployee extends EmployeeTargetComboboxEmployee>({
  employees,
  selectedEmployeeId,
  loading = false,
  ariaLabel = t("Employees"),
  onChange,
}: EmployeeTargetComboboxProps<TEmployee>) {
  const [isOpen, setIsOpen] = useState(false);
  const selectedEmployee = employees.find(employee => employee.id === selectedEmployeeId) ?? employees[0] ?? null;
  const disabled = loading || employees.length === 0;

  const handleBlur = (event: FocusEvent<HTMLDivElement>) => {
    if (!event.currentTarget.contains(event.relatedTarget as Node | null)) {
      setIsOpen(false);
    }
  };

  return (
    <div className={styles.comboBox} onBlur={handleBlur}>
      <button
        type="button"
        className={styles.comboTrigger}
        disabled={disabled}
        aria-haspopup="listbox"
        aria-expanded={isOpen}
        onClick={() => setIsOpen(value => !value)}
      >
        <span className={styles.comboAvatar} aria-hidden="true">
          {selectedEmployee ? getEmployeeInitials(selectedEmployee) : <EmployeeIcon size={15} />}
        </span>
        <span className={styles.comboContent}>
          <span className={styles.comboLabel}>
            {loading ? t("Loading employees") : selectedEmployee ? getEmployeeLabel(selectedEmployee) : t("No employees")}
          </span>
        </span>
        <ArrowIcon className={[styles.comboArrow, isOpen ? styles.comboArrowOpen : ""].filter(Boolean).join(" ")} size={12} />
      </button>

      {isOpen && !disabled ? (
        <div className={styles.comboMenu} role="listbox" aria-label={ariaLabel}>
          {employees.map(employee => {
            const isSelected = selectedEmployee?.id === employee.id;

            return (
              <button
                key={employee.id}
                type="button"
                className={[styles.comboOption, isSelected ? styles.comboOptionActive : ""].filter(Boolean).join(" ")}
                role="option"
                aria-selected={isSelected}
                onClick={() => {
                  onChange(employee.id);
                  setIsOpen(false);
                }}
              >
                <span className={styles.comboOptionAvatar} aria-hidden="true">{getEmployeeInitials(employee)}</span>
                <span className={styles.comboOptionText}>
                  <strong>{getEmployeeLabel(employee)}</strong>
                </span>
                {isSelected ? <CheckIcon className={styles.comboCheck} size={14} /> : null}
              </button>
            );
          })}
        </div>
      ) : null}
    </div>
  );
}
