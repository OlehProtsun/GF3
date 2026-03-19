import { useEffect, useLayoutEffect, useRef, useState, type CSSProperties, type ReactNode } from "react";
import { createPortal } from "react-dom";
import type { Employee } from "@entities/employees/model/types";
import { getEmployeeFullName } from "@entities/employees/model/presentation";
import { IosButton } from "@shared/ui/components/IosButton";
import { ErrorPill } from "@shared/ui/forms/Field";
import { CardSection } from "@shared/ui/sections/CardSection";
import { ArrowIcon, CheckIcon, CloseIcon, EmployeeIcon, PlusIcon, SearchIcon } from "@shared/ui/icons";
import styles from "./AvailabilityEmployeeCard.module.css";

type AvailabilityEmployeeCardProps = {
  employees: Employee[];
  selectedEmployeeId: number | null;
  employeeSearchText: string;
  assignedEmployees: { id: number; label: string }[];
  groupError?: string;
  headerRightSlot?: ReactNode;
  onEmployeeSearchTextChange: (value: string) => void;
  onSelectedEmployeeIdChange: (value: number | null) => void;
  onAddEmployee: () => void;
  onRemoveEmployee: () => void;
};

export function AvailabilityEmployeeCard({
  employees,
  selectedEmployeeId,
  employeeSearchText,
  assignedEmployees,
  groupError,
  headerRightSlot,
  onEmployeeSearchTextChange,
  onSelectedEmployeeIdChange,
  onAddEmployee,
  onRemoveEmployee,
}: AvailabilityEmployeeCardProps) {
  const selectRef = useRef<HTMLDivElement | null>(null);
  const dropdownRef = useRef<HTMLDivElement | null>(null);
  const [isDropdownOpen, setIsDropdownOpen] = useState(false);
  const [dropdownStyle, setDropdownStyle] = useState<CSSProperties>({});
  const selectedEmployee = employees.find(employee => employee.id === selectedEmployeeId) ?? null;
  const employeeGroupErrorId = groupError ? "availability-employee-group-error" : undefined;

  useLayoutEffect(() => {
    if (!isDropdownOpen || !selectRef.current) {
      return;
    }

    const updateDropdownPosition = () => {
      const rect = selectRef.current?.getBoundingClientRect();
      if (!rect) {
        return;
      }

      setDropdownStyle({
        left: rect.left,
        top: rect.bottom + 10,
        width: rect.width,
      });
    };

    updateDropdownPosition();
    window.addEventListener("resize", updateDropdownPosition);
    window.addEventListener("scroll", updateDropdownPosition, true);
    return () => {
      window.removeEventListener("resize", updateDropdownPosition);
      window.removeEventListener("scroll", updateDropdownPosition, true);
    };
  }, [isDropdownOpen]);

  useEffect(() => {
    if (!isDropdownOpen) {
      return;
    }

    const handlePointerDown = (event: MouseEvent) => {
      if (!(event.target instanceof Node)) {
        return;
      }

      const insideTrigger = selectRef.current?.contains(event.target);
      const insideDropdown = dropdownRef.current?.contains(event.target);
      if (!insideTrigger && !insideDropdown) {
        setIsDropdownOpen(false);
      }
    };

    const handleEscape = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setIsDropdownOpen(false);
      }
    };

    window.addEventListener("mousedown", handlePointerDown);
    window.addEventListener("keydown", handleEscape);
    return () => {
      window.removeEventListener("mousedown", handlePointerDown);
      window.removeEventListener("keydown", handleEscape);
    };
  }, [isDropdownOpen]);

  const handleSelectEmployee = (employeeId: number) => {
    onSelectedEmployeeIdChange(employeeId);
    setIsDropdownOpen(false);
  };

  const dropdown = isDropdownOpen ? createPortal(
    <div ref={dropdownRef} className={styles.dropdownPortal} style={dropdownStyle}>
      <div className={styles.dropdown}>
        <div className={styles.dropdownHeader}>
          <span>Employee list</span>
          <span>{employees.length}</span>
        </div>

        <div className={styles.optionList} role="listbox" aria-label="Employee list">
          {employees.length > 0 ? (
            employees.map(employee => {
              const isSelected = employee.id === selectedEmployeeId;

              return (
                <button
                  key={employee.id}
                  type="button"
                  role="option"
                  aria-selected={isSelected}
                  className={`${styles.option} ${isSelected ? styles.optionSelected : ""}`}
                  onClick={() => handleSelectEmployee(employee.id)}
                >
                  <div className={styles.optionText}>
                    <span className={styles.optionName}>{getEmployeeFullName(employee)}</span>
                    <span className={styles.optionMeta}>Employee ID: {employee.id}</span>
                  </div>

                  {isSelected ? <CheckIcon size={16} className={styles.optionCheck} /> : null}
                </button>
              );
            })
          ) : (
            <div className={styles.emptyDropdown}>No employees match the current search.</div>
          )}
        </div>
      </div>
    </div>,
    document.body,
  ) : null;

  return (
    <>
      <CardSection
        className={styles.card}
        title="Employee"
        icon={<EmployeeIcon size={18} style={{ transform: "scaleY(-1)" }} />}
        headerRightSlot={headerRightSlot}
      >
        <div className={styles.layout}>
          <label className={styles.field}>
            <span className={styles.label}>Search employee</span>
            <div className={styles.searchField}>
              <SearchIcon className={styles.searchIcon} />
              <input
                className={styles.searchInput}
                value={employeeSearchText}
                onChange={event => onEmployeeSearchTextChange(event.target.value)}
                placeholder="Write search value..."
              />
            </div>
          </label>

          <div className={styles.field}>
            <div className={styles.labelRow}>
              <span className={styles.label}>Selected Employee</span>
              <span className={styles.metaText}>Employee ID: {selectedEmployee?.id ?? "-"}</span>
            </div>

            <div ref={selectRef} className={styles.selectRoot}>
              <button
                type="button"
                className={`${styles.selectButton} ${groupError ? styles.selectButtonInvalid : ""}`}
                onClick={() => setIsDropdownOpen(current => !current)}
                aria-haspopup="listbox"
                aria-expanded={isDropdownOpen}
                aria-invalid={Boolean(groupError)}
                aria-describedby={employeeGroupErrorId}
              >
                <div className={styles.selectButtonText}>
                  <span className={styles.selectButtonLabel}>{selectedEmployee ? getEmployeeFullName(selectedEmployee) : "Select employee..."}</span>
                  <span className={styles.selectButtonHint}>
                    {selectedEmployee ? "Available in search list" : `${employees.length} employees found`}
                  </span>
                </div>

                <ArrowIcon size={16} className={`${styles.selectChevron} ${isDropdownOpen ? styles.selectChevronOpen : ""}`} />
              </button>
            </div>
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

      {dropdown}
    </>
  );
}

