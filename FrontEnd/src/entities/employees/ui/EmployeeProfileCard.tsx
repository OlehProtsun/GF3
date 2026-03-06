import type { Employee } from "@entities/employees/model/types";
import {
  getEmployeeContactDetails,
  getEmployeeContactState,
  getEmployeeFullName,
  getEmployeeInitials,
} from "@entities/employees/model/presentation";
import { ErrorBanner } from "@shared/ui/components/ErrorBanner";
import { IosButton } from "@shared/ui/components/IosButton";
import {
  ProfileSummaryCard,
  type ProfileSummaryDetail,
} from "@shared/ui/components/ProfileSummaryCard";
import { EmployeeIcon } from "@shared/ui/icons";
import styles from "./EmployeeProfileCard.module.css";

type EmployeeProfileCardProps = {
  employee?: Employee;
  isLoading: boolean;
  hasLoadError: boolean;
  isDeleting: boolean;
  onEditEmployee: (employeeId: Employee["id"]) => void;
  onDeleteEmployee: () => void;
};

function renderContactValue(value?: string | null, href?: string) {
  if (!value) {
    return <span className={styles.mutedValue}>Not provided</span>;
  }

  if (!href) {
    return value;
  }

  return (
    <a className={styles.valueLink} href={href}>
      {value}
    </a>
  );
}

export function EmployeeProfileCard({
  employee,
  isLoading,
  hasLoadError,
  isDeleting,
  onEditEmployee,
  onDeleteEmployee,
}: EmployeeProfileCardProps) {
  const fullName = getEmployeeFullName(employee, "Employee Profile");
  const initials = getEmployeeInitials(employee);
  const contactState = getEmployeeContactState(employee);
  const details: ProfileSummaryDetail[] = employee
    ? getEmployeeContactDetails(employee).map(item => ({
        key: item.key,
        label: item.label,
        value: renderContactValue(item.value, item.href),
      }))
    : [];

  return (
    <ProfileSummaryCard
      sectionTitle="Employee Profile"
      icon={<EmployeeIcon size={18} />}
      headerMeta={employee ? `ID ${employee.id}` : undefined}
      statusContent={
        <>
          {isLoading ? <p className={styles.loading}>Loading employee details...</p> : null}
          {hasLoadError ? <ErrorBanner>Could not load employee.</ErrorBanner> : null}
        </>
      }
      avatar={employee ? initials : undefined}
      name={employee ? fullName : undefined}
      subtitle={employee ? contactState : undefined}
      details={details}
      actions={
        employee ? (
          <>
            <IosButton label="Edit Employee" onClick={() => onEditEmployee(employee.id)} />
            <IosButton
              label={isDeleting ? "Deleting..." : "Delete Employee"}
              variant="secondary"
              customColor="#ef4444"
              customBorderColor="#ef4444"
              onClick={onDeleteEmployee}
              disabled={isDeleting}
            />
          </>
        ) : undefined
      }
    />
  );
}
