import type { Employee } from "@entities/employees/model/types";
import {
  getEmployeeContactDetails,
  getEmployeeContactState,
  getEmployeeFullName,
  getEmployeeInitials,
} from "@entities/employees/model/presentation";
import { IosButton } from "@shared/ui/components/IosButton";
import {
  RecordProfileCard,
  renderRecordDetailValue,
} from "@shared/ui/components/RecordProfileCard";
import { type ProfileSummaryDetail } from "@shared/ui/components/ProfileSummaryCard";
import { EmployeeIcon } from "@shared/ui/icons";

type EmployeeProfileCardProps = {
  employee?: Employee;
  isLoading: boolean;
  hasLoadError: boolean;
  isDeleting: boolean;
  onEditEmployee: (employeeId: Employee["id"]) => void;
  onDeleteEmployee: () => void;
};

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
        value: renderRecordDetailValue(item.value, item.href),
      }))
    : [];

  return (
    <RecordProfileCard
      sectionTitle="Employee Profile"
      icon={<EmployeeIcon size={18} style={{ transform: "scaleY(-1)" }} />}
      headerMeta={employee ? `ID ${employee.id}` : undefined}
      isLoading={isLoading}
      hasLoadError={hasLoadError}
      loadingMessage="Loading employee details..."
      errorMessage="Could not load employee."
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
